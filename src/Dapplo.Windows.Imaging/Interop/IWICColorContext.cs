// Copyright (c) Dapplo and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Runtime.InteropServices;
using Dapplo.Windows.Common.Enums;

namespace Dapplo.Windows.Imaging.Interop;

/// <summary>
/// A color context (ICC profile or EXIF color space), methods in the order of wincodec.idl.
/// See <a href="https://learn.microsoft.com/en-us/windows/win32/api/wincodec/nn-wincodec-iwiccolorcontext">IWICColorContext interface</a>
/// </summary>
[ComImport]
[InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
[Guid("3C613A02-34B2-44ea-9A7C-45AEA9C6FD6D")]
internal interface IWICColorContext
{
    [PreserveSig] HResult InitializeFromFilename([MarshalAs(UnmanagedType.LPWStr)] string wzFilename);
    [PreserveSig] HResult InitializeFromMemory(nint pbBuffer, uint cbBufferSize);
    [PreserveSig] HResult InitializeFromExifColorSpace(uint value);

    /// <summary>
    /// WICColorContextType: 0 = uninitialized, 1 = profile, 2 = EXIF color space
    /// </summary>
    [PreserveSig] HResult GetType(out uint pType);
    [PreserveSig] HResult GetProfileBytes(uint cbBuffer, nint pbBuffer, out uint pcbActual);
    [PreserveSig] HResult GetExifColorSpace(out uint pValue);
}
