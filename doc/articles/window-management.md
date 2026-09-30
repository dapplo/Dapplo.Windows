# Window management

The **Dapplo.Windows** package wraps a window handle (HWND) in an `IInteropWindow`, which reads information about the
window on demand and caches it. Static classes find windows (`InteropWindowQuery`, `WindowsEnumerator`) and report
window events (`WinEventHook`).

```powershell
dotnet add package Dapplo.Windows
```

Namespaces used on this page: `Dapplo.Windows.Desktop`, `Dapplo.Windows.Enums`, `Dapplo.Windows.User32`,
`Dapplo.Windows.User32.Enums`, `Dapplo.Windows.User32.Structs`, `Dapplo.Windows.Common.Structs`, `Dapplo.Windows.App`,
`Dapplo.Windows.Icons`, `Dapplo.Windows.Software`, `Dapplo.Windows.Messages`, `Dapplo.Windows.Messages.Enums`,
`System.Reactive.Linq`.

## Window information

`InteropWindowFactory.CreateFor(handle)` wraps a handle, nothing is read at that moment. The `Get...` extension methods
read a value the first time and store it in the window object; later calls return the stored value unless you pass
`forceUpdate: true`.

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

`GetInfo()` returns a `WindowInfo` with `Bounds`, `ClientBounds`, `Style`, `ExtendedStyle` and more. For top-level
windows the bounds are corrected with the DWM "extended frame bounds", so they don't include the invisible resize
border that Windows 10 and 11 add around windows.

### Reading several values at once

`Fill()` reads the values you select with `InteropWindowRetrieveSettings`. It respects the cache, so add `ForceUpdate`
when you need fresh values. The cached values are also available as properties (`Caption`, `Info`, `IsVisible`, ...),
which are `null` when they were not read yet.

<!-- sample: WindowSamples.FillSelective -->
```csharp
// Read several values at once, the default is everything except the children (CacheAllAutoCorrect)
window.Fill();

// Only what you need
window.Fill(InteropWindowRetrieveSettings.Caption | InteropWindowRetrieveSettings.Info);

// ForceUpdate reads the values again, even when they were cached
window.Fill(InteropWindowRetrieveSettings.Info | InteropWindowRetrieveSettings.ForceUpdate);

// The cached values are properties, null when they were not retrieved
string caption = window.Caption;
NativeRect? bounds = window.Info?.Bounds;
```

| Setting | Reads |
|---|---|
| `CacheAll` | caption, class name, info, maximized, minimized, parent, owner, placement, process id, text, visible, scroll info |
| `CacheAllAutoCorrect` (default of `Fill`) | the same, with the bounds corrected (DWM frame, clipped to the parent) |
| `CacheAllWithChildren` | `CacheAll` plus the direct children, in Z-order |
| `ForceUpdate` | combine with the above to read again |

## Finding windows

`InteropWindowQuery.GetVisibleApplicationWindows()` returns the windows a user sees as application windows, from top to
bottom (Z-order): top-level windows (no parent, owned windows are included) which are visible, not minimized, have a
title and a size, and are not a tool window, a background store app or one of a few known system windows (the desktop
`Progman`, `Button`, `Dwm`). The same test is available as `window.IsVisibleApplicationWindow()`;
`window.IsVisiblePopup()` is the similar test for visible `WS_POPUP` windows, which also accepts tool windows and
windows without a title. Change the list of ignored classes with `InteropWindowQuery.AddIgnoreClass` /
`RemoveIgnoreClass`, or pass `false` to include them.

`InteropWindowQuery.GetTopWindows()` returns *all* top-level windows in Z-order (index 0 is the top-most window),
without any filter. `GetTopWindows(parent)` returns the direct children of a window, also in Z-order.

Both are a snapshot: the list is taken at once, with `EnumWindows` / `EnumChildWindows`, when you call the method.
Windows builds the list before it reports the first window, so the result can't loop, skip or repeat windows when
windows are activated, created or destroyed in the meantime (a `GetWindow(GW_HWNDNEXT)` walk can). The windows in the
snapshot can of course still change or disappear afterwards; `GetVisibleApplicationWindows()` takes the snapshot when it is called
and applies its filter while you enumerate the result.

<!-- sample: WindowSamples.ApplicationWindows -->
```csharp
// The application windows the user sees (visible, with a title, not minimized), from top to bottom
foreach (var window in InteropWindowQuery.GetVisibleApplicationWindows())
{
    Console.WriteLine($"{window.GetCaption()} ({window.GetClassname()})");
}
```

Filter with LINQ, or let `WindowsEnumerator` filter while it enumerates:

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

<!-- sample: WindowSamples.FindByTitle -->
```csharp
IInteropWindow FindWindowByTitle(string title) =>
    InteropWindowQuery.GetVisibleApplicationWindows()
        .FirstOrDefault(window => window.GetCaption().IndexOf(title, StringComparison.OrdinalIgnoreCase) >= 0);

var calculator = FindWindowByTitle("Calculator");
```

### Children, parent and owner

A *child* window lives inside its *parent* (a button in a dialog). A top-level window can have an *owner* (a dialog
belongs to the main window of the application), but it has no parent. Dapplo.Windows keeps both apart: `GetParent()`
never returns the owner, use `GetOwner()` for that.

<!-- sample: WindowSamples.ChildWindows -->
```csharp
// The direct children, from top to bottom (Z-order)
foreach (var child in window.GetChildren())
{
    Console.WriteLine($"Child {child.GetClassname()}");
}

// Children, grandchildren, ...
var descendants = window.GetDescendants();
```

<!-- sample: WindowSamples.ParentAndOwner -->
```csharp
// The parent is the window a child window lives in, top-level windows have none (IntPtr.Zero)
IInteropWindow parent = window.GetParentWindow();

// The owner is the window a dialog or tool window belongs to, e.g. the main window of the application
IInteropWindow owner = window.GetOwnerWindow();

// All other top-level windows of the same process
var linked = window.GetLinkedWindows();
```

`GetChildren()` returns the direct children in Z-order (a snapshot, see above) and stores them in `Children`; pass
`forceUpdate: true` to read them again. `GetDescendants()` returns all levels, depth-first (a child is followed by its
own descendants), and doesn't store them.

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

After a monitor was removed, windows can end up where nobody sees them. `GetVisibleLocation` finds a place on one of the
current displays:

<!-- sample: WindowSamples.KeepVisible -->
```csharp
// Move a window, e.g. after a monitor was disconnected, to a place where it can be seen
if (!window.GetVisibleLocation(out var visibleLocation) || visibleLocation != window.GetInfo().Bounds.Location)
{
    window.MoveTo(visibleLocation);
}
```

### Bringing a window to the front

`ToForegroundAsync()` restores a minimized window, waits (up to 2 seconds) until it's restored and makes it the
foreground window. Windows has rules about which process may change the foreground window; when it refuses, the
taskbar button flashes instead.

<!-- sample: WindowSamples.ToForeground -->
```csharp
// Restores a minimized window and makes it the foreground window. Windows may still refuse,
// e.g. when the user is working in another application: then the taskbar button flashes.
await window.ToForegroundAsync();
```

### Z-order and always on top

`SetWindowPos` changes the Z-order. `WindowHandles` has the special handles for its `hWndInsertAfter` argument:
`HWND_TOP`, `HWND_BOTTOM`, `HWND_TOPMOST` and `HWND_NOTOPMOST` (and `HWND_MESSAGE` and `HWND_BROADCAST` for other APIs).

<!-- sample: WindowSamples.AlwaysOnTop -->
```csharp
bool isTopmost = (window.GetInfo(forceUpdate: true).ExtendedStyle & ExtendedWindowStyleFlags.WS_EX_TOPMOST) != 0;
User32Api.SetWindowPos(window.Handle, isTopmost ? WindowHandles.HWND_NOTOPMOST : WindowHandles.HWND_TOPMOST, 0, 0, 0, 0,
    WindowPos.SWP_NOMOVE | WindowPos.SWP_NOSIZE | WindowPos.SWP_NOACTIVATE);
```

<!-- sample: WindowSamples.ZOrder -->
```csharp
const WindowPos zOrderOnly = WindowPos.SWP_NOMOVE | WindowPos.SWP_NOSIZE | WindowPos.SWP_NOACTIVATE;

// Send a window behind all other windows, or bring it to the top without activating it
User32Api.SetWindowPos(window.Handle, WindowHandles.HWND_BOTTOM, 0, 0, 0, 0, zOrderOnly);
User32Api.SetWindowPos(window.Handle, WindowHandles.HWND_TOP, 0, 0, 0, 0, zOrderOnly);

// Place a window directly below another one
User32Api.SetWindowPos(window.Handle, other.Handle, 0, 0, 0, 0, zOrderOnly);

// A snapshot of the Z-order, index 0 is the top-most window
var zOrder = InteropWindowQuery.GetTopWindows().Select(w => w.Handle).ToList();
bool isAboveOther = zOrder.IndexOf(window.Handle) < zOrder.IndexOf(other.Handle);
```

### Posting messages

`window.PostMessage(...)` (or `User32Api.PostMessage` for a handle) puts a message in the message queue of the window's
thread and returns immediately, `SendMessage` waits until the message was processed. It returns `false` when the
message couldn't be posted, `Marshal.GetLastWin32Error()` tells why (e.g. the window is gone, or UIPI blocks messages
from a process with a lower integrity level). Only post messages with values in `wParam` / `lParam`, not with pointers
to your memory. `User32Api.PostThreadMessage` posts to a thread (its message loop) instead of a window.

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

## Window state

Window handles are reused by Windows after a window is destroyed. Check `Exists()` before you use a handle you kept
for a while.

<!-- sample: WindowSamples.WindowState -->
```csharp
// Handles are recycled, check that the window still exists before working with it
if (!window.Exists())
{
    return;
}
bool isApplicationWindow = window.IsVisibleApplicationWindow();
bool isOwnWindow = window.IsOwnedByCurrentProcess();
// A Windows Store (UWP) app window
bool isApp = window.IsApp();
```

## Screenshots, icons and scrolling

`PrintWindow()` asks the window to render itself into a `Bitmap`, so it also works for windows which are covered by
other windows. It uses `PW_RENDERFULLCONTENT` on Windows 8.1 and later, which captures DirectComposition content
(browsers, UWP apps) as well. Minimized windows can't be captured. For WPF use `PrintWindowAsBitmapSource()` from
Dapplo.Windows.Wpf.

<!-- sample: WindowSamples.Screenshot -->
```csharp
// Renders the window, also when it's covered by other windows (not when it's minimized).
// The result is cropped to the visible frame, without the invisible resize borders.
using Bitmap bitmap = window.PrintWindow();
bitmap?.Save("window.png", ImageFormat.Png);
```

<!-- sample: WindowSamples.WindowIcon -->
```csharp
// The icon of a window, as Bitmap or Icon
using var smallIcon = window.GetIcon<Bitmap>();
using var largeIcon = window.GetIcon<Icon>(useLargeIcons: true);
```

`GetWindowScroller()` returns a `WindowScroller` for a window with a scroll bar, for example to capture a long page in
parts. It returns `null` when the window can't be scrolled.

<!-- sample: WindowSamples.Scroll -->
```csharp
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
```

## Window events

`WinEventHook` turns [WinEvents](https://learn.microsoft.com/en-us/windows/win32/winauto/winevents) into observables.
The hook is installed when you subscribe and removed when the last subscription is disposed. The events are delivered
on the thread of the [SharedMessageWindow](window-messages.md): keep `OnNext` short and use `ObserveOn` for slow work.

The events are also raised for objects inside windows (menus, the caret, list items ...). Check
`ObjectIdentifier == ObjectIdentifiers.Window` (and `IsSelf`) when you only want windows.

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

<!-- sample: WindowSamples.MonitorTitle -->
```csharp
var subscription = WinEventHook.WindowTitleChangeObservable()
    .Select(info => InteropWindowFactory.CreateFor(info.Handle))
    .Where(window => window.IsVisibleApplicationWindow())
    .Subscribe(window => Console.WriteLine($"New title: {window.GetCaption(forceUpdate: true)}"));
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

Limit a hook to one process (or thread) when you can, Windows then only calls you for that process:

<!-- sample: WindowSamples.MonitorProcess -->
```csharp
// Only the events of one process: less work for Windows and for you
var subscription = WinEventHook.Create(WinEvents.EVENT_OBJECT_CREATE, WinEvents.EVENT_OBJECT_DESTROY, process: process.Id)
    .Where(info => info.ObjectIdentifier == ObjectIdentifiers.Window)
    .Subscribe(info => Console.WriteLine($"{process.ProcessName}: {info.WinEvent}"));
```

## Displays

`DisplayInfo.AllDisplayInfos` lists the monitors; it's updated when displays, resolutions or the work area change.

<!-- sample: WindowSamples.Displays -->
```csharp
foreach (var display in DisplayInfo.AllDisplayInfos)
{
    Console.WriteLine($"{display.DeviceName}: {display.Bounds}, work area {display.WorkingArea}, primary: {display.IsPrimary}");
}
// The bounds of all displays together
NativeRect desktop = DisplayInfo.ScreenBounds;
```

## Installed software

<!-- sample: WindowSamples.InstalledSoftware -->
```csharp
// Reads the uninstall information of the registry (64 and 32 bit, machine and user)
foreach (var software in InstallationInformation.InstalledSoftware().Where(s => s.Publisher == "Microsoft Corporation"))
{
    Console.WriteLine($"{software.DisplayName} {software.DisplayVersion}");
}
```

## Tips

- An `IInteropWindow` is a snapshot. Values which change (title, bounds, state) need `forceUpdate: true` when you read
  them again later.
- `GetTopWindows()`, `GetVisibleApplicationWindows()`, `GetChildren()` and `WindowsEnumerator` return a snapshot of the windows
  at the moment of the call; call them again for the current state.
- Reading the text of a window which belongs to a hung application times out after 500 ms instead of blocking.
- Keep WinEvent subscriptions narrow (event range, process) and throttle location events, they are frequent.

## See also

- [Window messages and the SharedMessageWindow](window-messages.md)
- [Windows Forms and WPF](forms-and-wpf.md)
- [Common scenarios](common-scenarios.md)
