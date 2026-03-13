# Icon Creation

The `Dapplo.Windows.Icons` package provides utilities for extracting system icons and for creating ICO and CUR files programmatically.

## Installation

```powershell
Install-Package Dapplo.Windows.Icons
```

## Extracting Icons

### From a File or Executable

```csharp
using Dapplo.Windows.Icons;

// Extract the large icon associated with a file
Icon icon = IconHelper.ExtractLargeIcon(@"C:\Windows\notepad.exe");

// Extract the small icon
Icon smallIcon = IconHelper.ExtractSmallIcon(@"C:\Windows\notepad.exe");
```

### From a Window

```csharp
using Dapplo.Windows.Desktop;
using Dapplo.Windows.Icons;

var window = InteropWindow.FromHandle(handle);
Icon icon = window.GetIcon();
```

## Creating ICO Files

`IconFileWriter` creates `.ico` files that comply with the [ICO file format specification](https://en.wikipedia.org/wiki/ICO_(file_format)).

### Multi-Resolution Icon

```csharp
using Dapplo.Windows.Icons;
using System.Collections.Generic;
using System.Drawing;

// Build one Bitmap per resolution
var images = new List<Bitmap>
{
    CreateIconBitmap(16),
    CreateIconBitmap(32),
    CreateIconBitmap(48),
    CreateIconBitmap(256)
};

// Write to a file
IconFileWriter.WriteIconFile("myapp.ico", images);

// — or — write to a stream
using (var stream = File.Create("myapp.ico"))
    IconFileWriter.WriteIconFile(stream, images);

foreach (var img in images) img.Dispose();
```

### Using `IconHelper.WriteIcon` (Legacy)

The original helper method is still fully supported:

```csharp
using Dapplo.Windows.Icons;
using System.Collections.Generic;
using System.Drawing;

var images = new List<Image> { /* your bitmaps */ };

using (var stream = new MemoryStream())
{
    IconHelper.WriteIcon(stream, images);
    File.WriteAllBytes("myapp.ico", stream.ToArray());
}
```

## Creating Cursor (CUR) Files

```csharp
using Dapplo.Windows.Icons;
using System.Collections.Generic;
using System.Drawing;

var cursorBitmap = new Bitmap(32, 32);
// … draw cursor graphics …

var cursorData = new List<(Image image, Point hotspot)>
{
    (cursorBitmap, new Point(16, 16))   // hotspot at centre
};

IconFileWriter.WriteCursorFile("custom.cur", cursorData);
cursorBitmap.Dispose();
```

## Working with the Structures Directly

For advanced scenarios (e.g., embedding icons in PE resource sections) you can use the underlying structs:

```csharp
using Dapplo.Windows.Icons.Structs;

// ICONDIR header
var iconDir = IconDir.CreateIcon(count: 2);

// ICONDIRENTRY for each resolution
var entry16 = IconDirEntry.CreateForIcon(
    width: 16, height: 16, bitCount: 32,
    imageSize: 1024, imageOffset: 22);

var entry32 = IconDirEntry.CreateForIcon(
    width: 32, height: 32, bitCount: 32,
    imageSize: 4096, imageOffset: 1046);

// GRPICONDIR / GRPICONDIRENTRY for resource files
var grpDir = GrpIconDir.CreateIcon(count: 2);
var grpEntry = GrpIconDirEntry.CreateForIcon(
    width: 32, height: 32, bitCount: 32,
    imageSize: 4096, resourceId: 1);
```

## ICO File Format Reference

An ICO file has three sections:

1. **ICONDIR** (6 bytes) — file header  
   - `Reserved` (2 bytes) — always 0  
   - `Type` (2 bytes) — `1` for icons, `2` for cursors  
   - `Count` (2 bytes) — number of images

2. **ICONDIRENTRY** (16 bytes × `Count`) — per-image metadata  
   - `Width`, `Height` (1 byte each) — `0` means 256  
   - `ColorCount`, `Reserved` (1 byte each)  
   - `Planes`, `BitCount` (2 bytes each)  
   - `BytesInRes` (4 bytes) — size of image data  
   - `ImageOffset` (4 bytes) — byte offset to image data

3. **Image data** — PNG or BMP data for each image

> **Tip:** Windows Vista+ supports PNG-compressed images inside ICO files, enabling compact 256×256 images.

## Best Practices

- Include at minimum 16×16, 32×32, and 48×48 resolutions in every ICO file.
- Add a 256×256 PNG-compressed image for modern Windows high-DPI scenarios.
- For application icons, also include 64×64 and 128×128 resolutions.
- Always `Dispose()` `Bitmap` and `Image` objects after passing them to `IconFileWriter`.

## References

- [The format of icon resources — Raymond Chen](https://devblogs.microsoft.com/oldnewthing/20101018-00/?p=12513)
- [ICO file format — Wikipedia](https://en.wikipedia.org/wiki/ICO_(file_format))

## See Also

- [[Getting-Started]]
- [[Window-Management]] — extracting icons from windows
