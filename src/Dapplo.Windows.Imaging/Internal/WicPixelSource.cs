// Copyright (c) Dapplo and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Buffers;
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

    public HResult CopyPalette(nint palette) => WicErrors.PaletteUnavailable;

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
