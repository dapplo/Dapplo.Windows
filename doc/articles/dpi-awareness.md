# DPI Awareness Guide

This guide covers building DPI-aware applications using the `Dapplo.Windows.Dpi` package.

## Overview

The DPI package helps you create applications that properly scale on high-DPI displays. It provides support for both Windows Forms and WPF applications.

## Installation

```powershell
Install-Package Dapplo.Windows.Dpi
```

## Windows Forms DPI Support

### Option 1: DPI-Aware Form (Recommended)

The easiest way to add DPI awareness is to extend `DpiAwareForm`:

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

This provides:
- Automatic scaling when DPI changes
- Bitmap scaling support
- Font scaling
- Control repositioning

### Option 2: DPI-Unaware Form

If you want the system to handle scaling (bitmap stretching):

```csharp
using Dapplo.Windows.Dpi.Forms;

public class MyForm : DpiUnawareForm
{
    public MyForm()
    {
        InitializeComponent();
        // System handles scaling via bitmap stretching
    }
}
```

### Option 3: Manual DPI Handling

For more control, use a `DpiHandler`:

```csharp
using Dapplo.Windows.Dpi;
using Dapplo.Windows.Dpi.Forms;
using System.Windows.Forms;

public class MyForm : Form
{
    private DpiHandler _dpiHandler;

    public MyForm()
    {
        InitializeComponent();
        
        // Create DPI handler for this form
        _dpiHandler = DpiHandler.Create(this);
        
        // Subscribe to DPI change events
        _dpiHandler.OnDpiChanged.Subscribe(dpiChangeInfo =>
        {
            HandleDpiChange(dpiChangeInfo);
        });
    }

    private void HandleDpiChange(DpiChangeInfo dpiChangeInfo)
    {
        Console.WriteLine($"DPI changed from {dpiChangeInfo.OldDpi} to {dpiChangeInfo.NewDpi}");
        
        // Scale custom elements
        ScaleCustomControls(dpiChangeInfo);
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _dpiHandler?.Dispose();
        }
        base.Dispose(disposing);
    }
}
```

## WPF DPI Support

### Window DPI Extensions

WPF has built-in DPI support, but you can enhance it:

```csharp
using Dapplo.Windows.Dpi.Wpf;
using System.Windows;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
        
        // Get current DPI
        var dpi = this.GetDpi();
        Console.WriteLine($"Current DPI: {dpi}");
        
        // Subscribe to DPI changes
        var dpiHandler = this.AttachDpiHandler();
        dpiHandler.OnDpiChanged.Subscribe(dpiInfo =>
        {
            Console.WriteLine($"DPI changed to {dpiInfo.NewDpi}");
        });
    }
}
```

## DPI Calculations

### Scale Values

```csharp
using Dapplo.Windows.Dpi;

// Scale a value from 96 DPI to current DPI
int size96 = 16; // Original size at 96 DPI
int currentDpi = 120;

int scaledSize = DpiCalculator.ScaleWithDpi(size96, currentDpi);
Console.WriteLine($"Scaled size: {scaledSize}"); // Output: 20
```

### Unscale Values

```csharp
using Dapplo.Windows.Dpi;

// Convert a scaled value back to 96 DPI
int scaledSize = 20;
int currentDpi = 120;

int originalSize = DpiCalculator.UnscaleWithDpi(scaledSize, currentDpi);
Console.WriteLine($"Original size: {originalSize}"); // Output: 16
```

### Scale Structures

```csharp
using Dapplo.Windows.Dpi;
using Dapplo.Windows.Common.Structs;

int currentDpi = 144;

// Scale NativeSize
var size = new NativeSize(16, 16);
var scaledSize = DpiCalculator.ScaleWithDpi(size, currentDpi);
Console.WriteLine($"Scaled: {scaledSize.Width}x{scaledSize.Height}"); // 24x24

// Scale NativeRect
var rect = new NativeRect(0, 0, 100, 100);
var scaledRect = DpiCalculator.ScaleWithDpi(rect, currentDpi);
```

### Calculate DPI from Scale Factor

```csharp
using Dapplo.Windows.Dpi;

// 150% scaling = 144 DPI
int dpi = DpiCalculator.DpiFromScaleFactor(1.5);
Console.WriteLine($"DPI: {dpi}"); // Output: 144

// Reverse: Get scale factor from DPI
double scaleFactor = DpiCalculator.ScaleFactorFromDpi(144);
Console.WriteLine($"Scale factor: {scaleFactor}"); // Output: 1.5
```

## Bitmap Scaling

### Scale Bitmaps with Quality

```csharp
using Dapplo.Windows.Dpi;
using System.Drawing;

// Create bitmap scale handler
var scaleHandler = BitmapScaleHandler.Create<Bitmap>(
    source => (Bitmap)source.Clone(),
    (source, size, interpolationMode) => 
    {
        var scaled = new Bitmap(size.Width, size.Height);
        using (var graphics = Graphics.FromImage(scaled))
        {
            graphics.InterpolationMode = interpolationMode;
            graphics.DrawImage(source, 0, 0, size.Width, size.Height);
        }
        return scaled;
    },
    bitmap => bitmap.Dispose()
);

// Original bitmap at 96 DPI
var originalBitmap = new Bitmap("icon_16x16.png");

// Scale for 144 DPI
var scaledBitmap = scaleHandler.ScaleWithDpi(originalBitmap, 144);
```

### Automatic Bitmap Scaling in Forms

When using `DpiAwareForm`, bitmaps in ImageLists are automatically scaled:

```csharp
using Dapplo.Windows.Dpi.Forms;
using System.Windows.Forms;

public class MyForm : DpiAwareForm
{
    private ImageList _imageList;

    public MyForm()
    {
        InitializeComponent();
        
        // Add images at 96 DPI
        _imageList = new ImageList { ImageSize = new Size(16, 16) };
        _imageList.Images.Add(Image.FromFile("icon_16x16.png"));
        
        // Images will automatically scale when DPI changes
    }
}
```

## Working with Different DPI Contexts

### Set Process DPI Awareness

```csharp
using Dapplo.Windows.Dpi;
using Dapplo.Windows.Dpi.Enums;

// Set DPI awareness for the entire process (call early in Main)
DpiHandler.SetProcessDpiAwareness(DpiAwareness.PerMonitorAwareV2);
```

### DPI Awareness Levels

- **Unaware**: Application is scaled by the system (bitmap stretching)
- **SystemAware**: Single DPI for all monitors
- **PerMonitorAware**: Different DPI per monitor (Windows 8.1+)
- **PerMonitorAwareV2**: Enhanced per-monitor DPI (Windows 10 1703+)

### Check Current DPI

```csharp
using Dapplo.Windows.Dpi;
using Dapplo.Windows.User32;

// Get DPI for specific monitor
var monitor = User32Api.MonitorFromWindow(windowHandle, MonitorFrom.DefaultToNearest);
uint dpiX, dpiY;
DpiHandler.GetDpiForMonitor(monitor, MonitorDpiType.EffectiveDpi, out dpiX, out dpiY);

Console.WriteLine($"Monitor DPI: {dpiX}x{dpiY}");
```

## DPI Change Events

### Subscribe to Changes

```csharp
using Dapplo.Windows.Dpi;

var dpiHandler = DpiHandler.Create(control);

var subscription = dpiHandler.OnDpiChanged.Subscribe(dpiChangeInfo =>
{
    Console.WriteLine($"Old DPI: {dpiChangeInfo.OldDpi}");
    Console.WriteLine($"New DPI: {dpiChangeInfo.NewDpi}");
    Console.WriteLine($"Scale Factor: {dpiChangeInfo.ScaleFactor}");
    
    // Update UI elements
    UpdateFonts(dpiChangeInfo);
    UpdateImages(dpiChangeInfo);
});

// Don't forget to dispose
subscription.Dispose();
```

### Handling DPI Changes in Custom Controls

```csharp
using Dapplo.Windows.Dpi;
using System.Windows.Forms;

public class DpiAwareButton : Button
{
    private DpiHandler _dpiHandler;
    private int _baseFontSize = 9;

    protected override void OnHandleCreated(EventArgs e)
    {
        base.OnHandleCreated(e);
        
        _dpiHandler = DpiHandler.Create(this);
        _dpiHandler.OnDpiChanged.Subscribe(dpiInfo =>
        {
            // Scale font
            float newSize = DpiCalculator.ScaleWithDpi(_baseFontSize, dpiInfo.NewDpi);
            Font = new Font(Font.FontFamily, newSize);
        });
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _dpiHandler?.Dispose();
        }
        base.Dispose(disposing);
    }
}
```

## Common DPI Values

| DPI  | Scale Factor | Common Name |
|------|--------------|-------------|
| 96   | 100%         | Standard    |
| 120  | 125%         | Medium      |
| 144  | 150%         | Large       |
| 192  | 200%         | Extra Large |

## Best Practices

### 1. Set DPI Awareness Early

Call `SetProcessDpiAwareness` as early as possible in your application:

```csharp
using Dapplo.Windows.Dpi;
using Dapplo.Windows.Dpi.Enums;

static class Program
{
    [STAThread]
    static void Main()
    {
        // Set DPI awareness before any UI is created
        DpiHandler.SetProcessDpiAwareness(DpiAwareness.PerMonitorAwareV2);
        
        Application.EnableVisualStyles();
        Application.SetCompatibleTextRenderingDefault(false);
        Application.Run(new MainForm());
    }
}
```

### 2. Use Vector Graphics When Possible

Vector graphics scale better than bitmaps:

```csharp
// Prefer fonts/text over bitmap icons
// Use SVG or drawing APIs instead of fixed-size images
```

### 3. Provide Multiple Bitmap Sizes

For best quality, provide bitmaps at multiple DPI levels:

```csharp
// icon_16x16.png  (96 DPI)
// icon_20x20.png  (120 DPI)
// icon_24x24.png  (144 DPI)
// icon_32x32.png  (192 DPI)
```

### 4. Test at Different DPI Settings

Test your application at various DPI settings:
- 100% (96 DPI)
- 125% (120 DPI)
- 150% (144 DPI)
- 200% (192 DPI)

### 5. Handle DPI Changes Gracefully

Users can change DPI without restarting:

```csharp
_dpiHandler.OnDpiChanged.Subscribe(dpiInfo =>
{
    // Reload images at new DPI
    // Recalculate layouts
    // Update font sizes
    // Refresh UI
});
```

## Application Manifest

Add DPI awareness to your app.manifest:

```xml
<?xml version="1.0" encoding="utf-8"?>
<assembly manifestVersion="1.0" xmlns="urn:schemas-microsoft-com:asm.v1">
  <application xmlns="urn:schemas-microsoft-com:asm.v3">
    <windowsSettings>
      <dpiAware xmlns="http://schemas.microsoft.com/SMI/2005/WindowsSettings">true/pm</dpiAware>
      <dpiAwareness xmlns="http://schemas.microsoft.com/SMI/2016/WindowsSettings">PerMonitorV2, PerMonitor</dpiAwareness>
    </windowsSettings>
  </application>
</assembly>
```

## Troubleshooting

### Blurry Text or Controls

If controls appear blurry, ensure:
1. DPI awareness is set correctly
2. You're inheriting from `DpiAwareForm`
3. The application manifest is configured

### Incorrect Scaling

If scaling is incorrect:
1. Check the base DPI (should be 96)
2. Verify DPI calculations
3. Ensure events are properly subscribed

### Performance Issues

If DPI changes are slow:
1. Cache scaled resources
2. Use async loading for images
3. Optimize layout calculations

## See Also

- [API Reference](../api/index.md)
- [Getting Started](intro.md)
- [Windows Forms Examples](https://github.com/dapplo/Dapplo.Windows/tree/master/src/Dapplo.Windows.Example.FormsExample)
- [Microsoft DPI Documentation](https://docs.microsoft.com/windows/win32/hidpi/high-dpi-desktop-application-development-on-windows)
