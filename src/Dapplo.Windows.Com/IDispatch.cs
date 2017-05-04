#region Usings

using System;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.CustomMarshalers;
using DISPPARAMS = System.Runtime.InteropServices.ComTypes.DISPPARAMS;
using EXCEPINFO = System.Runtime.InteropServices.ComTypes.EXCEPINFO;

#endregion

namespace Dapplo.Windows.Com
{
    /// <summary>
    /// Exposes objects, methods and properties to programming tools and other applications that support Automation. COM components implement the IDispatch interface to enable access by Automation clients, such as Visual Basic.
    /// </summary>
    [ComImport]
    [Guid("00020400-0000-0000-C000-000000000046")]
    [InterfaceType(ComInterfaceType.InterfaceIsDual)]
    public interface IDispatch : IUnknown
    {
        /// <summary>
        /// Retrieves the number of type information interfaces that an object provides (either 0 or 1).
        /// </summary>
        /// <param name="count"></param>
        /// <returns></returns>
        [PreserveSig]
        int GetTypeInfoCount(out int count);

        /// <summary>
        /// Retrieves the type information for an object, which can then be used to get the type information for an interface.
        /// </summary>
        /// <param name="iTInfo"></param>
        /// <param name="lcid"></param>
        /// <param name="typeInfo"></param>
        /// <returns></returns>
        [PreserveSig]
        int GetTypeInfo([MarshalAs(UnmanagedType.U4)] int iTInfo, [MarshalAs(UnmanagedType.U4)] int lcid,
            [MarshalAs(UnmanagedType.CustomMarshaler, MarshalTypeRef = typeof(TypeToTypeInfoMarshaler))] out Type typeInfo);

        /// <summary>
        /// Maps a single member and an optional set of argument names to a corresponding set of integer DISPIDs, which can be used on subsequent calls to Invoke.
        /// </summary>
        /// <param name="riid"></param>
        /// <param name="rgsNames"></param>
        /// <param name="cNames"></param>
        /// <param name="lcid"></param>
        /// <param name="rgDispId"></param>
        /// <returns></returns>
        [PreserveSig]
        int GetIDsOfNames(ref Guid riid, [MarshalAs(UnmanagedType.LPArray, ArraySubType = UnmanagedType.LPWStr)] string[] rgsNames, int cNames, int lcid,
            [MarshalAs(UnmanagedType.LPArray)] int[] rgDispId);

        /// <summary>
        /// Provides access to properties and methods exposed by an object.
        /// </summary>
        /// <param name="dispIdMember"></param>
        /// <param name="riid"></param>
        /// <param name="lcid"></param>
        /// <param name="wFlags"></param>
        /// <param name="pDispParams"></param>
        /// <param name="pVarResult"></param>
        /// <param name="pExcepInfo"></param>
        /// <param name="pArgErr"></param>
        /// <returns></returns>
        [PreserveSig]
        int Invoke(int dispIdMember, ref Guid riid, uint lcid, ushort wFlags, ref DISPPARAMS pDispParams,
            out object pVarResult, ref EXCEPINFO pExcepInfo, IntPtr[] pArgErr);
    }
}