# Upgrade prompt: moving an application to Dapplo.Windows 3.0

This page is written for an AI coding assistant, and works as a checklist for people too. Give it to the assistant
together with the application's repository (written with Greenshot in mind, it applies to any consumer).
The goal is an application that **works** with Dapplo.Windows 3.0, not one that merely compiles.

---

## Your task

Upgrade the application from Dapplo.Windows 2.x to 3.0.

Dapplo.Windows 3.0 fixed a large number of interop bugs and deliberately changed APIs whose concept was wrong.
Many changes are caught by the compiler (renames, moved types, changed signatures). The dangerous ones compile
without complaint but behave differently: a failure branch that never ran now runs, a flag that was ignored now
works, a subscriber that ran on the UI thread now runs on another thread, a stored setting reads back differently.

Reference material, in this order of authority:

1. The Dapplo.Windows source at the 3.0 tag (read it when in doubt; it is the truth).
2. `CHANGELOG.md`: every change, with finding IDs such as `A-06`.
3. `doc/articles/migration-3.0.md`: rename tables and before/after code.
4. The articles in `doc/articles` and their compiled samples in `src/Dapplo.Windows.Example.DocSamples`.

### Rules

- **Fix the cause, don't hide it.** No wrappers that re-create old names, no `#pragma` or `try { } catch { }` to
  silence new exceptions, no casting away new types. If 3.0 throws where 2.x silently misbehaved, the calling code
  was wrong: fix the calling code.
- **Understand each call site.** For every use of a changed API, decide what the code *meant* and choose the 3.0 API
  that does that. The rename tables give the mechanical mapping; the sections below tell you when the mechanical
  mapping is not enough.
- **Keep behaviour the user sees the same or better.** Where 3.0 changes behaviour on purpose (owned windows, DPI
  scaling, cursor position, hotkey threading), check the user-visible result and adjust the application.
- **Don't change Dapplo.Windows** to fit the application. If something in the library looks wrong or is missing,
  note it in your report.
- **Small commits by area** (packages, windows, input, clipboard, DPI, …), each building and passing tests.
- **Report** what you changed, every place where you had to decide about behaviour, and the manual checks from the
  last section that still need a human.

---

## Step 1: Inventory

Before changing anything, find every use of Dapplo.Windows and group it by area. Search for at least:

```text
using Dapplo.Windows
InteropWindow  IInteropWindow  InteropWindowQuery  InteropWindowFactory  WindowsEnumerator
GetParent  GetChildren  GetZOrderedChildren  IsTopLevel  IsPopup  GetTopLevelWindows  GetTopWindows
Fill(  InteropWindowRetrieveSettings  GetInfo(  PrintWindow  WindowScroller
KeyboardHook  MouseHook  KeyCombinationHandler  KeySequenceHandler  KeyOrCombinationHandler  TriggerOnKeyUp
KeyboardInputGenerator  MouseInputGenerator  VirtualKeyCode  KeyHelper
ClipboardNative  IClipboardAccessToken  SetAs  ClearContents  OnUpdate  OnRenderFormat  SetDelayedRenderedContent
SharedMessageWindow  WindowMessage  WindowMessageInfo  WinProcListener  WinProcFormsMessages  WinProcMessages
DpiHandler  DpiAwareForm  DpiUnawareForm  AttachDpiHandler  DpiAwarenessContext  NativeDpiMethods  BitmapScaleHandler
CursorHelper  CapturedCursor  IconInfo  IconHelper  GetIcon  GetAppLogo  DrawCursorOn
HResult  Succeeded  Failed  ThrowOnFailure  WindowsVersion  MonitorFrom  DisplayInfo
NativeRect  NativeSize  NativePoint  IsDocked  HasOverlap  IsEmpty  Intersect  TypeConverter
ApplicationRestartManager  ListenForEndSession  EndSessionMessage  InstallerRestartManager  RestartManager
SystemStateApi  PreventSleep  AllowSleep  WaitableTimer  DwmApi  WinMm  Shell32Api  WinEventHook
```

Also find everything the application **stores** that came from Dapplo.Windows types: settings files (sizes,
positions, rectangles, hotkeys, enum values), registry values, caches.

Write the inventory down (area, file, what it's used for). You'll need it for the report and for Step 4.

## Step 2: Packages and target frameworks

- Update every `Dapplo.Windows.*` package to the same 3.0 version.
- 3.0 targets `net480` and `net10.0-windows`. `netstandard2.0` and `net8.0-windows` are gone.
- The core packages no longer depend on WinForms or WPF. Add **Dapplo.Windows.Forms** where the application uses
  `DpiAwareForm`, `AttachDpiHandler(Form/ContextMenuStrip)`, `WinProcListener`, `WinProcMessages()` on a `Control`,
  `FormsExtensions`, or `BitmapScaleHandler` with buttons/menu items. Add **Dapplo.Windows.Wpf** for WPF windows,
  `ToBitmapSource()`, WPF struct conversions (`ToRect()`, `ToNativeRect()`, …) and `ColorizationColor.ToMediaColor()`.
- The `IInteropWindow` icon extensions (`GetIcon`, `GetIconFromWindow`, `GetAppLogo`) are in the **Dapplo.Windows**
  package now (namespace `Dapplo.Windows.Icons`).
- All assemblies are strong-named for every target now. Check binding redirects and `InternalsVisibleTo`.

## Step 3: Mechanical renames

Apply the tables in `migration-3.0.md` ("Target frameworks and packages", "Renames and moved types") and the
before/after snippets. Let the compiler guide you, but for every changed call read Step 4 for its area first; many
renames come with a behaviour change.

## Step 4: Changes that compile but behave differently

Work through every section that applies to your inventory. Each item says what changed, how to find affected code
and what to do.

### 4.1 Errors that used to be hidden

- **`HResult`** is a signed enum. In 2.x `Failed()` was never true and `Succeeded()` always true, so **every COM, DWM
  and DPI failure was reported as success**. Find all `Succeeded()`, `Failed()`, `ThrowOnFailure()`, comparisons with
  `HResult.*` and any `(uint)` casts of an HRESULT. For each failure branch that can now run, make sure it does
  something sensible (log, fall back, tell the user). `DwmApi` checks such as "is composition enabled", "is the
  window cloaked" and the DPI awareness calls are the usual ones.
- **Subscriber exceptions** in `SharedMessageWindow`, `KeyboardHook` and `MouseHook` no longer crash the process.
  They end that subscription and are published on `SharedMessageWindow.SubscriberErrors`,
  `KeyboardHook.SubscriberErrors` and `MouseHook.SubscriberErrors`. Subscribe to all three at startup and log them,
  otherwise a broken hotkey or clipboard listener just stops silently.
- **Dialogs** (`Dapplo.Windows.Dialogs`) throw `InvalidOperationException` when not called on an STA thread. Show
  them from the UI thread, never from `Task.Run` or a thread-pool continuation.
- **Clipboard `Set*`** throws `InvalidOperationException` when the clipboard content belongs to another window, which
  means the code forgot `ClearContents()`; see 4.6.

### 4.2 Windows: parent, owner, children and window lists

- **Parent vs owner (A-06).** `GetParent()` / `Parent` / `HasParent` are the real parent now (only for child windows).
  Owned windows (dialogs, tool windows, popups owned by another window) have an **owner** instead: `GetOwner()`,
  `Owner`, `HasOwner`. Every use of `GetParent` must be reviewed: "the window this dialog belongs to" is `GetOwner()`.
- **Children.** `GetChildren()` returns only **direct** children, in Z-order. Code that searched for a nested control
  (an edit field inside a panel, a browser render window) needs `GetDescendants()`. `GetZOrderedChildren()` is gone.
- **Visible application windows.** `IsTopLevel` / `GetTopLevelWindows` / `IsPopup` are now
  `IsVisibleApplicationWindow` / `GetVisibleApplicationWindows` / `IsVisiblePopup`, and they **accept owned windows**
  (2.x rejected them because `GetParent` returned the owner). A window picker or "capture window" list can therefore
  show more windows than before, for example an application's owned tool windows. Decide per call site whether that
  is wanted; filter with `HasOwner` / `GetOwner()` if not.
- **Window lists are snapshots.** `GetTopWindows()` returns an `IReadOnlyList` taken with `EnumWindows` at call
  time. It no longer skips or repeats windows while the Z-order changes. Re-query instead of holding it long.
- **`Fill()` caching works.** In 2.x `Fill()` ignored its flags: it always refreshed, always auto-corrected bounds,
  always queried the maximized state. Now values are cached unless you pass `InteropWindowRetrieveSettings.ForceUpdate`,
  and `CacheAll` / `CacheAllWithChildren` do **not** auto-correct bounds (use `CacheAllAutoCorrect` or add
  `AutoCorrectValues`). Find every `Fill(`, `GetInfo(`, `GetBounds(`… on windows that are **reused** (for example a
  window object kept while the user moves or resizes it) and pass `ForceUpdate` where fresh values are needed.
- **`GetInfo(autoCorrect)`** no longer crops owned dialogs to their owner's bounds. Capture code that relied on the
  cropped bounds now gets the real window bounds, which is the correct result.
- **Hung windows.** Reading a window's text or title-bar info times out after 500 ms instead of blocking forever.
  Text can be null for a hung window; handle that.

### 4.3 Capturing: `PrintWindow`, cursor and icons

- **`PrintWindow()`** returns a `Bitmap` (no longer generic), sized from the full window and cropped to the visible
  (DWM) bounds, rendered with `PW_RENDERFULLCONTENT` on Windows 8.1+. The 2.x image was shifted by the invisible
  border, and DirectComposition, Chromium and UWP windows came out black. It returns **null** for a zero-sized window.
  Remove any offset correction the application added for the 2.x shift, and handle null.
- **Cursor capture.** `CursorHelper.TryGetCurrentCursor` returns a `Size` and `HotSpot` that match the captured
  bitmaps at every DPI and pointer size. `DrawCursorOnGraphics` / `DrawCursorOnBitmap` take the **top-left of the
  cursor image**, not the mouse position: pass `mousePosition - cursor.HotSpot` (in the capture's coordinate space).
  Check the cursor lands exactly where the mouse was, at 100 %, 150 % and 200 % scaling and with an enlarged pointer.
- **`IconInfo` / `IconInfoEx`** expose raw, non-owning `ColorBitmap` / `BitmaskBitmap`. Take ownership once with
  `TakeBitmaps(out mask, out color)` or free them with `DeleteBitmaps()`. Never create a SafeHandle per read.
- **Icons returned by `IconHelper` and `GetIcon<Icon>()`** own their handle now (2.x returned a wrapper around a
  destroyed handle). **Dispose** them when you're done. Only `Icon` and `Bitmap` are supported; other types throw
  `NotSupportedException`. For WPF use `GetIcon<Bitmap>().ToBitmapSource()`.
- **ICO/CUR writing** scales images above 256 pixels down to 256 instead of truncating them.
- **`WindowsVersion`** reads the real version with `RtlGetVersion`, also for .NET Framework hosts without a manifest.
  `IsWindows10` and `IsWindowsVista` mean *exactly* that version now; "10 or later" is `IsWindows10OrLater`.

### 4.4 Geometry and stored values

- Right and Bottom are **exclusive** (Win32 `RECT`). `IsDockedToLeftOf` / `IsDockedToRightOf` treat flush rectangles
  (`a.Right == b.Left`) as docked; `HasOverlap` equals `IntersectsWith` (touching is not overlapping, containing is).
  Remove any ±1 corrections the application added around these.
- `IsEmpty` is true for a zero **or negative** width or height.
- `NativeRect.Union` ignores empty rectangles. `Intersect2` is gone, use `Intersect`.
- Float-to-int conversions are explicit: points floor, sizes round up, rectangles become the smallest containing
  integer rectangle. Use `Round()` when you want rounding. Values can differ by one pixel from 2.x.
- `NativeSize.CompareTo` sorts ascending by area (2.x sorted descending). Check every sort of sizes.
- **Stored settings (important).** 2.x's `NativeSizeTypeConverter` wrote `Height,Width` and read the values back the
  other way round. 3.0 reads and writes `Width,Height`, so every `NativeSize` stored by 2.x comes back **swapped**.
  Add a one-time settings migration (a settings version number, swap stored sizes once) or reset those values. All
  converters now use the invariant culture.
- **Stored enum numbers:** `MonitorFrom`, `SysColorIndexes.Color3Dface`, `ProcessAccessRights`,
  `DialogDpiChangeBehaviors`, `ObjectStates.STATE_SYSTEM_VALID` and `DesktopAccessRight.GENERIC_ALL` changed values.

### 4.5 Hotkeys and input

- **Hooks run on their own thread.** `KeyboardHook` and `MouseHook` subscribers are no longer called on the UI
  thread. Anything that touches UI, application state or non-thread-safe services must be marshalled:

  ```csharp
  KeyboardHook.KeyboardEvents
      .Where(handler)                              // decides Handled synchronously on the hook thread
      .ObserveOn(SynchronizationContext.Current)   // capture/UI work on the UI thread
      .Subscribe(_ => StartCapture());
  ```

  Setting `Handled` must happen synchronously in the `Where` / subscriber on the hook thread, and quickly (Windows
  removes a hook that is too slow). Listeners that never set `Handled` should use `KeyboardEventsNonBlocking` /
  `MouseEventsNonBlocking`.
- **One handler instance per subscription.** `Where(handler)` fails (OnError) when the same handler instance is used by
  another active subscription. If an observable is subscribed more than once, use `Where(() => new KeyCombinationHandler(...))`.
- **Trigger mode.** `TriggerOnKeyUp` is replaced by `TriggerMode`: `KeyDown` (default), `FirstKeyUp`, `AllKeysUp`.
  For a hotkey that **starts a capture or sends input**, prefer `AllKeysUp`: it fires when every key of the
  combination is released, so the captured application doesn't see a held Ctrl/Shift/Alt and no key gets stuck.
  Key-up modes never mark events as handled.
- **PrintScreen.** `VirtualKeyCode.Snapshot` is gone, the key is `VirtualKeyCode.PrintScreen` (`Print` is a different,
  rare key). Check every hotkey definition that means "the Print Screen key".
- **Win key.** `VirtualKeyCode.Win` (parsed from `"win"`) matches either Windows key; use `LeftWin` / `RightWin`
  only when a side matters.
- **Stored hotkey strings.** `KeyHelper.VirtualKeyCodeFromString` still accepts the old names `Snapshot`, `Hangul`,
  `Hangeul` and `Kanji`, and `ToString()` now always produces `PrintScreen`, `Kana`, `Hanja`. Check the application's
  own hotkey parser/serializer, if it has one, against these names.
- **Key sequences** reset after a wrong combination regardless of release order.
- **Injected input**: arrow, navigation, right Ctrl/Alt, Windows and media keys are sent as extended keys, and
  `KeyCombinationPress` releases keys in reverse order. Mouse coordinates are normalised correctly over the virtual
  desktop (2.x could jump to the screen edge). Remove workarounds for those bugs.
- **Typing text**: use `KeyboardInputGenerator.TypeText(text)` (Unicode, any layout). Replace "put text on the
  clipboard and send Ctrl+V" workarounds; they overwrite the user's clipboard.
- **Key state**: `KeyboardState.IsDown(...)`, `IsAnyDown(...)`, `IsToggled(...)` replace own `GetAsyncKeyState`
  P/Invokes.
- **Raw input**: HID data is `args.HidData`; monitors register when subscribed and unregister when disposed.

### 4.6 Clipboard

- **Writing**: always replace the content. The recommended form:

  ```csharp
  ClipboardNative.ReplaceContents(new ClipboardContents()
      .AddStream(StandardClipboardFormats.DeviceIndependentBitmap, dibStream)
      .AddStream("PNG", pngStream)
      .AddUnicodeString(filePath));
  ```

  `ReplaceContents` clears first and places all formats or none. The low-level `Set*` methods still exist but throw
  when the content belongs to another application (you forgot `ClearContents()`). Adding to your own content is
  `AddToCurrentContents`.
- **Threads**: an access token must be used and disposed on the thread that opened it, and you must not `await` while
  holding it. `AccessAsync` opens the clipboard on the thread that continues after the `await`. Cancelling throws
  `OperationCanceledException`.
- **Reading**: `GetAsStream` / `TryGetAsStream` return a copy that stays valid after the token is disposed.
  `GetAsUnicodeString` no longer returns trailing garbage; remove any trimming workaround.
- **Change notifications**: `ClipboardNative.OnUpdate` no longer opens the clipboard. The notification carries the
  sequence number, owner and formats; to read content, `ObserveOn` another thread and call `Access()` there.
- **Delayed rendering**: `OnRenderFormat` is gone. Register a renderer per format with
  `ClipboardNative.RegisterDelayedRenderer(format, request => …)` **before** announcing the format, and keep the
  registration alive as long as the content may be pasted. Renderers run synchronously on the shared window thread.
  Delayed content now survives the application exiting (the shared window is shut down on process exit and Windows
  asks for the data first).
- **Cloud / history options**: `SetCloudClipboardOptions()` without arguments places nothing; 2.x silently excluded
  every copy from clipboard history and cloud sync. Pass explicit values if the application wants to exclude content.

### 4.7 Window messages

- `WindowMessage` is a class and **`Handled` / `Result` now reach Windows**. In 2.x they were set on a copy and ignored.
  Review every subscriber that sets `Handled = true`: it now really suppresses the default window procedure, and
  `Result` is really returned. Set them synchronously in `OnNext`; after `ObserveOn` the answer is already sent.
- `WindowMessageInfo` is gone; the Forms/WPF `WinProcMessages()` and `DpiHandler` use `WindowMessage` too.
- `SharedMessageWindow.Handle` is never 0 and the window lives for the whole process. Remove "wait until the window
  exists" code. Do registrations in `Listen(onSetup, onTeardown)` (both run on the window thread) and use
  `SharedMessageWindow.Invoke(hwnd => …)` for other code that must run there. `WinEventHook` events and
  `PowerBroadcastListener` / session events arrive on that thread.

### 4.8 DPI

- `DpiAwarenessContext` is a pointer-sized struct; compare with `NativeDpiMethods.AreDpiAwarenessContextsEqual`.
- In 2.x, creating a `DpiAwareForm` or `DpiHandler`, or calling `AttachDpiHandler`, **left the UI thread in Per
  Monitor v2 awareness permanently**. 3.0 only changes it while the form handle is created. If other windows of the
  application only scaled correctly because of that leak, give the process the right awareness via its manifest
  (or `app.config` for WinForms on .NET Framework), or create those windows inside
  `NativeDpiMethods.ScopedThreadDpiAwarenessContext`.
- `DpiAwareForm` no longer swallows `WM_DPICHANGED`: WinForms' own Per Monitor v2 scaling and `Form.DpiChanged` run.
  Remove manual font/control scaling in `FormDpiHandler.OnDpiChanged`, or it scales twice. Without WinForms scaling
  (.NET Framework without PMv2 configuration) only the window bounds follow Windows' suggested rectangle.
- `DpiCalculator` / `DpiHandler` scaling rounds instead of truncating (`ScaleWithDpi(3, 144)` is 5), and
  `DpiHandler.Dpi` is 96 until the real DPI is known (`IsDpiKnown`).
- `EnableDpiAware()` falls back to older awareness modes and returns whether the process is really DPI aware.

### 4.9 Session end, restart, power

- `ApplicationRestartManager.ListenForEndSession()` takes no callbacks. Answer WM_QUERYENDSESSION synchronously in
  `OnNext`: `m.Veto("reason")` to block (the reason shows in Windows' shutdown screen), do nothing to allow. In 2.x
  "allow" was answered as "block" and nothing reached Windows, so check the application's shutdown and logoff
  behaviour.
- `WasRestartRequested("/restore")` checks the argument you registered.
- `InstallerRestartManager.Shutdown()` is graceful by default; `RmShutdownType.Force` is opt-in.
- `SystemStateApi.PreventSleep(...)` returns a disposable `SleepBlocker` that works from any thread; `AllowSleep()`
  is gone: `using (SystemStateApi.PreventSleep("Recording")) { … }`.
- `PowerManagementApi.Shutdown()` / `Restart()` enable the shutdown privilege themselves and `Shutdown` powers off.

### 4.10 Smaller items

- `WinMm`: `PlayResource` (embedded WAVE resource), `PlayFile` (file), `PlayWave(byte[])`, `PlaySystemSound`; all
  return `bool`. A `Play("file.wav")` call in 2.x never played the file.
- `DwmApi.ColorizationColor` is a `System.Drawing.Color`.
- `Shell32Api.TryGetTaskbarPosition(out var data)` replaces `TaskbarPosition`.
- Restart Manager types live in `Dapplo.Windows.InstallerManager` with readable field names.
- `WindowHandles.HWND_TOPMOST` etc. replace magic `new IntPtr(-1)` values; `User32Api.PostMessage` / `PostThreadMessage`
  replace own P/Invokes.

## Step 5: Verify

Build, run the unit tests, then work through these checks on a real Windows machine. Report each as done, not
applicable, or needs a human.

1. **Captures** (region, window, full screen, last region) at 100 %, 150 % and 200 % scaling, and on two monitors
   with different scaling. The image is not shifted, not black (try a browser and a UWP app), and the right size.
2. **Mouse pointer in captures**: the pointer is exactly where it was, also with an enlarged pointer.
3. **Window picker / window capture**: the list contains the expected windows (compare with 2.x; owned windows may
   now appear), a hung application doesn't freeze it, minimized and off-screen windows behave as before.
4. **Hotkeys**: every configured hotkey fires once, with and without modifiers, including PrintScreen variants; no key
   stays stuck; the captured window doesn't see a held modifier; hotkeys still work after a sleep/resume.
5. **Settings from 2.x**: start with an existing settings file. Stored sizes, positions and hotkeys load correctly
   (sizes not swapped).
6. **Clipboard**: copy a capture, paste it into Paint, Word/LibreOffice, Outlook and a browser; copy, close the
   application, then paste; copy while another application holds the clipboard; clipboard history (Win+V) works as
   the application intends.
7. **DPI changes**: move each window (editor, settings, dialogs) between monitors with different scaling: no double
   scaling, no blurry text, dialogs keep a sensible size.
8. **Session end**: log off and shut down with unsaved work and without; the application saves/vetoes as intended
   and does not block shutdown otherwise.
9. **Dialogs**: every open/save/folder dialog opens (from the UI thread).
10. **Logs**: no entries from `SubscriberErrors` during all of the above.

## Report

Finish with:

- the inventory (Step 1) with the status of every item;
- a list of behaviour decisions you made (which call sites, what you chose, why);
- the settings migration you added (or why none was needed);
- the Step 5 checklist with results;
- anything in Dapplo.Windows that looked wrong or was missing.
