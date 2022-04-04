// Copyright (c) Dapplo and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
using System;
using System.Security.Permissions;
using Dapplo.Log;
using Dapplo.Windows.User32.Enums;
using Microsoft.Win32.SafeHandles;

namespace Dapplo.Windows.User32.SafeHandles;

/// <summary>
///     A SafeHandle class implementation for the current input desktop
/// </summary>
public class SafeCurrentInputDesktopHandle : SafeHandleZeroOrMinusOneIsInvalid
{
    private static readonly LogSource Log = new LogSource();

    /// <summary>
    ///     Default constructor, this opens the input destop with GENERIC_ALL
    ///     This is needed to support marshalling!!
    /// </summary>
    public SafeCurrentInputDesktopHandle() : base(true)
    {
        var hDesktop = User32Api.OpenInputDesktop(0, true, DesktopAccessRight.GENERIC_ALL);
        if (hDesktop != IntPtr.Zero)
        {
            // Got desktop, store it as handle for the ReleaseHandle
            SetHandle(hDesktop);
            if (User32Api.SetThreadDesktop(hDesktop))
            {
                Log.Debug().WriteLine("Switched to desktop {0}", hDesktop);
            }
            else
            {
                Log.Warn().WriteLine("Couldn't switch to desktop {0}", hDesktop);
                Log.Error().WriteLine(User32Api.CreateWin32Exception("SetThreadDesktop"));
            }
        }
        else
        {
            Log.Warn().WriteLine("Couldn't get current desktop.");
            Log.Error().WriteLine(User32Api.CreateWin32Exception("OpenInputDesktop"));
        }
    }

    /// <summary>
    ///     Close the desktop
    /// </summary>
    /// <returns>true if this succeeded</returns>
#if !NET6_0
    [SecurityPermission(SecurityAction.LinkDemand, UnmanagedCode = true)]
#endif
    protected override bool ReleaseHandle()
    {
        return User32Api.CloseDesktop(handle);
    }
}