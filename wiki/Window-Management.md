# Window management

Package **Dapplo.Windows**. Full version: [Window management](https://www.dapplo.net/Dapplo.Windows/articles/window-management.html).

`InteropWindowFactory.CreateFor(handle)` wraps a window handle in an `IInteropWindow`. The `Get...` extension methods
read a value once and cache it; pass `forceUpdate: true` to read it again.

<!-- sample: WindowSamples.WindowInformation -->
```csharp
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
```

## Finding windows

`GetVisibleApplicationWindows()` returns the application windows a user sees: visible, not minimized, with a title, no tool
windows. `GetTopWindows()` returns all top-level windows without a filter, `GetTopWindows(parent)` and
`window.GetChildren()` the direct children. All of them are in Z-order (top-most first) and are a snapshot taken at
once with `EnumWindows` / `EnumChildWindows` when you call them, so they can't loop or skip windows while the Z-order
changes.

<!-- sample: WindowSamples.ApplicationWindows -->
```csharp
// The application windows the user sees (visible, with a title, not minimized), from top to bottom
foreach (var window in InteropWindowQuery.GetVisibleApplicationWindows())
{
    Console.WriteLine($"{window.GetCaption()} ({window.GetClassname()})");
}
```

<!-- sample: WindowSamples.FilterWindows -->
```csharp
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
```

Children live inside their parent; dialogs have an owner, not a parent. `GetParent()` never returns the owner.

<!-- sample: WindowSamples.ParentAndOwner -->
```csharp
// The parent is the window a child window lives in, top-level windows have none (IntPtr.Zero)
IInteropWindow parent = window.GetParentWindow();

// The owner is the window a dialog or tool window belongs to, e.g. the main window of the application
IInteropWindow owner = window.GetOwnerWindow();

// All other top-level windows of the same process
var linked = window.GetLinkedWindows();
```

## Changing windows

<!-- sample: WindowSamples.ShowHide -->
```csharp
window.Minimize();
window.Maximize();
window.Restore();

// Anything else ShowWindow supports
User32Api.ShowWindow(window.Handle, ShowWindowCommands.Hide);
User32Api.ShowWindow(window.Handle, ShowWindowCommands.ShowNoActivation);
```

<!-- sample: WindowSamples.MoveResize -->
```csharp
// Move, keeping the size
window.MoveTo(new NativePoint(100, 100));

// Move and resize
User32Api.SetWindowPos(window.Handle, IntPtr.Zero, 100, 100, 800, 600, WindowPos.SWP_NOZORDER | WindowPos.SWP_NOACTIVATE);

// Placement: the normal (restored) bounds and the show state, e.g. to save and restore a layout
WindowPlacement placement = window.GetPlacement();
window.SetPlacement(placement);
```

<!-- sample: WindowSamples.ToForeground -->
```csharp
// Restores a minimized window and makes it the foreground window. Windows may still refuse,
// e.g. when the user is working in another application: then the taskbar button flashes.
await window.ToForegroundAsync();
```

`WindowHandles` has the special handles `HWND_TOP`, `HWND_BOTTOM`, `HWND_TOPMOST`, `HWND_NOTOPMOST`, `HWND_MESSAGE`
and `HWND_BROADCAST`:

<!-- sample: WindowSamples.AlwaysOnTop -->
```csharp
bool isTopmost = (window.GetInfo(forceUpdate: true).ExtendedStyle & ExtendedWindowStyleFlags.WS_EX_TOPMOST) != 0;
User32Api.SetWindowPos(window.Handle, isTopmost ? WindowHandles.HWND_NOTOPMOST : WindowHandles.HWND_TOPMOST, 0, 0, 0, 0,
    WindowPos.SWP_NOMOVE | WindowPos.SWP_NOSIZE | WindowPos.SWP_NOACTIVATE);
```

`PostMessage` queues a message and returns immediately, `false` (see `Marshal.GetLastWin32Error()`) when it couldn't:

<!-- sample: WindowSamples.PostMessages -->
```csharp
// Ask a window to close, without waiting: an application which asks "Save changes?" doesn't block the caller
if (!window.PostMessage(WindowsMessages.WM_CLOSE))
{
    Console.WriteLine($"Posting failed, error {Marshal.GetLastWin32Error()}");
}

// Post a registered message to all top-level windows, e.g. to the other instances of your application
uint showMessage = RegisteredWindowMessages.Register("MyApp.ShowMainWindow");
User32Api.PostMessage(WindowHandles.HWND_BROADCAST, showMessage, IntPtr.Zero, IntPtr.Zero);
```

## Screenshots

<!-- sample: WindowSamples.Screenshot -->
```csharp
// Renders the window, also when it's covered by other windows (not when it's minimized).
// The result is cropped to the visible frame, without the invisible resize borders.
using Bitmap bitmap = window.PrintWindow();
bitmap?.Save("window.png", ImageFormat.Png);
```

## Scrolling

`window.GetWindowScroller()` scrolls windows with a Win32 scroll bar; for windows which draw their own scroll bars (browsers,
Electron, WPF, WinUI, Office) the **Dapplo.Windows.Automation** package has `UiAutomationScroller`, which uses the UI
Automation `ScrollPattern`; controls without one which expose a scroll bar element (e.g. the Visual Studio editor) are
scrolled with the mouse wheel (`IsScrollBarFallback`, `IsPositionKnown`). Both implement `IScroller`. `StepFraction` scrolls part of a page per step, so the frames of a
scrolling capture overlap; `ViewportBounds` is the area to capture. See
[Window management](https://www.dapplo.net/Dapplo.Windows/articles/window-management.html#scrolling) for the details.

<!-- sample: WindowSamples.ScrollingCapture -->
```csharp
// Windows with a Win32 scroll bar: WindowScroller; browsers, Electron, WPF, WinUI, Office: UI Automation.
// Both implement IScroller. Run this on a background thread, not on the UI thread of the window.
IScroller scroller = window.GetWindowScroller();
scroller ??= UiAutomationScroller.FromPoint(clickedPoint);   // or UiAutomationScroller.FromWindow(window)
// FromPoint also finds controls without a ScrollPattern which expose a scroll bar (e.g. the Visual Studio editor):
// IsScrollBarFallback is true, they are scrolled with the mouse wheel, so the area must be visible on the screen.
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
        // Give the application time to paint, then capture the visible part.
        // When UiAutomationScroller.IsPositionKnown is false, IsAtEnd can't tell the end: stop when the frame didn't change.
        captureFrame(scroller.ViewportBounds);
    }
    scroller.Reset();             // back to where the user was
}
finally
{
    (scroller as IDisposable)?.Dispose();   // releases the UI Automation COM objects
}
```

`UiAutomationScroller.FindScrollableAreas(window)` lists the screen bounds of all scrollable areas inside one window (it
blocks, run it on a background thread), e.g. to show which area under the mouse can scroll:

<!-- sample: WindowSamples.ScrollableAreas -->
```csharp
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
```

## Window events

`WinEventHook` events arrive on the thread of the [[SharedMessageWindow]]. Filter on
`ObjectIdentifier == ObjectIdentifiers.Window` when you only want windows.

<!-- sample: WindowSamples.MonitorCreateDestroy -->
```csharp
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
```

<!-- sample: WindowSamples.MonitorForeground -->
```csharp
// The user switched to another window
var subscription = WinEventHook.Create(WinEvents.EVENT_SYSTEM_FOREGROUND)
    .Subscribe(info => Console.WriteLine($"Active: {InteropWindowFactory.CreateFor(info.Handle).GetCaption()}"));
```

<!-- sample: WindowSamples.MonitorLocation -->
```csharp
// Moved or resized windows, this also fires for the caret and the cursor: filter on the window itself
var subscription = WinEventHook.Create(WinEvents.EVENT_OBJECT_LOCATIONCHANGE)
    .Where(info => info.ObjectIdentifier == ObjectIdentifiers.Window && info.IsSelf)
    // Many events arrive while dragging, only take the last one
    .Throttle(TimeSpan.FromMilliseconds(100))
    .Subscribe(info => Console.WriteLine($"Moved: {InteropWindowFactory.CreateFor(info.Handle).GetInfo(forceUpdate: true).Bounds}"));
```

More: filling several values at once, Z-order, always on top, scrolling, displays, installed software, see the
[documentation](https://www.dapplo.net/Dapplo.Windows/articles/window-management.html).
