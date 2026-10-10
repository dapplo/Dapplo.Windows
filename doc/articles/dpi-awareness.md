# DPI awareness

Windows runs monitors at different scale factors (100 %, 125 %, 150 %, ...), and a window can move between them.
**Dapplo.Windows.Dpi** helps an application to follow the DPI of its windows: DPI calculations, a `DpiHandler` which
reports DPI changes, bitmap scaling and the DPI awareness APIs of Windows. The Windows Forms and WPF parts are in
**Dapplo.Windows.Forms** and **Dapplo.Windows.Wpf**.

```powershell
dotnet add package Dapplo.Windows.Dpi
dotnet add package Dapplo.Windows.Forms   # DpiAwareForm, AttachDpiHandler for forms and context menus
dotnet add package Dapplo.Windows.Wpf     # AttachDpiHandler for WPF windows
```

Namespaces used on this page: `Dapplo.Windows.Dpi`, `Dapplo.Windows.Dpi.Enums`, `Dapplo.Windows.Forms.Dpi`,
`Dapplo.Windows.Wpf.Dpi`, `Dapplo.Windows.Common.Structs`, `Dapplo.Windows.User32`.

## First: make the process DPI aware

A process which is not DPI aware is scaled by Windows as a bitmap and looks blurry; nothing on this page changes that.
Declare the awareness in the application manifest (`<ApplicationManifest>app.manifest</ApplicationManifest>` in the
project file):

```xml
<application xmlns="urn:schemas-microsoft-com:asm.v3">
  <windowsSettings>
    <dpiAware xmlns="http://schemas.microsoft.com/SMI/2005/WindowsSettings">true/pm</dpiAware>
    <dpiAwareness xmlns="http://schemas.microsoft.com/SMI/2016/WindowsSettings">PerMonitorV2, PerMonitor</dpiAwareness>
  </windowsSettings>
</application>
```

- .NET (Core) Windows Forms: also call `Application.SetHighDpiMode(HighDpiMode.PerMonitorV2)` or set
  `<ApplicationHighDpiMode>PerMonitorV2</ApplicationHighDpiMode>` in the project, so WinForms scales on DPI changes.
- .NET Framework 4.8 Windows Forms: add `<add key="DpiAwareness" value="PerMonitorV2" />` to the
  `System.Windows.Forms.ApplicationConfigurationSection` in app.config, otherwise WinForms doesn't scale on DPI changes.
- WPF scales by itself when the process is Per Monitor aware.

If you can't use a manifest, call `NativeDpiMethods.EnableDpiAware()` before the first window is created:

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

### DpiAwareForm

Derive from `DpiAwareForm` (Dapplo.Windows.Forms). What it does:

- It creates its window handle with the Per Monitor V2 awareness context (Per Monitor as fallback), even when the
  thread or process uses another awareness. The awareness of the thread is restored right after.
- On Per Monitor (v1) it enables the scaling of the non-client area (title bar, menus).
- WM_DPICHANGED is always passed to WinForms. When WinForms handles the DPI change (it raises `Form.DpiChanged`), it
  scales fonts and controls and applies the window bounds Windows suggests. That is the case on .NET 10 with a Per
  Monitor V2 high DPI mode, and on .NET Framework 4.8 with the app.config setting above.
- When WinForms doesn't handle it, only the window bounds are set to the rectangle Windows suggests. **The content is
  not scaled**; do that yourself in `FormDpiHandler.OnDpiChanged`.
- A cancelled `Form.DpiChanged` (`e.Cancel = true`) is respected, the bounds are not changed.
- `FormDpiHandler` reports the DPI: the first time when the handle is created, then for every change. It survives
  handle re-creation and is disposed with the form.

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

### A form that stays DPI unaware

For a form which can't handle high DPI, derive from `DpiUnawareForm`. Windows then scales it as a bitmap: blurry, but
the right size.

<!-- sample: DpiSamples.DpiUnawareForm -->
```csharp
// Windows scales this form as a bitmap (blurry, but always the right size), even when the process is DPI aware
public class LegacyForm : DpiUnawareForm
{
}
```

### AttachDpiHandler

When you can't change the base class, attach a `DpiHandler` to a form. It moves and resizes the form to the rectangle
Windows suggests on a DPI change and reports the DPI. It does not enable the non-client scaling of Per Monitor v1, for
that you need `DpiAwareForm` (or call `DpiHandler.TryEnableNonClientDpiScaling(hWnd)` in WM_NCCREATE yourself).

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

A `ContextMenuStrip` is a window of its own and can open on another monitor than its form:

<!-- sample: DpiSamples.ContextMenu -->
```csharp
// A ContextMenuStrip is its own window, it can show up on another monitor than the form
DpiHandler menuDpiHandler = contextMenuStrip.AttachDpiHandler();
menuDpiHandler.OnDpiChanged.Subscribe(info => contextMenuStrip.ImageScalingSize = DpiCalculator.ScaleWithDpi(new Size(16, 16), info.NewDpi));
```

### Images

`BitmapScaleHandler` provides a bitmap for the current DPI and applies it again on every DPI change. It owns the
bitmaps: they are cached per DPI and disposed when the DPI changes or the handler is disposed, so the provider must
return a new instance for every call. Dispose the handler on the UI thread.

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

    protected override void OnFormClosing(FormClosingEventArgs e)
    {
        // Dispose on the UI thread
        _scaleHandler.Dispose();
        base.OnFormClosing(e);
    }
}
```

`BitmapScaleHandler.SimpleBitmapScaler` scales with nearest neighbor: sharp pixels for icons, but blocky at
uneven factors. For better results provide images for several sizes and pick the right one:

<!-- sample: DpiSamples.CustomBitmapProvider -->
```csharp
// Pick the best source image for the DPI yourself, return a new Bitmap for every call: the handler disposes them
var scaleHandler = BitmapScaleHandler.Create<string, Bitmap>(dpiHandler,
    (name, dpi) => new Bitmap(dpi > 120 ? $"Images\\{name}@2x.png" : $"Images\\{name}.png"));

scaleHandler.AddTargetAction(pictureBox, "logo", bitmap => pictureBox.Image = bitmap, execute: true);
```

## WPF

WPF already renders in device independent units. With a Per Monitor manifest WPF follows the monitor DPI itself, and
`AttachDpiHandler()` only reports the DPI. Only when the monitor DPI differs from the DPI WPF renders with (the process
isn't Per Monitor aware, or WPF's per-monitor support is disabled), it applies a `LayoutTransform` to the content and
moves the window to the rectangle Windows suggests. The handler is disposed with the window.

<!-- sample: DpiSamples.Wpf -->
```csharp
// With a Per Monitor (V2) manifest WPF scales the window itself, the handler only reports the DPI.
// Without it, a LayoutTransform is applied to the content when the monitor DPI differs from the DPI WPF renders with.
DpiHandler dpiHandler = window.AttachDpiHandler();
dpiHandler.OnDpiChanged.Subscribe(info => Console.WriteLine($"{window.Title} is now at {info.NewDpi} DPI"));
```

## Calculations

96 DPI is 100 %. `DpiCalculator` scales and unscales numbers and the native structs; results are rounded, not
truncated.

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

A `DpiHandler` knows the DPI of its window. `Dpi` is 96 until the DPI is known (`IsDpiKnown`).

<!-- sample: DpiSamples.HandlerScaling -->
```csharp
// A DpiHandler knows the DPI of its window: Dpi is 96 until the first DPI is known (IsDpiKnown)
int margin = dpiHandler.ScaleWithCurrentDpi(8);
NativeSize buttonSize = dpiHandler.ScaleWithCurrentDpi(new NativeSize(75, 23));
```

| DPI | Scale |
|---|---|
| 96 | 100 % |
| 120 | 125 % |
| 144 | 150 % |
| 168 | 175 % |
| 192 | 200 % |

## DPI of windows and monitors

<!-- sample: DpiSamples.GetDpi -->
```csharp
// The DPI of a window, and of the monitor at a location
int windowDpi = NativeDpiMethods.GetDpi(hWnd);
int cursorDpi = NativeDpiMethods.GetDpi(User32Api.GetCursorLocation());

// The DPI of every display
foreach (var display in DisplayInfo.AllDisplayInfos)
{
    Console.WriteLine($"{display.DeviceName} {display.Bounds}: {NativeDpiMethods.GetDpi(display.Bounds.Location)} DPI");
}
```

## Awareness contexts

`DpiAwarenessContext` values are handles, and the handles Windows returns are not the same values as the constants.
Compare them with `NativeDpiMethods.AreDpiAwarenessContextsEqual`. `ScopedThreadDpiAwarenessContext` creates windows
with another awareness and restores the thread's awareness afterwards.

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

`NativeDpiMethods` also wraps `GetDpiForWindow`, `GetDpiForMonitor`, `GetSystemMetricsForDpi`,
`AdjustWindowRectExForDpi`, `SetDialogDpiChangeBehavior` and more; `DpiApi` has convenience methods for system metrics
and `SystemParametersInfo` for a DPI.

## Testing

Test with at least two monitors at different scale factors, and move the windows between them. Change the scale factor
while the application runs, and start it on the secondary monitor. The example project
[Dapplo.Windows.Example.FormsExample](https://github.com/dapplo/Dapplo.Windows/tree/master/src/Dapplo.Windows.Example.FormsExample)
has forms for each approach.

## See also

- [Windows Forms and WPF](forms-and-wpf.md)
- [High DPI desktop application development on Windows](https://learn.microsoft.com/windows/win32/hidpi/high-dpi-desktop-application-development-on-windows)
