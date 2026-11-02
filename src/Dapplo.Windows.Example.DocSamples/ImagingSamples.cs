// Copyright (c) Dapplo and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.Globalization;
using System.IO;
using System.Linq;
using Dapplo.Windows.Imaging;

namespace Dapplo.Windows.Example.DocSamples;

/// <summary>
/// Samples for doc/articles/imaging.md
/// </summary>
public static class ImagingSamples
{
    public static void DiscoverCodecs()
    {
        #region DiscoverCodecs
        // Every decoder WIC has, also the packaged (Store) codecs like HEIF, AVIF, WebP and JPEG XL
        foreach (var decoder in WicCodecs.GetDecoders())
        {
            Console.WriteLine($"{decoder.FriendlyName}: {string.Join(" ", decoder.FileExtensions)} ({string.Join(" ", decoder.MimeTypes)})");
        }

        // Can this PC write JPEG XL?
        bool canWriteJpegXl = WicCodecs.GetEncoders().Any(encoder => encoder.ContainerFormat == WicContainerFormats.JpegXl);
        #endregion
    }

    public static void EncodeBitmap(Bitmap bitmap, Stream destination)
    {
        #region EncodeBitmap
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
        #endregion
    }

    public static Bitmap DecodeIntoBitmap(Stream source)
    {
        #region DecodeIntoBitmap
        // Load any image WIC can read into a new GDI+ bitmap, upright, without WPF and without an extra copy
        Bitmap bitmap = null;
        BitmapData bitmapData = null;
        try
        {
            WicFrameInfo info = WicImage.Decode(source, WicPixelFormats.Format32bppBgra, (WicFrameInfo frame, out int stride) =>
            {
                bitmap = new Bitmap(frame.Width, frame.Height, PixelFormat.Format32bppArgb);
                bitmap.SetResolution((float)frame.DpiX, (float)frame.DpiY);
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
        #endregion
        return bitmap;
    }

    public static void EncodeRows(Stream destination, int width, int height, Func<int, byte[]> getRow)
    {
        #region EncodeRows
        // Pixels which aren't in one buffer (e.g. ImageSharp ProcessPixelRows): the encoder pulls the rows it needs
        WicImage.Encode(destination, new WicEncodeOptions { ContainerFormat = WicContainerFormats.Png }, width, height, WicPixelFormats.Format32bppBgra,
            (top, count, rows, stride) =>
            {
                for (int row = 0; row < count; row++)
                {
                    getRow(top + row).AsSpan().CopyTo(rows.Slice(row * stride));
                }
            });
        #endregion
    }

    public static void WriteMetadata(Stream destination, IntPtr pixels, int width, int height, int stride, byte[] iccProfile)
    {
        #region WriteMetadata
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
        #endregion
    }

    public static void ReadMetadata(Stream source)
    {
        #region ReadMetadata
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
        #endregion
    }

    public static void CarryOverXmp(Stream source, Stream destination, IntPtr pixels, int width, int height, int stride)
    {
        #region CarryOverXmp
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
        #endregion
    }
}
