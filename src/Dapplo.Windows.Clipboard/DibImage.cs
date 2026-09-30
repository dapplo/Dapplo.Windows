// Copyright (c) Dapplo and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
using System;
using Dapplo.Windows.Clipboard.Internals;

namespace Dapplo.Windows.Clipboard;

/// <summary>
/// A decoded device independent bitmap (CF_DIB / CF_DIBV5), normalized to top-down 32 bpp BGRA with straight alpha,
/// so it can be handed to any imaging library (ImageSharp, System.Drawing, SkiaSharp, WPF, …).
/// </summary>
public sealed class DibImage
{
    internal DibImage(int width, int height, byte[] pixels, bool hasAlpha)
    {
        Width = width;
        Height = height;
        Pixels = pixels;
        HasAlpha = hasAlpha;
    }

    /// <summary>
    /// Width in pixels
    /// </summary>
    public int Width { get; }

    /// <summary>
    /// Height in pixels
    /// </summary>
    public int Height { get; }

    /// <summary>
    /// Bytes per row, always Width * 4 (no padding)
    /// </summary>
    public int Stride => Width * 4;

    /// <summary>
    /// The pixels: top-down rows of B, G, R, A bytes with straight alpha. Images without alpha have A = 255.
    /// </summary>
    public byte[] Pixels { get; }

    /// <summary>
    /// True when the bitmap has an alpha channel with at least one pixel which isn't fully opaque
    /// </summary>
    public bool HasAlpha { get; }

    /// <summary>
    /// The default maximum number of pixels (width * |height|) which <see cref="TryDecode(byte[], out DibImage)"/> and
    /// <see cref="ClipboardDibExtensions.TryGetAsDib(IClipboardDataSource, out DibImage)"/> decode: 64 megapixels (e.g. 8192 x 8192),
    /// which are 256 MiB of BGRA32 pixels. Larger bitmaps return false; pass a larger maximum when you really need them.
    /// </summary>
    public const long DefaultMaxPixelCount = 64L * 1024 * 1024;

    /// <summary>
    /// Decode a device independent bitmap: BITMAPINFOHEADER, BITMAPV4HEADER or BITMAPV5HEADER followed by the optional masks or
    /// palette and the pixels, or a BITMAPCOREHEADER with an RGBTRIPLE palette. Supported: BI_RGB with 1, 4, 8 (palette), 16, 24 and 32 bpp;
    /// BI_BITFIELDS / BI_ALPHABITFIELDS with 16 and 32 bpp; bottom-up and top-down. Compressed bitmaps (RLE, JPEG, PNG) return false.
    /// Bitmaps with more than <see cref="DefaultMaxPixelCount"/> pixels return false, see <see cref="TryDecode(byte[], long, out DibImage)"/>.
    /// </summary>
    /// <remarks>
    /// 32 bpp BI_RGB officially has no alpha channel; it's used when at least one pixel has a non-zero value there, otherwise the image is opaque.
    /// Masks which follow a BITMAPV4HEADER / BITMAPV5HEADER although they are already in the header (some writers do this, Greenshot even with
    /// reversed bytes) are detected and skipped.
    /// </remarks>
    /// <param name="dib">byte array with the CF_DIB or CF_DIBV5 data</param>
    /// <param name="image">DibImage</param>
    /// <returns>true when the bitmap could be decoded</returns>
    public static bool TryDecode(byte[] dib, out DibImage image) => DibCodec.TryDecode(dib, DefaultMaxPixelCount, out image);

    /// <summary>
    /// Decode a device independent bitmap like <see cref="TryDecode(byte[], out DibImage)"/>, with a maximum size.
    /// The size is checked from the header before anything is allocated, so a crafted header can't make the decoder allocate huge amounts
    /// of memory: bitmaps with more than <paramref name="maxPixelCount"/> pixels (width * |height|), or with more pixels than fit in one
    /// byte array, return false.
    /// </summary>
    /// <param name="dib">byte array with the CF_DIB or CF_DIBV5 data</param>
    /// <param name="maxPixelCount">long with the maximum number of pixels, e.g. <see cref="DefaultMaxPixelCount"/></param>
    /// <param name="image">DibImage</param>
    /// <returns>true when the bitmap could be decoded</returns>
    /// <exception cref="ArgumentOutOfRangeException">When <paramref name="maxPixelCount"/> isn't positive</exception>
    public static bool TryDecode(byte[] dib, long maxPixelCount, out DibImage image) => DibCodec.TryDecode(dib, maxPixelCount, out image);

    /// <summary>
    /// Create CF_DIBV5 data: BITMAPV5HEADER, 32 bpp BI_BITFIELDS (BGRA masks), sRGB, straight alpha, bottom-up rows.
    /// </summary>
    /// <param name="bgra32">the pixels, top-down rows of B, G, R, A bytes</param>
    /// <param name="width">int with the width</param>
    /// <param name="height">int with the height</param>
    /// <param name="stride">int with the bytes per row of <paramref name="bgra32"/>, at least width * 4</param>
    /// <param name="premultipliedAlpha">true when the pixels have premultiplied alpha, they are converted to straight alpha</param>
    /// <returns>byte array</returns>
    public static byte[] CreateDibV5(ReadOnlySpan<byte> bgra32, int width, int height, int stride, bool premultipliedAlpha) =>
        DibCodec.Encode(bgra32, width, height, stride, premultipliedAlpha, true);

    /// <summary>
    /// Create CF_DIB data: BITMAPINFOHEADER, 32 bpp BI_RGB, bottom-up rows. The alpha channel is written (straight), but many
    /// applications ignore it in CF_DIB.
    /// </summary>
    /// <param name="bgra32">the pixels, top-down rows of B, G, R, A bytes</param>
    /// <param name="width">int with the width</param>
    /// <param name="height">int with the height</param>
    /// <param name="stride">int with the bytes per row of <paramref name="bgra32"/>, at least width * 4</param>
    /// <param name="premultipliedAlpha">true when the pixels have premultiplied alpha, they are converted to straight alpha</param>
    /// <returns>byte array</returns>
    public static byte[] CreateDib(ReadOnlySpan<byte> bgra32, int width, int height, int stride, bool premultipliedAlpha) =>
        DibCodec.Encode(bgra32, width, height, stride, premultipliedAlpha, false);
}
