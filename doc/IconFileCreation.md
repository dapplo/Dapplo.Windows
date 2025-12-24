# Icon File Creation API

This document demonstrates how to use the new icon file creation APIs added to `Dapplo.Windows.Icons`.

## Overview

The new API provides structures and helper methods to create ICO and CUR files using the proper file format specifications, as documented in [Raymond Chen's blog](https://devblogs.microsoft.com/oldnewthing/20101018-00/?p=12513) and the [ICO file format specification](https://en.wikipedia.org/wiki/ICO_(file_format)).

## Key Components

### Structures

- **`IconDir`** - Represents the ICONDIR header in an ICO file
- **`IconDirEntry`** - Represents individual ICONDIRENTRY structures for each image
- **`GrpIconDir`** - Represents the GRPICONDIR header used in resource files
- **`GrpIconDirEntry`** - Represents individual GRPICONDIRENTRY structures for resource entries

### Helper Classes

- **`IconFileWriter`** - Provides static methods to create icon and cursor files

## Usage Examples

### Creating a Multi-Resolution Icon File

```csharp
using System.Drawing;
using System.Collections.Generic;
using Dapplo.Windows.Icons;

// Create images at different resolutions
var images = new List<Bitmap>
{
    new Bitmap(16, 16),
    new Bitmap(32, 32),
    new Bitmap(48, 48),
    new Bitmap(256, 256)
};

// Fill images with your icon graphics
// ... (draw your icon content)

// Write to an icon file
IconFileWriter.WriteIconFile("myicon.ico", images);

// Or write to a stream
using (var stream = new FileStream("myicon.ico", FileMode.Create))
{
    IconFileWriter.WriteIconFile(stream, images);
}

// Clean up
foreach (var image in images)
{
    image.Dispose();
}
```

### Creating a Cursor File

```csharp
using System.Drawing;
using Dapplo.Windows.Icons;

// Create a cursor bitmap
var cursorBitmap = new Bitmap(32, 32);

// Draw your cursor graphics
// ... (draw your cursor content)

// Define cursor data with hotspot (center of cursor)
var cursorData = new List<(Image image, Point hotspot)>
{
    (cursorBitmap, new Point(16, 16))
};

// Write to a cursor file
IconFileWriter.WriteCursorFile("mycursor.cur", cursorData);

cursorBitmap.Dispose();
```

### Using the Structures Directly

```csharp
using System.IO;
using Dapplo.Windows.Icons.Structs;

// Create an icon directory header
var iconDir = IconDir.CreateIcon(count: 3);

// Create icon directory entries
var entry1 = IconDirEntry.CreateForIcon(
    width: 16, 
    height: 16, 
    bitCount: 32, 
    imageSize: 1024, 
    imageOffset: 22);

var entry2 = IconDirEntry.CreateForIcon(
    width: 32, 
    height: 32, 
    bitCount: 32, 
    imageSize: 4096, 
    imageOffset: 1046);

// Write structures to a stream using BinaryWriter
using (var stream = new FileStream("custom.ico", FileMode.Create))
using (var writer = new BinaryWriter(stream))
{
    // Write icon directory header
    writer.Write(iconDir.Reserved);
    writer.Write(iconDir.Type);
    writer.Write(iconDir.Count);
    
    // Write entries
    // ... (write entry fields)
    
    // Write image data
    // ... (write PNG data)
}
```

### Creating Resource Format Structures

```csharp
using Dapplo.Windows.Icons.Structs;

// Create a group icon directory for resources
var grpIconDir = GrpIconDir.CreateIcon(count: 2);

// Create group icon directory entries
var grpEntry = GrpIconDirEntry.CreateForIcon(
    width: 48, 
    height: 48, 
    bitCount: 32, 
    imageSize: 4096, 
    resourceId: 1);

// These structures are useful when working with PE resource files
```

## Backward Compatibility

The existing `IconHelper.WriteIcon` method has been refactored to use the new structures internally, but maintains full backward compatibility:

```csharp
using System.Drawing;
using System.Collections.Generic;
using Dapplo.Windows.Icons;

var images = new List<Image> { /* your images */ };

// This still works exactly as before
using (var stream = new MemoryStream())
{
    IconHelper.WriteIcon(stream, images);
}
```

## Technical Details

### ICO File Format

An ICO file consists of:

1. **ICONDIR header** (6 bytes)
   - Reserved (2 bytes) - always 0
   - Type (2 bytes) - 1 for icons, 2 for cursors
   - Count (2 bytes) - number of images

2. **ICONDIRENTRY array** (16 bytes per entry)
   - Width, Height (1 byte each) - 0 means 256
   - ColorCount (1 byte) - 0 for modern formats
   - Reserved (1 byte)
   - Planes (2 bytes) - 0 or 1 for icons
   - BitCount (2 bytes) - bits per pixel
   - BytesInRes (4 bytes) - size of image data
   - ImageOffset (4 bytes) - offset to image data

3. **Image data** - PNG or BMP data for each image

### Resource Format

Resource files use GRPICONDIR and GRPICONDIRENTRY structures which are similar to their ICO counterparts, except:
- GRPICONDIRENTRY uses a resource ID (2 bytes) instead of a file offset (4 bytes)
- Total size is 14 bytes instead of 16 bytes per entry

## References

- [The format of icon resources - Raymond Chen](https://devblogs.microsoft.com/oldnewthing/20101018-00/?p=12513)
- [ICO file format - Wikipedia](https://en.wikipedia.org/wiki/ICO_(file_format))
- [Custom cursor in WPF - Stack Overflow](https://stackoverflow.com/questions/46805/custom-cursor-in-wpf)
