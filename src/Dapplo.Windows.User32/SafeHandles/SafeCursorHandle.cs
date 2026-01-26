// Copyright (c) Dapplo and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Security;
using Microsoft.Win32.SafeHandles;


#if !NET6_0_OR_GREATER
using System.Security.Permissions;
#endif

namespace Dapplo.Windows.User32.SafeHandles;

/// <summary>
///     A SafeHandle class implementation for the hCursor
/// </summary>
public class SafeCursorHandle : SafeHandleZeroOrMinusOneIsInvalid
{
    /// <summary>
    ///     Default constructor is needed to support marshalling!!
    /// </summary>
    [SecurityCritical]
    public SafeCursorHandle() : base(true)
    {
    }

    /// <summary>
    ///     Create a SafeCursorHandle from a hCursor
    /// </summary>
    /// <param name="hCursor">IntPtr to an hCursor</param>
    public SafeCursorHandle(IntPtr hCursor) : base(true)
    {
        SetHandle(hCursor);
    }

    /// <summary>
    ///     Call DestroyCursor
    /// </summary>
    /// <returns>true if this worked</returns>
#if !NET6_0_OR_GREATER
    [SecurityPermission(SecurityAction.LinkDemand, UnmanagedCode = true)]
#endif
    protected override bool ReleaseHandle()
    {
        return User32Api.DestroyCursor(handle);
    }
}