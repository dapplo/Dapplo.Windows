// Copyright (c) Dapplo and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
using System;

namespace Dapplo.Windows.DesktopWindowsManager.Enums
{
    /// <summary>
    ///     Configuration flags for the DwmEnableBlurBehindWindow function
    /// </summary>
    [Flags]
    public enum DwmBlurBehindFlags
    {
        /// <summary>
        ///     None
        /// </summary>
        None = 0,

        /// <summary>
        ///     Transparency Enabled
        /// </summary>
        Enable = 1,

        /// <summary>
        ///     Region enabled
        /// </summary>
        BlurRegion = 2,

        /// <summary>
        ///     Transition on maximized enabled
        /// </summary>
        TransitionMaximized = 4
    }
}