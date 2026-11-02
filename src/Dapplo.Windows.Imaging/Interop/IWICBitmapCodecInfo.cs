// Copyright (c) Dapplo and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Runtime.InteropServices;
using Dapplo.Windows.Common.Enums;

namespace Dapplo.Windows.Imaging.Interop;

/// <summary>
/// Information about a WIC codec (decoder or encoder).
/// IWICBitmapCodecInfo inherits from IWICComponentInfo, with InterfaceIsIUnknown the inherited methods must be repeated: they come first, in the order of wincodec.idl.
/// The string methods are called twice: with cch = 0 and a null buffer for the length (including the terminating 0), then with a buffer of that length.
/// See <a href="https://learn.microsoft.com/en-us/windows/win32/api/wincodec/nn-wincodec-iwicbitmapcodecinfo">IWICBitmapCodecInfo interface</a>
/// </summary>
[ComImport]
[InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
[Guid("E87A44C4-B76E-4C47-8B09-298EB12A2714")]
internal interface IWICBitmapCodecInfo
{
    // IWICComponentInfo (23BC3F0A-698B-4357-886B-F24D50671334)
    [PreserveSig] HResult GetComponentType(out WicComponentType pType);
    [PreserveSig] HResult GetCLSID(out Guid pclsid);
    [PreserveSig] HResult GetSigningStatus(out uint pStatus);
    [PreserveSig] HResult GetAuthor(uint cchAuthor, nint wzAuthor, out uint pcchActual);
    [PreserveSig] HResult GetVendorGUID(out Guid pguidVendor);
    [PreserveSig] HResult GetVersion(uint cchVersion, nint wzVersion, out uint pcchActual);
    [PreserveSig] HResult GetSpecVersion(uint cchSpecVersion, nint wzSpecVersion, out uint pcchActual);
    [PreserveSig] HResult GetFriendlyName(uint cchFriendlyName, nint wzFriendlyName, out uint pcchActual);

    // IWICBitmapCodecInfo
    [PreserveSig] HResult GetContainerFormat(out Guid pguidContainerFormat);
    [PreserveSig] HResult GetPixelFormats(uint cFormats, nint pguidPixelFormats, out uint pcActual);
    [PreserveSig] HResult GetColorManagementVersion(uint cchColorManagementVersion, nint wzColorManagementVersion, out uint pcchActual);
    [PreserveSig] HResult GetDeviceManufacturer(uint cchDeviceManufacturer, nint wzDeviceManufacturer, out uint pcchActual);
    [PreserveSig] HResult GetDeviceModels(uint cchDeviceModels, nint wzDeviceModels, out uint pcchActual);
    [PreserveSig] HResult GetMimeTypes(uint cchMimeTypes, nint wzMimeTypes, out uint pcchActual);
    [PreserveSig] HResult GetFileExtensions(uint cchFileExtensions, nint wzFileExtensions, out uint pcchActual);
    [PreserveSig] HResult DoesSupportAnimation(out int pfSupportAnimation);
    [PreserveSig] HResult DoesSupportChromakey(out int pfSupportChromakey);
    [PreserveSig] HResult DoesSupportLossless(out int pfSupportLossless);
    [PreserveSig] HResult DoesSupportMultiframe(out int pfSupportMultiframe);
    [PreserveSig] HResult MatchesMimeType([MarshalAs(UnmanagedType.LPWStr)] string wzMimeType, out int pfMatches);
}
