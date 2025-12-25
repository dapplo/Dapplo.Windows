# Dapplo.Windows

A comprehensive .NET library providing Windows-specific functionality for .NET Framework and .NET Core applications.

## Overview

Dapplo.Windows is a collection of packages that provide P/Invoke wrappers and higher-level abstractions for Windows API functionality. Originally developed for [Greenshot](https://getgreenshot.org/), this library makes it easy to interact with Windows features from managed code.

## Build Status & NuGet Packages

[![Build and Deploy](https://github.com/dapplo/Dapplo.Windows/actions/workflows/build.yml/badge.svg)](https://github.com/dapplo/Dapplo.Windows/actions/workflows/build.yml)
[![codecov](https://codecov.io/gh/dapplo/Dapplo.Windows/branch/master/graph/badge.svg)](https://codecov.io/gh/dapplo/Dapplo.Windows)

### Core Packages

| Package | NuGet | Description |
|---------|-------|-------------|
| Dapplo.Windows | [![NuGet](https://img.shields.io/nuget/v/Dapplo.Windows.svg)](https://www.nuget.org/packages/Dapplo.Windows) | Window management and event hooking |
| Dapplo.Windows.Clipboard | [![NuGet](https://img.shields.io/nuget/v/Dapplo.Windows.Clipboard.svg)](https://www.nuget.org/packages/Dapplo.Windows.Clipboard) | Clipboard monitoring and manipulation |
| Dapplo.Windows.Dpi | [![NuGet](https://img.shields.io/nuget/v/Dapplo.Windows.Dpi.svg)](https://www.nuget.org/packages/Dapplo.Windows.Dpi) | DPI awareness and scaling |
| Dapplo.Windows.Input | [![NuGet](https://img.shields.io/nuget/v/Dapplo.Windows.Input.svg)](https://www.nuget.org/packages/Dapplo.Windows.Input) | Keyboard and mouse input |

### Low-Level API Packages

| Package | NuGet | Description |
|---------|-------|-------------|
| Dapplo.Windows.User32 | [![NuGet](https://img.shields.io/nuget/v/Dapplo.Windows.User32.svg)](https://www.nuget.org/packages/Dapplo.Windows.User32) | User32.dll wrappers |
| Dapplo.Windows.Gdi32 | [![NuGet](https://img.shields.io/nuget/v/Dapplo.Windows.Gdi32.svg)](https://www.nuget.org/packages/Dapplo.Windows.Gdi32) | GDI32.dll wrappers |
| Dapplo.Windows.Kernel32 | [![NuGet](https://img.shields.io/nuget/v/Dapplo.Windows.Kernel32.svg)](https://www.nuget.org/packages/Dapplo.Windows.Kernel32) | Kernel32.dll wrappers |

### Specialized Packages

| Package | NuGet | Description |
|---------|-------|-------------|
| Dapplo.Windows.Citrix | [![NuGet](https://img.shields.io/nuget/v/Dapplo.Windows.Citrix.svg)](https://www.nuget.org/packages/Dapplo.Windows.Citrix) | Citrix environment detection |
| Dapplo.Windows.DesktopWindowsManager | [![NuGet](https://img.shields.io/nuget/v/Dapplo.Windows.DesktopWindowsManager.svg)](https://www.nuget.org/packages/Dapplo.Windows.DesktopWindowsManager) | DWM (Aero) functionality |
| Dapplo.Windows.EmbeddedBrowser | [![NuGet](https://img.shields.io/nuget/v/Dapplo.Windows.EmbeddedBrowser.svg)](https://www.nuget.org/packages/Dapplo.Windows.EmbeddedBrowser) | Enhanced WebBrowser control |
| Dapplo.Windows.Messages | [![NuGet](https://img.shields.io/nuget/v/Dapplo.Windows.Messages.svg)](https://www.nuget.org/packages/Dapplo.Windows.Messages) | Windows message definitions |
| Dapplo.Windows.Com | [![NuGet](https://img.shields.io/nuget/v/Dapplo.Windows.Com.svg)](https://www.nuget.org/packages/Dapplo.Windows.Com) | COM interop helpers |
| Dapplo.Windows.Common | [![NuGet](https://img.shields.io/nuget/v/Dapplo.Windows.Common.svg)](https://www.nuget.org/packages/Dapplo.Windows.Common) | Shared types and utilities |

## Features

### Window Management
- Query window properties (title, bounds, class name, process, etc.)
- Enumerate windows with filtering
- Monitor window events (creation, destruction, title changes, etc.)
- Manipulate windows (show, hide, minimize, maximize, move, resize)

### Input Handling
- Low-level keyboard hooks using Reactive Extensions
- Low-level mouse hooks using Reactive Extensions
- Generate keyboard input (type text, key combinations)
- Generate mouse input (move, click, scroll)
- Key combination and sequence handlers

### Clipboard
- Monitor clipboard changes with Reactive Extensions
- Access clipboard content in various formats (text, files, images, streams)
- Set clipboard content
- Delayed rendering support
- Control Windows Cloud Clipboard and Clipboard History behavior

### DPI Awareness
- DPI-aware forms for Windows Forms
- DPI handlers for custom scaling logic
- DPI calculations and conversions
- Bitmap scaling with quality preservation
- Support for both Windows Forms and WPF

### Native API Access
- Comprehensive User32 API wrappers
- GDI32 graphics API wrappers
- Kernel32 system API wrappers
- Proper SafeHandle usage for resource management
- Well-defined structs and enums

### Additional Features
- Citrix session detection and querying
- Desktop Window Manager (DWM) integration
- Enhanced WebBrowser control with configurable IE version
- Software installation enumeration
- Icon extraction
- And much more...

## Quick Start

### Installation

```powershell
Install-Package Dapplo.Windows
Install-Package Dapplo.Windows.Clipboard
Install-Package Dapplo.Windows.Dpi
Install-Package Dapplo.Windows.Input
```

### Examples

#### Monitor Window Title Changes

```csharp
using Dapplo.Windows.Desktop;

var subscription = WinEventHook.Create(WinEvents.EVENT_OBJECT_NAMECHANGE)
    .Subscribe(winEvent =>
    {
        var window = InteropWindow.FromHandle(winEvent.Handle);
        Console.WriteLine($"Window title changed: {window.GetCaption()}");
    });
```

#### Monitor Clipboard

```csharp
using Dapplo.Windows.Clipboard;
using System.Reactive.Linq;

var subscription = ClipboardNative.OnUpdate
    .Where(info => info.Formats.Contains("Text"))
    .Subscribe(info =>
    {
        using var clipboard = ClipboardNative.Access();
        var text = clipboard.GetAsString();
        Console.WriteLine($"Clipboard: {text}");
    });
```

#### Control Cloud Clipboard and History

```csharp
using Dapplo.Windows.Clipboard;

// Prevent sensitive data from being saved to clipboard history or cloud
using (var clipboard = ClipboardNative.Access())
{
    clipboard.SetAsUnicodeString("MyPassword123!");
    clipboard.SetCloudClipboardOptions(
        canIncludeInHistory: false,
        canUploadToCloud: false
    );
}
```

#### Create DPI-Aware Form

```csharp
using Dapplo.Windows.Dpi.Forms;

public class MyForm : DpiAwareForm
{
    public MyForm()
    {
        InitializeComponent();
        // Form automatically scales with DPI changes
    }
}
```

#### Hook Keyboard Input

```csharp
using Dapplo.Windows.Input.Keyboard;
using System.Reactive.Linq;

var keyboardHook = KeyboardHook.Create();
var subscription = keyboardHook.KeyboardEvents
    .Where(e => e.Key == VirtualKeyCode.A && e.IsDown)
    .Subscribe(e => Console.WriteLine("'A' pressed"));
```

## Documentation

- **[Full Documentation](http://www.dapplo.net/Dapplo.Windows)** - Complete guides and API reference
- **[Getting Started](doc/articles/intro.md)** - Quick start guide with examples
- **[API Reference](doc/api/index.md)** - Detailed API documentation

## Examples

The repository includes example projects:
- **Dapplo.Windows.Example.ConsoleDemo** - Console application examples
- **Dapplo.Windows.Example.FormsExample** - Windows Forms examples
- **Dapplo.Windows.Example.WpfExample** - WPF application examples

## Requirements

- .NET Framework 4.6.2 or higher
- .NET Core 3.1 or higher / .NET 5+ (for most packages)
- Windows operating system

## Contributing

Contributions are welcome! Please feel free to submit issues and pull requests.

## License

This project is licensed under the MIT License - see the [LICENSE](LICENSE) file for details.

## Acknowledgments

Originally developed for [Greenshot](https://getgreenshot.org/), this library has been extracted into a standalone project for easier maintenance and reuse.
