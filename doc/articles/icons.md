# Icons and cursors

**Dapplo.Windows.Icons** extracts icons from files and the shell, writes ICO and CUR files, and captures the mouse
cursor, for example to draw it into a screenshot. The icon of a window is in the main package, see
[Window management](window-management.md#screenshots-icons-and-scrolling).

```powershell
dotnet add package Dapplo.Windows.Icons
```

Namespaces used on this page: `Dapplo.Windows.Icons`, `Dapplo.Windows.Icons.Enums`, `Dapplo.Windows.Common.Structs`,
`Dapplo.Windows.User32`.

The generic methods (`<TIcon>`) return `System.Drawing.Icon` or `Bitmap`; other types throw a
`NotSupportedException`. For WPF convert the result with `ToBitmapSource()` from Dapplo.Windows.Wpf. Returned icons own
their handle, dispose them.

## Extracting icons

<!-- sample: IconSamples.ExtractIcons -->
```csharp
// The first icon of an executable or DLL, TIcon is Icon or Bitmap
using var notepadIcon = IconHelper.ExtractAssociatedIcon<Icon>(@"C:\Windows\notepad.exe");

// How many icons a file has, and one of them in the small size
int count = IconHelper.CountAssociatedIcons(@"C:\Windows\System32\shell32.dll");
using var smallIcon = IconHelper.ExtractAssociatedIcon<Bitmap>(@"C:\Windows\System32\shell32.dll", index: 3, useLargeIcon: false);

// The icon Explorer shows for a file type, the file doesn't need to exist
using var pdfIcon = IconHelper.GetFileExtensionIcon<Bitmap>("document.pdf", IconSize.Large, linkOverlay: false);

// The folder icon
using var folderIcon = IconHelper.GetFolderIcon<Icon>(IconSize.Small, FolderIconType.Closed);
```

`IconHelper.GetAppLogo<Bitmap>(exePath)` reads the logo of a store (UWP / MSIX) app from its AppxManifest, and
`IconHelper.LoadIconWithScaleDown` / `LoadIconWithSystemMetrics` load icon resources at a size.

The sizes of the system:

<!-- sample: IconSamples.SystemIconSizes -->
```csharp
// The icon sizes of the system, they depend on the DPI of the process
Size small = new Size(IconHelper.GetSmallIconWidth(), IconHelper.GetSmallIconHeight());
Size standard = new Size(IconHelper.GetStandardIconWidth(), IconHelper.GetStandardIconHeight());
```

## Writing ICO files

`IconFileWriter.WriteIconFile` writes one ICO with an image per size. Every image is stored as PNG, which Windows Vista
and later read; images larger than 256 x 256 are scaled down to fit.

<!-- sample: IconSamples.WriteIconFile -->
```csharp
// One image per size. Every image is stored as PNG (Windows Vista and later), larger than 256x256 is scaled down.
var images = new List<Image>();
foreach (var size in new[] { 16, 32, 48, 256 })
{
    var bitmap = new Bitmap(size, size);
    using (var graphics = Graphics.FromImage(bitmap))
    {
        graphics.Clear(Color.Transparent);
        graphics.FillEllipse(Brushes.SteelBlue, 0, 0, size - 1, size - 1);
    }
    images.Add(bitmap);
}

IconFileWriter.WriteIconFile("app.ico", images);

// Or into a stream
using (var stream = new MemoryStream())
{
    IconFileWriter.WriteIconFile(stream, images);
}

images.ForEach(image => image.Dispose());
```

## Writing CUR files

A cursor also needs the hot spot, the pixel which "clicks":

<!-- sample: IconSamples.WriteCursorFile -->
```csharp
using var cursorImage = new Bitmap(32, 32);
using (var graphics = Graphics.FromImage(cursorImage))
{
    graphics.DrawLine(Pens.Black, 16, 0, 16, 31);
    graphics.DrawLine(Pens.Black, 0, 16, 31, 16);
}

// The hot spot is the pixel which "clicks", here the center of the cross
IconFileWriter.WriteCursorFile("cross.cur", new[] { ((Image)cursorImage, new Point(16, 16)) });
```

## Capturing the mouse cursor

`CursorHelper.TryGetCurrentCursor` captures the current cursor with its color and mask layers, its `Size` and its
`HotSpot`, also for large pointer sizes and high DPI. `DrawCursorOnBitmap` / `DrawCursorOnGraphics` draw it; the
position is the top-left of the cursor image, so subtract the hot spot to draw it at the mouse position.

<!-- sample: IconSamples.CaptureCursor -->
```csharp
// Capture the current mouse cursor (also high DPI and animated system cursors, the current frame)
if (CursorHelper.TryGetCurrentCursor(out var cursor))
{
    using (cursor)
    {
        // Draw it into a screenshot: the position is the top-left of the cursor image, so subtract the hot spot
        var mouse = User32Api.GetCursorLocation();
        var position = new NativePoint(mouse.X - cursor.HotSpot.X - screenshotLocation.X, mouse.Y - cursor.HotSpot.Y - screenshotLocation.Y);
        CursorHelper.DrawCursorOnBitmap(screenshot, cursor, position);
    }
}
```

## The file format

For your own tools the structures of the format are available in `Dapplo.Windows.Icons.Structs`:

- `IconDir` / `IconDirEntry`: the ICONDIR header and the ICONDIRENTRY of each image in an .ico or .cur file
  (`CreateIcon` / `CreateCursor`, `CreateForIcon` / `CreateForCursor`).
- `GrpIconDir` / `GrpIconDirEntry`: the same for icon resources in executables, where an entry refers to a resource id
  instead of a file offset. `IconFileWriter.WriteGrpIconDir` / `WriteGrpIconDirEntry` write them.

An ICO file is a 6-byte ICONDIR (reserved, type 1 = icon or 2 = cursor, count), a 16-byte ICONDIRENTRY per image
(width and height where 0 means 256, color count, planes or hot spot X, bit count or hot spot Y, size, offset) and the
image data (PNG or BMP without the file header).

## See also

- [The format of icon resources (Raymond Chen)](https://devblogs.microsoft.com/oldnewthing/20101018-00/?p=12513)
- [ICO file format (Wikipedia)](https://en.wikipedia.org/wiki/ICO_(file_format))
