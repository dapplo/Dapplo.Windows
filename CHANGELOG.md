# Changelog

All notable changes to Dapplo.Windows are documented here. The format follows [Keep a Changelog](https://keepachangelog.com/en/1.1.0/),
and the packages use [Semantic Versioning](https://semver.org/). Finding IDs such as `A-02` refer to
[the September 2026 review](doc/review/REVIEW-2026-09.md).

Version 3.0 fixes a large number of interop bugs and deliberately breaks APIs whose concept was wrong.
Read the [migration guide](doc/articles/migration-3.0.md) before upgrading.

## [Unreleased] - 3.0.0

### Changed
- **Breaking:** `InteropWindowExtensions.Fill()` now respects `ForceUpdate`, `AutoCorrectValues` and `Maximized`. Before, it always re-read every value, always auto-corrected and always queried the maximized state, so caching never worked (A-02).
- **Breaking:** `WindowScroller.ScrollWheelLinesFromRegistry` is replaced by `WindowScroller.ScrollWheelLines`, read with `SystemParametersInfo(SPI_GETWHEELSCROLLLINES)`; the wheel delta is calculated by the new `WindowScroller.CalculateWheelDelta` (A-17).
- **Breaking:** removed `NativeRectExtensions.Intersect2`, which returned wrong results; use `Intersect` (C-05).
- **Breaking:** corrected `ProcessAccessRights.QueryLimitedInformation` (0x1000) and `All` (0x1FFFFF), and added `CreateProcess`, `SetQuota` and `SuspendResume` (D-12).
- **Breaking:** `WinMm.Play(byte[])` copies the wave data to unmanaged memory that stays alive until the next `Play(byte[])` or `StopPlaying()`, and returns `bool` (E-04).
- **Breaking:** `HResult` is a signed `int` enum. `Failed()`, `Succeeded()` and `ThrowOnFailure()` now detect failures; before, every HRESULT counted as success, so failure branches that never ran now run (C-01).
- **Breaking:** `IconInfo` / `IconInfoEx` expose the bitmaps as non-owning `IntPtr` properties (`ColorBitmap`, `BitmaskBitmap`); take ownership once with `TakeBitmaps()` or free them with `DeleteBitmaps()`. Every property read used to create a new owning SafeHandle, so the same HBITMAPs were deleted several times (C-02).
- **Breaking:** `NativeSizeTypeConverter` writes and reads `Width,Height`; it used to swap them (C-06).
- **Breaking:** `IsDockedToLeftOf` / `IsDockedToRightOf` treat flush rectangles (`Right == Left`, exclusive Right) as docked instead of requiring a 1-pixel gap (C-13).
- **Breaking:** `HasOverlap` returns false for rectangles that only touch and true when one contains the other, the same as `IntersectsWith` (C-14).
- **Breaking:** `WindowsVersion` uses `RtlGetVersion`, so it reports the real OS version without an application manifest. `IsWindowsVista` and `IsWindows10` now mean exactly that version; use the `…OrLater` properties (C-17).
- **Breaking:** `NativeSize` / `NativeSizeFloat.CompareTo` order by ascending area; it was reversed (C-23).
- **Breaking:** lossy float-to-int conversions of the `Native*` structs are explicit. Points floor, sizes round up, and rectangles become the containing integer rectangle (C-24).
- **Breaking:** `MonitorFrom` values match winuser.h (`DefaultToNull = 0`, `DefaultToPrimary = 1`, `DefaultToNearest = 2`) and the enum is no longer `[Flags]`; "nearest" lookups used to return NULL (A-01).
- **Breaking:** `SysColorIndexes.Color3Dface` is 15 (A-09).
- **Breaking:** `ScrollBarInfo.ThumbTop` / `ThumbBottom` were swapped; `ThumbSize` (really the arrow-button size) is renamed to `LineButtonSize` (A-10).
- **Breaking:** WPARAM, LPARAM and LRESULT are pointer-sized in every `User32Api.SendMessage` overload; the `int` overload is removed (A-20).
- **Breaking:** `DpiAwarenessContext` is a pointer-sized struct instead of an `int` enum, so the DPI-context APIs work on x64. Compare contexts with `NativeDpiMethods.AreDpiAwarenessContextsEqual` (D-07).
- **Breaking:** `IDispatch` has a correct vtable on every target and `GetTypeInfo` returns `ComTypes.ITypeInfo`; the unusable `IUnknown` interface is removed (D-11).
- **Breaking:** `NativeDpiMethods.GetWindowDpiHostingBehavior` takes the window handle it requires (D-13).
- **Breaking:** `DialogDpiChangeBehaviors.DisableControlRelayout` is 4 (D-14).
- **Breaking:** `NativeDpiMethods.EnableNonClientDpiScaling` returns `bool` instead of `HResult` (D-15).
- With `KeyCombinationHandler.TriggerOnKeyUp = true`, key events are never marked as handled, so `IsPassThrough` has no effect in that mode (B-05).
- Tests that send input, replace the clipboard or write to the registry are tagged `Category=Interactive` and are excluded from default runs (F-06).

### Added
- `tools/build-runner`: a local build and test runner driven by request files.
- `User32Api.GetWheelScrollLines()`, `User32Api.MaxWindowTextLength`, `WindowScroller.MaxMouseWheelSteps`, `WindowScroller.WheelDeltaPerNotch`.
- `MouseInput.NormalizeCoordinate()` and `KeyboardInput.IsExtendedKey()`.
- `DwmApi.DwmSetWindowAttribute` overload taking `ref uint`.
- `NativePointFloatTypeConverter`, `NativeRectFloat.GetContainingIntegerBounds`, `IconInfoEx.IsMonochrome`, `Gdi32Api.GetObject(IntPtr, …)`.
- `NativeDpiMethods.AreDpiAwarenessContextsEqual`, `GetWindowDpiAwarenessContext`, `SetDialogDpiChangeBehavior`, `GetDialogDpiChangeBehavior`, `DpiAwarenessContext.UnawareGdiScaled`.
- `DpiCalculator.ScaleWithDpi` / `UnscaleWithDpi` overloads for `NativePointFloat`.
- `WindowScroller.CreateScrollWParam`.

### Fixed
- Reading a window's text (`GetTextFromWindow`, `GetText()`, `Fill()`) no longer crashes the process with a StackOverflowException when a control holds a very large text; texts over 1M characters are truncated (A-03).
- Getting a window's text or title-bar info no longer blocks forever on a hung application; it times out after 500 ms (A-04).
- `WindowScroller.End()` and `Start()` in MouseWheel mode can no longer loop forever; they stop when the position stops changing or after `MaxMouseWheelSteps`, and only return true when the end or start was reached (A-05).
- A wheel-lines setting of 0 or "one page" no longer causes a DivideByZeroException, a negative or a zero wheel delta (A-17).
- Mouse moves and clicks at a given location land on the right pixel, including on monitors left of or above the primary one (B-03).
- `LastInputTimeSpan`, `LastInputDateTime`, `KeyboardHookEventArgs.EventTime` and `Msg.Time` are correct after 24.9 days of uptime and when the tick count wraps (B-04).
- `KeyCombinationHandler` with `TriggerOnKeyUp = true` fires, and no longer swallows the key-up, which left a stuck key (B-05, F-01).
- Injected arrow, navigation, right Ctrl/Alt, Windows and media keys are sent as extended keys, so they are no longer seen as numpad keys or the left modifier (B-15).
- `KeyboardInputGenerator.KeyCombinationPress` releases keys in reverse order.
- Input generation no longer throws an OverflowException in Debug builds after 24.9 days of uptime (B-28).
- `IconHelper` methods that return an `Icon` return an icon that owns its own copy of the handle, instead of one wrapping a destroyed HICON (C-03).
- `NativePoint.Offset`, `NativePointFloat.Offset` and `NativeRectFloat.Offset` no longer reset X or Y to 0 when that offset is omitted (C-04).
- `HResult.E_ACCESSDENIED` has the correct value `0x80070005` (C-07).
- `NativeIconMethods.GetIconInfoEx(SafeIconHandle, ...)` calls the Unicode export that matches the `IconInfoEx` layout (C-08).
- Loading a cursor from a path with non-ANSI characters works (C-09).
- Setting `BitmapV4Header.GammaGreen` / `BitmapV5Header.GammaGreen` no longer overwrites `GammaRed` (C-18).
- The clipboard no longer stays locked for the rest of the process after an open timeout, a cancellation or an exception in `ClipboardNative.Access()` / `AccessAsync()` (D-01).
- Disposing a clipboard access token more than once no longer closes the clipboard again or breaks the lock (D-26).
- `Kernel32Api.LoadLibrary` works; it passed an ANSI string to `LoadLibraryW` (D-10).
- `DpiApi.GetSystemParametersInfo` / `GetSystemParametersInfoForWindow` work for NONCLIENTMETRICS and ICONMETRICS (D-24).
- `DwmApi.GetWindowCornerPreference` / `SetWindowCornerPreference` no longer throw on Windows 11 (E-05).
- Citrix detection reads the connect state correctly on x64 (E-06).
- `WinMm.PlaySystemSound` plays the requested system sound (E-09).
- `PowerManagementApi.SetSuspendState` reports failure correctly (E-18).
- `ApplicationRestartManager.ListenForEndSession` answers WM_QUERYENDSESSION with TRUE/FALSE; it answered S_OK (0), which blocks shutdown (E-01, part of it: the reply reaches Windows only after the message pipeline fix in pass 3).
- Type converters write with the invariant culture, so negative values round-trip under cultures such as sv-SE; float converters accept exponent notation (C-16).
- Converting a non-normalised rectangle to a WPF `Rect` no longer throws (C-24).
- User32 imports that handle text always call the Unicode (W) entry points; `SendMessage`, `Get/SetWindowLong(Ptr)` and `GetClassLongPtr` used to resolve to the ANSI versions (A-07).
- `SetWindowStyle` / `SetExtendedWindowStyle` no longer throw an OverflowException in 32-bit processes or Debug builds for styles such as `WS_POPUP` (A-08).
- `WindowScroller` scrolls scroll-bar controls (`ScrollBarTypes.Control`) and no longer overflows for positions above 32767 on 32-bit (A-20).
- Creating a `DpiAwareForm` or `DpiHandler`, or calling `AttachDpiHandler`, no longer leaves the UI thread in Per Monitor v2 awareness (D-08).
- `NativeDpiMethods.EnableDpiAware` falls back to Per Monitor v1 and `SetProcessDpiAwareness`, and returns whether the process is actually DPI aware (D-09).
- `DpiHandler.TryEnableNonClientDpiScaling` reports failures and no longer throws on Windows 10 before 1607 (D-15).
- `DpiHandler.ScaleWithCurrentDpi(NativePointFloat)` / `UnscaleWithCurrentDpi(NativePointFloat)` no longer round to integers, and unscaling no longer scales.
