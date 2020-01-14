// Copyright (c) Dapplo and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
using System;

namespace Dapplo.Windows.DesktopWindowsManager.Enums
{
    /// <summary>
    ///     Configuration flags for the DwmSetIconicLivePreviewBitmap function
    /// </summary>
    [Flags]
    public enum DwmSetIconicLivePreviewFlags
    {
        /// <summary>
        ///     None
        /// </summary>
        None = 0,

        /// <summary>
        ///     Displays a frame around the provided bitmap.
        /// </summary>
        DisplayFrame = 1
    }
}