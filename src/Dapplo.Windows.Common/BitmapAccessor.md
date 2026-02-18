# BitmapAccessor

`BitmapAccessor<TPixel>` provides pixel-level access to System.Drawing.Bitmap using typed pixel structs for efficient row-based operations. This is a shim that mimics ImageSharp's ProcessPixelRows API for easier migration.

## Features

- **Typed pixel access**: Work with `Bgra32`, `Bgr24`, or `Indexed8` structs instead of raw bytes
- **Span-based**: Modern, high-performance Span<TPixel> API
- **ImageSharp-compatible**: Similar API makes migration easier
- **Format validation**: Compile-time type safety ensures pixel format compatibility
- **Palette support**: For indexed formats, access the color palette as a span

## Supported Formats

| Pixel Type | System.Drawing Format | Description |
|-----------|----------------------|-------------|
| `Bgra32` | Format32bppArgb, Format32bppRgb, Format32bppPArgb | 32-bit with blue, green, red, and alpha channels |
| `Bgr24` | Format24bppRgb | 24-bit with blue, green, and red channels |
| `Indexed8` | Format8bppIndexed | 8-bit indexed color with palette |

## Basic Usage

### 32-bit ARGB Bitmap

```csharp
using Dapplo.Windows.Common.Structs.PixelFormats;

var bitmap = new Bitmap(width, height, PixelFormat.Format32bppArgb);
using (var accessor = new BitmapAccessor<Bgra32>(bitmap))
{
    for (int y = 0; y < accessor.Height; y++)
    {
        var row = accessor.GetRowSpan(y);
        for (int x = 0; x < accessor.Width; x++)
        {
            // Direct pixel manipulation
            row[x] = new Bgra32(red, green, blue, alpha);
            
            // Or modify components
            ref var pixel = ref row[x];
            pixel.R = 255;
            pixel.G = 0;
            pixel.B = 0;
            pixel.A = 255;
        }
    }
}
```

### 24-bit RGB Bitmap

```csharp
var bitmap = new Bitmap(width, height, PixelFormat.Format24bppRgb);
using (var accessor = new BitmapAccessor<Bgr24>(bitmap))
{
    for (int y = 0; y < accessor.Height; y++)
    {
        var row = accessor.GetRowSpan(y);
        for (int x = 0; x < accessor.Width; x++)
        {
            row[x] = new Bgr24(red, green, blue);
        }
    }
}
```

### 8-bit Indexed Bitmap with Palette

```csharp
var bitmap = new Bitmap(width, height, PixelFormat.Format8bppIndexed);
using (var accessor = new BitmapAccessor<Indexed8>(bitmap))
{
    // Access the palette
    var palette = accessor.GetPaletteSpan();
    
    // Set palette colors
    palette[0] = new Bgra32(255, 0, 0, 255); // Red
    palette[1] = new Bgra32(0, 255, 0, 255); // Green
    palette[2] = new Bgra32(0, 0, 255, 255); // Blue
    
    // Set pixel indices
    for (int y = 0; y < accessor.Height; y++)
    {
        var row = accessor.GetRowSpan(y);
        for (int x = 0; x < accessor.Width; x++)
        {
            row[x] = new Indexed8((byte)(x % 3)); // Cycle through first 3 colors
        }
    }
}
```

### Using ProcessRows

```csharp
bitmap.ProcessPixelRows<Bgra32>(accessor =>
{
    accessor.ProcessRows((y, row) =>
    {
        for (int x = 0; x < accessor.Width; x++)
        {
            ref var pixel = ref row[x];
            pixel.R = 255;
            pixel.G = 0;
            pixel.B = 0;
            pixel.A = 255;
        }
    });
});
```

### Processing Two Bitmaps Together

```csharp
targetBitmap.ProcessPixelRows<Bgra32>(sourceBitmap, (targetAccessor, sourceAccessor) =>
{
    for (int y = 0; y < targetAccessor.Height; y++)
    {
        var targetRow = targetAccessor.GetRowSpan(y);
        var sourceRow = sourceAccessor.GetRowSpan(y);
        
        for (int x = 0; x < targetAccessor.Width; x++)
        {
            // Copy with alpha blending
            ref var source = ref sourceRow[x];
            ref var target = ref targetRow[x];
            
            if (source.A == 255)
            {
                target = source;
            }
            else if (source.A > 0)
            {
                int alpha = source.A;
                int invAlpha = 255 - alpha;
                target.R = (byte)((source.R * alpha + target.R * invAlpha) / 255);
                target.G = (byte)((source.G * alpha + target.G * invAlpha) / 255);
                target.B = (byte)((source.B * alpha + target.B * invAlpha) / 255);
                target.A = 255;
            }
        }
    }
});
```

## Important Notes

- The bitmap is locked during the lifetime of the accessor and unlocked when disposed
- Always use `using` statements or call `Dispose()` to ensure proper cleanup
- The pixel format must match the generic type parameter (e.g., Format32bppArgb with Bgra32)
- Row spans include the full stride and may contain padding bytes at the end
- For indexed formats, palette changes are applied when the accessor is disposed
