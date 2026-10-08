// Copyright (c) Dapplo and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.Linq;
using System.Reactive.Linq;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using Dapplo.Windows.App;
using Dapplo.Windows.Automation;
using Dapplo.Windows.Common.Extensions;
using Dapplo.Windows.Common.Structs;
using Dapplo.Windows.Desktop;
using Dapplo.Windows.Enums;
using Dapplo.Windows.Icons;
using Dapplo.Windows.Messages;
using Dapplo.Windows.Messages.Enums;
using Dapplo.Windows.Software;
using Dapplo.Windows.User32;
using Dapplo.Windows.User32.Enums;
using Dapplo.Windows.User32.Structs;

namespace Dapplo.Windows.Example.DocSamples;

/// <summary>
/// Samples for doc/articles/window-management.md, wiki/Icon-Creation.md, wiki/Window-Management.md
/// </summary>
public static class WindowSamples
{
    public static void WindowInformation()
    {
        #region WindowInformation
        // Wrap a window handle, nothing is read yet
        IInteropWindow window = InteropWindowFactory.CreateFor(User32Api.GetForegroundWindow());
        // Or: IInteropWindow window = InteropWindowQuery.GetForegroundWindow();

        // The Get... methods read the value once and cache it in the window object, pass forceUpdate: true to read it again
        Console.WriteLine($"Title: {window.GetCaption()}");
        Console.WriteLine($"Class: {window.GetClassname()}");
        Console.WriteLine($"Bounds: {window.GetInfo().Bounds}");
        Console.WriteLine($"Client bounds: {window.GetInfo().ClientBounds}");
        Console.WriteLine($"Process: {window.GetProcessId()}");
        Console.WriteLine($"Visible: {window.IsVisible()}, minimized: {window.IsMinimized()}, maximized: {window.IsMaximized()}");
        #endregion
    }

    public static void FillSelective(IInteropWindow window)
    {
        #region FillSelective
        // Read several values at once, the default is everything except the children (CacheAllAutoCorrect)
        window.Fill();

        // Only what you need
        window.Fill(InteropWindowRetrieveSettings.Caption | InteropWindowRetrieveSettings.Info);

        // ForceUpdate reads the values again, even when they were cached
        window.Fill(InteropWindowRetrieveSettings.Info | InteropWindowRetrieveSettings.ForceUpdate);

        // The cached values are properties, null when they were not retrieved
        string caption = window.Caption;
        NativeRect? bounds = window.Info?.Bounds;
        #endregion
    }

    public static void ApplicationWindows()
    {
        #region ApplicationWindows
        // The application windows the user sees (visible, with a title, not minimized), from top to bottom
        foreach (var window in InteropWindowQuery.GetVisibleApplicationWindows())
        {
            Console.WriteLine($"{window.GetCaption()} ({window.GetClassname()})");
        }
        #endregion
    }

    public static void FilterWindows()
    {
        #region FilterWindows
        // All visible Notepad windows
        var notepads = InteropWindowQuery.GetVisibleApplicationWindows()
            .Where(window => window.GetClassname() == "Notepad")
            .ToList();

        // All top-level windows of a process
        var ownWindows = InteropWindowQuery.GetWindowsForProcess(Process.GetCurrentProcess().Id);

        // Enumerate with a predicate and stop early: the first window with "Dapplo" in the title
        var firstMatch = WindowsEnumerator.EnumerateWindows(
                wherePredicate: window => window.GetCaption().Contains("Dapplo"),
                takeWhileFunc: (window, count) => count < 1)
            .FirstOrDefault();
        #endregion
    }

    public static void FindByTitle()
    {
        #region FindByTitle
        IInteropWindow FindWindowByTitle(string title) =>
            InteropWindowQuery.GetVisibleApplicationWindows()
                .FirstOrDefault(window => window.GetCaption().IndexOf(title, StringComparison.OrdinalIgnoreCase) >= 0);

        var calculator = FindWindowByTitle("Calculator");
        #endregion
    }

    public static void ChildWindows(IInteropWindow window)
    {
        #region ChildWindows
        // The direct children, from top to bottom (Z-order)
        foreach (var child in window.GetChildren())
        {
            Console.WriteLine($"Child {child.GetClassname()}");
        }

        // Children, grandchildren, ...
        var descendants = window.GetDescendants();
        #endregion
    }

    public static void ParentAndOwner(IInteropWindow window)
    {
        #region ParentAndOwner
        // The parent is the window a child window lives in, top-level windows have none (IntPtr.Zero)
        IInteropWindow parent = window.GetParentWindow();

        // The owner is the window a dialog or tool window belongs to, e.g. the main window of the application
        IInteropWindow owner = window.GetOwnerWindow();

        // All other top-level windows of the same process
        var linked = window.GetLinkedWindows();
        #endregion
    }

    public static void ShowHide(IInteropWindow window)
    {
        #region ShowHide
        window.Minimize();
        window.Maximize();
        window.Restore();

        // Anything else ShowWindow supports
        User32Api.ShowWindow(window.Handle, ShowWindowCommands.Hide);
        User32Api.ShowWindow(window.Handle, ShowWindowCommands.ShowNoActivation);
        #endregion
    }

    public static void MoveResize(IInteropWindow window)
    {
        #region MoveResize
        // Move, keeping the size
        window.MoveTo(new NativePoint(100, 100));

        // Move and resize
        User32Api.SetWindowPos(window.Handle, IntPtr.Zero, 100, 100, 800, 600, WindowPos.SWP_NOZORDER | WindowPos.SWP_NOACTIVATE);

        // Placement: the normal (restored) bounds and the show state, e.g. to save and restore a layout
        WindowPlacement placement = window.GetPlacement();
        window.SetPlacement(placement);
        #endregion
    }

    public static void KeepVisible(IInteropWindow window)
    {
        #region KeepVisible
        // Move a window, e.g. after a monitor was disconnected, to a place where it can be seen
        if (!window.GetVisibleLocation(out var visibleLocation) || visibleLocation != window.GetInfo().Bounds.Location)
        {
            window.MoveTo(visibleLocation);
        }
        #endregion
    }

    public static async Task ToForeground(IInteropWindow window)
    {
        #region ToForeground
        // Restores a minimized window and makes it the foreground window. Windows may still refuse,
        // e.g. when the user is working in another application: then the taskbar button flashes.
        await window.ToForegroundAsync();
        #endregion
    }

    public static void AlwaysOnTop(IInteropWindow window)
    {
        #region AlwaysOnTop
        bool isTopmost = (window.GetInfo(forceUpdate: true).ExtendedStyle & ExtendedWindowStyleFlags.WS_EX_TOPMOST) != 0;
        User32Api.SetWindowPos(window.Handle, isTopmost ? WindowHandles.HWND_NOTOPMOST : WindowHandles.HWND_TOPMOST, 0, 0, 0, 0,
            WindowPos.SWP_NOMOVE | WindowPos.SWP_NOSIZE | WindowPos.SWP_NOACTIVATE);
        #endregion
    }

    public static void ZOrder(IInteropWindow window, IInteropWindow other)
    {
        #region ZOrder
        const WindowPos zOrderOnly = WindowPos.SWP_NOMOVE | WindowPos.SWP_NOSIZE | WindowPos.SWP_NOACTIVATE;

        // Send a window behind all other windows, or bring it to the top without activating it
        User32Api.SetWindowPos(window.Handle, WindowHandles.HWND_BOTTOM, 0, 0, 0, 0, zOrderOnly);
        User32Api.SetWindowPos(window.Handle, WindowHandles.HWND_TOP, 0, 0, 0, 0, zOrderOnly);

        // Place a window directly below another one
        User32Api.SetWindowPos(window.Handle, other.Handle, 0, 0, 0, 0, zOrderOnly);

        // A snapshot of the Z-order, index 0 is the top-most window
        var zOrder = InteropWindowQuery.GetTopWindows().Select(w => w.Handle).ToList();
        bool isAboveOther = zOrder.IndexOf(window.Handle) < zOrder.IndexOf(other.Handle);
        #endregion
    }

    public static void PostMessages(IInteropWindow window)
    {
        #region PostMessages
        // Ask a window to close, without waiting: an application which asks "Save changes?" doesn't block the caller
        if (!window.PostMessage(WindowsMessages.WM_CLOSE))
        {
            Console.WriteLine($"Posting failed, error {Marshal.GetLastWin32Error()}");
        }

        // Post a registered message to all top-level windows, e.g. to the other instances of your application
        uint showMessage = RegisteredWindowMessages.Register("MyApp.ShowMainWindow");
        User32Api.PostMessage(WindowHandles.HWND_BROADCAST, showMessage, IntPtr.Zero, IntPtr.Zero);
        #endregion
    }

    public static void WindowState(IInteropWindow window)
    {
        #region WindowState
        // Handles are recycled, check that the window still exists before working with it
        if (!window.Exists())
        {
            return;
        }
        bool isApplicationWindow = window.IsVisibleApplicationWindow();
        bool isOwnWindow = window.IsOwnedByCurrentProcess();
        // A Windows Store (UWP) app window
        bool isApp = window.IsApp();
        #endregion
    }

    public static void Screenshot(IInteropWindow window)
    {
        #region Screenshot
        // Renders the window, also when it's covered by other windows (not when it's minimized).
        // The result is cropped to the visible frame, without the invisible resize borders.
        using Bitmap bitmap = window.PrintWindow();
        bitmap?.Save("window.png", ImageFormat.Png);
        #endregion
    }

    public static void WindowIcon(IInteropWindow window)
    {
        #region WindowIcon
        // The icon of a window, as Bitmap or Icon
        using var smallIcon = window.GetIcon<Bitmap>();
        using var largeIcon = window.GetIcon<Icon>(useLargeIcons: true);
        #endregion
    }

    public static void Scroll(IInteropWindow window)
    {
        #region Scroll
        // null when the window has no scroll bar
        WindowScroller scroller = window.GetWindowScroller();
        if (scroller == null)
        {
            return;
        }
        scroller.Start();                 // scroll to the top
        while (!scroller.IsAtEnd)
        {
            // e.g. capture the visible part here
            if (!scroller.Next())         // one page down
            {
                break;
            }
        }
        scroller.Reset();                 // back to the original position
        #endregion
    }

    public static void ScrollingCapture(IInteropWindow window, NativePoint clickedPoint, Action<NativeRect> captureFrame)
    {
        #region ScrollingCapture
        // Windows with a Win32 scroll bar: WindowScroller; browsers, Electron, WPF, WinUI, Office: UI Automation.
        // Both implement IScroller. Run this on a background thread, not on the UI thread of the window.
        IScroller scroller = window.GetWindowScroller();
        scroller ??= UiAutomationScroller.FromPoint(clickedPoint);   // or UiAutomationScroller.FromWindow(window)
        if (scroller == null)
        {
            return;                       // nothing to scroll here
        }
        try
        {
            scroller.StepFraction = 0.5;  // half a page per step, so consecutive frames overlap for stitching
            scroller.Start();
            captureFrame(scroller.ViewportBounds);
            while (!scroller.IsAtEnd)
            {
                if (!scroller.Next())
                {
                    break;
                }
                // Give the application time to paint, then capture the visible part
                captureFrame(scroller.ViewportBounds);
            }
            scroller.Reset();             // back to where the user was
        }
        finally
        {
            (scroller as IDisposable)?.Dispose();   // releases the UI Automation COM objects
        }
        #endregion
    }

    public static async Task ScrollableAreas(IInteropWindow window, NativePoint mouseLocation)
    {
        #region ScrollableAreas
        // Greenshot-style: list the scrollable areas of the window under the mouse once (it blocks, so not on the UI thread;
        // works while your own window covers the screen), hit test them on every mouse move, scroll the chosen one later.
        IReadOnlyList<NativeRect> areas = await Task.Run(() => UiAutomationScroller.FindScrollableAreas(window));
        // The smallest area containing the cursor wins: in Visual Studio the editor, not the whole window
        NativeRect? chosen = areas
            .Where(area => area.Contains(mouseLocation))
            .OrderBy(area => area.Width * area.Height)
            .Select(area => (NativeRect?)area)
            .FirstOrDefault();
        if (chosen is { } area)
        {
            // After your own window is gone, FromPoint finds the same element under the middle of the area
            using var scroller = UiAutomationScroller.FromPoint(new NativePoint(area.X + area.Width / 2, area.Y + area.Height / 2));
            // ... the scrolling capture loop from the sample above
        }
        #endregion
    }

    public static void MonitorCreateDestroy()
    {
        #region MonitorCreateDestroy
        // Created and destroyed top-level and child windows, events arrive on the SharedMessageWindow thread
        IDisposable subscription = WinEventHook.WindowCreateDestroyObservable()
            .Subscribe(info =>
            {
                if (info.WinEvent == WinEvents.EVENT_OBJECT_CREATE)
                {
                    Console.WriteLine($"Created {info.Handle}");
                }
                else
                {
                    // The window is gone, only the handle is left
                    Console.WriteLine($"Destroyed {info.Handle}");
                }
            });

        // Removes the hook
        subscription.Dispose();
        #endregion
    }

    public static void MonitorTitle()
    {
        #region MonitorTitle
        var subscription = WinEventHook.WindowTitleChangeObservable()
            .Select(info => InteropWindowFactory.CreateFor(info.Handle))
            .Where(window => window.IsVisibleApplicationWindow())
            .Subscribe(window => Console.WriteLine($"New title: {window.GetCaption(forceUpdate: true)}"));
        #endregion
    }

    public static void MonitorForeground()
    {
        #region MonitorForeground
        // The user switched to another window
        var subscription = WinEventHook.Create(WinEvents.EVENT_SYSTEM_FOREGROUND)
            .Subscribe(info => Console.WriteLine($"Active: {InteropWindowFactory.CreateFor(info.Handle).GetCaption()}"));
        #endregion
    }

    public static void MonitorLocation()
    {
        #region MonitorLocation
        // Moved or resized windows, this also fires for the caret and the cursor: filter on the window itself
        var subscription = WinEventHook.Create(WinEvents.EVENT_OBJECT_LOCATIONCHANGE)
            .Where(info => info.ObjectIdentifier == ObjectIdentifiers.Window && info.IsSelf)
            // Many events arrive while dragging, only take the last one
            .Throttle(TimeSpan.FromMilliseconds(100))
            .Subscribe(info => Console.WriteLine($"Moved: {InteropWindowFactory.CreateFor(info.Handle).GetInfo(forceUpdate: true).Bounds}"));
        #endregion
    }

    public static void MonitorProcess(Process process)
    {
        #region MonitorProcess
        // Only the events of one process: less work for Windows and for you
        var subscription = WinEventHook.Create(WinEvents.EVENT_OBJECT_CREATE, WinEvents.EVENT_OBJECT_DESTROY, process: process.Id)
            .Where(info => info.ObjectIdentifier == ObjectIdentifiers.Window)
            .Subscribe(info => Console.WriteLine($"{process.ProcessName}: {info.WinEvent}"));
        #endregion
    }

    public static void Displays()
    {
        #region Displays
        foreach (var display in DisplayInfo.AllDisplayInfos)
        {
            Console.WriteLine($"{display.DeviceName}: {display.Bounds}, work area {display.WorkingArea}, primary: {display.IsPrimary}");
        }
        // The bounds of all displays together
        NativeRect desktop = DisplayInfo.ScreenBounds;
        #endregion
    }

    public static void InstalledSoftware()
    {
        #region InstalledSoftware
        // Reads the uninstall information of the registry (64 and 32 bit, machine and user)
        foreach (var software in InstallationInformation.InstalledSoftware().Where(s => s.Publisher == "Microsoft Corporation"))
        {
            Console.WriteLine($"{software.DisplayName} {software.DisplayVersion}");
        }
        #endregion
    }
}
