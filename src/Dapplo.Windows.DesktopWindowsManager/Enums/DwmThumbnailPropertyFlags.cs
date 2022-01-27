// Copyright (c) Dapplo and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
using System;

namespace Dapplo.Windows.DesktopWindowsManager.Enums;

/// <summary>
///     A flag to indicate which properties are set by the DwmUpdateThumbnailProperties method
/// </summary>
[Flags]
public enum DwmThumbnailPropertyFlags
{
    /// <summary>
    ///     A value for the rcDestination member has been specified.
    /// </summary>
    Destination = 0x00000001,

    /// <summary>
    ///     A value for the rcSource member has been specified.
    /// </summary>
    Source = 0x00000002,

    /// <summary>
    ///     A value for the opacity member has been specified.
    /// </summary>
    Opacity = 0x00000004,

    /// <summary>
    ///     A value for the fVisible member has been specfied.
    /// </summary>
    Visible = 0x00000008,

    /// <summary>
    ///     A value for the fSourceClientAreaOnly member has been specified.
    /// </summary>
    SourceClientAreaOnly = 0x00000010
}