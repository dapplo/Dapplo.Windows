// Copyright (c) Dapplo and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Runtime.InteropServices;
using Dapplo.Windows.Common.Enums;

namespace Dapplo.Windows.Imaging.Interop;

/// <summary>
/// Information on a pixel format, inherits IWICComponentInfo: those methods are repeated first, in the order of wincodec.idl.
/// Only GetBitsPerPixel is used.
/// See <a href="https://learn.microsoft.com/en-us/windows/win32/api/wincodec/nn-wincodec-iwicpixelformatinfo">IWICPixelFormatInfo interface</a>
/// </summary>
[ComImport]
[InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
[Guid("E8EDA601-3D48-431A-AB44-69059BE88BBE")]
internal interface IWICPixelFormatInfo
{
    // IWICComponentInfo
    [PreserveSig] HResult GetComponentType(out WicComponentType pType);
    [PreserveSig] HResult GetCLSID(out Guid pclsid);
    [PreserveSig] HResult GetSigningStatus(out uint pStatus);
    [PreserveSig] HResult GetAuthor(uint cchAuthor, nint wzAuthor, out uint pcchActual);
    [PreserveSig] HResult GetVendorGUID(out Guid pguidVendor);
    [PreserveSig] HResult GetVersion(uint cchVersion, nint wzVersion, out uint pcchActual);
    [PreserveSig] HResult GetSpecVersion(uint cchSpecVersion, nint wzSpecVersion, out uint pcchActual);
    [PreserveSig] HResult GetFriendlyName(uint cchFriendlyName, nint wzFriendlyName, out uint pcchActual);

    // IWICPixelFormatInfo
    [PreserveSig] HResult GetFormatGUID(out Guid pFormat);
    [PreserveSig] HResult GetColorContext(out nint ppIColorContext);
    [PreserveSig] HResult GetBitsPerPixel(out uint puiBitsPerPixel);
    [PreserveSig] HResult GetChannelCount(out uint puiChannelCount);
    [PreserveSig] HResult GetChannelMask(uint uiChannelIndex, uint cbMaskBuffer, nint pbMaskBuffer, out uint pcbActual);
}
