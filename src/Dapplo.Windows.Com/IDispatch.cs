// Copyright (c) Dapplo and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
using System;
using System.Runtime.InteropServices;
using DISPPARAMS = System.Runtime.InteropServices.ComTypes.DISPPARAMS;
using EXCEPINFO = System.Runtime.InteropServices.ComTypes.EXCEPINFO;
using ITypeInfo = System.Runtime.InteropServices.ComTypes.ITypeInfo;

namespace Dapplo.Windows.Com;

/// <summary>
/// Exposes objects, methods and properties to programming tools and other applications that support Automation. COM components implement the IDispatch interface to enable access by Automation clients, such as Visual Basic.
/// This is declared as InterfaceIsIUnknown, as the runtime adds the IUnknown methods (slot 0-2) itself, so the IDispatch methods are in the vtable slots 3-6 on all target frameworks.
/// </summary>
[ComImport]
[Guid("00020400-0000-0000-C000-000000000046")]
[InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
public interface IDispatch
{
    /// <summary>
    /// Retrieves the number of type information interfaces that an object provides (either 0 or 1).
    /// </summary>
    /// <param name="count">out int with the number of type information interfaces</param>
    /// <returns>int with the HRESULT</returns>
    [PreserveSig]
    int GetTypeInfoCount(out int count);

    /// <summary>
    /// Retrieves the type information for an object, which can then be used to get the type information for an interface.
    /// </summary>
    /// <param name="iTInfo">int with the type information to return, pass 0 to retrieve type information for the IDispatch implementation</param>
    /// <param name="lcid">int with the locale identifier for the type information</param>
    /// <param name="typeInfo">out ITypeInfo</param>
    /// <returns>int with the HRESULT</returns>
    [PreserveSig]
    int GetTypeInfo(int iTInfo, int lcid, [MarshalAs(UnmanagedType.Interface)] out ITypeInfo typeInfo);

    /// <summary>
    /// Maps a single member and an optional set of argument names to a corresponding set of integer DISPIDs, which can be used on subsequent calls to Invoke.
    /// </summary>
    /// <param name="riid">Reserved for future use. Must be IID_NULL (Guid.Empty).</param>
    /// <param name="rgsNames">Array of names to be mapped.</param>
    /// <param name="cNames">The count of the names to be mapped.</param>
    /// <param name="lcid">The locale context in which to interpret the names.</param>
    /// <param name="rgDispId">Caller-allocated array, each element of which contains an identifier (ID) corresponding to one of the names passed in the rgszNames array.</param>
    /// <returns>int with the HRESULT</returns>
    [PreserveSig]
    int GetIDsOfNames(ref Guid riid, [MarshalAs(UnmanagedType.LPArray, ArraySubType = UnmanagedType.LPWStr)] string[] rgsNames, int cNames, int lcid, [MarshalAs(UnmanagedType.LPArray)] int[] rgDispId);

    /// <summary>
    /// Provides access to properties and methods exposed by an object.
    /// </summary>
    /// <param name="dispIdMember">Identifies the member.</param>
    /// <param name="riid">Reserved for future use. Must be IID_NULL (Guid.Empty).</param>
    /// <param name="lcid">The locale context in which to interpret arguments.</param>
    /// <param name="wFlags">Flags describing the context of the Invoke call.</param>
    /// <param name="pDispParams">Pointer to a DISPPARAMS structure containing an array of arguments.</param>
    /// <param name="pVarResult">The result.</param>
    /// <param name="pExcepInfo">A structure that contains exception information.</param>
    /// <param name="pArgErr">The index of the first argument that has an error.</param>
    /// <returns>int with the HRESULT</returns>
    [PreserveSig]
    int Invoke(int dispIdMember, ref Guid riid, uint lcid, ushort wFlags, ref DISPPARAMS pDispParams,
        out object pVarResult, ref EXCEPINFO pExcepInfo, out uint pArgErr);
}
