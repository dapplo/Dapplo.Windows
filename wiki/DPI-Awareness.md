# DPI Awareness

The `Dapplo.Windows.Dpi` package helps you build applications that look crisp on high-DPI and mixed-DPI multi-monitor setups. It supports both Windows Forms and WPF.

## Installation

```powershell
Install-Package Dapplo.Windows.Dpi
```

## Windows Forms

### Option 1 — Extend `DpiAwareForm` (Recommended)

The simplest approach: inherit from `DpiAwareForm` instead of `Form`.

```csharp
using Dapplo.Windows.Dpi.Forms;

public class MyForm : DpiAwareForm
{
    public MyForm()
    {
        InitializeComponent();
        // Font, layout, and bitmaps scale automatically when DPI changes.
    }
}
```

`DpiAwareForm` provides:
- Automatic layout scaling on DPI change
- Font scaling
- Bitmap / icon scaling
- Correct initial DPI when the form first opens on a non-default-DPI monitor

### Option 2 — Extend `DpiUnawareForm`

Lets the OS scale your UI by bitmap-stretching (blurry on high-DPI). Use only when you need compatibility with legacy controls.

```csharp
using Dapplo.Windows.Dpi.Forms;

public class MyForm : DpiUnawareForm
{
    // System handles scaling, but output may appear blurry.
}
```

### Option 3 — Use `DpiHandler` Manually

For full control, or when you cannot change the base class:

```csharp
using Dapplo.Windows.Dpi;
using System.Windows.Forms;

public class MyForm : Form
{
    private DpiHandler _dpiHandler;

    public MyForm()
    {
        InitializeComponent();

        _dpiHandler = DpiHandler.Create(this);

        _dpiHandler.OnDpiChanged.Subscribe(info =>
        {
            Console.WriteLine($"DPI: {info.OldDpi} → {info.NewDpi}");
            ScaleMyCustomControls(info);
        });
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing) _dpiHandler?.Dispose();
        base.Dispose(disposing);
    }
}
```

## WPF

WPF handles most DPI concerns natively, but `DpiHandler` is available for scenarios where you need to react to DPI changes explicitly:

```csharp
using Dapplo.Windows.Dpi;

public partial class MainWindow : Window
{
    private DpiHandler _dpiHandler;

    public MainWindow()
    {
        InitializeComponent();

        _dpiHandler = DpiHandler.Create(this);
        _dpiHandler.OnDpiChanged.Subscribe(info =>
        {
            // Re-render any custom drawing surfaces
        });
    }
}
```

## DPI Calculations

`DpiCalculator` converts between logical and physical pixel sizes:

```csharp
using Dapplo.Windows.Dpi;

// Scale 16 logical pixels at 120 DPI → 20 physical pixels
int physical = DpiCalculator.ScaleWithDpi(16, 120);

// Reverse: 20 physical pixels at 120 DPI → 16 logical pixels
int logical = DpiCalculator.UnscaleWithDpi(20, 120);

// Current system DPI
int systemDpi = DpiCalculator.GetDpi();
```

## Scaling Bitmaps

When you need to manually scale images with quality preservation:

```csharp
using Dapplo.Windows.Dpi;
using System.Drawing;

Bitmap original = new Bitmap("icon.png");

// Scale to the current DPI
Bitmap scaled = BitmapScaleHandler.Scale(original, DpiCalculator.GetDpi());

// Scale to a specific factor
Bitmap scaled2x = BitmapScaleHandler.Scale(original, scaleFactor: 2.0);
```

## Manifest Requirements

For the OS to enable per-monitor DPI awareness, your application manifest must declare the DPI awareness mode. Add or update `app.manifest`:

```xml
<application xmlns="urn:schemas-microsoft-com:asm.v3">
  <windowsSettings>
    <!-- Windows 10 Creators Update and later -->
    <dpiAwareness xmlns="http://schemas.microsoft.com/SMI/2016/WindowsSettings">
      PerMonitorV2
    </dpiAwareness>
    <!-- Fallback for older Windows -->
    <dpiAware xmlns="http://schemas.microsoft.com/SMI/2005/WindowsSettings">
      true/pm
    </dpiAware>
  </windowsSettings>
</application>
```

Or set it in code before creating any window:

```csharp
using Dapplo.Windows.Dpi;

// Call as early as possible — before any window is created
DpiHandler.SetProcessDpiAwareness(DpiAwarenessContext.PerMonitorAwareV2);
```

## Multi-Monitor Scenarios

Each monitor can have a different DPI. Handle the case where a window is dragged between monitors:

```csharp
_dpiHandler.OnDpiChanged.Subscribe(info =>
{
    // info.NewDpi reflects the DPI of the monitor the window moved to
    RescaleLayout(info.NewDpi);
});
```

## Best Practices

- **Declare the manifest** — without it, the OS virtualizes DPI and all coordinates appear as 96 DPI regardless.
- **Avoid hard-coded pixel sizes** — use `DpiCalculator.ScaleWithDpi()` to compute sizes at runtime.
- **Scale images** with `BitmapScaleHandler` rather than letting the OS stretch them.
- **Prefer `DpiAwareForm`** over manual handling in Windows Forms applications.
- **Test on a secondary monitor** at a different DPI to catch per-monitor issues early.

## See Also

- [[Getting-Started]]
- [[Common-Scenarios]]
- [Microsoft Per-Monitor DPI Awareness Docs](https://docs.microsoft.com/en-us/windows/win32/hidpi/high-dpi-desktop-application-development-on-windows)
