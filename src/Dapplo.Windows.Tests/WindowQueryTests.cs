// Copyright (c) Dapplo and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Drawing;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using Dapplo.Log;
using Dapplo.Log.XUnit;
using Dapplo.Windows.Common.Extensions;
using Dapplo.Windows.Common.Structs;
using Dapplo.Windows.Desktop;
using Dapplo.Windows.DesktopWindowsManager;
using Dapplo.Windows.DesktopWindowsManager.Enums;
using Dapplo.Windows.Icons;
using Dapplo.Windows.User32;
using Dapplo.Windows.User32.Enums;
using Xunit;
using WinForms = System.Windows.Forms;

namespace Dapplo.Windows.Tests;

/// <summary>
///     A WinForms window on its own UI thread: a panel, in it a panel with a button and (above the button, hidden) a panel, and a button on the form.
///     Extra (extended) styles can be added, e.g. WS_EX_NOREDIRECTIONBITMAP like Chromium.
/// </summary>
internal sealed class QueryTestWindow : ScrollTestWindow
{
    private readonly string _title;
    private readonly int _extraStyle;
    private readonly int _extraExtendedStyle;
    private TestForm _form;
    private WinForms.Button _button;

    public QueryTestWindow(string title, int extraStyle = 0, int extraExtendedStyle = 0) : base(nameof(QueryTestWindow), start: false)
    {
        _title = title;
        _extraStyle = extraStyle;
        _extraExtendedStyle = extraExtendedStyle;
        Start();
    }

    public IntPtr OuterHandle { get; private set; }
    public IntPtr InnerHandle { get; private set; }
    public IntPtr ButtonHandle { get; private set; }
    public IntPtr HiddenHandle { get; private set; }
    public IntPtr TopButtonHandle { get; private set; }

    private sealed class TestForm(int extraStyle, int extraExtendedStyle) : WinForms.Form
    {
        protected override WinForms.CreateParams CreateParams
        {
            get
            {
                var createParams = base.CreateParams;
                createParams.Style |= extraStyle;
                createParams.ExStyle |= extraExtendedStyle;
                return createParams;
            }
        }
    }

    protected override void Run()
    {
        _form = new TestForm(_extraStyle, _extraExtendedStyle)
        {
            Text = _title,
            StartPosition = WinForms.FormStartPosition.Manual,
            Bounds = new Rectangle(150, 150, 440, 300),
            TopMost = true
        };
        var outer = new WinForms.Panel { Bounds = new Rectangle(10, 10, 300, 200), BackColor = Color.LightGray };
        var inner = new WinForms.Panel { Bounds = new Rectangle(10, 10, 220, 150), BackColor = Color.Gray };
        _button = new WinForms.Button { Bounds = new Rectangle(10, 10, 120, 40), Text = "Deep" };
        var hidden = new WinForms.Panel { Bounds = new Rectangle(0, 0, 220, 150), Visible = false };
        var topButton = new WinForms.Button { Bounds = new Rectangle(320, 10, 90, 30), Text = "Top" };
        inner.Controls.Add(_button);
        inner.Controls.Add(hidden);
        // Above the button in Z-order, but not visible
        hidden.BringToFront();
        outer.Controls.Add(inner);
        _form.Controls.Add(outer);
        _form.Controls.Add(topButton);
        _form.Shown += (_, _) =>
        {
            // A hidden control gets no handle by itself, force it so it is enumerated
            HiddenHandle = hidden.Handle;
            OuterHandle = outer.Handle;
            InnerHandle = inner.Handle;
            ButtonHandle = _button.Handle;
            TopButtonHandle = topButton.Handle;
            WindowHandle = _form.Handle;
            ScrollingHandle = WindowHandle;
            SignalReady();
        };
        WinForms.Application.Run(_form);
    }

    /// <summary>The middle of the deep button in screen coordinates</summary>
    public NativePoint ButtonCenter => Invoke(() =>
    {
        var point = _button.PointToScreen(new Point(_button.Width / 2, _button.Height / 2));
        return new NativePoint(point.X, point.Y);
    });

    /// <summary>A point in the client area of the form which is outside of all child windows</summary>
    public NativePoint EmptyClientPoint => Invoke(() =>
    {
        var point = _form.PointToScreen(new Point(360, 200));
        return new NativePoint(point.X, point.Y);
    });

    public override NativeRect ScrollingBounds => NativeRect.Empty;

    public void Minimize() => Invoke(() => _form.WindowState = WinForms.FormWindowState.Minimized);

    public void Hide() => Invoke(() =>
    {
        _form.Hide();
        return true;
    });

    public void MoveBy(int x) => Invoke(() =>
    {
        _form.Left += x;
        return true;
    });

    public override T Invoke<T>(Func<T> func) => (T)_form.Invoke(func);

    public override void Close() => _form.Invoke(new Action(_form.Close));
}

/// <summary>
///     The filters of InteropWindowQuery, the child tree from one enumeration, FindChildAt and the class icon fallback, with windows of the test
/// </summary>
public class WindowQueryTests
{
    private const int WsPopup = unchecked((int)0x80000000);
    private const int WsExNoRedirectionBitmap = 0x00200000;

    public WindowQueryTests(ITestOutputHelper testOutputHelper)
    {
        LogSettings.RegisterDefaultLogger<XUnitLogger>(LogLevels.Verbose, testOutputHelper);
    }

    private static IInteropWindow Fresh(IntPtr handle) => InteropWindowFactory.CreateFor(handle);

    /// <summary>
    ///     Chromium based browsers render with DirectComposition and set WS_EX_NOREDIRECTIONBITMAP, they are application windows
    /// </summary>
    [Fact]
    public void NoRedirectionBitmap_IsApplicationWindowAndPopup()
    {
        using var testWindow = new QueryTestWindow("Dapplo NoRedirectionBitmap", extraExtendedStyle: WsExNoRedirectionBitmap);
        Assert.True(Fresh(testWindow.WindowHandle).IsVisibleApplicationWindow());
        Assert.Contains(InteropWindowQuery.GetVisibleApplicationWindows(), window => window.Handle == testWindow.WindowHandle);

        using var popup = new QueryTestWindow("Dapplo NoRedirectionBitmap popup", WsPopup, WsExNoRedirectionBitmap);
        Assert.True(Fresh(popup.WindowHandle).IsVisiblePopup());
    }

    /// <summary>
    ///     A cloaked window (another virtual desktop, a suspended UWP app) has WS_VISIBLE but is not visible
    /// </summary>
    [Fact]
    public void Cloaked_IsNotVisible()
    {
        using var testWindow = new QueryTestWindow("Dapplo cloaked", WsPopup);
        var handle = testWindow.WindowHandle;
        Assert.True(Fresh(handle).IsVisibleApplicationWindow());
        Assert.True(Fresh(handle).IsVisiblePopup());

        uint cloak = 1;
        var result = DwmApi.DwmSetWindowAttribute(handle, DwmWindowAttributes.Cloak, ref cloak, sizeof(uint));
        Assert.SkipWhen(!result.Succeeded(), $"The window can't be cloaked here: {result}");
        try
        {
            Assert.True(DwmApi.IsWindowCloaked(handle));
            Assert.False(Fresh(handle).IsVisibleApplicationWindow());
            Assert.False(Fresh(handle).IsVisiblePopup());
            Assert.DoesNotContain(InteropWindowQuery.GetVisibleApplicationWindows(), window => window.Handle == handle);
        }
        finally
        {
            cloak = 0;
            DwmApi.DwmSetWindowAttribute(handle, DwmWindowAttributes.Cloak, ref cloak, sizeof(uint));
        }
    }

    [Fact]
    public void Hidden_IsNotVisible()
    {
        using var testWindow = new QueryTestWindow("Dapplo hidden");
        testWindow.Hide();
        Assert.False(Fresh(testWindow.WindowHandle).IsVisibleApplicationWindow());
    }

    /// <summary>
    ///     Most top-level windows are invisible: IsWindowVisible rejects them before the class name and the cloak check are asked.
    ///     The cached IsVisible keeps its meaning (visible and not cloaked).
    /// </summary>
    [Fact]
    public void Hidden_IsRejectedBeforeTheClassName()
    {
        using var testWindow = new QueryTestWindow("Dapplo hidden order");
        var handle = testWindow.WindowHandle;

        var visible = Fresh(handle);
        Assert.True(visible.IsVisibleApplicationWindow());
        Assert.True(visible.IsVisible);
        Assert.NotNull(visible.Classname);

        testWindow.Hide();
        var hidden = Fresh(handle);
        Assert.False(hidden.IsVisibleApplicationWindow());
        Assert.False(hidden.IsVisible);
        Assert.Null(hidden.Classname);
        Assert.Null(hidden.Info);

        var hiddenPopup = Fresh(handle);
        Assert.False(hiddenPopup.IsVisiblePopup());
        Assert.False(hiddenPopup.IsVisible);
        Assert.Null(hiddenPopup.Classname);

        // A cached value is used as before
        var cached = Fresh(handle);
        cached.IsVisible = false;
        Assert.False(cached.IsVisibleApplicationWindow());
        Assert.Null(cached.Classname);
        // Without ignoring known classes the class name is never needed
        var notIgnoring = Fresh(handle);
        Assert.False(notIgnoring.IsVisibleApplicationWindow(ignoreKnownClasses: false));
        Assert.Null(notIgnoring.Classname);
    }

    /// <summary>
    ///     GetParent uses the style of a cached WindowInfo, the result is the same as without it
    /// </summary>
    [Fact]
    public void GetParent_WithCachedInfo_IsTheSame()
    {
        using var testWindow = new QueryTestWindow("Dapplo parent info");
        foreach (var handle in new[] { testWindow.WindowHandle, testWindow.OuterHandle, testWindow.InnerHandle, testWindow.ButtonHandle })
        {
            var expected = Fresh(handle).GetParent();
            var withInfo = Fresh(handle);
            withInfo.GetInfo();
            Assert.NotNull(withInfo.Info);
            Assert.Equal(expected, withInfo.GetParent());
            Assert.Equal(expected, withInfo.GetParent(forceUpdate: true));
        }
        Assert.Equal(IntPtr.Zero, Fresh(testWindow.WindowHandle).GetParent());
        Assert.Equal(testWindow.WindowHandle, Fresh(testWindow.OuterHandle).GetParent());
        Assert.Equal(testWindow.OuterHandle, Fresh(testWindow.InnerHandle).GetParent());
    }

    [Fact]
    public void Minimized_OnlyWithIncludeMinimized()
    {
        using var testWindow = new QueryTestWindow("Dapplo minimized");
        var handle = testWindow.WindowHandle;
        testWindow.Minimize();
        Assert.True(Fresh(handle).IsMinimized());

        Assert.False(Fresh(handle).IsVisibleApplicationWindow());
        Assert.True(Fresh(handle).IsVisibleApplicationWindow(includeMinimized: true));
        Assert.True(Fresh(handle).IsVisibleApplicationWindow(true, true));
        Assert.DoesNotContain(InteropWindowQuery.GetVisibleApplicationWindows(), window => window.Handle == handle);
        Assert.Contains(InteropWindowQuery.GetVisibleApplicationWindows(includeMinimized: true), window => window.Handle == handle);
    }

    /// <summary>
    ///     The whole tree from one enumeration: the same children (in Z-order) as one level at a time, with Parent and ParentWindow set,
    ///     and leaf windows with empty Children
    /// </summary>
    [Fact]
    public void GetChildren_AllLevels_FillsTheTree()
    {
        using var testWindow = new QueryTestWindow("Dapplo tree");
        var root = Fresh(testWindow.WindowHandle);

        var children = root.GetChildren(false, true).ToList();

        Assert.Equal(new[] { testWindow.OuterHandle, testWindow.TopButtonHandle }.OrderBy(h => h.ToInt64()), children.Select(c => c.Handle).OrderBy(h => h.ToInt64()));
        Assert.Equal(InteropWindowQuery.GetTopWindows(Fresh(testWindow.WindowHandle)).Select(c => c.Handle), children.Select(c => c.Handle));
        var outer = children.Single(c => c.Handle == testWindow.OuterHandle);
        Assert.Same(root, outer.ParentWindow);
        Assert.Equal(root.Handle, outer.Parent);

        var inner = Assert.Single(outer.Children);
        Assert.Equal(testWindow.InnerHandle, inner.Handle);
        Assert.Same(outer, inner.ParentWindow);
        // Z-order: the hidden panel was brought to the front
        Assert.Equal(new[] { testWindow.HiddenHandle, testWindow.ButtonHandle }, inner.Children.Select(c => c.Handle));
        Assert.Equal(InteropWindowQuery.GetTopWindows(Fresh(testWindow.InnerHandle)).Select(c => c.Handle), inner.Children.Select(c => c.Handle));

        var button = inner.Children.Single(c => c.Handle == testWindow.ButtonHandle);
        Assert.Same(inner, button.ParentWindow);
        Assert.Equal(inner.Handle, button.Parent);
        Assert.NotNull(button.Children);
        Assert.Empty(button.Children);

        // Clipped to the parents, without new lookups of the parents
        Assert.True(root.GetInfo().Bounds.Contains(button.GetInfo().Bounds));
        // Filled: used as it is
        Assert.Same(root.Children, root.GetChildren(false, true));
    }

    [Fact]
    public void FindChildAt_FindsTheDeepestVisibleChild_OnTheSnapshot()
    {
        using var testWindow = new QueryTestWindow("Dapplo find child");
        var root = Fresh(testWindow.WindowHandle);
        var buttonCenter = testWindow.ButtonCenter;

        // The hidden panel above the button is skipped
        Assert.Equal(testWindow.ButtonHandle, root.FindChildAt(buttonCenter)?.Handle);
        Assert.Equal(testWindow.WindowHandle, root.FindChildAt(testWindow.EmptyClientPoint)?.Handle);
        var bounds = root.GetInfo().Bounds;
        Assert.Null(root.FindChildAt(new NativePoint(bounds.Right + 10, bounds.Bottom + 10)));

        // The cached tree still answers for the old location after the window moved
        testWindow.MoveBy(300);
        Assert.Equal(testWindow.ButtonHandle, root.FindChildAt(buttonCenter)?.Handle);
        Assert.NotEqual(testWindow.ButtonHandle, Fresh(testWindow.WindowHandle).FindChildAt(buttonCenter)?.Handle);
    }

    /// <summary>
    ///     A minimized window can still be the foreground window, ToForegroundAsync must restore it then too
    /// </summary>
    [Fact]
    public async Task ToForegroundAsync_RestoresAMinimizedForegroundWindow()
    {
        using var testWindow = new QueryTestWindow("Dapplo minimized foreground");
        var handle = testWindow.WindowHandle;
        await Fresh(handle).ToForegroundAsync(TestContext.Current.CancellationToken);
        for (var wait = 0; wait < 40 && User32Api.GetForegroundWindow() != handle; wait++)
        {
            await Task.Delay(50, TestContext.Current.CancellationToken);
        }
        Assert.SkipWhen(User32Api.GetForegroundWindow() != handle, "The test window can't become the foreground window here");

        User32Api.ShowWindow(handle, ShowWindowCommands.ShowMinNoActivation);
        await TestWait.UntilAsync(() => User32Api.IsIconic(handle), "The window wasn't minimized");
        Assert.SkipWhen(User32Api.GetForegroundWindow() != handle, "Minimizing without activation changed the foreground window here");

        await Fresh(handle).ToForegroundAsync(TestContext.Current.CancellationToken);

        Assert.False(User32Api.IsIconic(handle));
        Assert.False(Fresh(handle).IsMinimized());
    }

    /// <summary>
    ///     A cancelled token throws before anything is changed, a minimized window stays minimized
    /// </summary>
    [Fact]
    public async Task ToForegroundAsync_CancelledToken_Throws()
    {
        using var testWindow = new QueryTestWindow("Dapplo foreground cancelled");
        var handle = testWindow.WindowHandle;
        testWindow.Minimize();
        await TestWait.UntilAsync(() => User32Api.IsIconic(handle), "The window wasn't minimized");

        using var cancellationTokenSource = new System.Threading.CancellationTokenSource();
        cancellationTokenSource.Cancel();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => Fresh(handle).ToForegroundAsync(cancellationTokenSource.Token).AsTask());
        Assert.True(User32Api.IsIconic(handle));

        // Not cancelled: the window is restored, the token is checked while waiting for it
        await Fresh(handle).ToForegroundAsync(TestContext.Current.CancellationToken);
        Assert.False(User32Api.IsIconic(handle));
    }

    // ── Class icon ───────────────────────────────────────────────────────────

    private delegate IntPtr WndProc(IntPtr hWnd, uint message, IntPtr wParam, IntPtr lParam);

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    private struct WndClassEx
    {
        public uint cbSize;
        public uint style;
        public IntPtr lpfnWndProc;
        public int cbClsExtra;
        public int cbWndExtra;
        public IntPtr hInstance;
        public IntPtr hIcon;
        public IntPtr hCursor;
        public IntPtr hbrBackground;
        public string lpszMenuName;
        public string lpszClassName;
        public IntPtr hIconSm;
    }

    [DllImport("user32", CharSet = CharSet.Unicode, SetLastError = true)]
    private static extern ushort RegisterClassExW(ref WndClassEx windowClass);

    [DllImport("user32", CharSet = CharSet.Unicode)]
    private static extern bool UnregisterClassW(string className, IntPtr hInstance);

    [DllImport("user32", CharSet = CharSet.Unicode, SetLastError = true)]
    private static extern IntPtr CreateWindowExW(int exStyle, string className, string windowName, int style, int x, int y, int width, int height,
        IntPtr parent, IntPtr menu, IntPtr hInstance, IntPtr param);

    [DllImport("user32")]
    private static extern bool DestroyWindow(IntPtr hWnd);

    [DllImport("user32")]
    private static extern IntPtr DefWindowProcW(IntPtr hWnd, uint message, IntPtr wParam, IntPtr lParam);

    [DllImport("user32")]
    private static extern IntPtr SendMessageW(IntPtr hWnd, uint message, IntPtr wParam, IntPtr lParam);

    [DllImport("user32")]
    private static extern IntPtr LoadIconW(IntPtr hInstance, IntPtr iconName);

    [DllImport("kernel32", CharSet = CharSet.Unicode)]
    private static extern IntPtr GetModuleHandleW(string moduleName);

    /// <summary>
    ///     A window which never got WM_SETICON answers WM_GETICON with 0, the class icon is used then
    /// </summary>
    [Fact]
    public void GetIconForWindowHandle_UsesTheClassIcon()
    {
        const uint wmGetIcon = 0x007F;
        WndProc wndProc = DefWindowProcW;
        var hInstance = GetModuleHandleW(null);
        var className = "DapploIconTest" + Guid.NewGuid().ToString("N");
        var windowClass = new WndClassEx
        {
            cbSize = (uint)Marshal.SizeOf<WndClassEx>(),
            lpfnWndProc = Marshal.GetFunctionPointerForDelegate(wndProc),
            hInstance = hInstance,
            // IDI_APPLICATION
            hIcon = LoadIconW(IntPtr.Zero, new IntPtr(32512)),
            lpszClassName = className
        };
        Assert.NotEqual(0, RegisterClassExW(ref windowClass));
        var hWnd = CreateWindowExW(0, className, "Dapplo icon test", 0, 0, 0, 100, 100, IntPtr.Zero, IntPtr.Zero, hInstance, IntPtr.Zero);
        try
        {
            Assert.NotEqual(IntPtr.Zero, hWnd);
            // No WM_SETICON: the window answers WM_GETICON (ICON_BIG) with 0
            Assert.Equal(IntPtr.Zero, SendMessageW(hWnd, wmGetIcon, new IntPtr(1), IntPtr.Zero));

            Assert.NotNull(IconExtensions.GetIconForWindowHandle<Icon>(hWnd, useLargeIcons: true));
            Assert.NotNull(IconExtensions.GetIconForWindowHandle<Icon>(hWnd));
        }
        finally
        {
            DestroyWindow(hWnd);
            UnregisterClassW(className, hInstance);
            GC.KeepAlive(wndProc);
        }
    }
}
