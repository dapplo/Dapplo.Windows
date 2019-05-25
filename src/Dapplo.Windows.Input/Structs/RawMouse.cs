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

using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;
using Dapplo.Windows.Input.Enums;

#endregion

namespace Dapplo.Windows.Input.Structs
{
    /// <summary>
    ///     Contains information about the state of the mouse.
    ///     See <a href="https://msdn.microsoft.com/en-us/library/windows/desktop/ms645578.aspx">RAWMOUSE structure</a>
    /// </summary>
    [StructLayout(LayoutKind.Explicit)]
    [SuppressMessage("ReSharper", "ConvertToAutoProperty")]
    [SuppressMessage("ReSharper", "ArrangeAccessorOwnerBody")]
    public struct RawMouse
    {
        [FieldOffset(0)] private readonly MouseStates _usFlags;
        // reserved
        [FieldOffset(4)] private readonly uint _ulButtons;
        [FieldOffset(4)] private readonly MouseButtonStates _usButtonFlags;
        [FieldOffset(6)] private readonly short _usButtonData;
        [FieldOffset(8)] private readonly uint _ulRawButtons;
        [FieldOffset(12)] private readonly int _lLastX;
        [FieldOffset(16)] private readonly int _lLastY;
        [FieldOffset(20)] private readonly uint _ulExtraInformation;

        /// <summary>
        /// The mouse state.
        /// </summary>
        public MouseStates State
        {
            get { return _usFlags; }
        }

        /// <summary>
        /// The button state
        /// </summary>
        public MouseButtonStates ButtonState
        {
            get { return _usButtonFlags; }
        }

        /// <summary>
        /// If usButtonFlags is RI_MOUSE_WHEEL, this member is a signed value that specifies the wheel delta.
        /// </summary>
        public short WheelData
        {
            get { return _usButtonData; }
        }

        /// <summary>
        /// The motion in the X direction.
        /// This is signed relative motion or absolute motion, depending on the value of usFlags.
        /// </summary>
        public int X
        {
            get { return _lLastX; }
        }

        /// <summary>
        /// The motion in the Y direction.
        /// This is signed relative motion or absolute motion, depending on the value of usFlags.
        /// </summary>
        public int Y
        {
            get { return _lLastY; }
        }
    }
}