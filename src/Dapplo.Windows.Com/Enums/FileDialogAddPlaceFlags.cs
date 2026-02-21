// Copyright (c) Dapplo and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Dapplo.Windows.Com.Enums;

/// <summary>
/// Specifies how the place is to be added to the list of available places.
/// See <a href="https://docs.microsoft.com/en-us/windows/win32/api/shobjidl_core/ne-shobjidl_core-fdap">FDAP enumeration</a>
/// </summary>
public enum FileDialogAddPlaceFlags : uint
{
    /// <summary>
    /// The place is added to the bottom of the default list.
    /// </summary>
    Bottom = 0x00000000,

    /// <summary>
    /// The place is added to the top of the default list.
    /// </summary>
    Top = 0x00000001
}
