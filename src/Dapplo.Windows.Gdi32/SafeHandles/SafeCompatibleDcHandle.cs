// Copyright (c) Dapplo and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
using System;
using System.Runtime.InteropServices;
using System.Security;

namespace Dapplo.Windows.Gdi32.SafeHandles;

/// <summary>
///     A CompatibleDC SafeHandle implementation
/// </summary>
public class SafeCompatibleDcHandle : SafeDcHandle
{
    /// <summary>
    ///     Default constructor is needed to support marshalling!!
    /// </summary>
    [SecurityCritical]
    public SafeCompatibleDcHandle() : base(true)
    {
    }

    /// <summary>
    ///     Create SafeCompatibleDcHandle from existing handle
    /// </summary>
    /// <param name="preexistingHandle">IntPtr with existing handle</param>
    [SecurityCritical]
    public SafeCompatibleDcHandle(IntPtr preexistingHandle) : base(true)
    {
        SetHandle(preexistingHandle);
    }

    [DllImport("gdi32", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool DeleteDC(IntPtr hDc);

    /// <summary>
    ///     Call DeleteDC, this disposes the unmanaged resources
    /// </summary>
    /// <returns>bool true if the DC was deleted</returns>
    protected override bool ReleaseHandle()
    {
        return DeleteDC(handle);
    }

    /// <summary>
    ///     Select an object onto the DC
    /// </summary>
    /// <param name="objectSafeHandle">SafeHandle for object</param>
    /// <returns>SafeSelectObjectHandle</returns>
    public SafeSelectObjectHandle SelectObject(SafeHandle objectSafeHandle)
    {
        return new SafeSelectObjectHandle(this, objectSafeHandle);
    }
}