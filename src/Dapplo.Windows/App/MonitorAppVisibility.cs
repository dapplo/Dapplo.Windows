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

using System.Diagnostics.CodeAnalysis;

namespace Dapplo.Windows.App
{
    /// <summary>
    ///     A simple enum for the GetAppVisibilityOnMonitor method, this tells us if an App is visible on the supplied monitor.
    /// </summary>
    [SuppressMessage("ReSharper", "InconsistentNaming")]
    internal enum MonitorAppVisibility
    {
        MAV_UNKNOWN = 0, // The mode for the monitor is unknown
        MAV_NO_APP_VISIBLE = 1,
        MAV_APP_VISIBLE = 2
    }
}