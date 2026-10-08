// Copyright (c) Dapplo and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
using System;
using Dapplo.Windows.Clipboard.Internals;

namespace Dapplo.Windows.Clipboard;

/// <summary>
/// Read and write bitmaps as CF_DIB / CF_DIBV5 without System.Drawing: the pixels are raw BGRA32 bytes,
/// so any imaging library can be used to produce or consume them.
/// </summary>
public static class ClipboardDibExtensions
{
    /// <summary>
    /// Place a bitmap as CF_DIBV5 and / or CF_DIB on the clipboard, see <see cref="DibImage.CreateDibV5"/> and <see cref="DibImage.CreateDib"/>.
    /// The DIBs are encoded directly into the clipboard memory, without intermediate arrays.
    /// Call ClearContents first. Tip: also place a "PNG" format, it's the best choice for applications which support it.
    /// </summary>
    /// <param name="clipboardAccessToken">IClipboardAccessToken</param>
    /// <param name="bgra32">the pixels, top-down rows of B, G, R, A bytes</param>
    /// <param name="width">int with the width</param>
    /// <param name="height">int with the height</param>
    /// <param name="stride">int with the bytes per row of <paramref name="bgra32"/>, at least width * 4</param>
    /// <param name="premultipliedAlpha">true when the pixels have premultiplied alpha, they are written with straight alpha</param>
    /// <param name="formats">DibFormats, default both; CF_DIBV5 is placed first</param>
    public static void SetAsDib(this IClipboardAccessToken clipboardAccessToken, ReadOnlySpan<byte> bgra32, int width, int height, int stride,
        bool premultipliedAlpha, DibFormats formats = DibFormats.DibV5 | DibFormats.Dib)
    {
        clipboardAccessToken.ThrowWhenNoAccess();
        if ((formats & DibFormats.DibV5) != 0)
        {
            WriteDib(clipboardAccessToken, bgra32, width, height, stride, premultipliedAlpha, true);
        }
        if ((formats & DibFormats.Dib) != 0)
        {
            WriteDib(clipboardAccessToken, bgra32, width, height, stride, premultipliedAlpha, false);
        }
    }

    /// <summary>
    /// Encode a DIB directly into newly allocated clipboard memory and place it
    /// </summary>
    private static unsafe void WriteDib(IClipboardAccessToken clipboardAccessToken, ReadOnlySpan<byte> bgra32, int width, int height, int stride,
        bool premultipliedAlpha, bool isV5)
    {
        var size = DibCodec.GetEncodedSize(bgra32.Length, width, height, stride, isV5);
        var formatId = (uint)(isV5 ? StandardClipboardFormats.DeviceIndependentBitmapV5 : StandardClipboardFormats.DeviceIndependentBitmap);
        using var writeInfo = clipboardAccessToken.WriteInfo(formatId, size);
        DibCodec.Encode(bgra32, width, height, stride, premultipliedAlpha, isV5, new Span<byte>((void*)writeInfo.MemoryPtr, size));
        writeInfo.Commit();
    }

    /// <summary>
    /// Add a bitmap as CF_DIBV5 and / or CF_DIB to the contents. The DIBs are encoded now, the pixels can be changed or reused right
    /// after this call. To avoid the encoded copies (two large arrays for a big bitmap), use
    /// <see cref="AddDib(ClipboardContents, ReadOnlyMemory{byte}, int, int, int, bool, DibFormats)"/>.
    /// </summary>
    /// <param name="contents">ClipboardContents</param>
    /// <param name="bgra32">the pixels, top-down rows of B, G, R, A bytes</param>
    /// <param name="width">int with the width</param>
    /// <param name="height">int with the height</param>
    /// <param name="stride">int with the bytes per row of <paramref name="bgra32"/>, at least width * 4</param>
    /// <param name="premultipliedAlpha">true when the pixels have premultiplied alpha</param>
    /// <param name="formats">DibFormats, default both</param>
    /// <returns>ClipboardContents for fluent usage</returns>
    public static ClipboardContents AddDib(this ClipboardContents contents, ReadOnlySpan<byte> bgra32, int width, int height, int stride,
        bool premultipliedAlpha, DibFormats formats = DibFormats.DibV5 | DibFormats.Dib)
    {
        if (contents == null)
        {
            throw new ArgumentNullException(nameof(contents));
        }
        if ((formats & DibFormats.DibV5) != 0)
        {
            contents.AddBytes(DibImage.CreateDibV5(bgra32, width, height, stride, premultipliedAlpha), StandardClipboardFormats.DeviceIndependentBitmapV5);
        }
        if ((formats & DibFormats.Dib) != 0)
        {
            contents.AddBytes(DibImage.CreateDib(bgra32, width, height, stride, premultipliedAlpha), StandardClipboardFormats.DeviceIndependentBitmap);
        }
        return contents;
    }

    /// <summary>
    /// Add a bitmap as CF_DIBV5 and / or CF_DIB to the contents, the DIBs are encoded now (see
    /// <see cref="AddDib(ClipboardContents, ReadOnlySpan{byte}, int, int, int, bool, DibFormats)"/>). This overload keeps calls with a
    /// byte array unambiguous now that there is a <see cref="ReadOnlyMemory{T}"/> overload.
    /// </summary>
    /// <param name="contents">ClipboardContents</param>
    /// <param name="bgra32">byte array with the pixels, top-down rows of B, G, R, A bytes</param>
    /// <param name="width">int with the width</param>
    /// <param name="height">int with the height</param>
    /// <param name="stride">int with the bytes per row of <paramref name="bgra32"/>, at least width * 4</param>
    /// <param name="premultipliedAlpha">true when the pixels have premultiplied alpha</param>
    /// <param name="formats">DibFormats, default both</param>
    /// <returns>ClipboardContents for fluent usage</returns>
    public static ClipboardContents AddDib(this ClipboardContents contents, byte[] bgra32, int width, int height, int stride,
        bool premultipliedAlpha, DibFormats formats = DibFormats.DibV5 | DibFormats.Dib) =>
        contents.AddDib(new ReadOnlySpan<byte>(bgra32), width, height, stride, premultipliedAlpha, formats);

    /// <summary>
    /// Add a bitmap as CF_DIBV5 and / or CF_DIB to the contents, encoded directly into the clipboard memory when the contents are placed:
    /// no intermediate arrays, which matters for large bitmaps (a 4K screenshot is about 33 MB per format).
    /// The pixels are read when the contents are placed: keep the memory valid and unchanged until then, as with
    /// <see cref="ClipboardContents.AddStream(uint, System.IO.Stream, long?)"/>. The arguments are checked now.
    /// </summary>
    /// <param name="contents">ClipboardContents</param>
    /// <param name="bgra32">ReadOnlyMemory with the pixels, top-down rows of B, G, R, A bytes</param>
    /// <param name="width">int with the width</param>
    /// <param name="height">int with the height</param>
    /// <param name="stride">int with the bytes per row of <paramref name="bgra32"/>, at least width * 4</param>
    /// <param name="premultipliedAlpha">true when the pixels have premultiplied alpha, they are written with straight alpha</param>
    /// <param name="formats">DibFormats, default both; CF_DIBV5 is placed first</param>
    /// <returns>ClipboardContents for fluent usage</returns>
    public static ClipboardContents AddDib(this ClipboardContents contents, ReadOnlyMemory<byte> bgra32, int width, int height, int stride,
        bool premultipliedAlpha, DibFormats formats = DibFormats.DibV5 | DibFormats.Dib)
    {
        if (contents == null)
        {
            throw new ArgumentNullException(nameof(contents));
        }
        // Fail now, not while the clipboard is open
        DibCodec.GetEncodedSize(bgra32.Length, width, height, stride, false);
        if ((formats & DibFormats.DibV5) != 0)
        {
            contents.Add((uint)StandardClipboardFormats.DeviceIndependentBitmapV5,
                token => WriteDib(token, bgra32.Span, width, height, stride, premultipliedAlpha, true));
        }
        if ((formats & DibFormats.Dib) != 0)
        {
            contents.Add((uint)StandardClipboardFormats.DeviceIndependentBitmap,
                token => WriteDib(token, bgra32.Span, width, height, stride, premultipliedAlpha, false));
        }
        return contents;
    }

    /// <summary>
    /// Read a bitmap from CF_DIBV5, or CF_DIB when there is no CF_DIBV5 (or it can't be decoded). Windows synthesizes both from CF_BITMAP,
    /// so this also reads bitmaps which were placed as a GDI handle. Bitmaps with more than <see cref="DibImage.DefaultMaxPixelCount"/>
    /// pixels aren't decoded, see <see cref="TryGetAsDib(IClipboardDataSource, long, out DibImage)"/>.
    /// </summary>
    /// <param name="source">IClipboardDataSource, e.g. a snapshot or clipboard.AsDataSource()</param>
    /// <param name="image">DibImage with top-down BGRA32 pixels</param>
    /// <returns>true when a bitmap could be read</returns>
    public static bool TryGetAsDib(this IClipboardDataSource source, out DibImage image) => source.TryGetAsDib(DibImage.DefaultMaxPixelCount, out image);

    /// <summary>
    /// Read a bitmap from CF_DIBV5, or CF_DIB when there is no CF_DIBV5 (or it can't be decoded), with a maximum size:
    /// bitmaps with more than <paramref name="maxPixelCount"/> pixels (width * |height|) aren't decoded, this is checked from the header
    /// before the pixels are allocated. See <see cref="DibImage.TryDecode(byte[], long, out DibImage)"/>.
    /// </summary>
    /// <param name="source">IClipboardDataSource, e.g. a snapshot or clipboard.AsDataSource()</param>
    /// <param name="maxPixelCount">long with the maximum number of pixels, e.g. <see cref="DibImage.DefaultMaxPixelCount"/></param>
    /// <param name="image">DibImage with top-down BGRA32 pixels</param>
    /// <returns>true when a bitmap could be read</returns>
    /// <exception cref="ArgumentOutOfRangeException">When <paramref name="maxPixelCount"/> isn't positive</exception>
    public static bool TryGetAsDib(this IClipboardDataSource source, long maxPixelCount, out DibImage image)
    {
        if (maxPixelCount <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(maxPixelCount), maxPixelCount, "The maximum pixel count must be positive.");
        }
        image = null;
        if (source.TryGetAsBytes(StandardClipboardFormats.DeviceIndependentBitmapV5.AsString(), out var dibV5) && DibImage.TryDecode(dibV5, maxPixelCount, out image))
        {
            return true;
        }
        return source.TryGetAsBytes(StandardClipboardFormats.DeviceIndependentBitmap.AsString(), out var dib) && DibImage.TryDecode(dib, maxPixelCount, out image);
    }
}
