using System;
using System.Runtime.InteropServices;

namespace Dapplo.Windows.Com
{
    /// <summary>
    /// Enables clients to get pointers to other interfaces on a given object through the QueryInterface method, and manage the existence of the object through the AddRef and Release methods. All other COM interfaces are inherited, directly or indirectly, from IUnknown. Therefore, the three methods in IUnknown are the first entries in the VTable for every interface.
    /// </summary>
    [ComImport]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    [Guid("00000000-0000-0000-C000-000000000046")]
    public interface IUnknown
    {
        /// <summary>
        /// Retrieves pointers to the supported interfaces on an object.
        /// </summary>
        /// <param name="riid"></param>
        /// <returns>IntPtr</returns>
        IntPtr QueryInterface(ref Guid riid);

        /// <summary>
        /// Increments the reference count for an interface on an object.
        /// </summary>
        /// <returns>uint</returns>
        [PreserveSig]
        uint AddRef();

        /// <summary>
        /// Decrements the reference count for an interface on an object.
        /// </summary>
        /// <returns>uint</returns>
        [PreserveSig]
        uint Release();
    }
}