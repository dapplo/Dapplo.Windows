// Copyright (c) Dapplo and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Runtime.InteropServices;
using Dapplo.Windows.Common.Enums;

namespace Dapplo.Windows.Imaging.Interop;

/// <summary>
/// The property bag with the encoder options, methods in the order of ocidl.idl.
/// See <a href="https://learn.microsoft.com/en-us/windows/win32/api/ocidl/nn-ocidl-ipropertybag2">IPropertyBag2 interface</a>
/// </summary>
[ComImport]
[InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
[Guid("22F55882-280B-11D0-A8A9-00A0C90C2004")]
internal interface IPropertyBag2
{
    [PreserveSig] HResult Read(uint cProperties, nint pPropBag, nint pErrLog, nint pvarValue, nint phrError);

    /// <summary>
    /// Write one property, the value is marshalled as VARIANT (float = VT_R4, bool = VT_BOOL, byte = VT_UI1 ...)
    /// </summary>
    [PreserveSig] HResult Write(uint cProperties, [In] ref PropBag2 pPropBag, [In, MarshalAs(UnmanagedType.Struct)] ref object pvarValue);

    [PreserveSig] HResult CountProperties(out uint pcProperties);

    /// <summary>
    /// Get the information of one property, the caller must free pstrName with CoTaskMemFree
    /// </summary>
    [PreserveSig] HResult GetPropertyInfo(uint iProperty, uint cProperties, out PropBag2 pPropBag, out uint pcProperties);

    [PreserveSig] HResult LoadObject(nint pstrName, uint dwHint, nint pUnkObject, nint pErrLog);
}

/// <summary>
/// PROPBAG2 from ocidl.idl
/// </summary>
[StructLayout(LayoutKind.Sequential)]
internal struct PropBag2
{
    public uint Type;
    public ushort VarType;
    public ushort ClipboardFormat;
    public uint Hint;
    public nint Name;
    public Guid Clsid;
}
