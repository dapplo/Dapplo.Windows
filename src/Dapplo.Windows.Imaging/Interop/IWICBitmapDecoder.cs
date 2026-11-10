// Copyright (c) Dapplo and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Runtime.InteropServices;
using Dapplo.Windows.Common.Enums;

namespace Dapplo.Windows.Imaging.Interop;

/// <summary>
/// A WIC decoder, methods in the order of wincodec.idl.
/// See <a href="https://learn.microsoft.com/en-us/windows/win32/api/wincodec/nn-wincodec-iwicbitmapdecoder">IWICBitmapDecoder interface</a>
/// </summary>
[ComImport]
[InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
[Guid("9EDDE9E7-8DEE-47ea-99DF-E6FAF2ED44BF")]
internal interface IWICBitmapDecoder
{
    [PreserveSig] HResult QueryCapability(IWicStream pIStream, out uint pdwCapability);
    [PreserveSig] HResult Initialize(IWicStream pIStream, WicDecodeOptions cacheOptions);
    [PreserveSig] HResult GetContainerFormat(out Guid pguidContainerFormat);
    [PreserveSig] HResult GetDecoderInfo(out nint ppIDecoderInfo);
    [PreserveSig] HResult CopyPalette(nint pIPalette);
    [PreserveSig] HResult GetMetadataQueryReader(out nint ppIMetadataQueryReader);
    [PreserveSig] HResult GetPreview(out nint ppIBitmapSource);
    [PreserveSig] HResult GetColorContexts(uint cCount, nint ppIColorContexts, out uint pcActualCount);
    [PreserveSig] HResult GetThumbnail(out nint ppIThumbnail);
    [PreserveSig] HResult GetFrameCount(out uint pCount);
    [PreserveSig] HResult GetFrame(uint index, out IWICBitmapFrameDecode ppIBitmapFrame);
}
