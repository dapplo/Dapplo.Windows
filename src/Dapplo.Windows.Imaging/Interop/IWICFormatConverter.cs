// Copyright (c) Dapplo and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Runtime.InteropServices;
using Dapplo.Windows.Common.Enums;

namespace Dapplo.Windows.Imaging.Interop;

/// <summary>
/// Converts the pixel format of a source, inherits IWICBitmapSource: those methods are repeated first, in the order of wincodec.idl.
/// See <a href="https://learn.microsoft.com/en-us/windows/win32/api/wincodec/nn-wincodec-iwicformatconverter">IWICFormatConverter interface</a>
/// </summary>
[ComImport]
[InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
[Guid("00000301-a8f2-4877-ba0a-fd2b6645fb94")]
internal interface IWICFormatConverter
{
    // IWICBitmapSource
    [PreserveSig] HResult GetSize(out uint width, out uint height);
    [PreserveSig] HResult GetPixelFormat(out Guid pixelFormat);
    [PreserveSig] HResult GetResolution(out double dpiX, out double dpiY);
    [PreserveSig] HResult CopyPalette(nint palette);
    [PreserveSig] HResult CopyPixels(nint rectangle, uint stride, uint bufferSize, nint buffer);

    // IWICFormatConverter
    [PreserveSig]
    HResult Initialize(IWICBitmapSource pISource, [In] ref Guid dstFormat, WicBitmapDitherType dither, IWICPalette pIPalette, double alphaThresholdPercent, WicBitmapPaletteType paletteTranslate);

    [PreserveSig]
    HResult CanConvert([In] ref Guid srcPixelFormat, [In] ref Guid dstPixelFormat, out int pfCanConvert);
}
