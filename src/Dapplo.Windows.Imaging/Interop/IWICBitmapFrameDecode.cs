// Copyright (c) Dapplo and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Runtime.InteropServices;
using Dapplo.Windows.Common.Enums;

namespace Dapplo.Windows.Imaging.Interop;

/// <summary>
/// A decoded frame, inherits IWICBitmapSource: those methods are repeated first, in the order of wincodec.idl.
/// See <a href="https://learn.microsoft.com/en-us/windows/win32/api/wincodec/nn-wincodec-iwicbitmapframedecode">IWICBitmapFrameDecode interface</a>
/// </summary>
[ComImport]
[InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
[Guid("3B16811B-6A43-4ec9-A813-3D930C13B940")]
internal interface IWICBitmapFrameDecode
{
    // IWICBitmapSource
    [PreserveSig] HResult GetSize(out uint width, out uint height);
    [PreserveSig] HResult GetPixelFormat(out Guid pixelFormat);
    [PreserveSig] HResult GetResolution(out double dpiX, out double dpiY);
    [PreserveSig] HResult CopyPalette(nint palette);
    [PreserveSig] HResult CopyPixels(nint rectangle, uint stride, uint bufferSize, nint buffer);

    // IWICBitmapFrameDecode, ppIColorContexts points to an array of IWICColorContext pointers created by the caller
    [PreserveSig] HResult GetMetadataQueryReader(out IWICMetadataQueryReader ppIMetadataQueryReader);
    [PreserveSig] HResult GetColorContexts(uint cCount, nint ppIColorContexts, out uint pcActualCount);
    [PreserveSig] HResult GetThumbnail(out nint ppIThumbnail);
}
