// Copyright (c) Dapplo and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if !NETSTANDARD2_0
using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Dapplo.Windows.Common.Structs.PixelFormats;

namespace Dapplo.Windows.Common;

/// <summary>
/// Provides pixel-level access to a bitmap using typed pixel structs for efficient row-based operations.
/// This is a shim that mimics ImageSharp's ProcessPixelRows API for easier migration.
/// See BitmapAccessor.md for detailed documentation and examples.
/// </summary>
/// <typeparam name="TPixel">The pixel type (Bgra32 for 32-bit, Bgr24 for 24-bit, Indexed8 for 8-bit indexed).</typeparam>
public sealed class BitmapAccessor<TPixel> : IDisposable
    where TPixel : struct
{
    private readonly Bitmap _bitmap;
    private readonly BitmapData _bitmapData;
    private readonly bool _readOnly;
    private Bgra32[] _paletteCache;

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

        // For indexed formats, cache the palette
        if (typeof(TPixel) == typeof(Indexed8))
        {
            CachePalette();
        }
    }

    private void CachePalette()
    {
        var palette = _bitmap.Palette;
        _paletteCache = new Bgra32[palette.Entries.Length];
        for (int i = 0; i < palette.Entries.Length; i++)
        {
            var color = palette.Entries[i];
            _paletteCache[i] = new Bgra32(color.R, color.G, color.B, color.A);
        }
    }

    private void ApplyPalette()
    {
        if (_paletteCache == null || _readOnly)
            return;

        var palette = _bitmap.Palette;
        for (int i = 0; i < _paletteCache.Length && i < palette.Entries.Length; i++)
        {
            var bgra = _paletteCache[i];
            palette.Entries[i] = Color.FromArgb(bgra.A, bgra.R, bgra.G, bgra.B);
        }
        _bitmap.Palette = palette;
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
        else if (pixelType == typeof(Indexed8))
        {
            if (pixelFormat != PixelFormat.Format8bppIndexed)
            {
                throw new NotSupportedException($"Pixel format {pixelFormat} is not compatible with {pixelType.Name}. Use Format8bppIndexed.");
            }
            if (pixelSize != 1)
            {
                throw new NotSupportedException($"Indexed8 must be 1 byte, but is {pixelSize} bytes.");
            }
        }
        else
        {
            throw new NotSupportedException($"Pixel type {pixelType.Name} is not supported. Use Bgra32, Bgr24, or Indexed8.");
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
    /// Gets the palette as a span of Bgra32 colors for indexed formats.
    /// </summary>
    /// <returns>A span of Bgra32 representing the palette colors.</returns>
    /// <exception cref="NotSupportedException">Thrown when the pixel format is not indexed.</exception>
    public Span<Bgra32> GetPaletteSpan()
    {
        if (typeof(TPixel) != typeof(Indexed8))
        {
            throw new NotSupportedException("GetPaletteSpan is only supported for Indexed8 pixel format.");
        }

        if (_paletteCache == null)
        {
            throw new InvalidOperationException("Palette cache is not initialized.");
        }

        return new Span<Bgra32>(_paletteCache);
    }

    /// <summary>
    /// Delegate for processing a single row of pixel data.
    /// </summary>
    /// <typeparam name="TRowPixel">The pixel type.</typeparam>
    /// <param name="rowIndex">The zero-based row index.</param>
    /// <param name="rowSpan">A span of pixels representing the row.</param>
    public delegate void ProcessRowDelegate<TRowPixel>(int rowIndex, Span<TRowPixel> rowSpan) where TRowPixel : struct;

    /// <summary>
    /// Processes all rows of the bitmap using the provided action.
    /// </summary>
    /// <param name="processRow">An action that processes each row. The action receives the row index and a span of typed pixels.</param>
    /// <exception cref="ArgumentNullException">Thrown when processRow is null.</exception>
    public void ProcessRows(ProcessRowDelegate<TPixel> processRow)
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
    /// For indexed formats, applies palette changes back to the bitmap.
    /// </summary>
    public void Dispose()
    {
        ApplyPalette();
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
    /// <typeparam name="TPixel">The pixel type (Bgra32 for 32-bit, Bgr24 for 24-bit, Indexed8 for 8-bit indexed).</typeparam>
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
    /// <typeparam name="TPixel">The pixel type (Bgra32 for 32-bit, Bgr24 for 24-bit, Indexed8 for 8-bit indexed).</typeparam>
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
