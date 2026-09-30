# Dapplo.Windows

[![Build](https://github.com/dapplo/Dapplo.Windows/actions/workflows/build.yml/badge.svg)](https://github.com/dapplo/Dapplo.Windows/actions/workflows/build.yml)
[![Coverage Status](https://coveralls.io/repos/github/dapplo/Dapplo.Windows/badge.svg?branch=master)](https://coveralls.io/github/dapplo/Dapplo.Windows?branch=master)
[![NuGet](https://img.shields.io/nuget/v/Dapplo.Windows.svg)](https://www.nuget.org/packages/Dapplo.Windows)
[![License: MIT](https://img.shields.io/badge/license-MIT-blue.svg)](LICENSE)

Dapplo.Windows gives .NET applications on Windows access to what .NET itself doesn't cover: windows and their events,
keyboard and mouse hooks, the clipboard, DPI awareness, power management, the Restart Manager, file dialogs and more.
It was built for [Greenshot](https://getgreenshot.org/) and is split into small packages, so you only install what you
use.

**Documentation: <https://www.dapplo.net/Dapplo.Windows/>** (guides with samples and the API reference)

## Requirements

- Windows
- .NET Framework 4.8 or .NET 10 (the packages target `net480` and `net10.0-windows`)

Upgrading from 2.x? Version 3.0 changes many APIs on purpose: read the
[migration guide](doc/articles/migration-3.0.md) and the [changelog](CHANGELOG.md).

## Packages

| Package | NuGet | What it's for | Depends on (Dapplo.Windows.*) |
|---|---|---|---|
| [Dapplo.Windows](https://www.nuget.org/packages/Dapplo.Windows) | [![NuGet](https://img.shields.io/nuget/v/Dapplo.Windows.svg)](https://www.nuget.org/packages/Dapplo.Windows) | Window information, enumeration and manipulation, WinEvent hooks, window screenshots and icons | Common, Com, DesktopWindowsManager, Gdi32, Icons, Input, Kernel32, Messages, User32 |
| [Dapplo.Windows.Clipboard](https://www.nuget.org/packages/Dapplo.Windows.Clipboard) | [![NuGet](https://img.shields.io/nuget/v/Dapplo.Windows.Clipboard.svg)](https://www.nuget.org/packages/Dapplo.Windows.Clipboard) | Monitor, read and write the clipboard, delayed rendering, clipboard history options | Kernel32, Messages |
| [Dapplo.Windows.Input](https://www.nuget.org/packages/Dapplo.Windows.Input) | [![NuGet](https://img.shields.io/nuget/v/Dapplo.Windows.Input.svg)](https://www.nuget.org/packages/Dapplo.Windows.Input) | Keyboard and mouse hooks, hotkeys and key sequences, generating input, raw input | Common, Messages, User32 |
| [Dapplo.Windows.Dpi](https://www.nuget.org/packages/Dapplo.Windows.Dpi) | [![NuGet](https://img.shields.io/nuget/v/Dapplo.Windows.Dpi.svg)](https://www.nuget.org/packages/Dapplo.Windows.Dpi) | DPI calculations, DPI change handling, bitmap scaling, DPI awareness APIs | Common, Gdi32, Messages, User32 |
| [Dapplo.Windows.Forms](https://www.nuget.org/packages/Dapplo.Windows.Forms) | [![NuGet](https://img.shields.io/nuget/v/Dapplo.Windows.Forms.svg)](https://www.nuget.org/packages/Dapplo.Windows.Forms) | Windows Forms integration: `DpiAwareForm`, window messages of controls, placement | Dapplo.Windows, Dpi, Messages |
| [Dapplo.Windows.Wpf](https://www.nuget.org/packages/Dapplo.Windows.Wpf) | [![NuGet](https://img.shields.io/nuget/v/Dapplo.Windows.Wpf.svg)](https://www.nuget.org/packages/Dapplo.Windows.Wpf) | WPF integration: DPI handling, window messages, conversions, `BitmapSource` | Dapplo.Windows, Dpi, Gdi32, Messages |
| [Dapplo.Windows.Messages](https://www.nuget.org/packages/Dapplo.Windows.Messages) | [![NuGet](https://img.shields.io/nuget/v/Dapplo.Windows.Messages.svg)](https://www.nuget.org/packages/Dapplo.Windows.Messages) | `SharedMessageWindow` for window messages without a window, session notifications | Common |
| [Dapplo.Windows.SystemState](https://www.nuget.org/packages/Dapplo.Windows.SystemState) | [![NuGet](https://img.shields.io/nuget/v/Dapplo.Windows.SystemState.svg)](https://www.nuget.org/packages/Dapplo.Windows.SystemState) | Prevent sleep, sleep / shut down / log off, wake timers, power events | Messages |
| [Dapplo.Windows.AppRestartManager](https://www.nuget.org/packages/Dapplo.Windows.AppRestartManager) | [![NuGet](https://img.shields.io/nuget/v/Dapplo.Windows.AppRestartManager.svg)](https://www.nuget.org/packages/Dapplo.Windows.AppRestartManager) | Register for restart, answer shutdown and Restart Manager requests | Messages |
| [Dapplo.Windows.InstallerManager](https://www.nuget.org/packages/Dapplo.Windows.InstallerManager) | [![NuGet](https://img.shields.io/nuget/v/Dapplo.Windows.InstallerManager.svg)](https://www.nuget.org/packages/Dapplo.Windows.InstallerManager) | For installers: find, close and restart the processes which lock files | - |
| [Dapplo.Windows.Dialogs](https://www.nuget.org/packages/Dapplo.Windows.Dialogs) | [![NuGet](https://img.shields.io/nuget/v/Dapplo.Windows.Dialogs.svg)](https://www.nuget.org/packages/Dapplo.Windows.Dialogs) | File open, file save and folder dialogs without WinForms or WPF | Common |
| [Dapplo.Windows.Icons](https://www.nuget.org/packages/Dapplo.Windows.Icons) | [![NuGet](https://img.shields.io/nuget/v/Dapplo.Windows.Icons.svg)](https://www.nuget.org/packages/Dapplo.Windows.Icons) | Extract icons, write ICO and CUR files, capture the mouse cursor | Common, Dpi, Gdi32, Kernel32, Messages, Shell32, User32 |
| [Dapplo.Windows.Devices](https://www.nuget.org/packages/Dapplo.Windows.Devices) | [![NuGet](https://img.shields.io/nuget/v/Dapplo.Windows.Devices.svg)](https://www.nuget.org/packages/Dapplo.Windows.Devices) | Device and volume arrival / removal notifications | Messages |
| [Dapplo.Windows.DesktopWindowsManager](https://www.nuget.org/packages/Dapplo.Windows.DesktopWindowsManager) | [![NuGet](https://img.shields.io/nuget/v/Dapplo.Windows.DesktopWindowsManager.svg)](https://www.nuget.org/packages/Dapplo.Windows.DesktopWindowsManager) | DWM: frame bounds, cloaking, accent color, corners | Common |
| [Dapplo.Windows.Citrix](https://www.nuget.org/packages/Dapplo.Windows.Citrix) | [![NuGet](https://img.shields.io/nuget/v/Dapplo.Windows.Citrix.svg)](https://www.nuget.org/packages/Dapplo.Windows.Citrix) | Citrix session detection and client information | Common |
| [Dapplo.Windows.EmbeddedBrowser](https://www.nuget.org/packages/Dapplo.Windows.EmbeddedBrowser) | [![NuGet](https://img.shields.io/nuget/v/Dapplo.Windows.EmbeddedBrowser.svg)](https://www.nuget.org/packages/Dapplo.Windows.EmbeddedBrowser) | Let the WinForms `WebBrowser` use the installed IE version | Com |
| [Dapplo.Windows.Multimedia](https://www.nuget.org/packages/Dapplo.Windows.Multimedia) | [![NuGet](https://img.shields.io/nuget/v/Dapplo.Windows.Multimedia.svg)](https://www.nuget.org/packages/Dapplo.Windows.Multimedia) | Play system sounds and WAV data | - |
| [Dapplo.Windows.Advapi32](https://www.nuget.org/packages/Dapplo.Windows.Advapi32) | [![NuGet](https://img.shields.io/nuget/v/Dapplo.Windows.Advapi32.svg)](https://www.nuget.org/packages/Dapplo.Windows.Advapi32) | Registry change notifications, logon SID | - |
| [Dapplo.Windows.User32](https://www.nuget.org/packages/Dapplo.Windows.User32) | [![NuGet](https://img.shields.io/nuget/v/Dapplo.Windows.User32.svg)](https://www.nuget.org/packages/Dapplo.Windows.User32) | User32 P/Invoke: windows, displays, messages | Common, Messages |
| [Dapplo.Windows.Gdi32](https://www.nuget.org/packages/Dapplo.Windows.Gdi32) | [![NuGet](https://img.shields.io/nuget/v/Dapplo.Windows.Gdi32.svg)](https://www.nuget.org/packages/Dapplo.Windows.Gdi32) | GDI / GDI+ P/Invoke and safe handles | Common, User32 |
| [Dapplo.Windows.Kernel32](https://www.nuget.org/packages/Dapplo.Windows.Kernel32) | [![NuGet](https://img.shields.io/nuget/v/Dapplo.Windows.Kernel32.svg)](https://www.nuget.org/packages/Dapplo.Windows.Kernel32) | Kernel32 P/Invoke: processes, DLL loading, package identity | Common |
| [Dapplo.Windows.Shell32](https://www.nuget.org/packages/Dapplo.Windows.Shell32) | [![NuGet](https://img.shields.io/nuget/v/Dapplo.Windows.Shell32.svg)](https://www.nuget.org/packages/Dapplo.Windows.Shell32) | Shell32 P/Invoke: taskbar, icons | Common |
| [Dapplo.Windows.Com](https://www.nuget.org/packages/Dapplo.Windows.Com) | [![NuGet](https://img.shields.io/nuget/v/Dapplo.Windows.Com.svg)](https://www.nuget.org/packages/Dapplo.Windows.Com) | COM helpers | Common |
| [Dapplo.Windows.Common](https://www.nuget.org/packages/Dapplo.Windows.Common) | [![NuGet](https://img.shields.io/nuget/v/Dapplo.Windows.Common.svg)](https://www.nuget.org/packages/Dapplo.Windows.Common) | Shared structs (`NativeRect`, ...), `HResult`, `WindowsVersion` | - |

Only Dapplo.Windows.Forms, Dapplo.Windows.Wpf and Dapplo.Windows.EmbeddedBrowser reference Windows Forms or WPF.

## Quick start

```powershell
dotnet add package Dapplo.Windows
dotnet add package Dapplo.Windows.Clipboard
dotnet add package Dapplo.Windows.Input
```

React to window title changes:

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

A global hotkey:

<!-- sample: GettingStartedSamples.FirstKeyboardHook -->
```csharp
// using Dapplo.Windows.Input.Enums; using Dapplo.Windows.Input.Keyboard; using System.Reactive.Linq;
// Ctrl+Shift+S anywhere in Windows. The handler runs on the hook thread, ObserveOn moves the work to the UI thread.
IDisposable subscription = KeyboardHook.KeyboardEvents
    .Where(new KeyCombinationHandler(VirtualKeyCode.Control, VirtualKeyCode.Shift, VirtualKeyCode.KeyS))
    .ObserveOn(SynchronizationContext.Current)
    .Subscribe(_ => Console.WriteLine("Ctrl+Shift+S pressed"));
```

Events arrive on background threads (the hook thread or the thread of the `SharedMessageWindow`): keep your handlers
short and use `ObserveOn` for UI work. [Getting started](doc/articles/intro.md#how-the-library-works) explains why.

## Documentation

- [Getting started](doc/articles/intro.md)
- [Window management](doc/articles/window-management.md)
- [Window messages and the SharedMessageWindow](doc/articles/window-messages.md)
- [Keyboard and mouse](doc/articles/input-handling.md)
- [Clipboard](doc/articles/clipboard-usage.md)
- [DPI awareness](doc/articles/dpi-awareness.md)
- [Power and system state](doc/articles/system-state.md)
- [Restart Manager](doc/articles/restart-manager.md)
- [Icons and cursors](doc/articles/icons.md)
- [File and folder dialogs](doc/articles/dialogs.md)
- [Windows Forms and WPF](doc/articles/forms-and-wpf.md)
- [More packages](doc/articles/more-packages.md)
- [Common scenarios](doc/articles/common-scenarios.md)
- [API reference](https://www.dapplo.net/Dapplo.Windows/api/index.html)

The [wiki](https://github.com/dapplo/Dapplo.Windows/wiki) has short versions of these pages.

## Examples

- **Dapplo.Windows.Example.ConsoleDemo**: keyboard hook in a console application
- **Dapplo.Windows.Example.FormsExample**: DPI-aware forms, context menus and bitmap scaling, embedded browser, restart registration
- **Dapplo.Windows.Example.WpfExample**: WPF window with DPI handling, keyboard hook and device notifications
- **Dapplo.Windows.Example.InstallerExample**: an "installer" which closes and restarts the FormsExample with the Restart Manager
- **Dapplo.Windows.Example.DocSamples**: every code sample of the documentation, compiled with each build

## Building

You need the .NET 10 SDK (see [global.json](global.json)) on Windows.

```powershell
dotnet build src/Dapplo.Windows.sln
dotnet test src/Dapplo.Windows.sln --filter "Category!=Interactive"
```

Tests with `Category=Interactive` send input, replace the clipboard or write to the registry; run them only on a
machine you're not using. [tools/build-runner](tools/build-runner/README.md) builds and tests on a Windows machine on
request, for tools which can only write files into the repository.

When you change a code sample, change it in `src/Dapplo.Windows.Example.DocSamples` and copy it into the markdown, see
[About the samples](doc/articles/intro.md#about-the-samples).

## Contributing

Issues and pull requests are welcome. Please add an entry to [CHANGELOG.md](CHANGELOG.md) for user-visible changes.

## License

MIT, see [LICENSE](LICENSE).
