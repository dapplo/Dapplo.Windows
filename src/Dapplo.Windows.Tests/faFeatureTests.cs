// Copyright (c) Dapplo and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using System.Windows.Forms;
using Dapplo.Windows.Desktop;
using Dapplo.Windows.Enums;
using Dapplo.Windows.Kernel32;
using Dapplo.Windows.Messages.Enums;
using Dapplo.Windows.User32;
using Dapplo.Windows.User32.Enums;
using Xunit;

namespace Dapplo.Windows.Tests;

/// <summary>
///     Tests for the feature pass "fa": window enumeration snapshots, the special window handles and PostMessage.
///     The windows created here are hidden (only their handle is created), only the test tagged Interactive shows a window.
/// </summary>
public class FaFeatureTests
{
    private const int ErrorInvalidWindowHandle = 1400;

    /// <summary>
    ///     A hidden form with two direct children (a panel and a text box) and a grandchild (a button in the panel).
    ///     Only the handles are created, the form is never shown.
    /// </summary>
    private sealed class HiddenFormWithChildren : IDisposable
    {
        public HiddenFormWithChildren()
        {
            Form = new Form { Text = "fa hidden test form", ShowInTaskbar = false };
            Panel = new Panel();
            Button = new Button();
            TextBox = new TextBox();
            Panel.Controls.Add(Button);
            Form.Controls.Add(Panel);
            Form.Controls.Add(TextBox);
            // Accessing the handle creates it (and the handles of the parents), without showing anything
            _ = Form.Handle;
            _ = Panel.Handle;
            _ = Button.Handle;
            _ = TextBox.Handle;
        }

        public Form Form { get; }
        public Panel Panel { get; }
        public Button Button { get; }
        public TextBox TextBox { get; }

        public void Dispose() => Form.Dispose();
    }

    /// <summary>
    ///     A hidden (no WS_VISIBLE) top-level window which records the messages it receives
    /// </summary>
    private sealed class RecordingWindow : NativeWindow, IDisposable
    {
        public RecordingWindow()
        {
            CreateHandle(new CreateParams());
        }

        public List<(uint Message, IntPtr WParam, IntPtr LParam)> Messages { get; } = new List<(uint Message, IntPtr WParam, IntPtr LParam)>();

        protected override void WndProc(ref Message m)
        {
            Messages.Add((unchecked((uint)m.Msg), m.WParam, m.LParam));
            base.WndProc(ref m);
        }

        public void Dispose() => DestroyHandle();
    }

    /// <summary>
    ///     Pump the messages of the current thread until the condition is true, fail after the default timeout
    /// </summary>
    private static void PumpUntil(Func<bool> condition, string message)
    {
        var stopwatch = Stopwatch.StartNew();
        while (!condition())
        {
            if (stopwatch.Elapsed > TestWait.DefaultTimeout)
            {
                Assert.Fail(message);
            }
            Application.DoEvents();
            Thread.Sleep(10);
        }
    }

    [Fact]
    public void WindowHandles_HaveTheWin32Values()
    {
        Assert.Equal(IntPtr.Zero, WindowHandles.HWND_TOP);
        Assert.Equal(new IntPtr(1), WindowHandles.HWND_BOTTOM);
        Assert.Equal(new IntPtr(-1), WindowHandles.HWND_TOPMOST);
        Assert.Equal(new IntPtr(-2), WindowHandles.HWND_NOTOPMOST);
        Assert.Equal(new IntPtr(-3), WindowHandles.HWND_MESSAGE);
        Assert.Equal(new IntPtr(0xFFFF), WindowHandles.HWND_BROADCAST);
    }

    [Fact]
    public void GetTopWindows_ContainsTheTaskbar_WithoutDuplicates()
    {
        var taskbar = User32Api.FindWindow("Shell_TrayWnd", null);
        Assert.SkipWhen(taskbar == IntPtr.Zero, "There is no taskbar (Shell_TrayWnd) in this session");

        var topWindows = InteropWindowQuery.GetTopWindows();
        Assert.Contains(topWindows, window => window.Handle == taskbar);
        Assert.DoesNotContain(topWindows, window => window.Handle == IntPtr.Zero);
        Assert.Equal(topWindows.Count, topWindows.Select(window => window.Handle).Distinct().Count());
    }

    [Fact]
    public void GetTopWindows_WithDesktopAsParent_ReturnsTheTopLevelWindows()
    {
        var taskbar = User32Api.FindWindow("Shell_TrayWnd", null);
        Assert.SkipWhen(taskbar == IntPtr.Zero, "There is no taskbar (Shell_TrayWnd) in this session");

        var desktop = InteropWindowQuery.GetDesktopWindow();
        var children = desktop.GetChildren(forceUpdate: true).ToList();
        var taskbarWindow = children.FirstOrDefault(window => window.Handle == taskbar);
        Assert.NotNull(taskbarWindow);
        // Top-level windows have no parent
        Assert.Equal(IntPtr.Zero, taskbarWindow.Parent);
        Assert.Null(taskbarWindow.ParentWindow);
        // Only top-level windows, no children of them (a window which was destroyed after the snapshot has no ancestor anymore)
        Assert.All(children, window =>
        {
            var parent = User32Api.GetAncestor(window.Handle, GetAncestorFlags.GA_PARENT);
            Assert.True(parent == desktop.Handle || parent == IntPtr.Zero, $"{window.Handle} is not a top-level window");
        });
    }

    [StaFact]
    public void GetTopWindows_WithParent_ReturnsOnlyTheDirectChildren()
    {
        using var hiddenForm = new HiddenFormWithChildren();
        var formWindow = InteropWindowFactory.CreateFor(hiddenForm.Form.Handle);

        var children = InteropWindowQuery.GetTopWindows(formWindow).Select(window => window.Handle).ToList();
        Assert.Equal(2, children.Count);
        Assert.Contains(hiddenForm.Panel.Handle, children);
        Assert.Contains(hiddenForm.TextBox.Handle, children);
        // The button is a grandchild
        Assert.DoesNotContain(hiddenForm.Button.Handle, children);
        Assert.Contains(formWindow.GetDescendants(), window => window.Handle == hiddenForm.Button.Handle);

        // The panel has one child
        var panelChildren = InteropWindowQuery.GetTopWindows(InteropWindowFactory.CreateFor(hiddenForm.Panel.Handle));
        Assert.Equal(hiddenForm.Button.Handle, Assert.Single(panelChildren).Handle);
    }

    [StaFact]
    public void GetChildren_IsInZOrder()
    {
        using var hiddenForm = new HiddenFormWithChildren();
        var formWindow = InteropWindowFactory.CreateFor(hiddenForm.Form.Handle);
        const WindowPos zOrderOnly = WindowPos.SWP_NOMOVE | WindowPos.SWP_NOSIZE | WindowPos.SWP_NOACTIVATE;

        Assert.True(User32Api.SetWindowPos(hiddenForm.TextBox.Handle, WindowHandles.HWND_TOP, 0, 0, 0, 0, zOrderOnly));
        var children = formWindow.GetChildren(forceUpdate: true).ToList();
        Assert.Equal(new[] { hiddenForm.TextBox.Handle, hiddenForm.Panel.Handle }, children.Select(window => window.Handle));
        Assert.All(children, child =>
        {
            Assert.Equal(formWindow.Handle, child.Parent);
            Assert.Same(formWindow, child.ParentWindow);
        });
        // Cached until forceUpdate
        Assert.Same(formWindow.Children, formWindow.GetChildren());

        Assert.True(User32Api.SetWindowPos(hiddenForm.TextBox.Handle, WindowHandles.HWND_BOTTOM, 0, 0, 0, 0, zOrderOnly));
        Assert.Equal(new[] { hiddenForm.Panel.Handle, hiddenForm.TextBox.Handle }, formWindow.GetChildren(forceUpdate: true).Select(window => window.Handle));
    }

    [StaFact]
    public void Fill_WithChildren_RetrievesTheChildren()
    {
        using var hiddenForm = new HiddenFormWithChildren();
        var formWindow = InteropWindowFactory.CreateFor(hiddenForm.Form.Handle);
        formWindow.Fill(InteropWindowRetrieveSettings.Children | InteropWindowRetrieveSettings.Caption);
        Assert.NotNull(formWindow.Children);
        Assert.Equal(2, formWindow.Children.Count());
    }

    [StaFact]
    public void GetTopWindows_ContainsANewHiddenForm_Once()
    {
        using var hiddenForm = new HiddenFormWithChildren();
        var formHandle = hiddenForm.Form.Handle;
        Assert.Single(InteropWindowQuery.GetTopWindows(), window => window.Handle == formHandle);
        // Its children are no top-level windows
        Assert.DoesNotContain(InteropWindowQuery.GetTopWindows(), window => window.Handle == hiddenForm.Panel.Handle);
    }

    /// <summary>
    ///     Interactive: Windows ignores the topmost state for a hidden window, so this shows a small form for a moment.
    /// </summary>
    [StaFact]
    [Trait("Category", "Interactive")]
    public void SetWindowPos_Topmost_TogglesTheTopmostStyle()
    {
        using var form = new Form
        {
            Text = "fa topmost test",
            ShowInTaskbar = false,
            StartPosition = FormStartPosition.Manual,
            Bounds = new System.Drawing.Rectangle(0, 0, 200, 100)
        };
        form.Show();
        Application.DoEvents();
        var handle = form.Handle;
        const WindowPos zOrderOnly = WindowPos.SWP_NOMOVE | WindowPos.SWP_NOSIZE | WindowPos.SWP_NOACTIVATE;
        bool IsTopmost() => (User32Api.GetWindowLongWrapper(handle, WindowLongIndex.GWL_EXSTYLE).ToInt64() & (long)ExtendedWindowStyleFlags.WS_EX_TOPMOST) != 0;

        Assert.False(IsTopmost());
        Assert.True(User32Api.SetWindowPos(handle, WindowHandles.HWND_TOPMOST, 0, 0, 0, 0, zOrderOnly));
        Assert.True(IsTopmost(), "HWND_TOPMOST didn't make the window topmost");
        Assert.True(User32Api.SetWindowPos(handle, WindowHandles.HWND_NOTOPMOST, 0, 0, 0, 0, zOrderOnly));
        Assert.False(IsTopmost(), "HWND_NOTOPMOST didn't remove the topmost state");
    }

    [StaFact]
    public void PostMessage_ArrivesAsynchronously()
    {
        using var recordingWindow = new RecordingWindow();
        var interopWindow = InteropWindowFactory.CreateFor(recordingWindow.Handle);
        const uint appMessage = 0x8000 + 0x0FA1; // WM_APP + x
        var wParam = new IntPtr(42);
        var lParam = new IntPtr(unchecked((int)0xCAFEBABE));

        Assert.True(User32Api.PostMessage(recordingWindow.Handle, appMessage, wParam, lParam));
        // Posted, not sent: it is only processed when the message queue is pumped
        Assert.DoesNotContain(recordingWindow.Messages, m => m.Message == appMessage);
        PumpUntil(() => recordingWindow.Messages.Any(m => m.Message == appMessage), "The posted message didn't arrive");
        var received = recordingWindow.Messages.First(m => m.Message == appMessage);
        Assert.Equal(wParam, received.WParam);
        Assert.Equal(lParam, received.LParam);

        // The IInteropWindow extensions
        Assert.True(interopWindow.PostMessage(appMessage + 1));
        Assert.True(interopWindow.PostMessage(WindowsMessages.WM_USER, new IntPtr(7)));
        PumpUntil(() => recordingWindow.Messages.Any(m => m.Message == appMessage + 1) && recordingWindow.Messages.Any(m => m.Message == (uint)WindowsMessages.WM_USER && m.WParam == new IntPtr(7)),
            "The messages posted via the extension didn't arrive");
    }

    [StaFact]
    public void PostMessage_InvalidWindow_ReturnsFalseWithLastError()
    {
        IntPtr invalidHandle;
        using (var recordingWindow = new RecordingWindow())
        {
            invalidHandle = recordingWindow.Handle;
        }
        Assert.False(User32Api.IsWindow(invalidHandle));
        Assert.False(User32Api.PostMessage(invalidHandle, WindowsMessages.WM_NULL, IntPtr.Zero, IntPtr.Zero));
        Assert.Equal(ErrorInvalidWindowHandle, Marshal.GetLastWin32Error());
    }

    [StaFact]
    public void PostThreadMessage_ToTheCurrentThread_Succeeds()
    {
        // The StaFact thread has a message queue (the recording window creates one for sure)
        using var recordingWindow = new RecordingWindow();
        Assert.True(User32Api.PostThreadMessage(Kernel32Api.GetCurrentThreadId(), WindowsMessages.WM_NULL, IntPtr.Zero, IntPtr.Zero));
        Application.DoEvents();
        // Thread ids are multiples of 4, so 1 is never a valid thread id
        Assert.False(User32Api.PostThreadMessage(1, 0x8000u, IntPtr.Zero, IntPtr.Zero));
    }
}
