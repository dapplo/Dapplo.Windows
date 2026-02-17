// Copyright (c) Dapplo and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if !NETSTANDARD2_0
using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;

namespace Dapplo.Windows.Icons;

/// <summary>
/// Provides pixel-level access to a bitmap using Span&lt;byte&gt; for efficient row-based operations.
/// This is a shim that mimics ImageSharp's ProcessPixelRows API for easier migration.
/// </summary>
/// <remarks>
/// This class provides a safe abstraction for accessing bitmap pixel data using modern Span&lt;&gt; types.
/// It supports both 32-bit ARGB and 24-bit RGB formats. The bitmap is locked during access to prevent
/// corruption, and automatically unlocked when disposed.
/// </remarks>
public sealed class BitmapAccessor : IDisposable
{
    private readonly Bitmap _bitmap;
    private readonly BitmapData _bitmapData;
    private readonly bool _readOnly;

    /// <summary>
    /// Gets the width of the bitmap in pixels.
    /// </summary>
    public int Width => _bitmapData.Width;

    /// <summary>
    /// Gets the height of the bitmap in pixels.
    /// </summary>
    public int Height => _bitmapData.Height;

    /// <summary>
    /// Gets the stride (bytes per row) of the bitmap data.
    /// </summary>
    public int Stride => Math.Abs(_bitmapData.Stride);

    /// <summary>
    /// Gets the pixel format of the bitmap.
    /// </summary>
    public PixelFormat PixelFormat => _bitmapData.PixelFormat;

    /// <summary>
    /// Gets the number of bytes per pixel for the current pixel format.
    /// </summary>
    public int BytesPerPixel
    {
        get
        {
            return PixelFormat switch
            {
                PixelFormat.Format32bppArgb => 4,
                PixelFormat.Format32bppRgb => 4,
                PixelFormat.Format32bppPArgb => 4,
                PixelFormat.Format24bppRgb => 3,
                _ => throw new NotSupportedException($"Pixel format {PixelFormat} is not supported.")
            };
        }
    }

    /// <summary>
    /// Gets the number of bytes required to store pixel data for one row (excluding padding).
    /// </summary>
    public int RowWidthInBytes => Width * BytesPerPixel;

    /// <summary>
    /// Initializes a new instance of the BitmapAccessor class.
    /// </summary>
    /// <param name="bitmap">The bitmap to access.</param>
    /// <param name="readOnly">If true, the bitmap is locked for read-only access; otherwise, it's locked for read-write access.</param>
    /// <exception cref="ArgumentNullException">Thrown when bitmap is null.</exception>
    /// <exception cref="NotSupportedException">Thrown when the bitmap's pixel format is not supported.</exception>
    public BitmapAccessor(Bitmap bitmap, bool readOnly = false)
    {
        _bitmap = bitmap ?? throw new ArgumentNullException(nameof(bitmap));
        _readOnly = readOnly;

        // Validate pixel format
        if (bitmap.PixelFormat != PixelFormat.Format32bppArgb &&
            bitmap.PixelFormat != PixelFormat.Format32bppRgb &&
            bitmap.PixelFormat != PixelFormat.Format32bppPArgb &&
            bitmap.PixelFormat != PixelFormat.Format24bppRgb)
        {
            throw new NotSupportedException($"Pixel format {bitmap.PixelFormat} is not supported. Only 32-bit ARGB and 24-bit RGB formats are supported.");
        }

        var flags = readOnly ? ImageLockMode.ReadOnly : ImageLockMode.ReadWrite;
        var rect = new Rectangle(0, 0, bitmap.Width, bitmap.Height);
        _bitmapData = bitmap.LockBits(rect, flags, bitmap.PixelFormat);
    }

    /// <summary>
    /// Gets a span representing a single row of pixel data.
    /// </summary>
    /// <remarks>
    /// The returned span includes the full stride, which may contain padding bytes after the pixel data.
    /// This method correctly handles both top-down and bottom-up bitmaps (negative stride).
    /// For pixel access, use offsets calculated as: pixelIndex * BytesPerPixel.
    /// </remarks>
    /// <param name="y">The zero-based row index.</param>
    /// <returns>A span of bytes representing the pixel data for the specified row.</returns>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when y is outside the bounds of the bitmap.</exception>
    public unsafe Span<byte> GetRowSpan(int y)
    {
        if (y < 0 || y >= Height)
        {
            throw new ArgumentOutOfRangeException(nameof(y), $"Row index {y} is out of range [0, {Height}).");
        }

        // Note: _bitmapData.Stride can be negative for bottom-up bitmaps.
        // The pointer arithmetic correctly handles both cases:
        // - Top-down: Scan0 is row 0, positive stride moves down
        // - Bottom-up: Scan0 is last row, negative stride moves up
        var ptr = (byte*)_bitmapData.Scan0 + (y * _bitmapData.Stride);
        return new Span<byte>(ptr, Stride);
    }

    /// <summary>
    /// Processes all rows of the bitmap using the provided action.
    /// </summary>
    /// <param name="processRow">An action that processes each row. The action receives the row index and a span of the row data.</param>
    /// <exception cref="ArgumentNullException">Thrown when processRow is null.</exception>
    public void ProcessRows(Action<int, Span<byte>> processRow)
    {
        if (processRow == null)
        {
            throw new ArgumentNullException(nameof(processRow));
        }

        for (int y = 0; y < Height; y++)
        {
            var rowSpan = GetRowSpan(y);
            processRow(y, rowSpan);
        }
    }

    /// <summary>
    /// Releases the bitmap lock and frees resources.
    /// </summary>
    public void Dispose()
    {
        _bitmap.UnlockBits(_bitmapData);
    }
}

/// <summary>
/// Provides extension methods for Bitmap to enable Span-based pixel access.
/// </summary>
public static class BitmapAccessorExtensions
{
    /// <summary>
    /// Processes pixel rows of two bitmaps simultaneously, providing access to both as source and target.
    /// </summary>
    /// <param name="targetBitmap">The target bitmap to write to.</param>
    /// <param name="sourceBitmap">The source bitmap to read from.</param>
    /// <param name="processRows">An action that processes each pair of rows.</param>
    /// <exception cref="ArgumentNullException">Thrown when any parameter is null.</exception>
    /// <exception cref="ArgumentException">Thrown when bitmaps have different dimensions.</exception>
    public static void ProcessPixelRows(this Bitmap targetBitmap, Bitmap sourceBitmap, Action<BitmapAccessor, BitmapAccessor> processRows)
    {
        if (targetBitmap == null)
        {
            throw new ArgumentNullException(nameof(targetBitmap));
        }
        if (sourceBitmap == null)
        {
            throw new ArgumentNullException(nameof(sourceBitmap));
        }
        if (processRows == null)
        {
            throw new ArgumentNullException(nameof(processRows));
        }

        if (targetBitmap.Width != sourceBitmap.Width || targetBitmap.Height != sourceBitmap.Height)
        {
            throw new ArgumentException("Source and target bitmaps must have the same dimensions.");
        }

        using var targetAccessor = new BitmapAccessor(targetBitmap, readOnly: false);
        using var sourceAccessor = new BitmapAccessor(sourceBitmap, readOnly: true);

        processRows(targetAccessor, sourceAccessor);
    }

    /// <summary>
    /// Processes pixel rows of a single bitmap.
    /// </summary>
    /// <param name="bitmap">The bitmap to process.</param>
    /// <param name="processRows">An action that processes the bitmap rows.</param>
    /// <exception cref="ArgumentNullException">Thrown when any parameter is null.</exception>
    public static void ProcessPixelRows(this Bitmap bitmap, Action<BitmapAccessor> processRows)
    {
        if (bitmap == null)
        {
            throw new ArgumentNullException(nameof(bitmap));
        }
        if (processRows == null)
        {
            throw new ArgumentNullException(nameof(processRows));
        }

        using var accessor = new BitmapAccessor(bitmap, readOnly: false);
        processRows(accessor);
    }
}
#endif
