// Copyright (c) Dapplo and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Runtime.InteropServices;
using Dapplo.Windows.Common.Enums;

namespace Dapplo.Windows.Imaging.Interop;

/// <summary>
/// Enumerates objects with the IUnknown interface, here the components returned by IWICImagingFactory::CreateComponentEnumerator.
/// See <a href="https://learn.microsoft.com/en-us/windows/win32/api/objidl/nn-objidl-ienumunknown">IEnumUnknown interface</a>
/// </summary>
[ComImport]
[InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
[Guid("00000100-0000-0000-C000-000000000046")]
internal interface IEnumUnknown
{
    /// <summary>
    /// Retrieves the next item, this is only used with celt = 1 so a single out parameter covers the array.
    /// Returns S_OK when an item was fetched, S_FALSE at the end of the enumeration.
    /// </summary>
    [PreserveSig]
    HResult Next(uint celt, [MarshalAs(UnmanagedType.IUnknown)] out object rgelt, out uint pceltFetched);

    [PreserveSig]
    HResult Skip(uint celt);

    [PreserveSig]
    HResult Reset();

    [PreserveSig]
    HResult Clone(out IEnumUnknown ppenum);
}
