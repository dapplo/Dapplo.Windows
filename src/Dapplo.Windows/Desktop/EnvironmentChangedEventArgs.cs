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

#region using

using System;
using Dapplo.Windows.User32.Enums;

#endregion

namespace Dapplo.Windows.Desktop
{
    /// <summary>
    ///     Event arguments for the WM_SETTINGCHANGE message
    /// </summary>
    public class EnvironmentChangedEventArgs : EventArgs
    {
        /// <summary>
        ///     When the system sends this message as a result of a SystemParametersInfo call, lParam is a pointer to a string that
        ///     indicates the area containing the system parameter that was changed. This parameter does not usually indicate which
        ///     specific system parameter changed. (Note that some applications send this message with lParam set to NULL.) In
        ///     general, when you receive this message, you should check and reload any system parameter settings that are used by
        ///     your application.
        ///     This string can be the name of a registry key or the name of a section in the Win.ini file. When the string is a
        ///     registry name, it typically indicates only the leaf node in the registry, not the full path.
        ///     When the system sends this message as a result of a change in policy settings, this parameter points to the string
        ///     "Policy".
        ///     When the system sends this message as a result of a change in locale settings, this parameter points to the string
        ///     "intl".
        ///     To effect a change in the environment variables for the system or the user, broadcast this message with lParam set
        ///     to the string "Environment".
        /// </summary>
        public string Area { get; private set; }

        /// <summary>
        ///     When the system sends this message as a result of a SystemParametersInfo call, the wParam parameter is the value of
        ///     the uiAction parameter passed to the SystemParametersInfo function. For a list of values, see SystemParametersInfo.
        ///     When the system sends this message as a result of a change in policy settings, this parameter indicates the type of
        ///     policy that was applied. This value is 1 if computer policy was applied or zero if user policy was applied.
        ///     When the system sends this message as a result of a change in locale settings, this parameter is zero.
        ///     When an application sends this message, this parameter must be NULL.
        /// </summary>
        public SystemParametersInfoActions SystemParametersInfoAction { get; private set; }

        /// <summary>
        ///     Factory for the EnvironmentChangedEventArgs
        /// </summary>
        /// <returns></returns>
        public static EnvironmentChangedEventArgs Create(SystemParametersInfoActions systemParametersInfoAction = SystemParametersInfoActions.SPI_NONE, string area = null)
        {
            return new EnvironmentChangedEventArgs
            {
                SystemParametersInfoAction = systemParametersInfoAction,
                Area = area
            };
        }
    }
}