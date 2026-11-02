// Copyright (c) Dapplo and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Runtime.InteropServices;
using Dapplo.Windows.Common.Enums;

namespace Dapplo.Windows.Imaging.Interop;

/// <summary>
/// The WIC imaging factory, only CreateComponentEnumerator is used.
/// Every method before it is declared, in the order of wincodec.idl, so the vtable slot of CreateComponentEnumerator is right.
/// The methods which aren't used have untyped (nint) parameters, they must not be called.
/// The methods after CreateComponentEnumerator (CreateFastMetadataEncoderFromDecoder etc.) are left out, which is allowed for the end of a vtable.
/// See <a href="https://learn.microsoft.com/en-us/windows/win32/api/wincodec/nn-wincodec-iwicimagingfactory">IWICImagingFactory interface</a>
/// </summary>
[ComImport]
[InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
[Guid("EC5EC8A9-C395-4314-9C77-54D7A935FF70")]
internal interface IWICImagingFactory
{
    [PreserveSig] HResult CreateDecoderFromFilename(nint wzFilename, nint pguidVendor, uint dwDesiredAccess, int metadataOptions, out nint ppIDecoder);
    [PreserveSig] HResult CreateDecoderFromStream(nint pIStream, nint pguidVendor, int metadataOptions, out nint ppIDecoder);
    [PreserveSig] HResult CreateDecoderFromFileHandle(nint hFile, nint pguidVendor, int metadataOptions, out nint ppIDecoder);
    [PreserveSig] HResult CreateComponentInfo(nint clsidComponent, out nint ppIInfo);
    [PreserveSig] HResult CreateDecoder(nint guidContainerFormat, nint pguidVendor, out nint ppIDecoder);
    [PreserveSig] HResult CreateEncoder(nint guidContainerFormat, nint pguidVendor, out nint ppIEncoder);
    [PreserveSig] HResult CreatePalette(out nint ppIPalette);
    [PreserveSig] HResult CreateFormatConverter(out nint ppIFormatConverter);
    [PreserveSig] HResult CreateBitmapScaler(out nint ppIBitmapScaler);
    [PreserveSig] HResult CreateBitmapClipper(out nint ppIBitmapClipper);
    [PreserveSig] HResult CreateBitmapFlipRotator(out nint ppIBitmapFlipRotator);
    [PreserveSig] HResult CreateStream(out nint ppIWICStream);
    [PreserveSig] HResult CreateColorContext(out nint ppIWICColorContext);
    [PreserveSig] HResult CreateColorTransformer(out nint ppIWICColorTransform);
    [PreserveSig] HResult CreateBitmap(uint uiWidth, uint uiHeight, nint pixelFormat, int option, out nint ppIBitmap);
    [PreserveSig] HResult CreateBitmapFromSource(nint piBitmapSource, int option, out nint ppIBitmap);
    [PreserveSig] HResult CreateBitmapFromSourceRect(nint piBitmapSource, uint x, uint y, uint width, uint height, out nint ppIBitmap);
    [PreserveSig] HResult CreateBitmapFromMemory(uint uiWidth, uint uiHeight, nint pixelFormat, uint cbStride, uint cbBufferSize, nint pbBuffer, out nint ppIBitmap);
    [PreserveSig] HResult CreateBitmapFromHBITMAP(nint hBitmap, nint hPalette, int options, out nint ppIBitmap);
    [PreserveSig] HResult CreateBitmapFromHICON(nint hIcon, out nint ppIBitmap);

    /// <summary>
    /// Creates an enumerator of the components of the specified types
    /// </summary>
    /// <param name="componentTypes">WICComponentType flags, e.g. WICDecoder (0x1) or WICEncoder (0x2)</param>
    /// <param name="options">WICComponentEnumerateOptions, e.g. WICComponentEnumerateDefault (0x0) or WICComponentEnumerateRefresh (0x1)</param>
    /// <param name="ppIEnumUnknown">IEnumUnknown with the components</param>
    [PreserveSig]
    HResult CreateComponentEnumerator(WicComponentType componentTypes, WicComponentEnumerateOptions options, out IEnumUnknown ppIEnumUnknown);
}
