// Copyright (c) Dapplo and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if !NETSTANDARD2_0
using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Dapplo.Windows.Icons;

/// <summary>
/// Provides pixel-level access to a bitmap using typed pixel structs for efficient row-based operations.
/// This is a shim that mimics ImageSharp's ProcessPixelRows API for easier migration.
/// </summary>
/// <remarks>
/// This class provides a safe abstraction for accessing bitmap pixel data using typed pixel structs.
/// It supports both 32-bit ARGB (Bgra32) and 24-bit RGB (Bgr24) formats. The bitmap is locked during access
/// to prevent corruption, and automatically unlocked when disposed.
/// 
/// <example>
/// Basic usage with typed pixel access:
/// <code>
/// var bitmap = new Bitmap(width, height, PixelFormat.Format32bppArgb);
/// using (var accessor = new BitmapAccessor&lt;Bgra32&gt;(bitmap))
/// {
///     for (int y = 0; y &lt; accessor.Height; y++)
///     {
///         var row = accessor.GetRowSpan(y);
///         for (int x = 0; x &lt; accessor.Width; x++)
///         {
///             row[x] = new Bgra32(red, green, blue, alpha);
///         }
///     }
/// }
/// </code>
/// 
/// Using ProcessRows for simpler iteration:
/// <code>
/// bitmap.ProcessPixelRows&lt;Bgra32&gt;(accessor =>
/// {
///     accessor.ProcessRows((y, row) =>
///     {
///         for (int x = 0; x &lt; accessor.Width; x++)
///         {
///             ref var pixel = ref row[x];
///             pixel.R = 255;
///             pixel.G = 0;
///             pixel.B = 0;
///             pixel.A = 255;
///         }
///     });
/// });
/// </code>
/// </example>
/// </remarks>
/// <typeparam name="TPixel">The pixel type (Bgra32 for 32-bit, Bgr24 for 24-bit).</typeparam>
public sealed class BitmapAccessor<TPixel> : IDisposable
    where TPixel : struct
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
    /// Initializes a new instance of the BitmapAccessor class.
    /// </summary>
    /// <param name="bitmap">The bitmap to access.</param>
    /// <param name="readOnly">If true, the bitmap is locked for read-only access; otherwise, it's locked for read-write access.</param>
    /// <exception cref="ArgumentNullException">Thrown when bitmap is null.</exception>
    /// <exception cref="NotSupportedException">Thrown when the bitmap's pixel format doesn't match TPixel.</exception>
    public BitmapAccessor(Bitmap bitmap, bool readOnly = false)
    {
        _bitmap = bitmap ?? throw new ArgumentNullException(nameof(bitmap));
        _readOnly = readOnly;

        // Validate pixel format matches TPixel
        ValidatePixelFormat(bitmap.PixelFormat);

        var flags = readOnly ? ImageLockMode.ReadOnly : ImageLockMode.ReadWrite;
        var rect = new Rectangle(0, 0, bitmap.Width, bitmap.Height);
        _bitmapData = bitmap.LockBits(rect, flags, bitmap.PixelFormat);
    }

    private static void ValidatePixelFormat(PixelFormat pixelFormat)
    {
        var pixelType = typeof(TPixel);
        var pixelSize = Unsafe.SizeOf<TPixel>();

        if (pixelType == typeof(Bgra32))
        {
            if (pixelFormat != PixelFormat.Format32bppArgb &&
                pixelFormat != PixelFormat.Format32bppRgb &&
                pixelFormat != PixelFormat.Format32bppPArgb)
            {
                throw new NotSupportedException($"Pixel format {pixelFormat} is not compatible with {pixelType.Name}. Use Format32bppArgb, Format32bppRgb, or Format32bppPArgb.");
            }
            if (pixelSize != 4)
            {
                throw new NotSupportedException($"Bgra32 must be 4 bytes, but is {pixelSize} bytes.");
            }
        }
        else if (pixelType == typeof(Bgr24))
        {
            if (pixelFormat != PixelFormat.Format24bppRgb)
            {
                throw new NotSupportedException($"Pixel format {pixelFormat} is not compatible with {pixelType.Name}. Use Format24bppRgb.");
            }
            if (pixelSize != 3)
            {
                throw new NotSupportedException($"Bgr24 must be 3 bytes, but is {pixelSize} bytes.");
            }
        }
        else
        {
            throw new NotSupportedException($"Pixel type {pixelType.Name} is not supported. Use Bgra32 or Bgr24.");
        }
    }

    /// <summary>
    /// Gets a span representing a single row of typed pixel data.
    /// </summary>
    /// <remarks>
    /// This method correctly handles both top-down and bottom-up bitmaps (negative stride).
    /// The returned span contains typed pixels, allowing direct manipulation of pixel components.
    /// </remarks>
    /// <param name="y">The zero-based row index.</param>
    /// <returns>A span of pixels representing the specified row.</returns>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when y is outside the bounds of the bitmap.</exception>
    public unsafe Span<TPixel> GetRowSpan(int y)
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
        return new Span<TPixel>(ptr, Width);
    }

    /// <summary>
    /// Processes all rows of the bitmap using the provided action.
    /// </summary>
    /// <param name="processRow">An action that processes each row. The action receives the row index and a span of typed pixels.</param>
    /// <exception cref="ArgumentNullException">Thrown when processRow is null.</exception>
    public void ProcessRows(Action<int, Span<TPixel>> processRow)
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
/// Provides extension methods for Bitmap to enable typed Span-based pixel access.
/// </summary>
public static class BitmapAccessorExtensions
{
    /// <summary>
    /// Processes pixel rows of two bitmaps simultaneously, providing typed access to both as source and target.
    /// </summary>
    /// <typeparam name="TPixel">The pixel type (Bgra32 for 32-bit, Bgr24 for 24-bit).</typeparam>
    /// <param name="targetBitmap">The target bitmap to write to.</param>
    /// <param name="sourceBitmap">The source bitmap to read from.</param>
    /// <param name="processRows">An action that processes each pair of rows.</param>
    /// <exception cref="ArgumentNullException">Thrown when any parameter is null.</exception>
    /// <exception cref="ArgumentException">Thrown when bitmaps have different dimensions.</exception>
    public static void ProcessPixelRows<TPixel>(this Bitmap targetBitmap, Bitmap sourceBitmap, Action<BitmapAccessor<TPixel>, BitmapAccessor<TPixel>> processRows)
        where TPixel : struct
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

        using var targetAccessor = new BitmapAccessor<TPixel>(targetBitmap, readOnly: false);
        using var sourceAccessor = new BitmapAccessor<TPixel>(sourceBitmap, readOnly: true);

        processRows(targetAccessor, sourceAccessor);
    }

    /// <summary>
    /// Processes pixel rows of a single bitmap with typed pixel access.
    /// </summary>
    /// <typeparam name="TPixel">The pixel type (Bgra32 for 32-bit, Bgr24 for 24-bit).</typeparam>
    /// <param name="bitmap">The bitmap to process.</param>
    /// <param name="processRows">An action that processes the bitmap rows.</param>
    /// <exception cref="ArgumentNullException">Thrown when any parameter is null.</exception>
    public static void ProcessPixelRows<TPixel>(this Bitmap bitmap, Action<BitmapAccessor<TPixel>> processRows)
        where TPixel : struct
    {
        if (bitmap == null)
        {
            throw new ArgumentNullException(nameof(bitmap));
        }
        if (processRows == null)
        {
            throw new ArgumentNullException(nameof(processRows));
        }

        using var accessor = new BitmapAccessor<TPixel>(bitmap, readOnly: false);
        processRows(accessor);
    }
}
#endif
