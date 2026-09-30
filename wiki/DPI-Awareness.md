# DPI awareness

Packages **Dapplo.Windows.Dpi**, **Dapplo.Windows.Forms** and **Dapplo.Windows.Wpf**. Full version:
[DPI awareness](https://www.dapplo.net/Dapplo.Windows/articles/dpi-awareness.html).

First make the process Per Monitor V2 aware with an application manifest (and for Windows Forms the high DPI mode of
WinForms); nothing else helps a DPI-unaware process. Without a manifest, call this before the first window:

<!-- sample: DpiSamples.EnableDpiAware -->
```csharp
// Prefer the manifest. If that's not possible, call this before any window is created:
// it tries Per Monitor V2, then Per Monitor, and returns if the process is DPI aware afterwards.
if (!NativeDpiMethods.EnableDpiAware())
{
    Console.WriteLine("The process is not DPI aware, Windows scales it as a bitmap");
}
```

## Windows Forms

`DpiAwareForm` creates its window Per Monitor V2 aware and lets WinForms scale the form on a DPI change. It doesn't
scale anything beyond what WinForms does: when WinForms doesn't handle the change (no high DPI mode configured), only
the bounds follow the rectangle Windows suggests. `FormDpiHandler.OnDpiChanged` reports the DPI for everything else.

<!-- sample: DpiSamples.DpiAwareForm -->
```csharp
public class MainForm : DpiAwareForm
{
    private readonly IDisposable _dpiSubscription;

    public MainForm()
    {
        // FormDpiHandler is created by DpiAwareForm, OnDpiChanged fires with the first DPI when the handle is created,
        // and for every change. WinForms scales the fonts and controls itself (Per Monitor V2), this is for everything else.
        _dpiSubscription = FormDpiHandler.OnDpiChanged.Subscribe(info =>
            Console.WriteLine($"DPI {info.PreviousDpi} -> {info.NewDpi}, scale factor {DpiCalculator.DpiScaleFactor(info.NewDpi)}"));
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _dpiSubscription.Dispose();
        }
        base.Dispose(disposing);
    }
}
```

<!-- sample: DpiSamples.AttachDpiHandler -->
```csharp
public class SettingsForm : Form
{
    private readonly DpiHandler _dpiHandler;

    public SettingsForm()
    {
        // Moves / resizes the form to the rectangle Windows suggests on a DPI change,
        // and publishes the DPI. It's disposed together with the form.
        _dpiHandler = this.AttachDpiHandler();
        _dpiHandler.OnDpiChanged.Subscribe(info => Console.WriteLine($"Now at {info.NewDpi} DPI"));
    }
}
```

<!-- sample: DpiSamples.BitmapScaling -->
```csharp
public class ToolbarForm : DpiAwareForm
{
    private readonly ToolStripButton _saveButton = new ToolStripButton();
    private readonly BitmapScaleHandler<string, Bitmap> _scaleHandler;

    public ToolbarForm()
    {
        // Load the image from the form's resources, scale it for the current DPI, and apply it again on every DPI change.
        // The handler owns and disposes the bitmaps.
        _scaleHandler = BitmapScaleHandler.WithComponentResourceManager<Bitmap>(FormDpiHandler, GetType(), BitmapScaleHandler.SimpleBitmapScaler)
            .AddTarget(_saveButton, "saveButton.Image", bitmap => bitmap);
    }

    protected override void OnClosing(CancelEventArgs e)
    {
        // Dispose on the UI thread
        _scaleHandler.Dispose();
        base.OnClosing(e);
    }
}
```

## WPF

<!-- sample: DpiSamples.Wpf -->
```csharp
// With a Per Monitor (V2) manifest WPF scales the window itself, the handler only reports the DPI.
// Without it, a LayoutTransform is applied to the content when the monitor DPI differs from the DPI WPF renders with.
DpiHandler dpiHandler = window.AttachDpiHandler();
dpiHandler.OnDpiChanged.Subscribe(info => Console.WriteLine($"{window.Title} is now at {info.NewDpi} DPI"));
```

## Calculations

<!-- sample: DpiSamples.Calculations -->
```csharp
// 96 DPI is 100%, 144 DPI is 150%
int scaled = DpiCalculator.ScaleWithDpi(16, 144);      // 24
int unscaled = DpiCalculator.UnscaleWithDpi(24, 144);  // 16
float factor = DpiCalculator.DpiScaleFactor(120);     // 1.25

// Structs are scaled too, values are rounded
NativeSize iconSize = DpiCalculator.ScaleWithDpi(new NativeSize(32, 32), 120); // 40x40

// From one DPI to another: 144 -> 96 is 0.667
float factor144To96 = DpiCalculator.DpiScaleFactor(144, 96);
```

<!-- sample: DpiSamples.AwarenessContext -->
```csharp
// Awareness contexts are handles: compare them with AreDpiAwarenessContextsEqual, not with ==
var threadContext = NativeDpiMethods.GetThreadDpiAwarenessContext();
bool isPerMonitorV2 = NativeDpiMethods.AreDpiAwarenessContextsEqual(threadContext, DpiAwarenessContext.PerMonitorAwareV2);

// Create a window with another awareness, the previous context of the thread is restored when the scope is disposed
using (NativeDpiMethods.ScopedThreadDpiAwarenessContext(DpiAwarenessContext.SystemAware))
{
    var toolWindow = new Form();
    toolWindow.CreateControl();
}
```
