// Copyright (c) Dapplo and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
namespace Dapplo.Windows.User32.Enums;

/// <summary>
///     Values for the MonitorFromPoint / MonitorFromRect / MonitorFromWindow "dwFlags" parameter (MONITOR_DEFAULTTO*).
///     These are plain values, not combinable flags.
///     see <a href="https://msdn.microsoft.com/en-us/library/windows/desktop/dd145063(v=vs.85).aspx">MonitorFromRect function</a>
///     or see <a href="https://msdn.microsoft.com/en-us/library/windows/desktop/dd145064(v=vs.85).aspx">MonitorFromWindow function</a>
/// </summary>
public enum MonitorFrom : uint
{
    /// <summary>
    ///     MONITOR_DEFAULTTONULL: Returns NULL if the window, rectangle or point does not intersect any display monitor.
    /// </summary>
    DefaultToNull = 0,

    /// <summary>
    ///     MONITOR_DEFAULTTOPRIMARY: Returns a handle to the primary display monitor if there is no intersection.
    /// </summary>
    DefaultToPrimary = 1,

    /// <summary>
    ///     MONITOR_DEFAULTTONEAREST: Returns a handle to the display monitor that is nearest to the window, rectangle or point.
    /// </summary>
    DefaultToNearest = 2
}