# Dapplo.Windows API Reference

This section contains the complete API reference documentation for all Dapplo.Windows packages.

## Package Overview

The API is organized into the following packages:

### Core Packages

#### Dapplo.Windows
The main package providing window management and event hooking functionality.

**Key Namespaces:**
- `Dapplo.Windows.Desktop` - Window querying and manipulation (`InteropWindow`, `WinEventHook`)
- `Dapplo.Windows.App` - Application-level functionality
- `Dapplo.Windows.Extensions` - Extension methods for common operations

**Common Classes:**
- `InteropWindow` - Represents a window with rich querying and manipulation capabilities
- `InteropWindowQuery` - Query and enumerate windows
- `WinEventHook` - Hook into Windows events (creation, destruction, title changes, etc.)

#### Dapplo.Windows.Clipboard
Clipboard monitoring and manipulation with a reactive API.

**Key Namespaces:**
- `Dapplo.Windows.Clipboard` - Clipboard access and monitoring

**Common Classes:**
- `ClipboardNative` - Main entry point for clipboard operations
- `ClipboardMonitor` - Monitor clipboard changes (deprecated, use `ClipboardNative.OnUpdate`)
- Extension methods for various clipboard formats (string, bytes, files, streams)

#### Dapplo.Windows.Dpi
DPI awareness and scaling support for building resolution-independent applications.

**Key Namespaces:**
- `Dapplo.Windows.Dpi` - Core DPI functionality
- `Dapplo.Windows.Dpi.Forms` - Windows Forms DPI support
- `Dapplo.Windows.Dpi.Wpf` - WPF DPI support

**Common Classes:**
- `DpiHandler` - Handles DPI changes for controls and forms
- `DpiAwareForm` - Base form class with automatic DPI scaling
- `DpiCalculator` - DPI scaling calculations
- `BitmapScaleHandler` - Scale bitmaps for different DPI values

#### Dapplo.Windows.Input
Keyboard and mouse input monitoring and generation.

**Key Namespaces:**
- `Dapplo.Windows.Input.Keyboard` - Keyboard input
- `Dapplo.Windows.Input.Mouse` - Mouse input

**Common Classes:**
- `KeyboardHook` - Low-level keyboard event hook
- `KeyboardInputGenerator` - Generate keyboard input
- `MouseHook` - Low-level mouse event hook
- `MouseInputGenerator` - Generate mouse input

### Low-Level API Packages

#### Dapplo.Windows.User32
P/Invoke wrappers for User32.dll functions.

**Key Namespaces:**
- `Dapplo.Windows.User32` - User32 API functions
- `Dapplo.Windows.User32.Enums` - User32 enumerations
- `Dapplo.Windows.User32.Structs` - User32 structures

**Common Classes:**
- `User32Api` - Static class with User32 function declarations
- Various structs: `WindowInfo`, `WindowPlacement`, `MonitorInfo`, etc.

#### Dapplo.Windows.Gdi32
P/Invoke wrappers for GDI32.dll functions.

**Key Namespaces:**
- `Dapplo.Windows.Gdi32` - GDI32 API functions
- `Dapplo.Windows.Gdi32.Enums` - GDI32 enumerations
- `Dapplo.Windows.Gdi32.Structs` - GDI32 structures

**Common Classes:**
- `Gdi32Api` - Static class with GDI32 function declarations
- `SafeHandle` implementations for GDI objects

#### Dapplo.Windows.Kernel32
P/Invoke wrappers for Kernel32.dll functions.

**Key Namespaces:**
- `Dapplo.Windows.Kernel32` - Kernel32 API functions

**Common Classes:**
- `Kernel32Api` - Static class with Kernel32 function declarations
- Process and module related functionality

### Specialized Packages

#### Dapplo.Windows.Messages
Windows message constants and helpers.

**Key Namespaces:**
- `Dapplo.Windows.Messages` - Message constants and utilities

**Common Classes:**
- `WindowsMessage` - Windows message definitions
- `WinProcHandler` - Handle window messages

#### Dapplo.Windows.Citrix
Citrix environment detection and session information.

**Key Namespaces:**
- `Dapplo.Windows.Citrix` - Citrix detection

**Common Classes:**
- `WinFrame` - Detect and query Citrix sessions

#### Dapplo.Windows.DesktopWindowsManager
Desktop Window Manager (DWM/Aero) functionality.

**Key Namespaces:**
- `Dapplo.Windows.DesktopWindowsManager` - DWM API

**Common Classes:**
- `DwmApi` - Desktop Window Manager functions
- Thumbnail and Aero effects support

#### Dapplo.Windows.EmbeddedBrowser
Enhanced WebBrowser control for Windows Forms.

**Key Namespaces:**
- `Dapplo.Windows.EmbeddedBrowser` - Enhanced browser control

**Common Classes:**
- `ExtendedWebBrowser` - Enhanced WebBrowser control
- `InternetExplorerVersion` - Manage embedded IE version

#### Dapplo.Windows.Common
Shared types and utilities used across packages.

**Key Namespaces:**
- `Dapplo.Windows.Common` - Common types
- `Dapplo.Windows.Common.Structs` - Shared structures

**Common Types:**
- `NativeRect`, `NativePoint`, `NativeSize` - Native geometry types
- Extension methods for common operations

## API Documentation

The detailed API documentation is generated from the source code XML comments. Use the navigation menu to browse through the namespaces and types.

### Conventions

- **Extension Methods**: Many packages provide extension methods for easier API usage
- **Observable Pattern**: Input and clipboard packages use Reactive Extensions (System.Reactive)
- **SafeHandles**: Low-level packages use SafeHandle for proper resource management
- **Fluent API**: Many APIs support method chaining for cleaner code

## Examples

For usage examples, see the [Getting Started Guide](../articles/intro.md).

## Source Code

The complete source code with additional examples is available on [GitHub](https://github.com/dapplo/Dapplo.Windows).
