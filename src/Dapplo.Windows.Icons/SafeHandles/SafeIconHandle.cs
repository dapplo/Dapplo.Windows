// Copyright (c) Dapplo and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Drawing;
using System.Security;
using Microsoft.Win32.SafeHandles;
using Dapplo.Windows.Gdi32.SafeHandles;

using Dapplo.Windows.User32;
using Dapplo.Windows.User32.Structs;
using Dapplo.Windows.User32.Enums;



#if !NET6_0_OR_GREATER
using System.Security.Permissions;
#endif

namespace Dapplo.Windows.Icons.SafeHandles;

/// <summary>
///     A SafeHandle class implementation for the hIcon
/// </summary>
public class SafeIconHandle : SafeHandleZeroOrMinusOneIsInvalid
{
    /// <summary>
    ///     Default constructor is needed to support marshalling!!
    /// </summary>
    [SecurityCritical]
    public SafeIconHandle() : base(true)
    {
    }

    /// <summary>
    ///     Create a SafeIconHandle from a bitmap by calling GetHicon
    /// </summary>
    /// <param name="bitmap">Bitmap</param>
    public SafeIconHandle(Bitmap bitmap) : base(true)
    {
        SetHandle(bitmap.GetHicon());
    }

    /// <summary>
    ///     Create a SafeIconHandle from a hIcon
    /// </summary>
    /// <param name="hIcon">IntPtr to an icon</param>
    public SafeIconHandle(IntPtr hIcon) : base(true)
    {
        SetHandle(hIcon);
    }

    /// <summary>
    ///     Call destroy icon
    /// </summary>
    /// <returns>true if this worked</returns>
#if !NET6_0_OR_GREATER
    [SecurityPermission(SecurityAction.LinkDemand, UnmanagedCode = true)]
#endif
    protected override bool ReleaseHandle()
    {
        return NativeIconMethods.DestroyIcon(handle);
    }
}