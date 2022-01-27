// Copyright (c) Dapplo and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
using System;
using System.Runtime.InteropServices;
using Dapplo.Windows.Shell32.Enums;
using Dapplo.Windows.Shell32.Structs;

namespace Dapplo.Windows.Shell32;

/// <summary>
/// An API for Shell32 functionality
/// </summary>
public static class Shell32Api
{
    /// <summary>
    /// Returns an AppBarData struct which describes the Taskbar bounds etc
    /// </summary>
    /// <returns>AppBarData</returns>
    public static AppBarData TaskbarPosition
    {
        get
        {
            var appBarData = AppBarData.Create();
            SHAppBarMessage(AppBarMessages.GetTaskbarPosition, ref appBarData);
            return appBarData;
        }
    }

    /// <summary>
    /// Sends an appbar message to the system.
    /// See <a href="https://msdn.microsoft.com/en-us/library/windows/desktop/bb762108.aspx">SHAppBarMessage function</a>
    /// </summary>
    /// <param name="dwMessage">AppBarMessages - Appbar message value to send.</param>
    /// <param name="pData">A pointer to an AppBarData structure. The content of the structure on entry and on exit depends on the value set in the dwMessage parameter.
    /// See the individual message pages for specifics.</param>
    /// <returns></returns>
    [DllImport("shell32", SetLastError = true)]
    private static extern IntPtr SHAppBarMessage(AppBarMessages dwMessage, [In] ref AppBarData pData);
}