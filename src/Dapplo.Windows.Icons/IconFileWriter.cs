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
                var imageStream = new MemoryStream();
                image.Save(imageStream, ImageFormat.Png);
                imageStream.Seek(0, SeekOrigin.Begin);
                encodedImages.Add((image.Size, imageStream));
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
                var imageStream = new MemoryStream();
                image.Save(imageStream, ImageFormat.Png);
                imageStream.Seek(0, SeekOrigin.Begin);
                encodedImages.Add((image.Size, hotspot, imageStream));
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
                // Note: For cursors, Planes field stores hotspot X and BitCount field stores hotspot Y
                var entry = IconDirEntry.CreateForCursor(
                    size.Width,
                    size.Height,
                    (ushort)hotspot.X,
                    (ushort)hotspot.Y,
                    32, // Bits per pixel for the cursor image
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
