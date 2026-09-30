# Dapplo.Windows

Dapplo.Windows gives .NET applications on Windows access to what .NET itself doesn't cover: windows and their events,
keyboard and mouse hooks, the clipboard, DPI awareness, power management, the Restart Manager, file dialogs and more.
It was built for [Greenshot](https://getgreenshot.org/) and is split into small NuGet packages, so you only install
what you use.

This wiki has short versions of the guides. The full documentation, with the API reference, is at
**<https://www.dapplo.net/Dapplo.Windows/>**.

## Requirements

- Windows
- .NET Framework 4.8 or .NET 10 (the packages target `net480` and `net10.0-windows`)

Upgrading from 2.x? Read the [migration guide](https://www.dapplo.net/Dapplo.Windows/articles/migration-3.0.html) and the
[changelog](https://github.com/dapplo/Dapplo.Windows/blob/master/CHANGELOG.md): version 3.0 changes many APIs on purpose.

## Pages

| Page | About |
|---|---|
| [[Getting-Started]] | Installation, first steps, how the library uses threads |
| [[Window-Management]] | Find, inspect, move and capture windows, window events |
| [[SharedMessageWindow]] | Window messages without a window of your own, session changes |
| [[Input-Handling]] | Keyboard and mouse hooks, hotkeys, generating input |
| [[Clipboard]] | Monitor, read and write the clipboard |
| [[DPI-Awareness]] | Follow the DPI of each monitor in WinForms and WPF |
| [[System-State]] | Prevent sleep, shut down, wake timers, power events |
| [[Restart-Manager]] | Restart registration and installer support |
| [[Icon-Creation]] | Extract icons, write ICO and CUR files, capture the cursor |
| [[Dialogs]] | File and folder dialogs without WinForms or WPF |
| [[Common-Scenarios]] | Recipes which combine several packages |

Not in the wiki: [Windows Forms and WPF](https://www.dapplo.net/Dapplo.Windows/articles/forms-and-wpf.html) and
[more packages](https://www.dapplo.net/Dapplo.Windows/articles/more-packages.html) (Citrix, DWM, devices, registry, sounds, embedded browser).

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

## Build status

[![Build](https://github.com/dapplo/Dapplo.Windows/actions/workflows/build.yml/badge.svg)](https://github.com/dapplo/Dapplo.Windows/actions/workflows/build.yml)
[![Coverage Status](https://coveralls.io/repos/github/dapplo/Dapplo.Windows/badge.svg?branch=master)](https://coveralls.io/github/dapplo/Dapplo.Windows?branch=master)

## License

MIT, see [LICENSE](https://github.com/dapplo/Dapplo.Windows/blob/master/LICENSE).
