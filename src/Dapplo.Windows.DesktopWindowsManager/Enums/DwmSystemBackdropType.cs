// Copyright (c) Dapplo and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Dapplo.Windows.DesktopWindowsManager.Enums;

/// <summary>
///     The system-drawn backdrop material of a window, used with <see cref="DwmWindowAttributes.SystemBackdropType"/>.
///     See <a href="https://learn.microsoft.com/en-us/windows/win32/api/dwmapi/ne-dwmapi-dwm_systembackdrop_type">DWM_SYSTEMBACKDROP_TYPE enumeration</a>
/// </summary>
public enum DwmSystemBackdropType : uint
{
    /// <summary>
    ///     DWMSBT_AUTO: let the Desktop Window Manager automatically decide the system-drawn backdrop material, this is the default.
    /// </summary>
    Auto = 0,

    /// <summary>
    ///     DWMSBT_NONE: don't draw any system backdrop.
    /// </summary>
    None = 1,

    /// <summary>
    ///     DWMSBT_MAINWINDOW: draw the backdrop material effect corresponding to a long-lived window (Mica).
    /// </summary>
    MainWindow = 2,

    /// <summary>
    ///     DWMSBT_TRANSIENTWINDOW: draw the backdrop material effect corresponding to a transient window (Acrylic).
    /// </summary>
    TransientWindow = 3,

    /// <summary>
    ///     DWMSBT_TABBEDWINDOW: draw the backdrop material effect corresponding to a window with a tabbed title bar (Mica Alt).
    /// </summary>
    TabbedWindow = 4
}
