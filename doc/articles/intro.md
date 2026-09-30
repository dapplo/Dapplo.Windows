# Getting started

Dapplo.Windows gives .NET applications access to the parts of Windows that the .NET base library doesn't cover:
window information and events, keyboard and mouse hooks, the clipboard, DPI awareness, power management, the
Restart Manager and more. It started as the native layer of [Greenshot](https://getgreenshot.org/) and is split into
small packages, so you only install what you use.

## Requirements

- Windows. The packages call Win32 APIs and don't work on other operating systems.
- .NET Framework 4.8 (`net480`) or .NET 10 (`net10.0-windows`). Every package targets both.
- Most APIs work on Windows 7 and later. Where a feature needs a newer Windows version (for example Per Monitor V2 DPI
  awareness, Windows 10 1703), the documentation says so.

## Packages

Install the package for the feature you need, NuGet brings in its dependencies.

| Package | What it's for | Depends on |
|---|---|---|
| **Dapplo.Windows** | Window information, enumeration and manipulation, WinEvent hooks, window screenshots and icons, installed software | Common, Com, DesktopWindowsManager, Gdi32, Icons, Input, Kernel32, Messages, User32 |
| **Dapplo.Windows.Clipboard** | Monitor, read and write the clipboard, delayed rendering, clipboard history and cloud options | Kernel32, Messages |
| **Dapplo.Windows.Input** | Low-level keyboard and mouse hooks, key combinations and sequences, generating input, raw input | Common, Messages, User32 |
| **Dapplo.Windows.Dpi** | DPI calculations, `DpiHandler` for DPI changes, bitmap scaling, DPI awareness contexts | Common, Gdi32, Messages, User32 |
| **Dapplo.Windows.Forms** | Windows Forms integration: `DpiAwareForm`, window messages of a `Control`, placement | Dapplo.Windows, Dpi, Messages |
| **Dapplo.Windows.Wpf** | WPF integration: DPI handler for a `Window`, window messages, conversions to WPF types, `BitmapSource` | Dapplo.Windows, Dpi, Gdi32, Messages |
| **Dapplo.Windows.Messages** | `SharedMessageWindow`, the `WindowsMessages` enum, session (lock / logon) notifications | Common |
| **Dapplo.Windows.SystemState** | Prevent sleep, sleep / hibernate / shut down / log off, waitable (wake) timers, power events | Messages |
| **Dapplo.Windows.AppRestartManager** | Register an application for restart, answer "end session" requests | Messages |
| **Dapplo.Windows.InstallerManager** | For installers: find the processes which lock files, close and restart them (Restart Manager) | - |
| **Dapplo.Windows.Dialogs** | Modern file open, file save and folder picker dialogs, without WinForms or WPF | Common |
| **Dapplo.Windows.Icons** | Extract icons, write ICO and CUR files, capture the mouse cursor | Common, Dpi, Gdi32, Kernel32, Messages, Shell32, User32 |
| **Dapplo.Windows.Devices** | Device arrival and removal notifications (USB, volumes, ...) | Messages |
| **Dapplo.Windows.DesktopWindowsManager** | DWM: extended frame bounds, cloaking, accent color, corner preference | Common |
| **Dapplo.Windows.Citrix** | Detect a Citrix session and query client information | Common |
| **Dapplo.Windows.EmbeddedBrowser** | Make the WinForms `WebBrowser` use the installed Internet Explorer version | Com |
| **Dapplo.Windows.Multimedia** | Play system sounds and WAV data | - |
| **Dapplo.Windows.Advapi32** | Registry change notifications, logon SID | - |
| **Dapplo.Windows.User32** | User32 P/Invoke: windows, displays, messages, scroll bars | Common, Messages |
| **Dapplo.Windows.Gdi32** | GDI and GDI+ P/Invoke, safe handles, bitmap helpers | Common, User32 |
| **Dapplo.Windows.Kernel32** | Kernel32 P/Invoke: processes, DLL loading, package identity | Common |
| **Dapplo.Windows.Shell32** | Shell32 P/Invoke: taskbar position and state, icon extraction | Common |
| **Dapplo.Windows.Com** | COM helpers | Common |
| **Dapplo.Windows.Common** | Shared structs (`NativeRect`, `NativePoint`, ...), `HResult`, `Win32` errors, `WindowsVersion` | - |

The packages don't depend on Windows Forms or WPF, except **Dapplo.Windows.Forms**, **Dapplo.Windows.Wpf** (see
[Windows Forms and WPF](forms-and-wpf.md)) and **Dapplo.Windows.EmbeddedBrowser**, which extends the Windows Forms
`WebBrowser`.

```powershell
dotnet add package Dapplo.Windows
dotnet add package Dapplo.Windows.Clipboard
dotnet add package Dapplo.Windows.Input
```

## First steps

List the visible top-level windows:

<!-- sample: GettingStartedSamples.FirstWindowQuery -->
```csharp
// using Dapplo.Windows.Desktop;
foreach (var window in InteropWindowQuery.GetTopLevelWindows())
{
    Console.WriteLine($"{window.GetCaption()} - {window.GetClassname()} at {window.GetInfo().Bounds}");
}
```

Get told when a window title changes:

<!-- sample: GettingStartedSamples.WindowTitles -->
```csharp
// using Dapplo.Windows.Desktop; using System.Reactive.Linq;
IDisposable subscription = WinEventHook.WindowTitleChangeObservable()
    .Subscribe(info =>
    {
        var window = InteropWindowFactory.CreateFor(info.Handle);
        Console.WriteLine($"Title changed: {window.GetCaption(forceUpdate: true)}");
    });
```

Log the text that is copied to the clipboard:

<!-- sample: GettingStartedSamples.FirstClipboardMonitor -->
```csharp
// using Dapplo.Windows.Clipboard; using System.Reactive.Linq;
IDisposable subscription = ClipboardNative.OnUpdate
    .Where(info => info.FormatIds.Contains((uint)StandardClipboardFormats.UnicodeText))
    .Throttle(TimeSpan.FromMilliseconds(100))
    .Subscribe(info =>
    {
        using var clipboard = ClipboardNative.Access();
        Console.WriteLine($"Copied: {clipboard.GetAsUnicodeString()}");
    });
```

React to a global hotkey:

<!-- sample: GettingStartedSamples.FirstKeyboardHook -->
```csharp
// using Dapplo.Windows.Input.Enums; using Dapplo.Windows.Input.Keyboard; using System.Reactive.Linq;
// Ctrl+Shift+S anywhere in Windows. The handler runs on the hook thread, ObserveOn moves the work to the UI thread.
IDisposable subscription = KeyboardHook.KeyboardEvents
    .Where(new KeyCombinationHandler(VirtualKeyCode.Control, VirtualKeyCode.Shift, VirtualKeyCode.KeyS))
    .ObserveOn(SynchronizationContext.Current)
    .Subscribe(_ => Console.WriteLine("Ctrl+Shift+S pressed"));
```

## How the library works

Most events in Dapplo.Windows are `IObservable<T>` streams ([System.Reactive](https://github.com/dotnet/reactive)).
A few rules apply to all of them.

**Subscribing installs, disposing removes.** A hook or registration is made when you subscribe and removed when the
last subscription is disposed. Keep the `IDisposable` and dispose it when you're done:

<!-- sample: GettingStartedSamples.Dispose -->
```csharp
// Hooks and registrations are made when you subscribe, and removed when you dispose the subscription
IDisposable subscription = ClipboardNative.OnUpdate.Subscribe(info => { });
// ...
subscription.Dispose();

// Clipboard access is a lock for all applications: hold it briefly and always dispose it
using (var clipboard = ClipboardNative.Access())
{
    // ...
}
```

**Events arrive on a background thread.** Windows delivers hook and window messages to the thread which registered
them, so the library uses its own threads:

- Window messages (clipboard updates, power, session, device, WinEvent hooks, raw input) arrive on the thread of the
  [SharedMessageWindow](window-messages.md), a hidden window with its own message loop that lives for the whole process.
- Keyboard and mouse hook events arrive on the hook thread of `KeyboardHook` / `MouseHook`.

Your `OnNext` runs on that thread and blocks it. Keep it short. Use `ObserveOn(SynchronizationContext.Current)`
(called on the UI thread) to continue on the UI thread, or `ObserveOn(TaskPoolScheduler.Default)` for slow work.

**Answer synchronously.** When you answer Windows, for example with `Handled` on a key event or `Result` on a window
message, do it inside `OnNext`, before any `ObserveOn`, `Throttle` or `await`. Once the thread changes, Windows already
has its answer.

**A failing subscriber doesn't take the others down.** An exception in your `OnNext` ends that subscription only. The
SharedMessageWindow and the hooks publish these exceptions on their `SubscriberErrors` observables and write them to
`System.Diagnostics.Trace`.

## About the samples

Every code sample in this documentation, in the README and in the wiki is compiled with each build. The samples live in
the project
[src/Dapplo.Windows.Example.DocSamples](https://github.com/dapplo/Dapplo.Windows/tree/master/src/Dapplo.Windows.Example.DocSamples),
one class per page (for example `ClipboardSamples` for [the clipboard page](clipboard-usage.md)). Each sample is a
`#region` in that class, and the markdown has a `<!-- sample: Class.Region -->` comment above the copy of the code.
When you change a sample, change the code in the project first, build, and copy the region into the markdown.

The samples show the calls, they are not meant to be run as they are. The `using` directives which a page needs are
listed at the top of the page, the example projects in the repository (`Dapplo.Windows.Example.*`) are complete
applications.

## Upgrading from 2.x

Version 3.0 changes many APIs on purpose. Read the [migration guide](migration-3.0.md) and the
[changelog](https://github.com/dapplo/Dapplo.Windows/blob/master/CHANGELOG.md).

## Where next

- [Window management](window-management.md)
- [Window messages and the SharedMessageWindow](window-messages.md)
- [Keyboard and mouse](input-handling.md)
- [Clipboard](clipboard-usage.md)
- [DPI awareness](dpi-awareness.md)
- [Power and system state](system-state.md)
- [Restart Manager](restart-manager.md)
- [Icons and cursors](icons.md)
- [File and folder dialogs](dialogs.md)
- [Windows Forms and WPF](forms-and-wpf.md)
- [More packages](more-packages.md)
- [Common scenarios](common-scenarios.md)
- [API reference](../api/index.md)

Questions and bugs: [GitHub issues](https://github.com/dapplo/Dapplo.Windows/issues).
