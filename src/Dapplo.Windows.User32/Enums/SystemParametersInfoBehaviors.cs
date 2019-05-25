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

namespace Dapplo.Windows.User32.Enums
{
    /// <summary>
    ///     If a system parameter is being set, specifies whether the user profile is to be updated, and if so, whether the
    ///     WM_SETTINGCHANGE message is to be broadcast to all top-level windows to notify them of the change.
    ///     This parameter can be zero if you do not want to update the user profile or broadcast the WM_SETTINGCHANGE message,
    ///     or it can be one or more of the following values.
    /// </summary>
    public enum SystemParametersInfoBehaviors : uint
    {
        /// <summary>
        ///     Do nothing
        /// </summary>
        None = 0x00,

        /// <summary>Writes the new system-wide parameter setting to the user profile.</summary>
        UpdateIniFile = 0x01,

        /// <summary>Broadcasts the WM_SETTINGCHANGE message after updating the user profile.</summary>
        SendChange = 0x02,

        /// <summary>Same as SPIF_SENDCHANGE.</summary>
        SendWinIniChange = SendChange
    }
}