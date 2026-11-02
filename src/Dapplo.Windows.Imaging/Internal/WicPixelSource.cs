// Copyright (c) Dapplo and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Buffers;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using Dapplo.Windows.Common.Enums;
using Dapplo.Windows.Imaging.Interop;

namespace Dapplo.Windows.Imaging.Internal;

/// <summary>
/// IWICBitmapSource implemented in .NET, WIC (an encoder or a format converter) pulls the rows it needs with CopyPixels.
/// Exceptions become an HRESULT for WIC and are kept in <see cref="LastException"/>.
/// </summary>
[ComVisible(true)]
internal abstract class WicPixelSource : IWICBitmapSource, IFailureSource
{
    protected WicPixelSource(int width, int height, Guid pixelFormat, int bitsPerPixel, double dpiX, double dpiY)
    {
        Width = width;
        Height = height;
        PixelFormat = pixelFormat;
        BitsPerPixel = bitsPerPixel;
        DpiX = dpiX;
        DpiY = dpiY;
        RowBytes = Wic.RowBytes(width, bitsPerPixel);
    }

    public int Width { get; }
    public int Height { get; }
    public Guid PixelFormat { get; }
    public int BitsPerPixel { get; }
    public int RowBytes { get; }
    public double DpiX { get; }
    public double DpiY { get; }
    public Exception LastException { get; private set; }

    public HResult GetSize(out uint width, out uint height)
    {
        width = (uint)Width;
        height = (uint)Height;
        return HResult.S_OK;
    }

    public HResult GetPixelFormat(out Guid pixelFormat)
    {
        pixelFormat = PixelFormat;
        return HResult.S_OK;
    }

    public HResult GetResolution(out double dpiX, out double dpiY)
    {
        dpiX = DpiX;
        dpiY = DpiY;
        return HResult.S_OK;
    }

    /// <summary>
    /// The colors of an indexed pixel format (WICColor, 0xAARRGGBB), null for the other formats
    /// </summary>
    public uint[] Palette { get; set; }

    public HResult CopyPalette(nint palette)
    {
        if (Palette == null || Palette.Length == 0)
        {
            return WicErrors.PaletteUnavailable;
        }
        if (palette == IntPtr.Zero)
        {
            return HResult.E_INVALIDARG;
        }
        object paletteObject = null;
        var handle = GCHandle.Alloc(Palette, GCHandleType.Pinned);
        try
        {
            paletteObject = Marshal.GetObjectForIUnknown(palette);
            return ((IWICPalette)paletteObject).InitializeCustom(handle.AddrOfPinnedObject(), (uint)Palette.Length);
        }
        catch (Exception ex)
        {
            LastException = ex;
            return ex.HResult < 0 ? (HResult)ex.HResult : HResult.E_FAIL;
        }
        finally
        {
            handle.Free();
            Wic.Release(paletteObject);
        }
    }

    public unsafe HResult CopyPixels(nint rectangle, uint stride, uint bufferSize, nint buffer)
    {
        try
        {
            var rect = rectangle == IntPtr.Zero ? new WicRect(0, 0, Width, Height) : *(WicRect*)rectangle;
            if (rect.X < 0 || rect.Y < 0 || rect.Width < 0 || rect.Height < 0 || rect.X + rect.Width > Width || rect.Y + rect.Height > Height)
            {
                return HResult.E_INVALIDARG;
            }
            if (rect.Width == 0 || rect.Height == 0)
            {
                return HResult.S_OK;
            }
            if (buffer == IntPtr.Zero || ((long)rect.X * BitsPerPixel) % 8 != 0)
            {
                return HResult.E_INVALIDARG;
            }
            var rectRowBytes = Wic.RowBytes(rect.Width, BitsPerPixel);
            if (stride < rectRowBytes || bufferSize < (long)(rect.Height - 1) * stride + rectRowBytes)
            {
                return HResult.E_INVALIDARG;
            }
            CopyRows(rect, (int)((long)rect.X * BitsPerPixel / 8), rectRowBytes, (int)stride, bufferSize, (byte*)buffer);
            return HResult.S_OK;
        }
        catch (Exception ex)
        {
            LastException = ex;
            return ex.HResult < 0 ? (HResult)ex.HResult : HResult.E_FAIL;
        }
    }

    /// <summary>
    /// Copy the rows of the rectangle into the destination
    /// </summary>
    /// <param name="rect">the rectangle, validated</param>
    /// <param name="xOffset">the byte offset of rect.X in a row</param>
    /// <param name="rectRowBytes">bytes of a row of the rectangle</param>
    /// <param name="destinationStride">bytes per row in the destination</param>
    /// <param name="destinationSize">size of the destination</param>
    /// <param name="destination">the destination</param>
    protected abstract unsafe void CopyRows(WicRect rect, int xOffset, int rectRowBytes, int destinationStride, uint destinationSize, byte* destination);
}

/// <summary>
/// Pixels in memory, e.g. GDI+ BitmapData.Scan0 or a contiguous ImageSharp buffer, read without a copy of the image
/// </summary>
internal sealed class WicMemoryPixelSource : WicPixelSource
{
    private readonly nint _pixels;
    private readonly int _stride;

    public WicMemoryPixelSource(nint pixels, int width, int height, int stride, Guid pixelFormat, int bitsPerPixel, double dpiX, double dpiY)
        : base(width, height, pixelFormat, bitsPerPixel, dpiX, dpiY)
    {
        _pixels = pixels;
        _stride = stride;
    }

    protected override unsafe void CopyRows(WicRect rect, int xOffset, int rectRowBytes, int destinationStride, uint destinationSize, byte* destination)
    {
        var source = (byte*)_pixels;
        for (int row = 0; row < rect.Height; row++)
        {
            Buffer.MemoryCopy(source + (long)(rect.Y + row) * _stride + xOffset, destination + (long)row * destinationStride, rectRowBytes, rectRowBytes);
        }
    }
}

/// <summary>
/// Pixels supplied by a <see cref="WicRowProvider"/>, for buffers which aren't contiguous (e.g. ImageSharp ProcessPixelRows)
/// </summary>
internal sealed class WicRowPixelSource : WicPixelSource
{
    private readonly WicRowProvider _rows;

    public WicRowPixelSource(WicRowProvider rows, int width, int height, Guid pixelFormat, int bitsPerPixel, double dpiX, double dpiY)
        : base(width, height, pixelFormat, bitsPerPixel, dpiX, dpiY)
    {
        _rows = rows;
    }

    protected override unsafe void CopyRows(WicRect rect, int xOffset, int rectRowBytes, int destinationStride, uint destinationSize, byte* destination)
    {
        if (rect.X == 0 && rect.Width == Width)
        {
            // The whole width: the callback writes straight into the WIC buffer
            var length = (int)Math.Min(destinationSize, (long)(rect.Height - 1) * destinationStride + rectRowBytes);
            _rows(rect.Y, rect.Height, new Span<byte>(destination, length), destinationStride);
            return;
        }

        // Part of the width: get each full row and copy the requested part
        var rowBuffer = ArrayPool<byte>.Shared.Rent(RowBytes);
        try
        {
            for (int row = 0; row < rect.Height; row++)
            {
                _rows(rect.Y + row, 1, new Span<byte>(rowBuffer, 0, RowBytes), RowBytes);
                new ReadOnlySpan<byte>(rowBuffer, xOffset, rectRowBytes).CopyTo(new Span<byte>(destination + (long)row * destinationStride, rectRowBytes));
            }
        }
        finally
        {
            ArrayPool<byte>.Shared.Return(rowBuffer);
        }
    }
}

/// <summary>
/// 8 bit palette indexes for 32bpp BGRA pixels, each pixel gets its exact color or the nearest color of the palette.
/// The WIC format converter maps colors through a reduced lookup and can pick a neighbor of an exact color, this keeps the colors exact.
/// </summary>
internal sealed class WicIndexingPixelSource : WicPixelSource
{
    private readonly IWICBitmapSource _bgra;
    private readonly IFailureSource _bgraFailures;
    private readonly Dictionary<uint, byte> _indexes = new Dictionary<uint, byte>();
    private readonly int _transparentIndex = -1;

    /// <param name="bgra">the pixels as GUID_WICPixelFormat32bppBGRA</param>
    /// <param name="bgraFailures">the source which reports the exceptions of the pixels, e.g. of a row callback</param>
    /// <param name="palette">at most 256 colors (0xAARRGGBB)</param>
    public WicIndexingPixelSource(IWICBitmapSource bgra, IFailureSource bgraFailures, uint[] palette, int width, int height, double dpiX, double dpiY)
        : base(width, height, WicPixelFormats.Format8bppIndexed, 8, dpiX, dpiY)
    {
        _bgra = bgra;
        _bgraFailures = bgraFailures;
        Palette = palette;
        for (int i = 0; i < palette.Length; i++)
        {
            if (palette[i] >> 24 == 0)
            {
                _transparentIndex = i;
                break;
            }
        }
    }

    protected override unsafe void CopyRows(WicRect rect, int xOffset, int rectRowBytes, int destinationStride, uint destinationSize, byte* destination)
    {
        var rowPixels = rect.Width;
        var bandRows = Math.Max(1, Math.Min(rect.Height, 1024 * 1024 / Math.Max(1, rowPixels)));
        var buffer = ArrayPool<uint>.Shared.Rent(bandRows * rowPixels);
        try
        {
            fixed (uint* bufferPointer = buffer)
            {
                for (int top = 0; top < rect.Height; top += bandRows)
                {
                    var count = Math.Min(bandRows, rect.Height - top);
                    var band = new WicRect(rect.X, rect.Y + top, rect.Width, count);
                    Wic.Check(_bgra.CopyPixels((nint)(&band), (uint)(rowPixels * 4), (uint)(count * rowPixels * 4), (nint)bufferPointer), "IWICBitmapSource.CopyPixels", _bgraFailures);
                    for (int row = 0; row < count; row++)
                    {
                        var target = destination + (long)(top + row) * destinationStride;
                        var source = bufferPointer + row * rowPixels;
                        for (int x = 0; x < rowPixels; x++)
                        {
                            target[x] = IndexOf(source[x]);
                        }
                    }
                }
            }
        }
        finally
        {
            ArrayPool<uint>.Shared.Return(buffer);
        }
    }

    private byte IndexOf(uint color)
    {
        // Mostly transparent pixels (below the threshold of 50 % WIC uses) get the transparent color, when the palette has one
        if (color >> 24 < 128 && _transparentIndex >= 0)
        {
            return (byte)_transparentIndex;
        }
        if (_indexes.TryGetValue(color, out var index))
        {
            return index;
        }
        var best = 0;
        var bestDistance = long.MaxValue;
        for (int i = 0; i < Palette.Length; i++)
        {
            var entry = Palette[i];
            if (entry == color)
            {
                best = i;
                break;
            }
            if (i == _transparentIndex)
            {
                continue;
            }
            long db = (int)(entry & 0xFF) - (int)(color & 0xFF);
            long dg = (int)(entry >> 8 & 0xFF) - (int)(color >> 8 & 0xFF);
            long dr = (int)(entry >> 16 & 0xFF) - (int)(color >> 16 & 0xFF);
            var distance = dr * dr + dg * dg + db * db;
            if (distance < bestDistance)
            {
                bestDistance = distance;
                best = i;
            }
        }
        _indexes[color] = (byte)best;
        return (byte)best;
    }
}
