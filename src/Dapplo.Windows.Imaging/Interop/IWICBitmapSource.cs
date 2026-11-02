// Copyright (c) Dapplo and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Runtime.InteropServices;
using Dapplo.Windows.Common.Enums;

namespace Dapplo.Windows.Imaging.Interop;

/// <summary>
/// A source of pixels for WIC. It is called on WIC objects (decoded frames, format converters) and implemented by Dapplo.Windows.Imaging
/// to hand pixels from memory or from a row callback to an encoder without a copy, so it is public and COM visible.
/// See <a href="https://learn.microsoft.com/en-us/windows/win32/api/wincodec/nn-wincodec-iwicbitmapsource">IWICBitmapSource interface</a>
/// </summary>
[ComImport]
[ComVisible(true)]
[InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
[Guid("00000120-a8f2-4877-ba0a-fd2b6645fb94")]
public interface IWICBitmapSource
{
    /// <summary>
    /// The size of the bitmap in pixels
    /// </summary>
    [PreserveSig]
    HResult GetSize(out uint width, out uint height);

    /// <summary>
    /// The pixel format GUID, see <see cref="WicPixelFormats"/>
    /// </summary>
    [PreserveSig]
    HResult GetPixelFormat(out Guid pixelFormat);

    /// <summary>
    /// The resolution in dots per inch
    /// </summary>
    [PreserveSig]
    HResult GetResolution(out double dpiX, out double dpiY);

    /// <summary>
    /// Copy the palette into the IWICPalette (pointer), for indexed formats
    /// </summary>
    [PreserveSig]
    HResult CopyPalette(nint palette);

    /// <summary>
    /// Copy the pixels of a rectangle (pointer to a WICRect, null for the whole bitmap) into the buffer
    /// </summary>
    /// <param name="rectangle">pointer to a WICRect (X, Y, Width, Height as 32 bit integers), or 0 for the whole bitmap</param>
    /// <param name="stride">bytes per row in the buffer</param>
    /// <param name="bufferSize">size of the buffer in bytes</param>
    /// <param name="buffer">pointer to the buffer</param>
    [PreserveSig]
    HResult CopyPixels(nint rectangle, uint stride, uint bufferSize, nint buffer);
}
