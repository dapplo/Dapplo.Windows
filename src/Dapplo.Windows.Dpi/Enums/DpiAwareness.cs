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
    ///     Identifies the dots per inch (dpi) setting for a thread, process, or window.
    ///     Can be used everywhere ProcessDpiAwareness is passed.
    /// </summary>
    public enum DpiAwareness
    {
        /// <summary>
        ///     Invalid DPI awareness. This is an invalid DPI awareness value.
        /// </summary>
        Invalid = -1,

        /// <summary>
        ///     DPI unaware.
        ///     This process does not scale for DPI changes and is always assumed to have a scale factor of 100% (96 DPI).
        ///     It will be automatically scaled by the system on any other DPI setting.
        /// </summary>
        Unaware = 0,

        /// <summary>
        ///     System DPI aware.
        ///     This process does not scale for DPI changes.
        ///     It will query for the DPI once and use that value for the lifetime of the process.
        ///     If the DPI changes, the process will not adjust to the new DPI value.
        ///     It will be automatically scaled up or down by the system when the DPI changes from the system value.
        /// </summary>
        SystemAware = 1,

        /// <summary>
        ///     Per monitor DPI aware.
        ///     This process checks for the DPI when it is created and adjusts the scale factor whenever the DPI changes.
        ///     These processes are not automatically scaled by the system.
        /// </summary>
        PerMonitorAware = 2
    }
}