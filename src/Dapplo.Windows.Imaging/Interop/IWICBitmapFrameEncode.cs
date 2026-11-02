// Copyright (c) Dapplo and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Runtime.InteropServices;
using Dapplo.Windows.Common.Enums;

namespace Dapplo.Windows.Imaging.Interop;

/// <summary>
/// A frame of a WIC encoder, methods in the order of wincodec.idl.
/// See <a href="https://learn.microsoft.com/en-us/windows/win32/api/wincodec/nn-wincodec-iwicbitmapframeencode">IWICBitmapFrameEncode interface</a>
/// </summary>
[ComImport]
[InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
[Guid("00000105-a8f2-4877-ba0a-fd2b6645fb94")]
internal interface IWICBitmapFrameEncode
{
    [PreserveSig] HResult Initialize(IPropertyBag2 pIEncoderOptions);
    [PreserveSig] HResult SetSize(uint uiWidth, uint uiHeight);
    [PreserveSig] HResult SetResolution(double dpiX, double dpiY);
    [PreserveSig] HResult SetPixelFormat([In, Out] ref Guid pPixelFormat);
    /// <summary>
    /// ppIColorContext points to an array of IWICColorContext pointers
    /// </summary>
    [PreserveSig] HResult SetColorContexts(uint cCount, nint ppIColorContext);
    [PreserveSig] HResult SetPalette(nint pIPalette);
    [PreserveSig] HResult SetThumbnail(nint pIThumbnail);
    [PreserveSig] HResult WritePixels(uint lineCount, uint cbStride, uint cbBufferSize, nint pbPixels);
    [PreserveSig] HResult WriteSource(IWICBitmapSource pIBitmapSource, nint prc);
    [PreserveSig] HResult Commit();
    [PreserveSig] HResult GetMetadataQueryWriter(out IWICMetadataQueryWriter ppIMetadataQueryWriter);
}
