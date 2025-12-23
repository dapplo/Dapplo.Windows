# Getting Started with Dapplo.Windows

This guide will help you get started with Dapplo.Windows and its various packages.

## Installation

Dapplo.Windows is distributed as multiple NuGet packages. Install only the packages you need for your application.

### Core Packages

```powershell
# Main package with window management
Install-Package Dapplo.Windows

# Clipboard functionality
Install-Package Dapplo.Windows.Clipboard

# DPI awareness and scaling
Install-Package Dapplo.Windows.Dpi

# Keyboard and mouse input
Install-Package Dapplo.Windows.Input
```

### Low-Level API Packages

```powershell
# User32 API wrappers
Install-Package Dapplo.Windows.User32

# GDI32 API wrappers
Install-Package Dapplo.Windows.Gdi32

# Kernel32 API wrappers
Install-Package Dapplo.Windows.Kernel32
```

## Common Usage Scenarios

### Working with Windows

The main `Dapplo.Windows` package provides the `InteropWindow` class for working with windows.

#### Get Information About a Window

```csharp
using Dapplo.Windows.Desktop;
using Dapplo.Windows.User32;

// Get the foreground window
var handle = User32Api.GetForegroundWindow();
var window = InteropWindow.FromHandle(handle);

// Get window information
window.Fill(); // Populate all properties
Console.WriteLine($"Title: {window.Caption}");
Console.WriteLine($"Class: {window.Classname}");
Console.WriteLine($"Bounds: {window.Bounds}");
Console.WriteLine($"Visible: {window.IsVisible()}");
```

#### Enumerate All Windows

```csharp
using Dapplo.Windows.Desktop;
using Dapplo.Windows.Enums;

// Get all visible windows
var windows = InteropWindowQuery.GetTopLevelWindows()
    .Where(w => w.IsVisible())
    .ToList();

foreach (var window in windows)
{
    Console.WriteLine($"{window.Caption} ({window.Classname})");
}
```

#### Monitor Window Events

```csharp
using Dapplo.Windows.Desktop;

// Subscribe to window creation events
var subscription = WinEventHook.Create(WinEvents.EVENT_OBJECT_CREATE)
    .Subscribe(winEvent =>
    {
        var window = InteropWindow.FromHandle(winEvent.Handle);
        Console.WriteLine($"Window created: {window.GetCaption()}");
    });

// Don't forget to dispose when done
subscription.Dispose();
```

### Clipboard Monitoring

The `Dapplo.Windows.Clipboard` package uses Reactive Extensions for clipboard monitoring.

#### Monitor Clipboard Changes

```csharp
using Dapplo.Windows.Clipboard;
using System.Reactive.Linq;

// Subscribe to clipboard updates
var subscription = ClipboardNative.OnUpdate
    .Subscribe(info =>
    {
        Console.WriteLine($"Clipboard changed. Formats: {string.Join(", ", info.Formats)}");
    });
```

#### Filter by Format

```csharp
using Dapplo.Windows.Clipboard;

// Only react to text clipboard changes
var subscription = ClipboardNative.OnUpdate
    .Where(info => info.Formats.Contains("Text"))
    .Subscribe(info =>
    {
        using var clipboard = ClipboardNative.Access();
        var text = clipboard.GetAsString();
        Console.WriteLine($"Clipboard text: {text}");
    });
```

#### Access Clipboard Content

```csharp
using Dapplo.Windows.Clipboard;

// Read clipboard content
using (var clipboard = ClipboardNative.Access())
{
    if (clipboard.AvailableFormats().Contains("Text"))
    {
        var text = clipboard.GetAsString();
        Console.WriteLine(text);
    }
}

// Write to clipboard
using (var clipboard = ClipboardNative.Access())
{
    clipboard.SetAsUnicodeString("Hello, World!");
}
```

### DPI Awareness

The `Dapplo.Windows.Dpi` package helps create DPI-aware applications.

#### Windows Forms DPI Support

```csharp
using Dapplo.Windows.Dpi.Forms;

// Option 1: Extend DpiAwareForm
public class MyForm : DpiAwareForm
{
    public MyForm()
    {
        InitializeComponent();
        // Form will automatically scale with DPI changes
    }
}

// Option 2: Use DpiHandler
public class MyForm : Form
{
    private DpiHandler _dpiHandler;
    
    public MyForm()
    {
        InitializeComponent();
        _dpiHandler = DpiHandler.Create(this);
        _dpiHandler.OnDpiChanged.Subscribe(dpiInfo =>
        {
            // Handle DPI changes manually
            Console.WriteLine($"DPI changed to {dpiInfo.NewDpi}");
        });
    }
}
```

#### DPI Calculations

```csharp
using Dapplo.Windows.Dpi;

// Scale a size with DPI
var scaledSize = DpiCalculator.ScaleWithDpi(16, 120); // Scale 16px at 120 DPI
Console.WriteLine(scaledSize); // Output: 20

// Unscale a size
var originalSize = DpiCalculator.UnscaleWithDpi(20, 120);
Console.WriteLine(originalSize); // Output: 16
```

### Keyboard and Mouse Input

The `Dapplo.Windows.Input` package provides input monitoring and generation.

#### Monitor Keyboard Input

```csharp
using Dapplo.Windows.Input.Keyboard;
using System.Reactive.Linq;

// Create keyboard hook
var keyboardHook = KeyboardHook.Create();

// Subscribe to key events
var subscription = keyboardHook.KeyboardEvents
    .Where(e => e.Key == VirtualKeyCode.A && e.IsDown)
    .Subscribe(e =>
    {
        Console.WriteLine("'A' key pressed");
    });

// Clean up
subscription.Dispose();
keyboardHook.Dispose();
```

#### Generate Keyboard Input

```csharp
using Dapplo.Windows.Input.Keyboard;

// Type text
KeyboardInputGenerator.TypeText("Hello, World!");

// Press key combination
KeyboardInputGenerator.KeyCombinationPress(VirtualKeyCode.Control, VirtualKeyCode.C);
```

#### Monitor Mouse Input

```csharp
using Dapplo.Windows.Input.Mouse;

// Create mouse hook
var mouseHook = MouseHook.Create();

// Subscribe to mouse events
var subscription = mouseHook.MouseEvents
    .Where(e => e.Button == MouseButtons.Left && e.IsButtonDown)
    .Subscribe(e =>
    {
        Console.WriteLine($"Left click at {e.Point}");
    });
```

### Citrix Detection

The `Dapplo.Windows.Citrix` package detects Citrix environments.

```csharp
using Dapplo.Windows.Citrix;

if (WinFrame.IsAvailable)
{
    var clientHostname = WinFrame.QuerySessionInformation(InfoClasses.ClientName);
    Console.WriteLine($"Running in Citrix, client: {clientHostname}");
}
```

### Embedded Browser

The `Dapplo.Windows.EmbeddedBrowser` package provides an enhanced WebBrowser control.

```csharp
using Dapplo.Windows.EmbeddedBrowser;

// Set IE version for embedded browser
InternetExplorerVersion.ChangeEmbeddedVersion();

// Use ExtendedWebBrowser control in your forms
// (Drag and drop from toolbox or instantiate programmatically)
```

## Best Practices

### Resource Cleanup

Always dispose of subscriptions and hooks when you're done:

```csharp
// Use using statements
using (var clipboard = ClipboardNative.Access())
{
    // Work with clipboard
}

// Or explicit disposal
var subscription = ClipboardNative.OnUpdate.Subscribe(...);
// Later...
subscription.Dispose();
```

### Thread Safety

Many Windows APIs must be called from specific threads:

```csharp
using System.Reactive.Concurrency;

// Ensure UI thread execution
var subscription = ClipboardNative.OnUpdate
    .ObserveOn(SynchronizationContext.Current)
    .Subscribe(info =>
    {
        // This runs on the UI thread
        UpdateUI(info);
    });
```

### Error Handling

Windows APIs can fail. Handle errors appropriately:

```csharp
try
{
    using var clipboard = ClipboardNative.Access();
    var text = clipboard.GetAsString();
}
catch (ClipboardAccessDeniedException ex)
{
    Console.WriteLine("Clipboard access denied: " + ex.Message);
}
```

## Next Steps

- Explore the [API Reference](../api/index.md) for detailed class documentation
- Check out the example projects in the source repository
- Review the test projects for more usage examples

## Need Help?

- [GitHub Issues](https://github.com/dapplo/Dapplo.Windows/issues) - Report bugs or request features
- [GitHub Repository](https://github.com/dapplo/Dapplo.Windows) - View source code and examples
