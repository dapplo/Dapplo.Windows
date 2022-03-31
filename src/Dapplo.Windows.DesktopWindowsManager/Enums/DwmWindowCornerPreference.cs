// Copyright (c) Dapplo and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Diagnostics.CodeAnalysis;

namespace Dapplo.Windows.DesktopWindowsManager.Enums
{
    /// <summary>
    /// Flags used by the DwmSetWindowAttribute function to specify the rounded corner preference for a window.
    /// </summary>
    [SuppressMessage("ReSharper", "InconsistentNaming")]
    public enum DwmWindowCornerPreference : uint
    {
        /// <summary>
        /// Let the system decide when to round window corners.
        /// </summary>
        DWMWCP_DEFAULT = 0,
        /// <summary>
        /// Never round window corners.
        /// </summary>
        DWMWCP_DONOTROUND = 1,
        /// <summary>
        /// Round the corners, if appropriate.
        /// </summary>
        DWMWCP_ROUND = 2,
        /// <summary>
        /// Round the corners if appropriate, with a small radius.
        /// </summary>
        DWMWCP_ROUNDSMALL = 3
    }
}