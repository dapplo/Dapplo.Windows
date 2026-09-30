# Windows Forms and WPF

The core packages of Dapplo.Windows don't reference Windows Forms or WPF, so they can be used in console applications,
services and other UI frameworks. The integration lives in two packages:

| Package | Contents |
|---|---|
| **Dapplo.Windows.Forms** | `DpiAwareForm`, `DpiUnawareForm`, `AttachDpiHandler()` for `Form` and `ContextMenuStrip`, `BitmapScaleHandler.AddTarget` for buttons and tool strip items, `WinProcFormsMessages()` for a `Control`, `WinProcListener`, placement and `AsInteropWindow()` for a `Form` |
| **Dapplo.Windows.Wpf** | `AttachDpiHandler()` for a `Window`, `WinProcMessages()` for a `Window` / `HwndSource`, `WinProcHandler`, conversions between the native structs and WPF types, `ToBitmapSource()`, `PrintWindowAsBitmapSource()`, `ToMediaColor()`, placement, `GetHandle()` and `AsInteropWindow()` for a `Window` |

```powershell
dotnet add package Dapplo.Windows.Forms
dotnet add package Dapplo.Windows.Wpf
```

Namespaces: `Dapplo.Windows.Forms`, `Dapplo.Windows.Forms.Dpi`, `Dapplo.Windows.Forms.Messages`,
`Dapplo.Windows.Wpf`, `Dapplo.Windows.Wpf.Dpi`, `Dapplo.Windows.Wpf.Messages`.

- DPI support for both is described in [DPI awareness](dpi-awareness.md).
- Window messages of forms and WPF windows are described in
  [Window messages](window-messages.md#messages-of-your-own-windows).

## Windows Forms

Save and restore the position of a form, including its maximized state and its normal bounds:

<!-- sample: FormsAndWpfSamples.FormsPlacement -->
```csharp
// Save the placement (normal bounds, maximized / minimized) when closing ...
WindowPlacement placement = form.RetrievePlacement();
// ... and restore it before the form is shown the next time
form.ApplyPlacement(placement);

// Everything of Dapplo.Windows for your own form
InteropWindow interopWindow = form.AsInteropWindow();
```

## WPF

<!-- sample: FormsAndWpfSamples.WpfWindow -->
```csharp
// The handle, it's created when needed (also before the window is shown)
IntPtr handle = window.GetHandle();
InteropWindow interopWindow = window.AsInteropWindow();

WindowPlacement placement = window.RetrievePlacement();
window.ApplyPlacement(placement);
```

The native structs (`NativeRect`, `NativePoint`, `NativeSize` and their `Float` variants) convert to and from WPF
types with extension methods; to and from `System.Drawing` types they convert implicitly (lossy conversions, from float
to int, are explicit):

<!-- sample: FormsAndWpfSamples.WpfConversions -->
```csharp
// Between the native structs and WPF
System.Windows.Rect wpfRect = nativeRect.ToRect();
System.Windows.Int32Rect int32Rect = nativeRect.ToInt32Rect();
NativeRectFloat back = wpfRect.ToNativeRectFloat();
NativeRect fromInt32Rect = int32Rect.ToNativeRect();

// System.Drawing types convert implicitly
Rectangle drawingRectangle = nativeRect;
NativeRect fromDrawing = drawingRectangle;
```

Images for WPF:

<!-- sample: FormsAndWpfSamples.WpfImages -->
```csharp
// A screenshot of a window for an Image control
BitmapSource screenshot = window.PrintWindowAsBitmapSource();

// A Bitmap or Icon for WPF, the alpha channel is kept
using (var icon = window.GetIcon<Icon>())
{
    BitmapSource iconSource = icon?.ToBitmapSource();
}

// The accent color as WPF color
System.Windows.Media.Color accent = DwmApi.ColorizationSystemDrawingColor.ToMediaColor();
```
