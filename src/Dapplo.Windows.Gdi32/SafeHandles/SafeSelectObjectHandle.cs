// Copyright (c) Dapplo and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
using System;
using System.Runtime.InteropServices;
using System.Security;
using Microsoft.Win32.SafeHandles;

namespace Dapplo.Windows.Gdi32.SafeHandles;

/// <summary>
///     A select object safehandle implementation
///     This will select the passed SafeHandle to the HDC and replace the returned value when disposing
/// </summary>
public class SafeSelectObjectHandle : SafeHandleZeroOrMinusOneIsInvalid
{
    private readonly SafeHandle _hdc;

    /// <summary>
    ///     Default constructor is needed to support marshalling!!
    /// </summary>
    [SecurityCritical]
    public SafeSelectObjectHandle() : base(true)
    {
    }

    /// <summary>
    ///     Constructor for the SafeSelectObjectHandle
    /// </summary>
    /// <param name="hdc">SafeHandle for the DC</param>
    /// <param name="newObjectSafeHandle">SafeHandle to the object which is select to the DC</param>
    [SecurityCritical]
    public SafeSelectObjectHandle(SafeHandle hdc, SafeHandle newObjectSafeHandle) : base(true)
    {
        _hdc = hdc;
        var selectedObject = SelectObject(hdc.DangerousGetHandle(), newObjectSafeHandle.DangerousGetHandle());
        SetHandle(selectedObject);
    }

    /// <summary>
    ///     Place the original object back on the DC
    /// </summary>
    /// <returns>allways true (except for exceptions)</returns>
    protected override bool ReleaseHandle()
    {
        SelectObject(_hdc.DangerousGetHandle(), handle);
        return true;
    }

    /// <summary>
    ///     The SelectObject function selects an object into the specified device context (DC).
    ///     The new object replaces the previous object of the same type.
    /// </summary>
    /// <param name="hDc">IntPtr to DC</param>
    /// <param name="hObject">IntPtr to the Object</param>
    /// <returns></returns>
    [DllImport("gdi32.dll", SetLastError = true)]
    private static extern IntPtr SelectObject(IntPtr hDc, IntPtr hObject);
}