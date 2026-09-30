// Copyright (c) Dapplo and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using Dapplo.Windows.Icons.Structs;

namespace Dapplo.Windows.Icons;

/// <summary>
/// Helper class for creating icon files using the proper ICO file format structures.
/// See <a href="https://devblogs.microsoft.com/oldnewthing/20101018-00/?p=12513">The format of icon resources</a>
/// </summary>
public static class IconFileWriter
{
    /// <summary>
    /// Writes icon images to a stream using the ICO file format with proper structures.
    /// Images larger than 256 pixels in either dimension are scaled down to fit into 256x256, see <see cref="CalculateIconImageSize"/>.
    /// </summary>
    /// <param name="stream">Stream to write to</param>
    /// <param name="images">Collection of images to include in the icon</param>
    public static void WriteIconFile(Stream stream, IEnumerable<Image> images)
    {
        if (stream == null)
        {
            throw new ArgumentNullException(nameof(stream));
        }
        if (images == null)
        {
            throw new ArgumentNullException(nameof(images));
        }

        var imageList = images.ToList();
        if (imageList.Count == 0)
        {
            throw new ArgumentException("At least one image is required", nameof(images));
        }

        using var binaryWriter = new BinaryWriter(stream, System.Text.Encoding.Default, leaveOpen: true);

        // Encode all images to PNG format
        var encodedImages = new List<(Size size, MemoryStream data)>();
        try
        {
            foreach (var image in imageList)
            {
                if (image == null)
                {
                    throw new ArgumentException("The images must not contain null", nameof(images));
                }
                var imageStream = EncodeImage(image, out var size, out _);
                encodedImages.Add((size, imageStream));
            }

            // Write ICONDIR header
            var iconDir = IconDir.CreateIcon((ushort)encodedImages.Count);
            WriteIconDir(binaryWriter, iconDir);

            // Calculate offsets for image data
            var offset = (uint)(IconDir.Size + encodedImages.Count * IconDirEntry.Size);

            // Write ICONDIRENTRY structures
            var entries = new List<IconDirEntry>();
            foreach (var (size, data) in encodedImages)
            {
                var entry = IconDirEntry.CreateForIcon(
                    size.Width,
                    size.Height,
                    32, // 32 bits per pixel for PNG
                    (uint)data.Length,
                    offset
                );
                entries.Add(entry);
                WriteIconDirEntry(binaryWriter, entry);
                offset += (uint)data.Length;
            }

            // Write image data
            foreach (var (_, data) in encodedImages)
            {
                data.WriteTo(stream);
            }
        }
        finally
        {
            // Clean up encoded image streams
            foreach (var (_, data) in encodedImages)
            {
                data?.Dispose();
            }
        }
    }

    /// <summary>
    /// Writes icon images to a file using the ICO file format.
    /// </summary>
    /// <param name="filePath">Path to the output icon file</param>
    /// <param name="images">Collection of images to include in the icon</param>
    public static void WriteIconFile(string filePath, IEnumerable<Image> images)
    {
        if (filePath == null)
        {
            throw new ArgumentNullException(nameof(filePath));
        }

        using var fileStream = new FileStream(filePath, FileMode.Create, FileAccess.Write, FileShare.None);
        WriteIconFile(fileStream, images);
    }

    /// <summary>
    /// Writes cursor images to a stream using the CUR file format.
    /// Images larger than 256 pixels in either dimension are scaled down to fit into 256x256, the hotspot is scaled with them.
    /// </summary>
    /// <param name="stream">Stream to write to</param>
    /// <param name="images">Collection of images with hotspot information</param>
    public static void WriteCursorFile(Stream stream, IEnumerable<(Image image, Point hotspot)> images)
    {
        if (stream == null)
        {
            throw new ArgumentNullException(nameof(stream));
        }
        if (images == null)
        {
            throw new ArgumentNullException(nameof(images));
        }

        var imageList = images.ToList();
        if (imageList.Count == 0)
        {
            throw new ArgumentException("At least one image is required", nameof(images));
        }

        using var binaryWriter = new BinaryWriter(stream, System.Text.Encoding.Default, leaveOpen: true);

        // Encode all images to PNG format
        var encodedImages = new List<(Size size, Point hotspot, MemoryStream data)>();
        try
        {
            foreach (var (image, hotspot) in imageList)
            {
                if (image == null)
                {
                    throw new ArgumentException("The images must not contain null", nameof(images));
                }
                if (hotspot.X < 0 || hotspot.Y < 0 || hotspot.X >= image.Width || hotspot.Y >= image.Height)
                {
                    throw new ArgumentOutOfRangeException(nameof(images), hotspot, $"The hotspot must be inside the {image.Width}x{image.Height} image.");
                }
                var imageStream = EncodeImage(image, out var size, out var scale);
                // Scale the hotspot together with the image
                var scaledHotspot = new Point(
                    Math.Min(size.Width - 1, (int)(hotspot.X * scale)),
                    Math.Min(size.Height - 1, (int)(hotspot.Y * scale)));
                encodedImages.Add((size, scaledHotspot, imageStream));
            }

            // Write ICONDIR header (Type = 2 for cursor)
            var iconDir = IconDir.CreateCursor((ushort)encodedImages.Count);
            WriteIconDir(binaryWriter, iconDir);

            // Calculate offsets for image data
            var offset = (uint)(IconDir.Size + encodedImages.Count * IconDirEntry.Size);

            // Write ICONDIRENTRY structures for cursors
            var entries = new List<IconDirEntry>();
            foreach (var (size, hotspot, data) in encodedImages)
            {
                var entry = IconDirEntry.CreateForCursor(
                    size.Width,
                    size.Height,
                    checked((ushort)hotspot.X),
                    checked((ushort)hotspot.Y),
                    (uint)data.Length,
                    offset
                );
                entries.Add(entry);
                WriteIconDirEntry(binaryWriter, entry);
                offset += (uint)data.Length;
            }

            // Write image data
            foreach (var (_, _, data) in encodedImages)
            {
                data.WriteTo(stream);
            }
        }
        finally
        {
            // Clean up encoded image streams
            foreach (var (_, _, data) in encodedImages)
            {
                data?.Dispose();
            }
        }
    }

    /// <summary>
    /// Writes cursor images to a file using the CUR file format.
    /// </summary>
    /// <param name="filePath">Path to the output cursor file</param>
    /// <param name="images">Collection of images with hotspot information</param>
    public static void WriteCursorFile(string filePath, IEnumerable<(Image image, Point hotspot)> images)
    {
        if (filePath == null)
        {
            throw new ArgumentNullException(nameof(filePath));
        }

        using var fileStream = new FileStream(filePath, FileMode.Create, FileAccess.Write, FileShare.None);
        WriteCursorFile(fileStream, images);
    }

    /// <summary>
    /// Encode the image as PNG, an image which is larger than 256 pixels in either dimension is scaled down (keeping the aspect ratio) to fit into 256x256,
    /// as this is the maximum the ICO and CUR formats support.
    /// </summary>
    /// <param name="image">Image to encode</param>
    /// <param name="size">Size of the encoded image</param>
    /// <param name="scale">double with the scale factor which was applied (1 if not scaled)</param>
    /// <returns>MemoryStream with the PNG data, positioned at the start</returns>
    private static MemoryStream EncodeImage(Image image, out Size size, out double scale)
    {
        if (image.Width < 1 || image.Height < 1)
        {
            throw new ArgumentException($"Image size {image.Width}x{image.Height} is not valid for an icon or cursor.", nameof(image));
        }
        size = CalculateIconImageSize(image.Size, out scale);
        var imageStream = new MemoryStream();
        try
        {
            if (size == image.Size)
            {
                image.Save(imageStream, ImageFormat.Png);
            }
            else
            {
                using var scaledImage = new Bitmap(size.Width, size.Height, PixelFormat.Format32bppArgb);
                using (var graphics = Graphics.FromImage(scaledImage))
                {
                    graphics.CompositingMode = System.Drawing.Drawing2D.CompositingMode.SourceCopy;
                    graphics.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.HighQualityBicubic;
                    graphics.PixelOffsetMode = System.Drawing.Drawing2D.PixelOffsetMode.HighQuality;
                    graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.HighQuality;
                    using var imageAttributes = new ImageAttributes();
                    imageAttributes.SetWrapMode(System.Drawing.Drawing2D.WrapMode.TileFlipXY);
                    graphics.DrawImage(image, new Rectangle(0, 0, size.Width, size.Height), 0, 0, image.Width, image.Height, GraphicsUnit.Pixel, imageAttributes);
                }
                scaledImage.Save(imageStream, ImageFormat.Png);
            }
            imageStream.Seek(0, SeekOrigin.Begin);
            return imageStream;
        }
        catch
        {
            imageStream.Dispose();
            throw;
        }
    }

    /// <summary>
    /// Calculate the size an image will have in an icon or cursor file: images larger than 256 pixels in either dimension are scaled down,
    /// keeping the aspect ratio, to fit into 256x256.
    /// </summary>
    /// <param name="imageSize">Size of the original image</param>
    /// <param name="scale">double with the scale factor (1 if not scaled)</param>
    /// <returns>Size of the image in the icon or cursor file</returns>
    public static Size CalculateIconImageSize(Size imageSize, out double scale)
    {
        const int maxSize = IconDirEntry.MaxImageSize;
        if (imageSize.Width <= maxSize && imageSize.Height <= maxSize)
        {
            scale = 1;
            return imageSize;
        }
        scale = Math.Min((double)maxSize / imageSize.Width, (double)maxSize / imageSize.Height);
        var width = Math.Max(1, Math.Min(maxSize, (int)Math.Round(imageSize.Width * scale)));
        var height = Math.Max(1, Math.Min(maxSize, (int)Math.Round(imageSize.Height * scale)));
        return new Size(width, height);
    }

    /// <summary>
    /// Writes an ICONDIR structure to a binary writer.
    /// </summary>
    /// <param name="writer">Binary writer</param>
    /// <param name="iconDir">IconDir structure to write</param>
    private static void WriteIconDir(BinaryWriter writer, IconDir iconDir)
    {
        writer.Write(iconDir.Reserved);
        writer.Write(iconDir.Type);
        writer.Write(iconDir.Count);
    }

    /// <summary>
    /// Writes an ICONDIRENTRY structure to a binary writer.
    /// </summary>
    /// <param name="writer">Binary writer</param>
    /// <param name="entry">IconDirEntry structure to write</param>
    private static void WriteIconDirEntry(BinaryWriter writer, IconDirEntry entry)
    {
        writer.Write(entry.Width);
        writer.Write(entry.Height);
        writer.Write(entry.ColorCount);
        writer.Write(entry.Reserved);
        writer.Write(entry.Planes);
        writer.Write(entry.BitCount);
        writer.Write(entry.BytesInRes);
        writer.Write(entry.ImageOffset);
    }

    /// <summary>
    /// Writes a GRPICONDIR structure to a binary writer.
    /// </summary>
    /// <param name="writer">Binary writer</param>
    /// <param name="grpIconDir">GrpIconDir structure to write</param>
    public static void WriteGrpIconDir(BinaryWriter writer, GrpIconDir grpIconDir)
    {
        writer.Write(grpIconDir.Reserved);
        writer.Write(grpIconDir.Type);
        writer.Write(grpIconDir.Count);
    }

    /// <summary>
    /// Writes a GRPICONDIRENTRY structure to a binary writer.
    /// </summary>
    /// <param name="writer">Binary writer</param>
    /// <param name="entry">GrpIconDirEntry structure to write</param>
    public static void WriteGrpIconDirEntry(BinaryWriter writer, GrpIconDirEntry entry)
    {
        writer.Write(entry.Width);
        writer.Write(entry.Height);
        writer.Write(entry.ColorCount);
        writer.Write(entry.Reserved);
        writer.Write(entry.Planes);
        writer.Write(entry.BitCount);
        writer.Write(entry.BytesInRes);
        writer.Write(entry.Id);
    }
}
