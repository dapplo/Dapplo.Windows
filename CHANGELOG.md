# Changelog

All notable changes to Dapplo.Windows are documented here. The format follows [Keep a Changelog](https://keepachangelog.com/en/1.1.0/),
and the packages use [Semantic Versioning](https://semver.org/). Finding IDs such as `A-02` refer to
[the September 2026 review](doc/review/REVIEW-2026-09.md).

Version 3.0 fixes a large number of interop bugs and deliberately breaks APIs whose concept was wrong.
Read the [migration guide](doc/articles/migration-3.0.md) before upgrading.

## [3.4.0]

Find the scrollable areas inside a window, for a scrolling capture which lets the user pick one.

### Added
- `UiAutomationScroller.FindScrollableAreas(windowHandle or IInteropWindow, horizontal, timeout)`: the screen bounds of the window element and every descendant which can scroll in that direction, in tree order, without hit testing on the screen (works while another window covers it). One `FindAll` with a cache request for `BoundingRectangle` and `IsOffscreen`; empty, offscreen and duplicate rectangles are left out; an empty list, never `null`. The call blocks, run it on a background thread; the UI Automation connection and transaction timeouts are set to `timeout` (default `UiAutomationScroller.DefaultFindTimeout`, 2 seconds, Windows 8 and later).
- Interop: `IUIAutomation.CreateCacheRequest`, `IUIAutomationCacheRequest`, `IUIAutomationElement.FindAllBuildCache` and the cached `BoundingRectangle` / `IsOffscreen`, `IUIAutomationElementArray`, `IUIAutomation2` timeouts.

## [3.3.0]

Everything Greenshot needs for a scrolling capture: scroll part of a page, wheel at the right spot, and scroll windows which draw their own scroll bars.

### Added
- `WindowScroller.StepFraction` (greater than 0, at most 1, default 1.0 = a page as before): `Next()` / `Previous()` scroll part of a page, so the frames of a scrolling capture overlap. `AbsoluteWindowMessage` moves by `max(1, PageSize * fraction)` (clamped), `WindowsMessage` sends `SB_LINEDOWN` / `SB_LINEUP` until the position moved by that many scroll units, checked after every message so windows which scroll in pixels work too (1.0 still sends `SB_PAGEDOWN`), `MouseWheel` scales `WheelDelta` by the fraction rounded to whole notches (`UseFractionalWheelDelta` for exact deltas), `KeyboardPageUpDown` ignores it. Helpers: `ScaleWheelDelta`, `CalculateLineSteps`, `CalculateStepPosition`. `End()` / `Start()` in `MouseWheel` mode still use full pages.
- `WindowScroller.WheelLocation` (default: the middle of `ScrollingWindow` as before) and `RestoreCursorAfterWheel` (default off): the cursor, which the system needs at the wheel location, is moved back in the same `SendInput` call.
- `WindowScroller.ViewportBounds`: the client area of the scrolling window in screen coordinates.
- `IScroller` (`IsAtStart`, `IsAtEnd`, `StepFraction`, `ViewportBounds`, `Start`, `End`, `Next`, `Previous`, `Reset`), implemented by `WindowScroller` and `UiAutomationScroller`, so a caller tries `GetWindowScroller()` first and falls back to UI Automation with one code path.
- New package **Dapplo.Windows.Automation** with `UiAutomationScroller`: scrolls Chromium / Electron, Firefox, WPF, WinUI, Office and the Explorer file list via the UI Automation `ScrollPattern`, using the native UI Automation COM API (no WPF dependency on `net10.0-windows`). `FromPoint(point, horizontal)` walks up from the element under the point to the first scrollable one, `FromWindow(window, horizontal)`; `ViewportBounds`, `ScrollPercent`, `VisibleFraction`, `IsAtStart` / `IsAtEnd` with `PercentTolerance`; `SetScrollPercent` with a fallback to `Scroll` increments; a `MouseWheel` mode for controls which report the pattern but ignore it; `IsAvailable` / `Refresh()` when the element is gone. Use it from a background thread, not from the UI thread which owns the target window.
- `MouseInputGenerator.MoveMouseHorizontalWheel`, `MouseInput.MoveMouseHorizontalWheel` and `MouseInputGenerator.MoveMouseWheelAt(delta, location, restoreCursor, horizontal)`.
- CI reports failed tests as annotations, so they are visible in the pull request.

### Fixed
- `WindowScroller` in `MouseWheel` mode sends horizontal wheel input (`MOUSEEVENTF_HWHEEL`) for a horizontal scroll bar; it sent vertical wheel input.

## [3.2.0]

What Greenshot still had to do itself around the clipboard moves into Dapplo.Windows.Clipboard, so every application gets it right.

### Added
- The name of the application which keeps the clipboard open: `ClipboardAccessDeniedException.BlockingProcessName` and, for a token from `Access()` / `AccessAsync()` which couldn't open the clipboard, `token.GetBlockingProcessName()` (new `ClipboardAccessTokenExtensions`). It's the file name of the executable (e.g. `notepad.exe`, via QueryFullProcessImageName, so also for elevated processes), else the process name, else the window title; `null` when unknown. It's determined once when opening fails, together with the exception message. For your own `IClipboardAccessToken` implementations the extension determines it from `BlockingProcessId` / `BlockingWindow`, the interface is unchanged.
- A size limit for DIB decoding: `DibImage.TryDecode(dib, maxPixelCount, out image)` and `TryGetAsDib(maxPixelCount, out image)`. The pixel count (width * |height|) is checked from the header before anything is allocated, overflow-safe for every header value; the default is `DibImage.DefaultMaxPixelCount`, 64 megapixels (256 MiB of BGRA32 pixels).
- `DibImage.TryDecode` also reads DIBs with a BITMAPCOREHEADER (16-bit width and height, RGBTRIPLE palette, 1, 4, 8 and 24 bpp).
- Text from sources without synthesized formats: for CF_UNICODETEXT, `GetAsUnicodeString()` / `TryGetAsUnicodeString()` fall back to CF_TEXT, decoded with the code page of CF_LOCALE (else the ANSI code page), and then to CF_OEMTEXT with the OEM code page, up to the first NUL, e.g. for a `DataObjectReader` of a drop which only has CF_TEXT. The open clipboard, `ClipboardSnapshot` and `ClipboardNative.GetOleDataObject()` behave as before.
- Virtual files from the clipboard in one call: `ClipboardSnapshot.TryUseVirtualFiles(use, out result, maxDataSize)` takes the OLE data object only when the snapshot has a file group descriptor, the thread is STA and the clipboard didn't change since the snapshot, lets `use` read the files and releases the data object again (the files can't be read after `use` returned). `HasVirtualFiles()` for every `IClipboardDataSource`, `ClipboardNative.HasVirtualFiles()` without opening the clipboard, and `DataObjectReader.DefaultMaxDataSize`.
- `ClipboardNative.AvailableFormats(preferred, max)`: the first `max` formats of `preferred` which are available, in that order, without opening the clipboard, so a snapshot only reads (and makes the source render) what is needed.

### Fixed
- `SharedMessageWindow` crashed the process when its non-default AppDomain was unloaded (.NET Framework, e.g. vstest / xunit test hosts, 0xE0434352 or 0xC000041D): only `ProcessExit` was handled, so the CLR aborted the window thread in `GetMessage` and the `ThreadAbortException` escaped through the window procedure, a user32 callback. The window is now also shut down on `AppDomain.DomainUnload` (only subscribed in a non-default AppDomain), with `ProcessExitShutdownTimeout`; `IsProcessExiting` is `true` from then on and the window is not created again. Nothing changes on .NET (Core).
- On .NET Framework the window procedure runs in a finally block, so a thread abort which still arrives (the shutdown timed out) is raised in the message loop, not inside the user32 callback, and the loop ends; after a timed out shutdown on DomainUnload `WM_CLOSE` is posted.

### Changed
- **Behaviour change:** `DibImage.TryDecode(dib, out image)` and `TryGetAsDib(out image)` return `false` for bitmaps with more than 64 megapixels; the internal limit was 256 megapixels. Pass a larger `maxPixelCount` to decode them.
- The message of `ClipboardAccessDeniedException` names the executable of the blocking application (e.g. `notepad.exe` instead of `notepad`), also when it's thrown by `ThrowWhenNoAccess()` of a token, which only named the window and process ID before.

## [3.1.0]

The clipboard package becomes a complete, async-friendly replacement for WinForms/OLE clipboard code, without System.Drawing, WinForms or WPF in its API.

### Added
- `ClipboardNative.UseAsync(work)` and `UseAsync<T>(work)` with `ClipboardAccessOptions` (owner, retries, retry interval, lock timeout): wait asynchronously until the clipboard can be opened, then open it, run the work and close it again synchronously on one thread, so an opened clipboard can never end up on another thread. The work runs on the context of the caller; an async lambda is a compile error and work returning a `Task` throws.
- `ClipboardNative.ReadSnapshotAsync(formats, [maxBytesPerFormat])` and `clipboard.ReadSnapshot(...)`: copy clipboard formats into a `ClipboardSnapshot` in one short session, so decoding or uploading never happens while the clipboard is open. `null` reads every global-memory format (handle formats like `CF_BITMAP`, `CF_ENHMETAFILE`, `CF_PALETTE` are skipped); `SkippedFormats`, `SequenceNumber`, `OwnerHandle`, `GetSize` and `ToContents()` (restore the clipboard).
- `IClipboardDataSource` (`Formats`, `HasFormat`, `TryGetStream`), implemented by `ClipboardSnapshot` and by `clipboard.AsDataSource()` for the open clipboard, with the extensions `GetAsUnicodeString`, `TryGetAsUtf8String`, `GetAsBytes` / `TryGetAsBytes` and `GetFileNames` (Unicode and ANSI `DROPFILES`), so one piece of code reads every source.
- Who blocks the clipboard: `ClipboardNative.OpenClipboardWindow` (`GetOpenClipboardWindow`), and `BlockingWindow` / `BlockingProcessId` on the access token and on `ClipboardAccessDeniedException` when opening failed. The exception also has `IsOpenTimeout` / `IsLockTimeout`, and its message names the blocking process.
- CF_HTML: `ClipboardHtml.Create` / `TryParse`, `AddHtml` / `SetAsHtml` and `TryGetAsHtml` (any `IClipboardDataSource`) with correct UTF-8 byte offsets, `SourceURL`, `Fragment` and `FullHtml`. Offsets which don't match the `StartFragment` / `EndFragment` comments (producers which count characters) fall back to the comments; version 1.0 without context (`StartHTML:-1`) is supported.
- CF_DIB / CF_DIBV5 without System.Drawing: `DibImage` (top-down BGRA32, straight alpha, `HasAlpha`), `DibImage.TryDecode` / `CreateDibV5` / `CreateDib`, `AddDib` / `SetAsDib` (`DibFormats`) and `TryGetAsDib`. Reads BITMAPINFOHEADER / V4 / V5, BI_RGB 1-32 bpp and BI_BITFIELDS 16/32 bpp, bottom-up and top-down, and masks repeated after a V5 header (Greenshot's byte-reversed ones included).
- `TryGetEnhancedMetafileBits`: CF_ENHMETAFILE as EMF bytes.
- `SetDelayedRenderedContent(format, Func<Stream>)`: delayed rendering for the current content without a registration, dropped on `WM_DESTROYCLIPBOARD`, rendered at exit with `WM_RENDERALLFORMATS`.
- `DataObjectReader` for OLE data objects (`System.Runtime.InteropServices.ComTypes.IDataObject`, e.g. from drag and drop): `TryGetStream(format, index, out stream)` with `lindex` support for `TYMED_HGLOBAL` and `TYMED_ISTREAM`, and `GetVirtualFiles()` which parses `FileGroupDescriptorW` (and the ANSI `FileGroupDescriptor`) into `VirtualFile` (name, size, attributes, times, lazy `OpenContent()` from `FileContents`). It's an `IClipboardDataSource`, so all read helpers work on it. Hardened against hostile sources: `MaxDataSize` (default 512 MiB) for HGLOBAL and IStream data, streams which fail or report impossible read counts return `false`, format enumeration is bounded, and `VirtualFile.SafeFileName` strips paths, invalid characters and device names.
- `ClipboardNative.GetOleDataObject()` (OleGetClipboard): needs an STA thread with OLE initialized, retries while another application has the clipboard open and then throws a `ClipboardAccessDeniedException` naming the blocking window.

### Changed
- **Behaviour change:** using a clipboard access token on another thread than the one which opened the clipboard throws an `InvalidOperationException` (was `ClipboardAccessDeniedException`), and disposing it there throws an `InvalidOperationException` instead of failing to close the clipboard silently, which left the clipboard open for every application. The token stays valid and can still be disposed on the owner thread.
- The clipboard documentation states the threading rules: any thread, no STA; never await while the clipboard is open; prefer `UseAsync`.
- New tests cover `AccessAsync` when it has to retry, with and without a `SynchronizationContext`: the token is usable after the await and disposing it closes the clipboard. `AccessAsync` stays supported.
- **Breaking** for your own implementations of `IClipboardAccessToken` (the library's tokens are internal): the interface has the new `BlockingWindow` and `BlockingProcessId` properties.

## [3.0.3]

### Fixed
- Enlarged colour cursors with an alpha channel ("Make mouse pointer bigger") are rendered by `DrawIconEx` at the requested size instead of scaling up the 32x32 pixels, which looked blurry.

### Changed
- Every merge to `master` is released after approval of the `NuGet` environment; no tags need to be created by hand.

## [3.0.0] - 2026-09-30

### Changed
- **Breaking:** the Windows message types are unambiguous: `WindowMessage` lives in `Dapplo.Windows.Messages` and is also what the Forms/WPF `WinProcMessages()` and `DpiHandler` use (`WindowMessageInfo` is gone); `WindowsMessage` is now `RegisteredWindowMessages` (`Register`, `GetName`); `Dapplo.Windows.Messages.Enumerations` is now `.Enums`; `Msg.wParam` / `lParam` are `WParam` / `LParam`.
- **Breaking:** the Forms `WinProcFormsMessages()` is renamed to `WinProcMessages()`, the same name as in WPF.
- **Breaking:** `IsTopLevel` / `GetTopLevelWindows` / `IsPopup` are renamed to `IsVisibleApplicationWindow` / `GetVisibleApplicationWindows` / `IsVisiblePopup`, after what they check (no parent, visible, has a title, not a tool window, not minimized, …); owned windows are accepted.
- **Breaking:** consistent WinMm names: `PlayResource`, `PlayFile`, `PlayWave(byte[])`, `PlayWave(IntPtr, SoundSettings)` and `PlaySystemSound` all return `bool`; `PlayResource` no longer plays the default sound for a missing resource.
- **Breaking:** `RmShutdownType` is a `[Flags]` enum with `Graceful`, `Force` and `OnlyRegistered`.
- **Breaking:** `DrawIconExFlags` moved to `Dapplo.Windows.Icons.Enums`; misspellings fixed: `GdiExtensions.AreRectangleCornersVisible`, `CieXyzTriple`; `SystemStateApi.CloseHandle` is removed.
- **Breaking:** `KeyCombinationHandler.TriggerOnKeyUp` is replaced by `TriggerMode` (`KeyDown`, `FirstKeyUp`, `AllKeysUp`).
- **Breaking:** `InteropWindowQuery.GetTopWindows()` / `GetTopWindows(parent)` take a snapshot with `EnumWindows` / `EnumChildWindows` and return `IReadOnlyList<IInteropWindow>`, so they can no longer skip, repeat or loop over windows when the Z-order changes during the walk. `GetVisibleApplicationWindows()` takes its snapshot when called.
- **Breaking:** `GetChildren()` returns the direct children in Z-order; `GetZOrderedChildren()`, `HasZOrderedChildren`, `InteropWindowRetrieveSettings.ZOrderedChildren` and `CacheAllChildZorder` are removed.
- **Breaking:** the clipboard `Set*` methods throw `InvalidOperationException` when the content belongs to another window (you forgot `ClearContents`), instead of mixing your formats into another application's content.
- **Breaking:** once the process is exiting and the shared window is gone, using `SharedMessageWindow` throws `ObjectDisposedException`.
- **Breaking:** the file and folder dialogs throw `InvalidOperationException` on a non-STA thread instead of failing inside COM.
- `KeyCombinationHandler` and `KeySequenceHandler` ignore VK_PACKET events, so typed text never interrupts a combination or sequence.
- **Breaking:** targets are `net480` and `net10.0-windows` only; `netstandard2.0` and `net8.0-windows` are dropped (F-11).
- **Breaking:** the core packages no longer depend on WinForms or WPF. WinForms helpers moved to the new **Dapplo.Windows.Forms** package, WPF helpers to the new **Dapplo.Windows.Wpf** package, under the `Dapplo.Windows.Forms.*` / `Dapplo.Windows.Wpf.*` namespaces (F-05, F-12).
- **Breaking:** conversions between the native structs and WPF types are extension methods (`ToRect()`, `ToNativeRect()`, `ToSize()`, `ToInt32Rect()`, …) in Dapplo.Windows.Wpf.
- **Breaking:** `WinProcListener` (now in Dapplo.Windows.Forms) takes a `WinProcHook` delegate instead of the WPF `HwndSourceHook`.
- **Breaking:** `InteropWindowExtensions.PrintWindow()` returns a `Bitmap`; `PrintWindowAsBitmapSource()` is in Dapplo.Windows.Wpf.
- **Breaking:** icon helpers support `Icon` and `Bitmap` only and throw `NotSupportedException` for other types instead of returning null; use `ToBitmapSource()` from Dapplo.Windows.Wpf.
- **Breaking:** `DwmApi.ColorizationColor` returns a `System.Drawing.Color` (the WPF `Color` version and the duplicate `ColorizationDrawingColor` / `ColorizationSystemDrawingColor` are gone); convert with `ToMediaColor()` from Dapplo.Windows.Wpf. It returns white instead of throwing when the registry value isn't a DWORD.
- **Breaking:** `BitmapScaleHandler.AddTarget(Button / ToolStripItem)` moved to Dapplo.Windows.Forms extension methods; the core has `AddTargetAction`.
- **Breaking:** `Parent` / `HasParent` / `GetParent()` mean the real parent (child windows only, `GetAncestor(GA_PARENT)`), never the owner; new `Owner` / `HasOwner` / `GetOwner()` and `InteropWindowRetrieveSettings.Owner`. `GetInfo(autoCorrect)` no longer crops owned dialogs to their owner (A-06).
- **Breaking:** `GetChildren()` returns direct children only; new `GetDescendants()` returns all descendants (A-14).
- **Breaking:** `SafeCurrentInputDesktopHandle` is replaced by `SafeDesktopHandle.OpenInputDesktop()` and `ThreadDesktopScope.SwitchToInputDesktop()` (A-18).
- **Breaking:** `TrySendMessage(hWnd, msg, wParam, lParam, out result, timeout)`; `AttachThreadInput` takes and returns `bool`; `MapWindowPoints` has `ref NativePoint`, `ref NativeRect` and `NativePoint[]` overloads; `FillRect` returns `bool`; `DisplayInfo.Index` is `int` (A-22, A-36, A-41, A-48).
- **Breaking:** `DesktopAccessRight.GENERIC_ALL` is the real 0x10000000 (the old value is `DESKTOP_ALL_SPECIFIC`); `ObjectStates.STATE_SYSTEM_VALID` is 0x7FFFFFFF (A-30, A-38).
- **Breaking:** `InteropWindow` conversions to and from `IntPtr` are explicit and `Equals` is symmetric; `InteropWindowQuery.IgnoreClasses` is read-only (use `AddIgnoreClass` / `RemoveIgnoreClass`); `WinEventInfo` is a readonly struct with 32-bit fields; `WndClassEx.LpfnWndProc` is a function pointer; the sync window enumerators return `IReadOnlyList` (A-39, A-42, A-43, A-46, A-47).
- **Breaking:** the `IInteropWindow` icon extensions (`GetIcon`, `GetIconFromWindow`, `GetAppLogo`) moved from Dapplo.Windows.Icons to the Dapplo.Windows package (same namespace); `IconHelper.GetAppLogo<T>` takes the executable path (F-28).
- **Breaking:** `Win32.GetHResult` returns `HResult` and maps success to `S_OK`; `ColorSpace` profile values are corrected and the rendering intent is the new `ColorSpaceIntent` enum; `BitfieldColorMask` has the correct layout (`Rgb888`, `Rgb565`, `Rgb555` presets) (C-29, C-30, C-31).
- **Breaking:** `IsEmpty` of the rectangle and size structs is true for zero or negative width or height, like Win32 `IsRectEmpty` (C-35).
- **Breaking:** `Bgra32.AlphaBlend` uses Porter-Duff "over" for non-opaque targets; new `AlphaBlendPremultiplied`, `ToPremultiplied`, `ToStraight` (C-12).
- **Breaking:** `DpiAwareForm` no longer swallows WM_DPICHANGED: WinForms' own Per Monitor v2 scaling and `Form.DpiChanged` run; without it only the window bounds follow the suggested rectangle (D-21). The WPF `AttachDpiHandler` no longer double-scales (D-22).
- **Breaking:** DPI scaling rounds instead of truncating; `DpiHandler.Dpi` is 96 until known, see `IsDpiKnown` (D-32).
- **Breaking:** `Kernel32Api.GlobalSize` returns `UIntPtr`; `PackageInfo.IsRunningOnUwp` is renamed to `HasPackageIdentity`; `Ole32Api.ClassIdFromProgId` returns `Guid?`; `IOleCommandTarget` takes `IntPtr pguidCmdGroup`; `ComWrapper` no longer force-releases the RCW (D-28, D-30, D-36, D-37, D-38).
- **Breaking:** `MonitorDpiType` and `WindowsProductTypes` are no longer `[Flags]` (D-39).
- **Breaking:** clipboard `GetAsStream` / `TryGetAsStream` return a copy of the data that stays valid after the token is disposed (D-34).
- **Breaking:** the Restart Manager types moved from Kernel32 to Dapplo.Windows.InstallerManager (`RestartManagerApi`, `Enums`, `Structs`), with readable field names (`AppName`, `ProcessId`, `IsRestartable`, …); the duplicate Kernel32 `RestartManager` is removed (F-38).
- **Breaking:** `InstallerRestartManager.Shutdown()` defaults to the new `RmShutdownType.Graceful`; forcing is opt-in (E-16).
- **Breaking:** `Shell32Api.ExtractIconEx` takes `IntPtr[]` arrays (new `CountIcons`); `AppBarData.lParam` is pointer-sized; `Shell32Api.TaskbarPosition` is replaced by `TryGetTaskbarPosition` (new `GetTaskbarState`) (E-19, E-26).
- **Breaking:** Citrix `EventMask` is a 32-bit enum, `SessionTime` exposes UTC `DateTime?` (E-15, E-23); `Advapi32Api.CurrentSessionId` is renamed to `CurrentLogonSid` (E-27); `WaitableTimer` APIs use `SafeWaitHandle` (E-28).
- **Breaking:** fixed typos in public names: `WinFrame.IsAvailabe` → `IsAvailable`, `ClientLatency.Avarage` → `Average`, `Derivation` → `Deviation`, `UserInfo.Domainname` / `Username` → `DomainName` / `UserName`, `AppBarStates.AllwaysOnTop` → `AlwaysOnTop`, `AppBarMessages.SetAutohideAppBar` → `SetAutoHideAppBar`, `DwmWindowAttributes.NcrenderingPolicy` → `NcRenderingPolicy`, `TransitionsForcedisabled` → `TransitionsForceDisabled` (E-33, F-35).
- **Breaking:** Input and Messages enums lost their duplicate-value aliases and misspellings: `VirtualKeyCode.Snapshot` → `PrintScreen`, `Hangul` → `Kana`, `Kanji` → `Hanja`; `MouseButtonStates.Button*` aliases removed, `Buttonx1Up` → `ButtonX1Up`; `LowerIntegretyInjected` → `LowerIntegrityInjected`; `WM_KEYFIRST` / `WM_KEYLAST` / `WM_MOUSEFIRST` removed (B-32).
- **Breaking:** `KeyboardHookExtensions.Where(handler)` rejects a handler already used by another active subscription; use the new `Where(() => handler)` factory overload for observables subscribed more than once (B-33).
- **Breaking:** all assemblies are strong-named for every target framework and configuration (F-26).
- **Breaking:** removed `ApplicationRestartManager.MaxCommandLineLength` (use `RestartMaxCmdLine`), `PowerBroadcastEvent` values that can't arrive, and undocumented DWM ordinals (`DwmpStartOrStopFlip3D`, `GetSharedSurface`, `UpdateWindowShared`) (E-31, E-35, E-36).
- Debug builds no longer enable checked arithmetic, so Debug and Release behave the same (F-25, F-37).
- The documentation, README and wiki use the real API, list every package with its dependencies, and every code sample is compiled in `Dapplo.Windows.Example.DocSamples`; the package project URL points to the documentation site (F-03, F-17, B-29, D-42, F-33).
- Every package contains a README; builds are deterministic and use `ContinuousIntegrationBuild` on CI; unnecessary dependencies (Dapplo.Log in Advapi32, Microsoft.SourceLink.GitHub) are gone and no empty `.dll.config` is shipped (F-27, F-29, F-30).
- `DpiHandler.HandleWindowMessages`, `HandleContextMenuMessages` and `MessageHandler` are public, so other UI frameworks can feed messages to a `DpiHandler`.
- `User32Api.GetCursorLocation()` falls back to `GetCursorPos` instead of WinForms `Cursor.Position`.
- Packages are published to NuGet only from version tags (`v*`), each with a GitHub Release built from this changelog; other builds are prereleases. The API reference is generated again with docfx as a dotnet tool, and the wiki mirror removes deleted pages (F-02, F-07, F-16, F-19).
- The tests also run on .NET Framework 4.8, and CI runs only the non-interactive tests (F-10). `global.json` is in the repository root.
- **Breaking:** `InteropWindowExtensions.Fill()` now respects `ForceUpdate`, `AutoCorrectValues` and `Maximized`. Before, it always re-read every value, always auto-corrected and always queried the maximized state, so caching never worked (A-02).
- **Breaking:** `WindowScroller.ScrollWheelLinesFromRegistry` is replaced by `WindowScroller.ScrollWheelLines`, read with `SystemParametersInfo(SPI_GETWHEELSCROLLLINES)`; the wheel delta is calculated by the new `WindowScroller.CalculateWheelDelta` (A-17).
- **Breaking:** removed `NativeRectExtensions.Intersect2`, which returned wrong results; use `Intersect` (C-05).
- **Breaking:** corrected `ProcessAccessRights.QueryLimitedInformation` (0x1000) and `All` (0x1FFFFF), and added `CreateProcess`, `SetQuota` and `SuspendResume` (D-12).
- **Breaking:** `WinMm.PlayWave(byte[])` (was `Play(byte[])`) copies the wave data to unmanaged memory that stays alive until the next `PlayWave` or `StopPlaying()`, and returns `bool` (E-04).
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
- **Breaking:** `WindowMessage` is a sealed class, so `Handled` and `Result` set by a subscriber reach Windows. `Result` is `nint` (signed LRESULT) and `WndProc` returns `nint` (B-01).
- **Breaking:** `SharedMessageWindow` is created on first use and lives for the whole process. `Handle` blocks until the window exists and never returns 0; new `Invoke`, `IsWindowThread` and `SubscriberErrors`. `Listen(onSetup, onTeardown)` runs both callbacks on the window thread (B-02, B-16, B-17, D-02).
- **Breaking:** `EndSessionMessage` is a class in `Dapplo.Windows.AppRestartManager` that answers WM_QUERYENDSESSION through `CanEndSession` / `Veto(reason)`. `ListenForEndSession()` takes no callbacks and works for every subscriber (E-01, E-02, E-03).
- **Breaking:** `ApplicationRestartManager.WasRestartRequested(string restartArgument)` checks the argument you registered (E-17).
- **Breaking:** `KeyboardHook` and `MouseHook` are static classes whose hooks run on their own message-loop thread; subscribers are called on that thread. New `SubscriberErrors`, `KeyboardEventsNonBlocking` and `MouseEventsNonBlocking` (B-06, B-09).
- **Breaking:** new pseudo key `VirtualKeyCode.Win` matches either Windows key in key combinations; `"win"` parses to it; `KeyboardHookEventArgs.IsModifier` is computed from `Key` (B-22).
- **Breaking:** `RawHID` is the 8-byte header only; HID reports are in `RawInputEventArgs.HidData`, read with `RawInputApi.TryGetRawInputData` (B-08).
- **Breaking:** clipboard delayed rendering uses `ClipboardNative.RegisterDelayedRenderer(format, renderer)`; `OnRenderFormat` is removed. WM_RENDERFORMAT is answered on the window thread without opening the clipboard, WM_RENDERALLFORMATS checks ownership first (D-06).
- **Breaking:** `ClipboardNative.OnUpdate` no longer opens the clipboard; `ClipboardUpdateInformation.Create()` has no hWnd parameter and every subscriber gets the current state first (D-04).
- **Breaking:** `AccessAsync()` opens the clipboard on the thread that continues after the `await`; a token used on another thread reports no access. Cancelling throws `OperationCanceledException` (D-03).
- **Breaking:** `SetCloudClipboardOptions(bool? canIncludeInHistory, bool? canUploadToCloud, bool excludeFromMonitoring)` places only the options you pass; `SetExcludeClipboardContentFromMonitorProcessing` is replaced by `ExcludeFromMonitorProcessing()` (D-05).
- **Breaking:** `SystemStateApi.PreventSleep()` / `PreventSystemSleep()` return a disposable `SleepBlocker` (a power request that works across threads); `AllowSleep()` is removed (E-13).
- `WinEventHook`, raw input and session-notification registrations happen on the SharedMessageWindow thread, so events arrive there (A-12, B-02, B-11).
- `RawInputMonitor` and `RawInputDeviceMonitor` are available for netstandard2.0 too (B-31).
- In the key-up trigger modes (`TriggerMode.FirstKeyUp` / `AllKeysUp`), key events are never marked as handled, so `IsPassThrough` has no effect there (B-05).
- Tests that send input, replace the clipboard or write to the registry are tagged `Category=Interactive` and are excluded from default runs (F-06).

### Added
- `WinMm.PlayFile(string path)` plays a WAV file asynchronously and returns false when the file doesn't exist.
- `KeyboardInputGenerator.TypeText(string)` types text as Unicode characters (KEYEVENTF_UNICODE), whatever the keyboard layout; line breaks become Enter, `\t` becomes Tab, other control characters are skipped. `KeyboardInput.ForText`, `ForUnicodeKeyDown` and `ForUnicodeKeyUp` build the input.
- `KeyboardState` with `IsDown`, `IsAnyDown`, `IsDownForCurrentThread` and `IsToggled`.
- `TriggerMode.AllKeysUp`: a `KeyCombinationHandler` can fire once all keys of the combination are released; another key pressed in between cancels it.
- `KeyboardHookEventArgs.IsPacket`, `PacketCharacter` and `Packet(char, bool)` for VK_PACKET (Unicode) events.
- `ClipboardContents` builder with `ClipboardNative.ReplaceContents` / `ReplaceContentsAsync`, `IClipboardAccessToken.ReplaceContents` (always clears first, all or nothing) and `AddToCurrentContents` (only while we own the content).
- `SharedMessageWindow.Shutdown(timeout)`, `IsProcessExiting` and `ProcessExitShutdownTimeout`; the window is destroyed automatically on process exit.
- `WindowHandles` with `HWND_TOP`, `HWND_BOTTOM`, `HWND_TOPMOST`, `HWND_NOTOPMOST`, `HWND_MESSAGE` and `HWND_BROADCAST`.
- `User32Api.PostMessage` / `PostThreadMessage` and `IInteropWindow.PostMessage(...)`.
- `tools/build-runner`: a local build and test runner driven by request files.
- `User32Api.GetWheelScrollLines()`, `User32Api.MaxWindowTextLength`, `WindowScroller.MaxMouseWheelSteps`, `WindowScroller.WheelDeltaPerNotch`.
- `MouseInput.NormalizeCoordinate()` and `KeyboardInput.IsExtendedKey()`.
- `DwmApi.DwmSetWindowAttribute` overload taking `ref uint`.
- `NativePointFloatTypeConverter`, `NativeRectFloat.GetContainingIntegerBounds`, `IconInfoEx.IsMonochrome`, `Gdi32Api.GetObject(IntPtr, …)`.
- `NativeDpiMethods.AreDpiAwarenessContextsEqual`, `GetWindowDpiAwarenessContext`, `SetDialogDpiChangeBehavior`, `GetDialogDpiChangeBehavior`, `DpiAwarenessContext.UnawareGdiScaled`.
- `DpiCalculator.ScaleWithDpi` / `UnscaleWithDpi` overloads for `NativePointFloat`.
- `WindowScroller.CreateScrollWParam`.
- `ClipboardNative.RegisterDelayedRenderer`, `HasFormat(StandardClipboardFormats)`.
- `RawInputApi.TryGetRawInputData`, `TryParseRawInput`, `GetRegisteredDevices`; `KeyCombinationHandler.KeyStateVerifier`.
- Keyboard hook args: `ScanCode`, `IsExtended`, `IsFromKeyboardHook`; mouse hook args: `MouseData`, `WheelDelta`, `XButton`, `Flags`, `IsInjectedByProcess`, `IsInjectedByLowerIntegrityLevelProcess`, `TimeStamp`.
- `WindowsSessionListener.IsRegistered` and `RegistrationFailed`; `DevBroadcastDeviceInterface.TryGetDevBroadcastPort` / `TryGetDevBroadcastHandle`; `PowerManagementApi.EnableShutdownPrivilege()`.

### Fixed
- CI collects code coverage only on .NET 10 and runs the .NET Framework 4.8 tests without coverlet, whose version 8 injects a `System.Runtime 8.0` reference that breaks every instrumented call on .NET Framework.
- `KeyHelper.VirtualKeyCodeFromString` still parses the removed alias names `Snapshot`, `Hangul`, `Hangeul` and `Kanji`, so hotkeys stored by 2.x keep working.
- Delayed-rendered clipboard formats survive process exit: the shared window is destroyed on exit, so Windows sends WM_RENDERALLFORMATS.
- The children of the desktop window no longer get the desktop as parent, so `IsVisibleApplicationWindow()` is correct for them.
- Window information: `GetCaption` works for the calling thread's own windows without deadlocks (A-11); `GetTopWindows` no longer yields handle 0 (A-13); `GetInfo` no longer overwrites `Children` as a side effect (A-15); `GetText` no longer truncates at 259 characters (A-21); `GetAppLauncher`, `AppVisible` and `IsLauncherVisible` give correct answers (A-23..A-25); `MonitorInfoEx.DeviceName` stays in its buffer (A-26); `GetVisibleLocation` returns the working-area origin (A-32); `InstalledSoftware()` reads both registry views and HKCU (A-33); `GetInfo` doesn't cache failures (A-34); `ToForegroundAsync` really waits for the restore (A-22); WPF `GetHandle()` works before the window is shown (A-45).
- `PrintWindow` crops to the visible bounds instead of shifting the capture and uses `PW_RENDERFULLCONTENT` on Windows 8.1+, so DirectComposition, Chromium and UWP content is no longer black (A-16, A-27).
- `ToBitmapSource` keeps the alpha channel (A-28, C-25).
- Cursor capture: `TryGetCurrentCursor` returns a `Size` / `HotSpot` that match the captured layers at every DPI and pointer size; drawing respects the Graphics transform, handles non-square and premultiplied cursors, and no longer leaks handles on errors (C-10, C-11, C-12, C-34).
- `SafeWindowDcHandle.FromWindowClientArea` returns a client-area DC (C-15); GDI SafeHandles no longer touch finalized objects (C-26).
- DIB header factories compute correct image sizes, pixel offsets and file sizes (C-19); the ICO/CUR writer scales images above 256 pixels instead of truncating them (C-20); `ExtractVistaIcon` returns a Bitmap that outlives the stream (C-21); the app logo lookup finds scaled logos (C-33); `GetIcon` no longer leaks `Process` objects (C-36).
- The GDI+ blur works again on .NET 10 (C-22).
- `NativeRect.Union(NativeRect.Empty)` no longer includes (0,0) (C-27); `Transform(Matrix)` uses all four corners, so rotation and mirroring work (C-28); float struct equality is consistent with `GetHashCode` and reflexive for NaN (C-32).
- `DpiHandler` keeps working after a WinForms handle recreate (D-20); `BitmapScaleHandler` no longer leaks bitmaps or touches UI from its finalizer (D-23).
- `GetProcessPath` returns correct paths for more processes and long paths (D-25); `PreventDllHijacking` handles non-ANSI paths and reports failures (D-29); `PackageInfo.CurrentPackageFullName` is correct (D-30); Restart Manager callbacks no longer unbalance the stack on x86 (D-31).
- The EmbeddedBrowser registers the real executable name and keeps page scripts running after a script error (E-30).
- File open, save and folder dialogs keep the default options and no longer change the current directory (E-08).
- `DwmApi.IsDwmEnabled` is true on every Windows 8+; `IsWindowCloaked` reads a DWORD; added `DwmWindowAttributes.SystemBackdropType` (E-20, E-21).
- Citrix `ClientDisplay.ColorDepth` maps high values correctly (E-22).
- `InstallerRestartManager` retries when the process list grows, ends leaked sessions and keeps callbacks alive (E-29).
- `WinMm.PlayResource` plays WAVE resources from the given module (E-34); `SetLastError` usage matches the native APIs (A-40, E-32).
- The InstallerExample finds the FormsExample for any build configuration; the WpfExample only swallows PrintScreen while active (F-34).
- `dotnet pack` of a single project no longer fails on the missing icon; package release notes point to this changelog (F-09).
- Benchmarks target the runtimes the project builds for (F-04); FormsExample embeds its app.manifest, so the DPI demos run Per Monitor v2 (F-18).
- Reading a window's text (`GetTextFromWindow`, `GetText()`, `Fill()`) no longer crashes the process with a StackOverflowException when a control holds a very large text; texts over 1M characters are truncated (A-03).
- Getting a window's text or title-bar info no longer blocks forever on a hung application; it times out after 500 ms (A-04).
- `WindowScroller.End()` and `Start()` in MouseWheel mode can no longer loop forever; they stop when the position stops changing or after `MaxMouseWheelSteps`, and only return true when the end or start was reached (A-05).
- A wheel-lines setting of 0 or "one page" no longer causes a DivideByZeroException, a negative or a zero wheel delta (A-17).
- Mouse moves and clicks at a given location land on the right pixel, including on monitors left of or above the primary one (B-03).
- `LastInputTimeSpan`, `LastInputDateTime`, `KeyboardHookEventArgs.EventTime` and `Msg.Time` are correct after 24.9 days of uptime and when the tick count wraps (B-04).
- A `KeyCombinationHandler` that triggers on key-up fires, and no longer swallows the key-up, which left a stuck key (B-05, F-01).
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
- An exception in a SharedMessageWindow, keyboard-hook or mouse-hook subscriber no longer crashes the process; it is published on `SubscriberErrors` (B-06).
- Disposing a `Window.WinProcMessages()` subscription no longer destroys the WPF window (B-07).
- `WinProcListener` returns the handling hook's result to Windows; the Forms `WinProcMessages()` survives handle recreation and completes when the control is disposed (B-10, B-18).
- `WinProcHandler` no longer recreates its window during disposal (B-25); `MessageLoop.ProcessMessages` throws on a `GetMessage` error (B-26).
- Raw-input monitors register when you subscribe and unregister when you dispose, and no longer overwrite each other's registrations (B-02, B-13); `RawInputDeviceMonitor` no longer throws for unknown devices (B-12).
- F10 and the Alt key-up no longer report a phantom Alt (B-14); lock-key state is not flipped by auto-repeat (B-23).
- `KeyHelper` display names are correct for extended keys and no longer throw for numpad * and / (B-19).
- A `KeySequenceHandler` resets after a wrong combination regardless of the order in which its keys are released; before, releasing the modifier last left the sequence stuck on the failed stage.
- Key combinations no longer stay blocked after a missed key-up (B-20); after a `KeySequenceHandler` timeout the first press restarts the sequence (B-21).
- The clipboard is no longer left with partial data or leaked memory after a failed write (D-16); format-name caches are thread-safe and case-insensitive (D-17); `GetAsUnicodeString` has no trailing garbage (D-18); `GetFileNames` supports long paths (D-19); read errors are reported correctly (D-27); `SetAsStream` honours `size` (D-35).
- `WindowsSessionListener` no longer crashes when `WTSRegisterSessionNotification` fails early at logon; it retries (B-11).
- Device notification events can be used after `ObserveOn` and have full-length names (E-07, E-25); `DisplayName` is fixed (E-10); `DeviceInterfaceClass.Keyboard` / `BluetoothLeDevice` GUIDs are correct (E-11); `DevBroadcastHandle` has the native layout (E-24).
- `PowerManagementApi.Shutdown()` / `Restart()` work for normal desktop apps, `Shutdown` powers off, and the event log records a planned shutdown (E-14).
- `RegistryMonitor` no longer fires phantom notifications and reports the real error (E-12).
- `WinEventHook` works when subscribed from any thread, unhooks reliably and traces subscriber exceptions (A-12).
- `DisplayInfo.AllDisplayInfos` never returns null and follows display, work-area and DPI changes (A-19).
- `DpiHandler.ScaleWithCurrentDpi(NativePointFloat)` / `UnscaleWithCurrentDpi(NativePointFloat)` no longer round to integers, and unscaling no longer scales.
