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
`System.Reactive.Linq`, `Dapplo.Windows.Input.Mouse` and, for UI Automation, `Dapplo.Windows.Automation`.

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

## Scrolling

`GetWindowScroller()` returns a `WindowScroller` for a window with a Win32 scroll bar, for example to capture a long page in
parts. It returns `null` when the window can't be scrolled that way.

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

### Scrolling capture

A scrolling capture stitches frames, so consecutive frames must overlap: set `StepFraction` (greater than 0, at most 1,
default 1.0 = a page) to scroll part of a page per `Next()` / `Previous()`.

| `ScrollMode` | A step of `StepFraction` |
|---|---|
| `AbsoluteWindowMessage` | The position moves by `max(1, PageSize * StepFraction)`, clamped to the range |
| `WindowsMessage` | Below 1.0, `SB_LINEDOWN` / `SB_LINEUP` until the position moved by `max(1, PageSize * StepFraction)` scroll units (checked after every message, so windows which scroll in pixels work too); 1.0 sends `SB_PAGEDOWN` / `SB_PAGEUP` |
| `MouseWheel` | `WheelDelta` (a page) times the fraction, rounded to whole notches (at least one); set `UseFractionalWheelDelta` for applications which handle high-resolution wheel deltas. A horizontal scroll bar gets horizontal wheel input |
| `KeyboardPageUpDown` | The fraction is ignored, a key press scrolls a page |

In `MouseWheel` mode the wheel input goes to the middle of `ScrollingWindow`. For an area inside a window (the page of a
browser, a list in a dialog) set `WheelLocation` (screen coordinates). The system delivers wheel input to the window under
the cursor, so the cursor moves there; `RestoreCursorAfterWheel = true` moves it back after every wheel movement (in the
same `SendInput` call). `MouseInputGenerator.MoveMouseWheelAt(delta, location, restoreCursor, horizontal)` does the same
for your own wheel input.

`ViewportBounds` is the scrolling area in screen coordinates, to crop the frames.

### Windows without a Win32 scroll bar: UI Automation

Chromium / Electron, Firefox, WPF, WinUI, Office and the Explorer file list draw their own scroll bars, so
`GetWindowScroller()` returns `null` for them. The **Dapplo.Windows.Automation** package scrolls them with the UI Automation
`ScrollPattern`, using the native UI Automation COM API (no WPF needed):

```powershell
dotnet add package Dapplo.Windows.Automation
```

- `UiAutomationScroller.FromPoint(screenPoint, horizontal)` takes the element under the point and walks up to the first one
  with a `ScrollPattern` which can scroll in that direction; `FromWindow(window, horizontal)` uses the window, the element in
  its middle or the first scrollable descendant.
- `ViewportBounds` is the bounding rectangle of that element, `ScrollPercent` the position (0 to 100), `VisibleFraction` the
  visible part of the content; `IsAtStart` / `IsAtEnd` allow `PercentTolerance` (0.01 percentage points).
- `Start()` / `End()` / `Next()` / `Previous()` / `Reset()` use `SetScrollPercent`, or `Scroll` with small or large increments
  when setting the percentage is not supported. A step of `StepFraction` moves the content by that part of the viewport.
- `ScrollMode = UiAutomationScrollModes.MouseWheel` wheels at the viewport centre (or `WheelLocation`) one notch at a time
  until the position moved far enough, for controls which report the pattern but ignore it.
- Use it on a background thread, never on the UI thread which owns the target window; an STA thread is not needed. When the
  element is gone (navigation, closed window) `IsAvailable` is false, `IsAtEnd` is true so loops end, and `Refresh()` finds
  it again. `Next()` returns false when the position didn't change, so a loop can't run forever. Chromium builds its
  accessibility tree on the first request, so retry a `null` from `FromPoint` once after a short delay. Dispose it to release
  the COM objects.

#### Controls without a ScrollPattern

Some controls scroll by themselves and expose no `ScrollPattern` anywhere, but do expose their scroll bar as an element with
control type ScrollBar (e.g. the Visual Studio editor and Output pane). When no element on the way up from the point has a
`ScrollPattern`, `FromPoint` takes the first one which has a visible child scroll bar in the direction; `FromWindow` does the
same after its `ScrollPattern` lookups found nothing. Such a scroller has `IsScrollBarFallback` true and always scrolls with
the mouse wheel (`ScrollMode` is `MouseWheel`, setting `ScrollPattern` throws), so the area has to be visible on the screen;
`ViewportBounds` is the control without its scroll bar when the scroll bar is at an edge (and without a scroll bar of the other
orientation at an edge, e.g. a horizontal one at the bottom), so the frames of a capture don't show it; `WheelLocation` and
`RestoreCursorAfterWheel` work as usual.

The position comes from the scroll bar:
- its RangeValue pattern: the position from `Value` between `Minimum` and `Maximum`, the visible part from `LargeChange`;
  `Start()`, `End()` and `Reset()` set the value when it's writable;
- else the position of its thumb between the line buttons (to a pixel);
- else it is unknown: `IsPositionKnown` is false, `ScrollPercent` is -1, `IsAtStart` / `IsAtEnd` are only true when the line
  button in that direction is disabled and the other one enabled (WPF disables both while the mouse isn't over the scroll
  bar), `Next()` / `Previous()` move one wheel notch per step and return true, and `Start()`,
  `End()` and `Reset()` return false. The caller detects the end itself, e.g. when the captured content stops changing.

With a known position a step wheels one notch at a time until about `StepFraction` of a page moved. `Start()` and `End()` set
a writable RangeValue and check the position read back; otherwise, like `Reset()`, they wheel several pages per input
(independent of `StepFraction`, more notches when one input didn't move the thumb) until the start or end is reached or the
position stops moving, with a limit from the remaining percentage. They return false when the start or end wasn't reached.

Both scrollers implement `IScroller`, so one piece of code handles both:

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

### Finding the scrollable areas of a window

One window can hold several scrollable areas: in Visual Studio the editor, the Output pane and the Solution Explorer are
all in one WPF window. `UiAutomationScroller.FindScrollableAreas(window, horizontal)` lists the screen bounds (pixels) of the
window element and every descendant which can scroll in that direction, and the parent of every scroll bar element in that
direction (the controls without a `ScrollPattern` above; without the scroll bars at its edges, like `ViewportBounds`), in tree order, without hit testing on the screen,
so it works while another window (a selection window) covers it. It is one `FindAll` with a cache request for the bounding
rectangle, plus one call per scroll bar for its parent; empty and offscreen elements are left out, duplicates (a
`ScrollPattern` element and the parent of its own scroll bar) are returned once, an outer area comes before the areas
inside it, and the result is an empty list (never `null`) when nothing scrolls
or UI Automation isn't available. `FindScrollableAreas(window, horizontal, timeout, includeScrollBarAreas: false)`
returns the `ScrollPattern` elements only. The areas can stick out of the window (e.g. a ScrollViewer larger than its pane), clip
them yourself when needed.

The call blocks: large trees (Visual Studio, Office, browsers) take a while. Run it on a background thread and cache the
result per window. The UI Automation connection and transaction timeouts are set to `timeout` (default
`UiAutomationScroller.DefaultFindTimeout`, 2 seconds; Windows 8 and later), so a hanging provider can't block for the
default 20 seconds.

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
