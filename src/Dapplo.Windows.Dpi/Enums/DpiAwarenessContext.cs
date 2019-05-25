//  Dapplo - building blocks for desktop applications
//  Copyright (C) 2017-2019  Dapplo
// 
//  For more information see: http://dapplo.net/
//  Dapplo repositories are hosted on GitHub: https://github.com/dapplo
// 
//  This file is part of Dapplo.Windows
// 
//  Dapplo.Windows is free software: you can redistribute it and/or modify
//  it under the terms of the GNU Lesser General Public License as published by
//  the Free Software Foundation, either version 3 of the License, or
//  (at your option) any later version.
// 
//  Dapplo.Windows is distributed in the hope that it will be useful,
//  but WITHOUT ANY WARRANTY; without even the implied warranty of
//  MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
//  GNU Lesser General Public License for more details.
// 
//  You should have a copy of the GNU Lesser General Public License
//  along with Dapplo.Windows. If not, see <http://www.gnu.org/licenses/lgpl.txt>.

namespace Dapplo.Windows.Dpi.Enums
{
    /// <summary>
    /// </summary>
    public enum DpiAwarenessContext
    {
        /// <summary>
        ///     DPI unaware.
        ///     This window does not scale for DPI changes and is always assumed to have a scale factor of 100% (96 DPI).
        ///     It will be automatically scaled by the system on any other DPI setting.
        /// </summary>
        Unaware = -1,

        /// <summary>
        ///     System DPI aware.
        ///     This window does not scale for DPI changes.
        ///     It will query for the DPI once and use that value for the lifetime of the process.
        ///     If the DPI changes, the process will not adjust to the new DPI value.
        ///     It will be automatically scaled up or down by the system when the DPI changes from the system value.
        /// </summary>
        SystemAware = -2,

        /// <summary>
        ///     Per monitor DPI aware.
        ///     This window checks for the DPI when it is created and adjusts the scale factor whenever the DPI changes.
        ///     These processes are not automatically scaled by the system.
        /// </summary>
        PerMonitorAware = -3,

        /// <summary>
        ///     Also known as Per Monitor v2. An advancement over the original per-monitor DPI awareness mode, which enables applications to access new DPI-related scaling behaviors on a per top-level window basis.
        ///     Per Monitor v2 was made available in the Creators Update of Windows 10, and is not available on earlier versions of the operating system.
        ///     The additional behaviors introduced are as follows:
        ///     * Child window DPI change notifications - In Per Monitor v2 contexts, the entire window tree is notified of any DPI changes that occur.
        ///     * Scaling of non-client area - All windows will automatically have their non-client area drawn in a DPI sensitive fashion. Calls to EnableNonClientDpiScaling are unnecessary.
        ///     * Scaling of Win32 menus - All NTUSER menus created in Per Monitor v2 contexts will be scaling in a per-monitor fashion.
        ///     * Dialog Scaling - Win32 dialogs created in Per Monitor v2 contexts will automatically respond to DPI changes.
        ///     * Improved scaling of comctl32 controls - Various comctl32 controls have improved DPI scaling behavior in Per Monitor v2 contexts.
        ///     * Improved theming behavior - UxTheme handles opened in the context of a Per Monitor v2 window will operate in terms of the DPI associated with that window.
        /// </summary>
        PerMonitorAwareV2 = -4
    }
}