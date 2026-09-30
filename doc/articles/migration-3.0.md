# Migrating to Dapplo.Windows 3.0

Dapplo.Windows 3.0 fixes many interop bugs. Where an API encoded a wrong concept, it was changed instead of kept for
compatibility. This page lists every breaking change with the code you need to update. The full list of changes is in
the [changelog](../../CHANGELOG.md).

## Target frameworks and packages

3.0 targets `net480` and `net10.0-windows`. The core packages no longer pull in WinForms or WPF; add
**Dapplo.Windows.Forms** or **Dapplo.Windows.Wpf** when you use those helpers.

| 2.x | 3.0 |
|---|---|
| `Dapplo.Windows.Dpi.Forms.DpiAwareForm`, `DpiUnawareForm`, `FormsDpiExtensions.AttachDpiHandler(Form / ContextMenuStrip)` | Dapplo.Windows.Forms, `Dapplo.Windows.Forms.Dpi` |
| `BitmapScaleHandler.AddTarget(Button / ToolStripItem, …)` | `Dapplo.Windows.Forms.Dpi.BitmapScaleHandlerExtensions`; core: `AddTargetAction(target, key, apply, execute)` |
| `Dapplo.Windows.Messages.WinProcListener` (`AddHook(HwndSourceHook)`) | `Dapplo.Windows.Forms.Messages.WinProcListener` (`AddHook(WinProcHook)`, same signature) |
| `Dapplo.Windows.Messages.WinProcFormsExtensions.WinProcFormsMessages()` | `Dapplo.Windows.Forms.Messages` |
| `Dapplo.Windows.Extensions.FormsExtensions` | `Dapplo.Windows.Forms.FormsExtensions` |
| `Dapplo.Windows.Messages.WinProcHandler`, `WinProcHandlerHook`, `WinProcWindowsExtensions.WinProcMessages(Window)` | Dapplo.Windows.Wpf, `Dapplo.Windows.Wpf.Messages` |
| `Dapplo.Windows.Dpi.Wpf.WindowDpiExtensions` | `Dapplo.Windows.Wpf.Dpi.WindowDpiExtensions` |
| `Dapplo.Windows.Extensions.WindowsExtensions` (`AsInteropWindow`, `GetHandle`, `ApplyPlacement`, `RetrievePlacement`) | `Dapplo.Windows.Wpf.WindowExtensions` |
| `Dapplo.Windows.Extensions.BitmapExtensions.ToBitmapSource(Bitmap / Image)` | `Dapplo.Windows.Wpf.BitmapSourceExtensions.ToBitmapSource(Bitmap / Image / Icon)` |
| `interopWindow.PrintWindow<TBitmap>()` | `PrintWindow()` returns `Bitmap`; `PrintWindowAsBitmapSource()` in Dapplo.Windows.Wpf |
| `GetIcon<BitmapSource>()`, `IconHelper.*<BitmapSource>` | `GetIcon<Bitmap>().ToBitmapSource()` |
| `DwmApi.ColorizationColor` (WPF `Color`) | `DwmApi.ColorizationSystemDrawingColor.ToMediaColor()` |
| Casts between `Native*` structs and `System.Windows.Point` / `Size` / `Rect` / `Int32Rect` | `Dapplo.Windows.Wpf.NativeStructWpfExtensions`: `ToPoint`, `ToNativePointFloat`, `ToSize`, `ToNativeSize`, `ToNativeSizeFloat`, `ToRect`, `ToNativeRectFloat`, `ToInt32Rect`, `ToNativeRect` |
| `NativeRect(Float).Transform(Matrix)` | `Dapplo.Windows.Wpf.NativeStructWpfExtensions.Transform` |

```csharp
// 2.x
System.Windows.Rect wpfRect = nativeRect;
// 3.0
using Dapplo.Windows.Wpf;
var wpfRect = nativeRect.ToRect();
```

## Renames and moved types (sweep)

| 2.x | 3.0 |
|---|---|
| `window.GetParent()` for an owned dialog | `window.GetOwner()` (`GetParent()` is only the real parent now) |
| `window.GetChildren()` meaning all descendants | `window.GetDescendants()` |
| `new SafeCurrentInputDesktopHandle()` | `ThreadDesktopScope.SwitchToInputDesktop()` / `SafeDesktopHandle.OpenInputDesktop()` |
| `TrySendMessage(h, msg, wp, out r, lp, t)` | `TrySendMessage(h, msg, wp, lp, out r, t)` |
| `AttachThreadInput(a, b, 1)` | `AttachThreadInput(a, b, true)` |
| `MapWindowPoints(a, b, ref pt, 1)` | `MapWindowPoints(a, b, ref pt)` |
| `InteropWindow w = hWnd;` / `IntPtr h = w;` | `InteropWindowFactory.CreateFor(hWnd)` / `w.Handle` |
| `InteropWindowQuery.IgnoreClasses.Add(x)` | `InteropWindowQuery.AddIgnoreClass(x)` |
| `DesktopAccessRight.GENERIC_ALL` (0x1FF) | `DesktopAccessRight.DESKTOP_ALL_SPECIFIC` |
| `IconHelper.GetAppLogo<Bitmap>(window)` | `window.GetAppLogo<Bitmap>()` (package Dapplo.Windows) |
| `long hr = Win32.GetHResult(e)` | `HResult hr = Win32.GetHResult(e)` |
| `ColorSpace.LCS_GM_IMAGES` | `ColorSpaceIntent.LCS_GM_IMAGES` |
| `BitfieldColorMask.Create(255, 255, 255)` | `BitfieldColorMask.Rgb888` |
| `int s = Kernel32Api.GlobalSize(h)` | `ulong s = Kernel32Api.GlobalSize(h).ToUInt64()` |
| `PackageInfo.IsRunningOnUwp` | `PackageInfo.HasPackageIdentity` |
| `Ole32Api.ClassIdFromProgId(p) == Guid.Empty` | `!Ole32Api.ClassIdFromProgId(p).HasValue` |
| `Dapplo.Windows.Kernel32.RestartManager`, `Kernel32.Enums.Rm*`, `Kernel32.Structs.Rm*` | `Dapplo.Windows.InstallerManager.InstallerRestartManager`, `InstallerManager.Enums`, `InstallerManager.Structs` |
| `RmProcessInfo.strAppName` / `Process.dwProcessId` / `bRestartable` | `AppName` / `Process.ProcessId` / `IsRestartable` |
| `session.Shutdown()` (forced) | `session.Shutdown(RmShutdownType.RmForceShutdown)`; the default is now graceful |
| `ExtractIconEx(f, i, out l, out s, 1)` | `var l = new IntPtr[1]; var s = new IntPtr[1]; ExtractIconEx(f, i, l, s, 1)`; count: `CountIcons(f)` |
| `Shell32Api.TaskbarPosition` | `Shell32Api.TryGetTaskbarPosition(out var data)` |
| `Advapi32Api.CurrentSessionId` | `Advapi32Api.CurrentLogonSid` (the session id is `Process.GetCurrentProcess().SessionId`) |
| `IntPtr timer = SystemStateApi.CreateWaitableTimer(...)` | `SafeWaitHandle timer = ...` |
| `WinFrame.IsAvailabe`, `ClientLatency.Avarage` / `Derivation`, `UserInfo.Domainname` / `Username` | `IsAvailable`, `Average` / `Deviation`, `DomainName` / `UserName` |
| `AppBarStates.AllwaysOnTop`, `AppBarMessages.SetAutohideAppBar` | `AlwaysOnTop`, `SetAutoHideAppBar` |
| `DwmWindowAttributes.NcrenderingPolicy`, `TransitionsForcedisabled` | `NcRenderingPolicy`, `TransitionsForceDisabled` |
| `VirtualKeyCode.Snapshot`, `Hangul`, `Kanji` | `PrintScreen`, `Kana`, `Hanja` |
| `MouseButtonStates.Button4Up` / `Button6Up` / `Button1Down` | `ButtonX1Up` / `ButtonX2Up` / `LeftButtonDown` |
| `LowerIntegretyInjected`, `HidUsagesGeneric.Consumer` | `LowerIntegrityInjected`, `HidUsagePages.Consumer` |
| `WM_KEYFIRST`, `WM_MOUSEFIRST` | `WM_KEYDOWN`, `WM_MOUSEMOVE` |
| `ApplicationRestartManager.MaxCommandLineLength` | `RestartMaxCmdLine` |

| `new KeyCombinationHandler(...) { TriggerOnKeyUp = true }` | `{ TriggerMode = TriggerMode.FirstKeyUp }`, or `TriggerMode.AllKeysUp` when the hotkey sends input |
| `window.GetZOrderedChildren(force)` | `window.GetChildren(force)` |
| `InteropWindowRetrieveSettings.ZOrderedChildren` / `CacheAllChildZorder` | `Children` / `CacheAllWithChildren` |
| `new IntPtr(-1)` / `new IntPtr(-2)` for `SetWindowPos` | `WindowHandles.HWND_TOPMOST` / `WindowHandles.HWND_NOTOPMOST` |
| `token.SetAsUnicodeString(x)` without `ClearContents()` | `ClipboardNative.ReplaceContents(new ClipboardContents().AddUnicodeString(x))` |

Behaviour changes to check:

- **Dialogs** must be shown from an STA thread (the UI thread or a `[STAThread]` Main), not from `Task.Run`.
- **`GetTopWindows()`** returns a list taken at call time; call it again for the current state.

- **Owner vs parent:** `IsTopLevel` / `IsPopup` now accept owned windows, and `GetInfo(autoCorrect)` no longer crops owned dialogs.
- **DPI:** `DpiAwareForm` lets WinForms scale on WM_DPICHANGED. Remove manual font or control scaling you did in `FormDpiHandler.OnDpiChanged`, or cancel `Form.DpiChanged`. DPI scaling now rounds (`ScaleWithDpi(3, 144)` is 5).
- **Cursor drawing:** `DrawCursorOnGraphics` / `DrawCursorOnBitmap` take the top-left of the cursor image; subtract `cursor.HotSpot` to draw at the mouse position.
- **Empty rectangles:** rectangles and sizes with a negative width or height count as empty.
- **Keyboard handlers:** use one handler instance per subscription, or the `Where(() => new KeyCombinationHandler(...))` factory overload.
- **Strong names:** the net10 assemblies are now strong-named too.

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
