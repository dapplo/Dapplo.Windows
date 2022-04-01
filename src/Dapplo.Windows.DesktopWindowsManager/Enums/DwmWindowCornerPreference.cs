// Copyright (c) Dapplo and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Dapplo.Windows.DesktopWindowsManager.Enums
{
    /// <summary>
    /// Flags used by the DwmSetWindowAttribute function to specify the rounded corner preference for a window.
    /// </summary>
    public enum DwmWindowCornerPreference : uint
    {
        /// <summary>
        /// Let the system decide when to round window corners.
        /// </summary>
        Default = 0,
        /// <summary>
        /// Never round window corners.
        /// </summary>
        DoNotRound = 1,
        /// <summary>
        /// Round the corners, if appropriate.
        /// </summary>
        Round = 2,
        /// <summary>
        /// Round the corners if appropriate, with a small radius.
        /// </summary>
        RoundSmall = 3
    }
}