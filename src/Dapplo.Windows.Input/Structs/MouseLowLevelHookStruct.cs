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
using System.Runtime.InteropServices;
using Dapplo.Windows.Common.Structs;
using Dapplo.Windows.Input.Enums;

#endregion

namespace Dapplo.Windows.Input.Structs
{
    /// <summary>
    ///     This struct is passed in the WH_MOUSE_LL hook
    ///     See: https://msdn.microsoft.com/en-us/library/windows/desktop/ms644970.aspx
    /// </summary>
    [StructLayout(LayoutKind.Sequential)]
    public struct MouseLowLevelHookStruct
    {
        /// <summary>
        ///     The x- and y-coordinates of the cursor, in per-monitor-aware screen coordinates.
        /// </summary>
        public NativePoint pt;

        /// <summary>
        ///     If the message is WM_MOUSEWHEEL, the high-order word of this member is the wheel delta.
        ///     The low-order word is reserved. A positive value indicates that the wheel was rotated forward, away from the user;
        ///     a negative value indicates that the wheel was rotated backward, toward the user.
        ///     One wheel click is defined as WHEEL_DELTA, which is 120.
        ///     If the message is WM_XBUTTONDOWN, WM_XBUTTONUP, WM_XBUTTONDBLCLK, WM_NCXBUTTONDOWN, WM_NCXBUTTONUP, or
        ///     WM_NCXBUTTONDBLCLK,
        ///     the high-order word specifies which X button was pressed or released, and the low-order word is reserved.
        ///     This value can be one or more of the following values.
        ///     Otherwise, mouseData is not used.
        /// </summary>
        public uint MouseData;

        /// <summary>
        ///     The event-injected flags.
        /// </summary>
        public ExtendedMouseFlags Flags;

        /// <summary>
        ///     The time stamp for this message.
        /// </summary>
        public uint TimeStamp;

        /// <summary>
        ///     Additional information associated with the message.
        /// </summary>
        public UIntPtr dwExtraInfo;
    }
}