# Dapplo.Windows

A comprehensive library providing Windows-specific functionality for .NET Framework and .NET Core applications.

## Overview

Dapplo.Windows is a collection of packages that provide P/Invoke wrappers and higher-level abstractions for Windows API functionality. Originally developed for [Greenshot](https://getgreenshot.org/), this library makes it easy to interact with Windows features from managed code.

## Key Features

- **Window Management**: Query and manipulate windows, monitor window events
- **Input Handling**: Hook keyboard and mouse events, generate input events
- **Clipboard Access**: Monitor and manipulate clipboard content with a reactive API
- **DPI Awareness**: Build DPI-aware applications with automatic scaling
- **GDI/User32 Interop**: Access to native Windows API functions and structures
- **Citrix Support**: Detect and interact with Citrix environments
- **Desktop Window Manager**: Access DWM (Aero) functionality

## Package Structure

The library is split into focused packages for better modularity:

| Package | Description |
|---------|-------------|
| **Dapplo.Windows** | Main package with window management and event hooking |
| **Dapplo.Windows.Clipboard** | Clipboard monitoring and manipulation |
| **Dapplo.Windows.Dpi** | DPI awareness and scaling support |
| **Dapplo.Windows.Input** | Keyboard and mouse input handling |
| **Dapplo.Windows.User32** | User32.dll API wrappers |
| **Dapplo.Windows.Gdi32** | GDI32.dll API wrappers |
| **Dapplo.Windows.Kernel32** | Kernel32.dll API wrappers |
| **Dapplo.Windows.Messages** | Windows message definitions |
| **Dapplo.Windows.Citrix** | Citrix environment detection |
| **Dapplo.Windows.DesktopWindowsManager** | DWM API access |
| **Dapplo.Windows.EmbeddedBrowser** | Enhanced WebBrowser control |
| **Dapplo.Windows.Com** | COM interop helpers |
| **Dapplo.Windows.Common** | Shared types and utilities |

## Getting Started

Install the packages you need via NuGet:

```powershell
# For window management and event hooking
Install-Package Dapplo.Windows

# For clipboard functionality
Install-Package Dapplo.Windows.Clipboard

# For DPI awareness
Install-Package Dapplo.Windows.Dpi

# For input handling
Install-Package Dapplo.Windows.Input
```

## Quick Examples

### Monitor Window Events
```csharp
using Dapplo.Windows.Desktop;

// Subscribe to window title changes
var subscription = WinEventHook.Create(WinEvents.EVENT_OBJECT_NAMECHANGE)
    .Subscribe(winEvent =>
    {
        var window = InteropWindow.FromHandle(winEvent.Handle);
        Console.WriteLine($"Window title changed: {window.GetCaption()}");
    });
```

### Monitor Clipboard
```csharp
using Dapplo.Windows.Clipboard;

// Subscribe to clipboard updates
var subscription = ClipboardNative.OnUpdate
    .Where(info => info.Formats.Contains("Text"))
    .Subscribe(info =>
    {
        using var clipboard = ClipboardNative.Access();
        var text = clipboard.GetAsString();
        Console.WriteLine($"Clipboard text: {text}");
    });
```

### Handle DPI Changes
```csharp
using Dapplo.Windows.Dpi.Forms;

// Extend DpiAwareForm for automatic DPI scaling
public class MyForm : DpiAwareForm
{
    public MyForm()
    {
        InitializeComponent();
    }
}
```

## Documentation

- [Getting Started Guide](articles/intro.md)
- [API Reference](api/index.md)
- [GitHub Repository](https://github.com/dapplo/Dapplo.Windows)

## Build Status

[![Build Status](https://dev.azure.com/Dapplo/Dapplo%20framework/_apis/build/status/dapplo.Dapplo.Windows?branchName=master)](https://dev.azure.com/Dapplo/Dapplo%20framework/_build/latest?definitionId=10&branchName=master)
[![Coverage Status](https://coveralls.io/repos/github/dapplo/Dapplo.Windows/badge.svg?branch=master)](https://coveralls.io/github/dapplo/Dapplo.Windows?branch=master)

## License

This project is licensed under the MIT License - see the [LICENSE](https://github.com/dapplo/Dapplo.Windows/blob/master/LICENSE) file for details.

