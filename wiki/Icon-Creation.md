# Icons and cursors

Package **Dapplo.Windows.Icons**. Full version: [Icons and cursors](https://www.dapplo.net/Dapplo.Windows/articles/icons.html).

The generic methods return `Icon` or `Bitmap`; for WPF convert with `ToBitmapSource()` (Dapplo.Windows.Wpf).

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

The icon of a window is in the Dapplo.Windows package:

<!-- sample: WindowSamples.WindowIcon -->
```csharp
// The icon of a window, as Bitmap or Icon
using var smallIcon = window.GetIcon<Bitmap>();
using var largeIcon = window.GetIcon<Icon>(useLargeIcons: true);
```

## Writing ICO and CUR files

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

## Capturing the cursor

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
