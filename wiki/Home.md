# Dapplo.Windows

**Dapplo.Windows** is a comprehensive .NET library providing Windows-specific functionality for .NET Framework and .NET Core applications. Originally developed for [Greenshot](https://getgreenshot.org/), it is now a standalone, modular library that makes interacting with Windows APIs easy from managed code.

## Why Dapplo.Windows?

Working with native Windows APIs from .NET can be tedious: you have to write P/Invoke declarations, manage unmanaged resources safely, and deal with threading requirements. Dapplo.Windows handles all of that for you and adds reactive, observable APIs built on top of [Reactive Extensions (Rx.NET)](https://github.com/dotnet/reactive).

## Package Overview

The library is intentionally split into focused, independent NuGet packages so you only pull in what you need.

### High-Level Feature Packages

| Package | NuGet | Description |
|---------|-------|-------------|
| [Dapplo.Windows](https://www.nuget.org/packages/Dapplo.Windows) | [![NuGet](https://img.shields.io/nuget/v/Dapplo.Windows.svg)](https://www.nuget.org/packages/Dapplo.Windows) | Window management, enumeration, and WinEvent hooks |
| [Dapplo.Windows.Clipboard](https://www.nuget.org/packages/Dapplo.Windows.Clipboard) | [![NuGet](https://img.shields.io/nuget/v/Dapplo.Windows.Clipboard.svg)](https://www.nuget.org/packages/Dapplo.Windows.Clipboard) | Reactive clipboard monitoring and manipulation |
| [Dapplo.Windows.Dpi](https://www.nuget.org/packages/Dapplo.Windows.Dpi) | [![NuGet](https://img.shields.io/nuget/v/Dapplo.Windows.Dpi.svg)](https://www.nuget.org/packages/Dapplo.Windows.Dpi) | DPI awareness and scaling support |
| [Dapplo.Windows.Input](https://www.nuget.org/packages/Dapplo.Windows.Input) | [![NuGet](https://img.shields.io/nuget/v/Dapplo.Windows.Input.svg)](https://www.nuget.org/packages/Dapplo.Windows.Input) | Keyboard/mouse hooks and input generation |
| [Dapplo.Windows.Messages](https://www.nuget.org/packages/Dapplo.Windows.Messages) | [![NuGet](https://img.shields.io/nuget/v/Dapplo.Windows.Messages.svg)](https://www.nuget.org/packages/Dapplo.Windows.Messages) | Windows message definitions and WndProc helper |
| [Dapplo.Windows.Devices](https://www.nuget.org/packages/Dapplo.Windows.Devices) | [![NuGet](https://img.shields.io/nuget/v/Dapplo.Windows.Devices.svg)](https://www.nuget.org/packages/Dapplo.Windows.Devices) | Device information and change notifications |
| [Dapplo.Windows.Dialogs](https://www.nuget.org/packages/Dapplo.Windows.Dialogs) | [![NuGet](https://img.shields.io/nuget/v/Dapplo.Windows.Dialogs.svg)](https://www.nuget.org/packages/Dapplo.Windows.Dialogs) | Modern file/folder picker dialogs (no WinForms/WPF required) |
| [Dapplo.Windows.Icons](https://www.nuget.org/packages/Dapplo.Windows.Icons) | [![NuGet](https://img.shields.io/nuget/v/Dapplo.Windows.Icons.svg)](https://www.nuget.org/packages/Dapplo.Windows.Icons) | Icon extraction and handling |
| [Dapplo.Windows.Multimedia](https://www.nuget.org/packages/Dapplo.Windows.Multimedia) | [![NuGet](https://img.shields.io/nuget/v/Dapplo.Windows.Multimedia.svg)](https://www.nuget.org/packages/Dapplo.Windows.Multimedia) | Windows Multimedia (WinMM) wrappers |
| [Dapplo.Windows.AppRestartManager](https://www.nuget.org/packages/Dapplo.Windows.AppRestartManager) | [![NuGet](https://img.shields.io/nuget/v/Dapplo.Windows.AppRestartManager.svg)](https://www.nuget.org/packages/Dapplo.Windows.AppRestartManager) | Automatic restart registration with Windows Restart Manager |
| [Dapplo.Windows.InstallerManager](https://www.nuget.org/packages/Dapplo.Windows.InstallerManager) | [![NuGet](https://img.shields.io/nuget/v/Dapplo.Windows.InstallerManager.svg)](https://www.nuget.org/packages/Dapplo.Windows.InstallerManager) | Installer-specific Restart Manager APIs |
| [Dapplo.Windows.SystemState](https://www.nuget.org/packages/Dapplo.Windows.SystemState) | [![NuGet](https://img.shields.io/nuget/v/Dapplo.Windows.SystemState.svg)](https://www.nuget.org/packages/Dapplo.Windows.SystemState) | Power state management: sleep, hibernate, shutdown, wake timers, and power event monitoring |

### Specialized Feature Packages

| Package | NuGet | Description |
|---------|-------|-------------|
| [Dapplo.Windows.Citrix](https://www.nuget.org/packages/Dapplo.Windows.Citrix) | [![NuGet](https://img.shields.io/nuget/v/Dapplo.Windows.Citrix.svg)](https://www.nuget.org/packages/Dapplo.Windows.Citrix) | Citrix environment detection and querying |
| [Dapplo.Windows.DesktopWindowsManager](https://www.nuget.org/packages/Dapplo.Windows.DesktopWindowsManager) | [![NuGet](https://img.shields.io/nuget/v/Dapplo.Windows.DesktopWindowsManager.svg)](https://www.nuget.org/packages/Dapplo.Windows.DesktopWindowsManager) | DWM (Aero/Composition) API access |
| [Dapplo.Windows.EmbeddedBrowser](https://www.nuget.org/packages/Dapplo.Windows.EmbeddedBrowser) | [![NuGet](https://img.shields.io/nuget/v/Dapplo.Windows.EmbeddedBrowser.svg)](https://www.nuget.org/packages/Dapplo.Windows.EmbeddedBrowser) | Enhanced WebBrowser control with configurable IE version |

### Low-Level API Packages

These packages provide thin, well-typed P/Invoke wrappers and are used as building blocks by the high-level packages.

| Package | NuGet | Description |
|---------|-------|-------------|
| [Dapplo.Windows.User32](https://www.nuget.org/packages/Dapplo.Windows.User32) | [![NuGet](https://img.shields.io/nuget/v/Dapplo.Windows.User32.svg)](https://www.nuget.org/packages/Dapplo.Windows.User32) | User32.dll wrappers (windows, input, display) |
| [Dapplo.Windows.Gdi32](https://www.nuget.org/packages/Dapplo.Windows.Gdi32) | [![NuGet](https://img.shields.io/nuget/v/Dapplo.Windows.Gdi32.svg)](https://www.nuget.org/packages/Dapplo.Windows.Gdi32) | GDI32.dll and GDI+ wrappers (graphics) |
| [Dapplo.Windows.Kernel32](https://www.nuget.org/packages/Dapplo.Windows.Kernel32) | [![NuGet](https://img.shields.io/nuget/v/Dapplo.Windows.Kernel32.svg)](https://www.nuget.org/packages/Dapplo.Windows.Kernel32) | Kernel32.dll wrappers (system operations) |
| [Dapplo.Windows.Advapi32](https://www.nuget.org/packages/Dapplo.Windows.Advapi32) | [![NuGet](https://img.shields.io/nuget/v/Dapplo.Windows.Advapi32.svg)](https://www.nuget.org/packages/Dapplo.Windows.Advapi32) | Advapi32.dll wrappers (registry, services, security) |
| [Dapplo.Windows.Shell32](https://www.nuget.org/packages/Dapplo.Windows.Shell32) | [![NuGet](https://img.shields.io/nuget/v/Dapplo.Windows.Shell32.svg)](https://www.nuget.org/packages/Dapplo.Windows.Shell32) | Shell32.dll wrappers (shell integration) |
| [Dapplo.Windows.Com](https://www.nuget.org/packages/Dapplo.Windows.Com) | [![NuGet](https://img.shields.io/nuget/v/Dapplo.Windows.Com.svg)](https://www.nuget.org/packages/Dapplo.Windows.Com) | COM interop helpers |
| [Dapplo.Windows.Common](https://www.nuget.org/packages/Dapplo.Windows.Common) | [![NuGet](https://img.shields.io/nuget/v/Dapplo.Windows.Common.svg)](https://www.nuget.org/packages/Dapplo.Windows.Common) | Shared types, extensions, and utilities |

## Quick Start

```powershell
# Install only what you need
Install-Package Dapplo.Windows           # Window management
Install-Package Dapplo.Windows.Clipboard # Clipboard monitoring
Install-Package Dapplo.Windows.Input     # Keyboard/mouse hooks
Install-Package Dapplo.Windows.Dpi       # DPI awareness
```

See the [[Getting-Started]] page for code examples and setup instructions.

## Wiki Pages

| Topic | Description |
|-------|-------------|
| [[Getting-Started]] | Installation, setup, and first steps |
| [[Window-Management]] | Querying, enumerating, and manipulating windows |
| [[Clipboard]] | Monitoring and manipulating the clipboard |
| [[Input-Handling]] | Keyboard and mouse hooks and input generation |
| [[DPI-Awareness]] | Building DPI-aware Windows applications |
| [[Restart-Manager]] | Registering applications with Windows Restart Manager |
| [[System-State]] | Sleep, hibernate, shutdown, wake timers, and power event monitoring |
| [[Icon-Creation]] | Extracting and working with icons |
| [[Common-Scenarios]] | Multi-package recipes for common real-world tasks |
| [[SharedMessageWindow]] | The hidden message window powering clipboard, input, devices, sessions, and more |

## Requirements

- **Windows OS** (the library wraps native Windows APIs)
- **.NET Framework 4.6.2** or higher
- **.NET Core 3.1** or higher / **.NET 5+** (for most packages)

## Build Status

[![Build and Deploy](https://github.com/dapplo/Dapplo.Windows/actions/workflows/build.yml/badge.svg)](https://github.com/dapplo/Dapplo.Windows/actions/workflows/build.yml)
[![Coverage Status](https://coveralls.io/repos/github/dapplo/Dapplo.Windows/badge.svg?branch=master)](https://coveralls.io/github/dapplo/Dapplo.Windows?branch=master)

## License

MIT — see [LICENSE](https://github.com/dapplo/Dapplo.Windows/blob/master/LICENSE).
