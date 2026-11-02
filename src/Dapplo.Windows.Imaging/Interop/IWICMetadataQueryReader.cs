// Copyright (c) Dapplo and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.ComTypes;
using Dapplo.Windows.Common.Enums;

namespace Dapplo.Windows.Imaging.Interop;

/// <summary>
/// Reads metadata with the WIC metadata query language, methods in the order of wincodec.idl.
/// See <a href="https://learn.microsoft.com/en-us/windows/win32/api/wincodec/nn-wincodec-iwicmetadataqueryreader">IWICMetadataQueryReader interface</a>
/// </summary>
[ComImport]
[InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
[Guid("30989668-E1C9-4597-B395-458EEDB808DF")]
internal interface IWICMetadataQueryReader
{
    [PreserveSig] HResult GetContainerFormat(out Guid pguidContainerFormat);
    [PreserveSig] HResult GetLocation(uint cchMaxLength, nint wzNamespace, out uint pcchActualLength);

    /// <summary>
    /// Read a value, pvarValue points to a PROPVARIANT which the caller clears with PropVariantClear
    /// </summary>
    [PreserveSig] HResult GetMetadataByName([MarshalAs(UnmanagedType.LPWStr)] string wzName, nint pvarValue);
    [PreserveSig] HResult GetEnumerator(out IEnumString ppIEnumString);
}

/// <summary>
/// Writes metadata with the WIC metadata query language, inherits IWICMetadataQueryReader: those methods are repeated first.
/// See <a href="https://learn.microsoft.com/en-us/windows/win32/api/wincodec/nn-wincodec-iwicmetadataquerywriter">IWICMetadataQueryWriter interface</a>
/// </summary>
[ComImport]
[InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
[Guid("A721791A-0DEF-4d06-BD91-2118BF1DB10B")]
internal interface IWICMetadataQueryWriter
{
    // IWICMetadataQueryReader
    [PreserveSig] HResult GetContainerFormat(out Guid pguidContainerFormat);
    [PreserveSig] HResult GetLocation(uint cchMaxLength, nint wzNamespace, out uint pcchActualLength);
    [PreserveSig] HResult GetMetadataByName([MarshalAs(UnmanagedType.LPWStr)] string wzName, nint pvarValue);
    [PreserveSig] HResult GetEnumerator(out IEnumString ppIEnumString);

    // IWICMetadataQueryWriter
    [PreserveSig] HResult SetMetadataByName([MarshalAs(UnmanagedType.LPWStr)] string wzName, nint pvarValue);
    [PreserveSig] HResult RemoveMetadataByName([MarshalAs(UnmanagedType.LPWStr)] string wzName);
}
