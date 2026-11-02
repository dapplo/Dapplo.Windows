// Copyright (c) Dapplo and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.ComTypes;
using Dapplo.Windows.Common.Enums;

namespace Dapplo.Windows.Imaging.Interop;

/// <summary>
/// A WIC encoder, methods in the order of wincodec.idl.
/// See <a href="https://learn.microsoft.com/en-us/windows/win32/api/wincodec/nn-wincodec-iwicbitmapencoder">IWICBitmapEncoder interface</a>
/// </summary>
[ComImport]
[InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
[Guid("00000103-a8f2-4877-ba0a-fd2b6645fb94")]
internal interface IWICBitmapEncoder
{
    [PreserveSig] HResult Initialize(IStream pIStream, WicBitmapEncoderCacheOption cacheOption);
    [PreserveSig] HResult GetContainerFormat(out Guid pguidContainerFormat);
    [PreserveSig] HResult GetEncoderInfo(out nint ppIEncoderInfo);
    [PreserveSig] HResult SetColorContexts(uint cCount, nint ppIColorContext);
    [PreserveSig] HResult SetPalette(nint pIPalette);
    [PreserveSig] HResult SetThumbnail(nint pIThumbnail);
    [PreserveSig] HResult SetPreview(nint pIPreview);

    /// <summary>
    /// Creates a new frame, ppIEncoderOptions is [in, out, unique]: passing a pointer to null makes the encoder return its property bag
    /// </summary>
    [PreserveSig] HResult CreateNewFrame(out IWICBitmapFrameEncode ppIFrameEncode, out IPropertyBag2 ppIEncoderOptions);
    [PreserveSig] HResult Commit();
    [PreserveSig] HResult GetMetadataQueryWriter(out nint ppIMetadataQueryWriter);
}
