// Copyright (c) Dapplo and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Runtime.InteropServices;
using Dapplo.Windows.Common.Enums;
using Dapplo.Windows.Common.Extensions;

namespace Dapplo.Windows.Imaging.Interop;

/// <summary>
/// Creation of the WIC imaging factory
/// </summary>
internal static class WicNativeMethods
{
    /// <summary>
    /// CLSID_WICImagingFactory, the version 1 factory which is available on every supported Windows version
    /// </summary>
    internal static readonly Guid ClsidWicImagingFactory = new Guid("CACAF262-9370-4615-A13B-9F5539DA4C0A");

    private const uint ClsCtxInprocServer = 0x1;

    /// <summary>
    /// CO_E_NOTINITIALIZED: CoInitialize has not been called on the current thread
    /// </summary>
    private const HResult CoENotInitialized = unchecked((HResult)0x800401F0);

    /// <summary>
    /// Create the WIC imaging factory in-process. The factory is free-threaded, so this works from STA and MTA threads.
    /// </summary>
    /// <returns>IWICImagingFactory, the caller must release it</returns>
    /// <exception cref="COMException">when the factory can't be created</exception>
    internal static IWICImagingFactory CreateImagingFactory()
    {
        var clsid = ClsidWicImagingFactory;
        var iid = typeof(IWICImagingFactory).GUID;
        var hResult = CoCreateInstance(ref clsid, IntPtr.Zero, ClsCtxInprocServer, ref iid, out var factory);
        if (hResult == CoENotInitialized)
        {
            // COM isn't initialized on this (native) thread yet, the runtime's COM activation initializes it (implicit MTA) and creates the factory
            factory = Activator.CreateInstance(Type.GetTypeFromCLSID(ClsidWicImagingFactory, true));
            return (IWICImagingFactory)factory;
        }
        if (!hResult.Succeeded() || factory == null)
        {
            throw new COMException("Couldn't create the WIC imaging factory", (int)hResult);
        }
        return (IWICImagingFactory)factory;
    }

    /// <summary>
    /// See <a href="https://learn.microsoft.com/en-us/windows/win32/api/combaseapi/nf-combaseapi-cocreateinstance">CoCreateInstance</a>
    /// </summary>
    [DllImport("ole32.dll", ExactSpelling = true)]
    private static extern HResult CoCreateInstance([In] ref Guid rclsid, IntPtr pUnkOuter, uint dwClsContext, [In] ref Guid riid, [MarshalAs(UnmanagedType.IUnknown)] out object ppv);
}
