# Images through WIC

**Dapplo.Windows.Imaging** uses the Windows Imaging Component (WIC) directly: it lists the image codecs of the PC, encodes
and decodes images, and reads and writes their metadata. It needs no WPF, no System.Drawing and no other pixel library:
pixels are passed as memory (a pointer and a stride) or row by row through a callback, and the input and output are .NET
streams.

```powershell
dotnet add package Dapplo.Windows.Imaging
```

Namespace used on this page: `Dapplo.Windows.Imaging`.

Every COM object is created per call and released before the call returns, and every method works from STA and MTA
threads.

## Which formats the PC can read and write

WIC lists every codec it has, including the packaged (Store) codecs like HEIF (also AVIF), WebP, Raw and JPEG XL. The
registry key of the WIC decoder category (`HKCR\CLSID\{7ED96837-96F0-4812-B211-F13C24117ED3}\Instance`) doesn't list
the built-in codecs on newer Windows versions and never lists the packaged ones, `WicCodecs` asks WIC itself.

<!-- sample: ImagingSamples.DiscoverCodecs -->
```csharp
// Every decoder WIC has, also the packaged (Store) codecs like HEIF, AVIF, WebP and JPEG XL
foreach (var decoder in WicCodecs.GetDecoders())
{
    Console.WriteLine($"{decoder.FriendlyName}: {string.Join(" ", decoder.FileExtensions)} ({string.Join(" ", decoder.MimeTypes)})");
}

// Can this PC write JPEG XL?
bool canWriteJpegXl = WicCodecs.GetEncoders().Any(encoder => encoder.ContainerFormat == WicContainerFormats.JpegXl);
```

Each `WicCodecInfo` has the CLSID, the container format, the vendor, the friendly name, author and version, the file
extensions (lower case with the dot, e.g. `.heic`) and MIME types (lower case), and the signing status. `IsFromMicrosoft`
is true for the codecs of Windows and for the Store codecs of Microsoft. A codec which can't be read is left out (and
logged), it doesn't break the list. `GetDecoders(refresh: true)` lets WIC refresh its list, e.g. after a codec was
installed.

## Encoding

`WicImage.Encode` takes the pixels as memory, for example the `Scan0` of a locked GDI+ bitmap or a contiguous ImageSharp
buffer, and reads them without a copy of the image.

<!-- sample: ImagingSamples.EncodeBitmap -->
```csharp
// Save a GDI+ bitmap as lossless JPEG XL, the pixels are read straight from the locked bitmap
var bitmapData = bitmap.LockBits(new Rectangle(Point.Empty, bitmap.Size), ImageLockMode.ReadOnly, PixelFormat.Format32bppArgb);
try
{
    var options = new WicEncodeOptions
    {
        ContainerFormat = WicContainerFormats.JpegXl,
        Lossless = true,
        DpiX = bitmap.HorizontalResolution,
        DpiY = bitmap.VerticalResolution,
        Metadata = metadata => metadata.Set("System.ApplicationName", "Greenshot")
    };
    // Format32bppArgb has the byte order B, G, R, A: WicPixelFormats.Format32bppBgra
    WicEncodeResult result = WicImage.Encode(destination, options, bitmapData.Scan0, bitmapData.Width, bitmapData.Height, bitmapData.Stride, WicPixelFormats.Format32bppBgra);
}
catch (WicCodecNotFoundException)
{
    // The JPEG XL extension isn't installed (or can't be used), fall back to PNG
}
finally
{
    bitmap.UnlockBits(bitmapData);
}
```

The pixel formats are WIC GUIDs, `WicPixelFormats` has the common ones: `Format32bppBgra` (GDI+ `Format32bppArgb`,
ImageSharp `Bgra32`), `Format32bppPbgra`, `Format24bppBgr` (GDI+ `Format24bppRgb`, ImageSharp `Bgr24`),
`Format32bppRgba` (ImageSharp `Rgba32`), and `Format64bppRgbaHalf` and `Format128bppRgbaFloat` for HDR. When the encoder
wants another pixel format (e.g. JPEG has no alpha), WIC converts the pixels while it reads them;
`WicEncodeResult.PixelFormat` is the format which was written.

For an indexed format (GIF) a palette is made from the pixels: with at most 256 colors (and no partly transparent
pixel) exactly these colors, so the GIF has the same pixels; otherwise WIC chooses the colors, with
`WicEncodeOptions.Dither` using error diffusion. `WicEncodeOptions.Palette` (up to 256 colors as `0xAARRGGBB`, alpha 0
is the transparent color) sets the palette yourself: pixels in `WicPixelFormats.Format8bppIndexed` (or 1, 2, 4 bpp)
are indexes into it, other pixels get the nearest color and the encoder is asked for an indexed format, so a PNG gets a
palette too.

When the pixels aren't in one buffer (ImageSharp can split a large image), the other overload takes a callback. The
encoder pulls the rows it needs, no copy of the image is made.

<!-- sample: ImagingSamples.EncodeRows -->
```csharp
// Pixels which aren't in one buffer (e.g. ImageSharp ProcessPixelRows): the encoder pulls the rows it needs
WicImage.Encode(destination, new WicEncodeOptions { ContainerFormat = WicContainerFormats.Png }, width, height, WicPixelFormats.Format32bppBgra,
    (top, count, rows, stride) =>
    {
        for (int row = 0; row < count; row++)
        {
            getRow(top + row).AsSpan().CopyTo(rows.Slice(row * stride));
        }
    });
```

`ImageQuality` (0..1) and `Lossless` are set when the encoder has them, `EncoderOptions` takes any other option by
name. An option the encoder doesn't have is skipped; `WicEncodeResult.AppliedOptions` lists what was set and
`WicImage.GetEncoderOptions(containerFormat)` lists what an encoder has:

| Encoder | Options |
|---|---|
| JPEG | ImageQuality, BitmapTransform, SuppressApp0, JpegYCrCbSubsampling, Luminance, Chrominance and the Huffman tables |
| TIFF | TiffCompressionMethod, CompressionQuality |
| PNG | InterlaceOption, FilterOption |
| JPEG XR | ImageQuality, Lossless, BitmapTransform, UseCodecOptions, Quality, Overlap, Subsampling, tile slices, FrequencyOrder, InterleavedAlpha, AlphaQuality ... |
| BMP | EnableV5Header32bppBGRA |
| GIF | none |

A stream which can't seek is written through a `MemoryStream`. A seekable stream is written from its current position
and ends up after the image.

## Decoding

Decoding reads the first frame and converts it to the pixel format you ask for. `GetFrameInfo` reads only the size,
resolution, pixel format and metadata. `Decode` writes the pixels into your memory, either memory you allocated with
the size from `GetFrameInfo`, or memory you supply once the size is known, so the stream is read once:

<!-- sample: ImagingSamples.DecodeIntoBitmap -->
```csharp
// Load any image WIC can read into a new GDI+ bitmap, upright, without WPF and without an extra copy
Bitmap bitmap = null;
BitmapData bitmapData = null;
try
{
    WicFrameInfo info = WicImage.Decode(source, WicPixelFormats.Format32bppBgra, (WicFrameInfo frame, out int stride) =>
    {
        bitmap = new Bitmap(frame.Width, frame.Height, PixelFormat.Format32bppArgb);
        // The resolution of the file; when it stores none, WIC reports the default of the decoder (96 or 72), use your own
        bitmap.SetResolution(frame.HasResolution ? (float)frame.DpiX : 96, frame.HasResolution ? (float)frame.DpiY : 96);
        bitmapData = bitmap.LockBits(new Rectangle(0, 0, frame.Width, frame.Height), ImageLockMode.WriteOnly, PixelFormat.Format32bppArgb);
        stride = bitmapData.Stride;
        return bitmapData.Scan0;
    }, applyOrientation: true);
}
finally
{
    if (bitmapData != null)
    {
        bitmap.UnlockBits(bitmapData);
    }
}
```

With `applyOrientation: true` the EXIF orientation is applied (rotated and flipped before the pixel format conversion),
`WicFrameInfo.Width` and `Height` are then those of the upright image. A third overload hands the rows to a
`WicRowConsumer`, in bands from a pooled buffer.

### Resolution

WIC always reports a resolution: when the file stores none, each decoder fills in its own default (e.g. 96 for
PNG, 72 for WebP). `WicFrameInfo.HasResolution` tells the two apart, it is true only when the file itself stores a
resolution:

| Container | Stored resolution |
|---|---|
| PNG | a pHYs chunk with the unit meter (unit 0 is only an aspect ratio) |
| JPEG | JFIF density with the unit inch or cm, or EXIF XResolution with a ResolutionUnit other than none |
| TIFF, DNG | XResolution with a ResolutionUnit other than none (no ResolutionUnit means inch) |
| JPEG XR | WIDTH_RESOLUTION of the container |
| BMP | pixels per meter other than 0 |
| GIF, ICO, DDS | never, GIF only has an aspect ratio |
| others (WebP, HEIF, AVIF, JPEG XL, RAW, ...) | an EXIF IFD with XResolution and a ResolutionUnit other than none |

The metadata of the frame is used, the bytes of the file are read only for a PNG whose pHYs WIC doesn't expose, JPEG XR
and BMP. When the resolution is only in EXIF, `DpiX` / `DpiY` are the EXIF values: the JPEG decoder of WIC reports its
default (96) for a JPEG with only EXIF XResolution.

## When there is no codec

When WIC has no codec for the container format or doesn't recognize the stream, a `WicCodecNotFoundException` (a
`COMException`) is thrown, so you can fall back. It is also thrown when a codec is registered but can't be initialized:
on the CI runner (Windows Server) the HEIF and JPEG XL encoders are listed but fail with
`WINCODEC_ERR_COMPONENTINITIALIZEFAILURE` (0x88982F8B); HEIF encoding needs the HEVC video extension. Other WIC
failures are a `COMException` with the HRESULT, and an exception of your stream or row callback reaches you unchanged.

## Metadata

The metadata uses the [WIC metadata query language](https://learn.microsoft.com/en-us/windows/win32/wic/-wic-codec-metadataquerylanguage),
so it is not a fixed set of properties. The same EXIF tag has a different path in each container, `WicMetadataPaths` has
them, e.g. `Software(WicContainerFormats.Jpeg)` is `/app1/ifd/{ushort=305}` and `Software(WicContainerFormats.Tiff)` is
`/ifd/{ushort=305}`.

### Writing

`WicEncodeOptions.Metadata` is called after the frame is initialized and before the pixels are written. Everything the
encoder doesn't accept is skipped and listed in `WicEncodeResult.MetadataIssues` (and logged), it never fails the
encoding.

<!-- sample: ImagingSamples.WriteMetadata -->
```csharp
var container = WicContainerFormats.Jpeg;
var options = new WicEncodeOptions
{
    ContainerFormat = container,
    ImageQuality = 0.9f,
    Metadata = metadata =>
    {
        // The EXIF ASCII tags are VT_LPSTR
        metadata.Set(WicMetadataPaths.Software(container), WicMetadataValue.AnsiString("Greenshot"));
        metadata.Set(WicMetadataPaths.DateTimeOriginal(container), WicMetadataValue.AnsiString(DateTime.Now.ToString("yyyy:MM:dd HH:mm:ss", CultureInfo.InvariantCulture)));
        // dc:title becomes a language alternative with x-default
        metadata.Set(WicMetadataPaths.XmpProperty(container, "dc:title"), "Capture of the Explorer window");
        metadata.Set(WicMetadataPaths.XmpProperty(container, "xmp:CreatorTool"), "Greenshot");
        metadata.SetColorProfile(iccProfile);
    }
};
var result = WicImage.Encode(destination, options, pixels, width, height, stride, WicPixelFormats.Format32bppBgra);
foreach (var issue in result.MetadataIssues)
{
    Console.WriteLine($"Not written: {issue}");
}
```

Values: `string` is written as VT_LPWSTR, `WicMetadataValue.AnsiString` as VT_LPSTR (the EXIF ASCII tags like Software,
DateTime and DateTimeOriginal), `bool`, the integer types, `float`, `double`, `WicRational` and `WicSignedRational` (EXIF
RATIONAL and SRATIONAL), `DateTime` (FILETIME), `Guid`, `byte[]` (VT_BLOB, or `WicMetadataValue.ByteVector` for
VT_VECTOR | VT_UI1), `ushort[]`, `uint[]`, `int[]` and `string[]`.

The photo metadata policies of Windows, e.g. `System.ApplicationName`, `System.Title`, `System.Comment` and
`System.Photo.Orientation`, write every location the container has: `System.ApplicationName` writes the EXIF Software
and the XMP xmp:CreatorTool, `System.Title` the EXIF ImageDescription and the XMP dc:title as language alternative. A
plain string at `/xmp/dc:title` makes a simple XMP property.

### Reading

`WicFrameInfo.Metadata` is a snapshot of every readable item of the first frame, walked through the nested blocks.

<!-- sample: ImagingSamples.ReadMetadata -->
```csharp
WicFrameInfo info = WicImage.GetFrameInfo(source);
WicMetadata metadata = info.Metadata;
if (metadata.TryGet(WicMetadataPaths.Software(info.ContainerFormat), out var software))
{
    Console.WriteLine($"Written by {software}");
}
int? orientation = metadata.Orientation; // 1..8
byte[] icc = metadata.ColorProfile;      // null without a profile
foreach (var item in metadata.Items)
{
    // e.g. "/app1/ifd/exif/{ushort=36867}" = "2026:11:02 12:34:56"
    Console.WriteLine($"{item.Key} = {item.Value}");
}
```

The paths use the names of the blocks (`/app1/ifd/exif`), the paths with the tags as WIC enumerates them
(`/app1/{ushort=0}/{ushort=34665}`) work for `TryGet` too. EXIF rationals are stored by WIC as `ulong` / `long`
(numerator in the low 32 bits), `TryGetRational` converts them. A nested block is listed as a `WicMetadataBlock` with its
metadata format. `Orientation` is read with the path of the container or `System.Photo.Orientation`, `ColorProfile` is
the ICC profile of the first color context.

### Raw blocks

To carry metadata over unchanged, `RawBlocks` has the EXIF, XMP and IPTC blocks as bytes, and `AddRawBlock` writes one:

<!-- sample: ImagingSamples.CarryOverXmp -->
```csharp
// Copy the XMP block of the original unchanged into the new file
var original = WicImage.GetFrameInfo(source).Metadata;
WicImage.Encode(destination, new WicEncodeOptions
{
    ContainerFormat = WicContainerFormats.Jpeg,
    Metadata = metadata =>
    {
        foreach (var block in original.RawBlocks.Where(block => block.Format == WicMetadataFormats.Xmp))
        {
            metadata.AddRawBlock(new WicMetadataRawBlock(WicMetadataPaths.Xmp(WicContainerFormats.Jpeg), block.Format, block.Data));
        }
    }
}, pixels, width, height, stride, WicPixelFormats.Format32bppBgra);
```

### What the codecs of Windows accept

Tested with the codecs of Windows on the CI runner (Windows Server 2025, `windows-latest`), see `WicMetadataTests`:

| | JPEG | TIFF | PNG | JPEG XR | GIF | BMP |
|---|---|---|---|---|---|---|
| EXIF tags (Software, ImageDescription, Orientation, XResolution) | `/app1/ifd/{ushort=n}` | `/ifd/{ushort=n}` | - | `/ifd/{ushort=n}` | - | - |
| EXIF sub IFD (DateTimeOriginal, ExposureTime) | `/app1/ifd/exif/{ushort=n}` | `/ifd/exif/{ushort=n}` | - | `/ifd/exif/{ushort=n}` | - | - |
| XMP | `/xmp/...` | `/ifd/xmp/...` | `/xmp/...` | `/ifd/xmp/...` | - | - |
| PNG text | - | - | `/tEXt/{str=keyword}`, `/[1]tEXt/...`, `/iTXt/...` | - | - | - |
| Policies (System.ApplicationName, System.Title, System.Comment, System.Photo.Orientation) | yes | yes | yes | yes | - | - |
| Color profile | yes | yes | yes (iCCP) | yes | - | - |
| Raw blocks | `/app1`, `/xmp` | `/ifd/xmp` (tag 700) | `/xmp` | `/ifd/xmp` (tag 700) | - | - |

GIF has only its own blocks (`/imgdesc`, `/grctlext`, ...), BMP has no metadata. A PNG tEXt chunk holds one keyword: a
second keyword at `/tEXt` replaces the first, give each its own chunk index (`WicMetadataPaths.PngText("Description", 1)`).
HEIF and JPEG XL couldn't be tested on the CI runner (their encoders don't initialize there); their decoders are listed.
