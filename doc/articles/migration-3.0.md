# Migrating to Dapplo.Windows 3.0

Dapplo.Windows 3.0 fixes many interop bugs. Where an API encoded a wrong concept, it was changed instead of kept for
compatibility. This page lists every breaking change with the code you need to update. The full list of changes is in
the [changelog](../../CHANGELOG.md).

## HRESULT

`HResult` is now `enum HResult : int`. `Failed()` and `Succeeded()` work, and `ThrowOnFailure()` throws. Failures
that were silently treated as success in 2.x now take the failure branch, so review code that calls COM, DWM or DPI
functions. Casts to `uint` need `unchecked((uint)hr)`.

## Window information and scrolling

### `MonitorFrom`

The member names are unchanged but the values now match Win32, and the enum is no longer `[Flags]`. Recompiling is
enough, unless you stored the numbers or combined members with `|`.

### `SendMessage`

WPARAM, LPARAM and LRESULT are pointer-sized everywhere.

```csharp
// 2.x
User32Api.SendMessage(hWnd, msg, 5, 0);
int r = User32Api.SendMessage(hWnd, WindowsMessages.WM_VSCROLL, ScrollBarCommands.SB_LINEDOWN, 0);
// 3.0
User32Api.SendMessage(hWnd, msg, new IntPtr(5), IntPtr.Zero);
IntPtr r = User32Api.SendMessage(hWnd, WindowsMessages.WM_VSCROLL, ScrollBarCommands.SB_LINEDOWN, IntPtr.Zero);
```

### `ScrollBarInfo`

`ThumbTop` and `ThumbBottom` were swapped. `ThumbSize` is renamed to `LineButtonSize`, because it is the size of the
arrow buttons. The thumb size is `ThumbBottom - ThumbTop`.

### `SysColorIndexes.Color3Dface`

Now 15 (`COLOR_3DFACE`). No code change is needed.


### `Fill()` caches again

`InteropWindowExtensions.Fill()` used to ignore its settings and always refresh, auto-correct and query the
maximized state. It now does what the flags say.

- Pass `InteropWindowRetrieveSettings.ForceUpdate` when you need fresh values.
- `CacheAll`, `CacheAllWithChildren` and `CacheAllChildZorder` no longer auto-correct bounds. Use `CacheAllAutoCorrect`
  or add `AutoCorrectValues`.

### Wheel scroll lines

```csharp
// 2.x
int lines = WindowScroller.ScrollWheelLinesFromRegistry;
// 3.0: 0 means the wheel doesn't scroll, uint.MaxValue means one notch scrolls a page
uint lines = WindowScroller.ScrollWheelLines;
int delta = WindowScroller.CalculateWheelDelta(pageSize, lines);
```

## Geometry

`NativeRectExtensions.Intersect2` is removed; it returned the union on the Y axis. Use `Intersect`, which returns
`NativeRect.Empty` when the rectangles don't overlap.

```csharp
// 2.x
var overlap = rect1.Intersect2(rect2);
// 3.0
var overlap = rect1.Intersect(rect2);
```

### Docking and overlap

Right and Bottom are exclusive, as in a Win32 `RECT`. Flush rectangles (`a.Right == b.Left`) are now docked; if you
built docked rectangles with a 1-pixel gap, make them flush. `HasOverlap` is now the same as `IntersectsWith`.

### Conversions

Lossy conversions are explicit:

```csharp
// 2.x
NativeRect r = rectFloat;
NativePoint p = pointFloat;
// 3.0: the containing integer rectangle, or round explicitly
var r = (NativeRect)rectFloat;
var p = pointFloat.Round();
```

Points floor, sizes round up and rectangles become the smallest containing integer rectangle, so values can differ
slightly from 2.x (10.5/30.5 now becomes 10/31 where it was 10/30).

### Sorting and type converters

`NativeSize.CompareTo` now sorts ascending by area. `NativeSizeTypeConverter` writes `Width,Height`; strings saved by
2.x were `Height,Width`, so swap them once or re-save them.

## Windows version

`WindowsVersion` reads the real version with `RtlGetVersion`. `IsWindowsVista` and `IsWindows10` now mean exactly
that version:

```csharp
// 2.x meaning "Windows 10 or later"
if (WindowsVersion.IsWindows10) { }
// 3.0
if (WindowsVersion.IsWindows10OrLater) { }
```

## Icons and cursors

`IconInfo` and `IconInfoEx` no longer create a SafeHandle on every property read.

```csharp
// 2.x
using var color = iconInfo.ColorBitmapHandle;
iconInfoEx.Dispose();
// 3.0: raw, non-owning handles
IntPtr color = iconInfo.ColorBitmap;
// take ownership once ...
iconInfoEx.TakeBitmaps(out var mask, out var colorBitmap);
using (mask) using (colorBitmap) { /* ... */ }
// ... or just free them
iconInfoEx.DeleteBitmaps();
```

## DPI

`DpiAwarenessContext` is a pointer-sized struct. Compare contexts with the Win32 function, because the handles Windows
returns are not the pseudo values:

```csharp
// 2.x
if (NativeDpiMethods.GetThreadDpiAwarenessContext() == DpiAwarenessContext.PerMonitorAwareV2) { }
// 3.0
if (NativeDpiMethods.AreDpiAwarenessContextsEqual(NativeDpiMethods.GetThreadDpiAwarenessContext(), DpiAwarenessContext.PerMonitorAwareV2)) { }
```

The members are static properties, so they can't be used in `case` labels or as default parameter values.

- `GetWindowDpiHostingBehavior()` → `GetWindowDpiHostingBehavior(hWnd)`.
- `EnableNonClientDpiScaling(hWnd).Succeeded()` → `EnableNonClientDpiScaling(hWnd)` (returns `bool`).
- `DialogDpiChangeBehaviors.DisableControlRelayout` is now 4.

## COM

`IUnknown` is removed; use `Marshal.QueryInterface`, `Marshal.AddRef` and `Marshal.Release`. `IDispatch.GetTypeInfo`
now returns `System.Runtime.InteropServices.ComTypes.ITypeInfo` on every target, and `Invoke` reports the argument
error as `out uint`.

## Messages and SharedMessageWindow

`WindowMessage` is a sealed class. Set `Handled` and `Result` synchronously inside `OnNext`; after `ObserveOn` the
reply has already been sent.

```csharp
// 2.x (had no effect, the struct was a copy)
SharedMessageWindow.Messages.Subscribe(m => { m.Result = (nuint)1; m.Handled = true; });
// 3.0
SharedMessageWindow.Messages.Subscribe(m => { m.Result = 1; m.Handled = true; });
```

The window now exists for the whole process after first use. `Handle` never returns 0, so drop any "wait until the
window exists" code. Registrations that need the window go into `Listen(onSetup, onTeardown)`, which runs both on the
window thread. Use `SharedMessageWindow.Invoke(hwnd => ...)` to run other code there.

## End of session

```csharp
// 2.x
ApplicationRestartManager.ListenForEndSession(onQuerySession: reason => canClose, onEndSession: reason => Save()).Subscribe();
// 3.0: answer synchronously inside OnNext
ApplicationRestartManager.ListenForEndSession().Subscribe(m =>
{
    if (m.IsQuery)
    {
        if (!canClose) m.Veto("Unsaved captures");
    }
    else if (m.IsSessionEnding)
    {
        Save();
    }
});
```

`EndSessionMessage` moved from `Dapplo.Windows.Messages.Structs` to `Dapplo.Windows.AppRestartManager`.
`WasRestartRequested()` becomes `WasRestartRequested("/restore")` with the argument you registered.

## Keyboard and mouse hooks

`KeyboardHook` and `MouseHook` run on their own thread, so subscribers are no longer called on the UI thread. Decide
`Handled` quickly and synchronously, and move UI work to the UI thread:

```csharp
KeyboardHook.KeyboardEvents
    .Where(handler)                              // sets Handled synchronously
    .ObserveOn(SynchronizationContext.Current)   // then do the slow work on the UI thread
    .Subscribe(args => OnHotkey());
```

For listeners that never set `Handled`, use `KeyboardEventsNonBlocking` / `MouseEventsNonBlocking`.

`"win"` parses to the new `VirtualKeyCode.Win`, which matches either Windows key. Use `LeftWin` to require the left key.

## Raw input

`args.RawInput.Device.HID.GetData()` becomes `args.HidData`. `RawInputApi.GetRawInputData(…)` becomes
`RawInputApi.TryGetRawInputData(lParam, out var rawInput, out var hidData)`.

## Clipboard

- Delayed rendering: register the renderer before `SetDelayedRenderedContent`.

  ```csharp
  // 2.x
  ClipboardNative.OnRenderFormat.Subscribe(r => r.AccessToken.SetAsUnicodeString(text, r.RequestedFormatId));
  // 3.0
  using var registration = ClipboardNative.RegisterDelayedRenderer(StandardClipboardFormats.UnicodeText,
      r => r.AccessToken.SetAsUnicodeString(text, r.RequestedFormatId));
  ```

- `ClipboardUpdateInformation.Create(hWnd)` becomes `Create()`. `OnUpdate` no longer reads content; call `Access()`
  after `ObserveOn` to read it.
- Keep an access token on one thread and don't `await` while holding it. Cancelling `AccessAsync` throws
  `OperationCanceledException` instead of returning a token with `IsOpenTimeout`.
- `SetCloudClipboardOptions()` without arguments now places nothing. `SetExcludeClipboardContentFromMonitorProcessing(true)`
  becomes `ExcludeFromMonitorProcessing()`.

## System state

```csharp
// 2.x
SystemStateApi.PreventSleep(); /* ... */ SystemStateApi.AllowSleep();
// 3.0
using (SystemStateApi.PreventSleep("Recording")) { /* ... */ }
```

## Kernel32

`ProcessAccessRights.QueryLimitedInformation` had the value of `QueryInformation` (0x400). It is now 0x1000, which also
works for elevated processes. If you relied on the full query right, use `QueryInformation`. `All` is now the
Vista-and-later value 0x1FFFFF.

## Multimedia

`WinMm.Play(byte[])` returns `bool` and copies the data, so you no longer need to keep the array pinned. Callers must
recompile.
