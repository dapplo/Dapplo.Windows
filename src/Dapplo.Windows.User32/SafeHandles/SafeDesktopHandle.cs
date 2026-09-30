// Copyright (c) Dapplo and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
using System;
using System.Security.Permissions;
using Dapplo.Log;
using Dapplo.Windows.User32.Enums;
using Microsoft.Win32.SafeHandles;

namespace Dapplo.Windows.User32.SafeHandles;

/// <summary>
///     A SafeHandle for a desktop (HDESK), which is closed with CloseDesktop.
///     To run code on the current input desktop use <see cref="ThreadDesktopScope"/>, which also restores the previous desktop of the thread.
/// </summary>
public class SafeDesktopHandle : SafeHandleZeroOrMinusOneIsInvalid
{
    private static readonly LogSource Log = new LogSource();

    /// <summary>
    ///     The access rights which are requested by default for the input desktop: enough to switch a thread to it, create windows and read or write the objects on it.
    ///     The journal and hook rights are not requested, as these are often denied for normal users or on the secure desktop.
    /// </summary>
    public const DesktopAccessRight DefaultInputDesktopAccess = DesktopAccessRight.DESKTOP_READOBJECTS | DesktopAccessRight.DESKTOP_WRITEOBJECTS |
                                                                 DesktopAccessRight.DESKTOP_ENUMERATE | DesktopAccessRight.DESKTOP_CREATEWINDOW |
                                                                 DesktopAccessRight.DESKTOP_CREATEMENU;

    /// <summary>
    ///     Default constructor, this doesn't open anything and is needed for marshalling
    /// </summary>
    public SafeDesktopHandle() : base(true)
    {
    }

    /// <summary>
    ///     Open the desktop which receives the user input, the handle is not inheritable.
    /// </summary>
    /// <param name="desiredAccess">DesktopAccessRight, default is <see cref="DefaultInputDesktopAccess"/></param>
    /// <returns>SafeDesktopHandle, check IsInvalid to see if this worked</returns>
    public static SafeDesktopHandle OpenInputDesktop(DesktopAccessRight desiredAccess = DefaultInputDesktopAccess)
    {
        var desktopHandle = User32Api.OpenInputDesktop(0, false, desiredAccess);
        if (desktopHandle.IsInvalid)
        {
            Log.Warn().WriteLine(User32Api.CreateWin32Exception("OpenInputDesktop"), "Couldn't open the input desktop.");
        }
        return desktopHandle;
    }

    /// <summary>
    ///     Close the desktop
    /// </summary>
    /// <returns>true if this succeeded</returns>
#if !NET6_0_OR_GREATER
    [SecurityPermission(SecurityAction.LinkDemand, UnmanagedCode = true)]
#endif
    protected override bool ReleaseHandle()
    {
        return User32Api.CloseDesktop(handle);
    }
}
