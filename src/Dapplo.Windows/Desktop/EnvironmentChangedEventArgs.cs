// Copyright (c) Dapplo and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
using System;
using Dapplo.Windows.User32.Enums;

namespace Dapplo.Windows.Desktop;

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