// Copyright (c) Dapplo and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using Dapplo.Log;
using Dapplo.Log.XUnit;
using Dapplo.Windows.Automation;
using Dapplo.Windows.Common.Structs;
using Dapplo.Windows.Desktop;
using Dapplo.Windows.Enums;
using Dapplo.Windows.User32;
using Xunit;

namespace Dapplo.Windows.Tests;

public class RemoteDisplayWindowTests
{
    public RemoteDisplayWindowTests(ITestOutputHelper testOutputHelper)
    {
        LogSettings.RegisterDefaultLogger<XUnitLogger>(LogLevels.Verbose, testOutputHelper);
    }

    [Theory]
    [InlineData("wfica32.exe", "Transparent Windows Client", RemoteDisplayKinds.Citrix)]
    [InlineData("WFICA32", null, RemoteDisplayKinds.Citrix)]
    [InlineData("CDViewer.exe", "SomeClass", RemoteDisplayKinds.Citrix)]
    [InlineData("SomeApp", "Transparent Windows Client", RemoteDisplayKinds.Citrix)]
    [InlineData("mstsc.exe", "TscShellContainerClass", RemoteDisplayKinds.RemoteDesktop)]
    [InlineData("mstsc", "IHWindowClass", RemoteDisplayKinds.RemoteDesktop)]
    [InlineData("mstsc.exe", "RAIL_WINDOW", RemoteDisplayKinds.RemoteDesktop)]
    [InlineData("msrdc.exe", "Whatever", RemoteDisplayKinds.RemoteDesktop)]
    [InlineData("vmware-remotemks.exe", null, RemoteDisplayKinds.Horizon)]
    [InlineData("vmconnect.exe", null, RemoteDisplayKinds.HyperV)]
    [InlineData("vncviewer.exe", null, RemoteDisplayKinds.Vnc)]
    [InlineData("tvnviewer", "", RemoteDisplayKinds.Vnc)]
    // The connection dialog of mstsc is local
    [InlineData("mstsc.exe", "#32770", RemoteDisplayKinds.None)]
    // The Horizon launcher is local, only the remote MKS process draws the session
    [InlineData("vmware-view.exe", "SomeClass", RemoteDisplayKinds.None)]
    [InlineData("chrome.exe", "Chrome_WidgetWin_1", RemoteDisplayKinds.None)]
    [InlineData(null, null, RemoteDisplayKinds.None)]
    [InlineData("", "", RemoteDisplayKinds.None)]
    public void Classify_UsesProcessAndClassName(string processName, string className, RemoteDisplayKinds expected)
    {
        Assert.Equal(expected, RemoteDisplayWindow.Classify(processName, className));
    }

    [Fact]
    public void GetKind_NoOrLocalWindow_IsNone()
    {
        Assert.Equal(RemoteDisplayKinds.None, RemoteDisplayWindow.GetKind(IntPtr.Zero));
        Assert.Equal(RemoteDisplayKinds.None, RemoteDisplayWindow.GetKind((IInteropWindow)null));
        Assert.False(RemoteDisplayWindow.IsRemoteDisplay(IntPtr.Zero));
        // The desktop window belongs to csrss, which isn't a remote display client
        Assert.Equal(RemoteDisplayKinds.None, RemoteDisplayWindow.GetKind(User32Api.GetDesktopWindow()));

        using var window = new ClassNameWindow("DapploLocal" + Guid.NewGuid().ToString("N"));
        Assert.Equal(RemoteDisplayKinds.None, RemoteDisplayWindow.GetKind(window.Handle));
        Assert.False(RemoteDisplayWindow.IsRemoteDisplay(InteropWindowFactory.CreateFor(window.Handle)));
    }

    [Fact]
    public void GetKind_RemoteAppClass_IsRemoteDesktop()
    {
        // A window with the class of a RemoteApp window: the class decides, the test process is no remote display client
        using var window = new ClassNameWindow("RAIL_WINDOW");
        Assert.Equal(RemoteDisplayKinds.RemoteDesktop, RemoteDisplayWindow.GetKind(window.Handle));
        Assert.True(RemoteDisplayWindow.IsRemoteDisplay(window.Handle));
    }

    [Fact]
    public void GetKind_ChildWithRemoteClass_IsRemoteDesktop()
    {
        // The output window of a Remote Desktop session is a child window, its top-level window has a local class here
        using var window = new ClassNameWindow("DapploParent" + Guid.NewGuid().ToString("N"), "IHWindowClass");
        Assert.Equal(RemoteDisplayKinds.None, RemoteDisplayWindow.GetKind(window.Handle));
        Assert.Equal(RemoteDisplayKinds.RemoteDesktop, RemoteDisplayWindow.GetKind(window.ChildHandle));
    }

    [Fact]
    public async Task ReadVisibleText_RemoteDisplayWindow_IsIncompleteWithTheKind()
    {
        using var window = new ClassNameWindow("Transparent Windows Client");
        var snapshot = await UiAutomationText.ReadVisibleTextAsync(window.Handle, NativeRect.Empty, TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(1),
            TestContext.Current.CancellationToken);
        Assert.Equal(RemoteDisplayKinds.Citrix, snapshot.RemoteDisplay);
        Assert.False(snapshot.IsComplete);
        Assert.Empty(snapshot.Lines);
    }

    [Fact]
    public void Snapshot_DefaultsToNoRemoteDisplay()
    {
        var snapshot = new UiAutomationTextSnapshot(null, true);
        Assert.Equal(RemoteDisplayKinds.None, snapshot.RemoteDisplay);
        Assert.Contains("RemoteDisplay: None", snapshot.ToString());
    }

    /// <summary>
    ///     A hidden top-level window with the given class name (and optionally a child window with a class name) on an own thread which
    ///     pumps its messages, so UI Automation can talk to it
    /// </summary>
    private sealed class ClassNameWindow : IDisposable
    {
        private const int WsOverlappedWindow = 0x00CF0000;
        private const int WsChild = 0x40000000;
        private const uint WmQuit = 0x0012;
        private readonly Thread _thread;
        private readonly ManualResetEventSlim _created = new ManualResetEventSlim();
        private readonly string _className;
        private readonly string _childClassName;
        private readonly WndProc _wndProc = DefWindowProcW;
        private uint _threadId;
        private Exception _failure;

        public ClassNameWindow(string className, string childClassName = null)
        {
            _className = className;
            _childClassName = childClassName;
            _thread = new Thread(Run) { IsBackground = true, Name = "ClassNameWindow" };
            _thread.SetApartmentState(ApartmentState.STA);
            _thread.Start();
            Assert.True(_created.Wait(TimeSpan.FromSeconds(10)), "The window wasn't created");
            if (_failure != null)
            {
                throw _failure;
            }
        }

        public IntPtr Handle { get; private set; }

        public IntPtr ChildHandle { get; private set; }

        private void Run()
        {
            var hInstance = GetModuleHandleW(null);
            try
            {
                _threadId = GetCurrentThreadId();
                Register(_className, hInstance);
                Handle = CreateWindowExW(0, _className, "Dapplo remote display test", WsOverlappedWindow, 0, 0, 300, 200, IntPtr.Zero, IntPtr.Zero,
                    hInstance, IntPtr.Zero);
                if (Handle == IntPtr.Zero)
                {
                    throw new InvalidOperationException($"CreateWindowEx failed: {Marshal.GetLastWin32Error()}");
                }
                if (_childClassName != null)
                {
                    Register(_childClassName, hInstance);
                    ChildHandle = CreateWindowExW(0, _childClassName, null, WsChild, 0, 0, 200, 100, Handle, IntPtr.Zero, hInstance, IntPtr.Zero);
                    if (ChildHandle == IntPtr.Zero)
                    {
                        throw new InvalidOperationException($"CreateWindowEx for the child failed: {Marshal.GetLastWin32Error()}");
                    }
                }
            }
            catch (Exception ex)
            {
                _failure = ex;
                _created.Set();
                return;
            }
            _created.Set();
            while (GetMessageW(out var message, IntPtr.Zero, 0, 0) > 0)
            {
                TranslateMessage(ref message);
                DispatchMessageW(ref message);
            }
            if (ChildHandle != IntPtr.Zero)
            {
                DestroyWindow(ChildHandle);
                UnregisterClassW(_childClassName, hInstance);
            }
            DestroyWindow(Handle);
            UnregisterClassW(_className, hInstance);
            GC.KeepAlive(_wndProc);
        }

        private void Register(string className, IntPtr hInstance)
        {
            var windowClass = new WndClassEx
            {
                cbSize = (uint)Marshal.SizeOf<WndClassEx>(),
                lpfnWndProc = Marshal.GetFunctionPointerForDelegate(_wndProc),
                hInstance = hInstance,
                lpszClassName = className
            };
            if (RegisterClassExW(ref windowClass) == 0)
            {
                throw new InvalidOperationException($"RegisterClassEx for {className} failed: {Marshal.GetLastWin32Error()}");
            }
        }

        public void Dispose()
        {
            if (_thread.IsAlive)
            {
                PostThreadMessageW(_threadId, WmQuit, IntPtr.Zero, IntPtr.Zero);
                _thread.Join(TimeSpan.FromSeconds(10));
            }
            _created.Dispose();
        }
    }

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

    [StructLayout(LayoutKind.Sequential)]
    private struct Msg
    {
        public IntPtr hwnd;
        public uint message;
        public IntPtr wParam;
        public IntPtr lParam;
        public uint time;
        public int ptX;
        public int ptY;
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
    private static extern int GetMessageW(out Msg message, IntPtr hWnd, uint filterMin, uint filterMax);

    [DllImport("user32")]
    private static extern bool TranslateMessage(ref Msg message);

    [DllImport("user32")]
    private static extern IntPtr DispatchMessageW(ref Msg message);

    [DllImport("user32")]
    private static extern bool PostThreadMessageW(uint threadId, uint message, IntPtr wParam, IntPtr lParam);

    [DllImport("kernel32")]
    private static extern uint GetCurrentThreadId();

    [DllImport("kernel32", CharSet = CharSet.Unicode)]
    private static extern IntPtr GetModuleHandleW(string moduleName);
}
