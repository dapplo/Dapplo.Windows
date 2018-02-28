//  Dapplo - building blocks for desktop applications
//  Copyright (C) 2017-2018  Dapplo
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

namespace Dapplo.Windows.Dpi
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
        ContextUnaware = -1,

        /// <summary>
        ///     System DPI aware.
        ///     This window does not scale for DPI changes.
        ///     It will query for the DPI once and use that value for the lifetime of the process.
        ///     If the DPI changes, the process will not adjust to the new DPI value.
        ///     It will be automatically scaled up or down by the system when the DPI changes from the system value.
        /// </summary>
        ContextSystemAware = -2,

        /// <summary>
        ///     Per monitor DPI aware.
        ///     This window checks for the DPI when it is created and adjusts the scale factor whenever the DPI changes.
        ///     These processes are not automatically scaled by the system.
        /// </summary>
        ContextPerMonitorAware = -3
    }
}