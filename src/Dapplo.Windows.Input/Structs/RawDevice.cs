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

#endregion

namespace Dapplo.Windows.Input.Structs
{
    /// <summary>
    ///     This is used to similate a union in the RawInput struct, were cannot use Explicit due to 32/64 bit
    /// </summary>
    [StructLayout(LayoutKind.Explicit)]
    [SuppressMessage("ReSharper", "ConvertToAutoProperty")]
    [SuppressMessage("ReSharper", "ArrangeAccessorOwnerBody")]
    public struct RawDevice
    {
        [FieldOffset(0)] private readonly RawMouse _mouse;
        [FieldOffset(0)] private readonly RawKeyboard _keyboard;
        [FieldOffset(0)] private readonly RawHID _hid;

        /// <summary>
        /// Information on the mouse
        /// </summary>
        public RawMouse Mouse
        {
            get { return _mouse; }
        }

        /// <summary>
        /// Information on the keyboard
        /// </summary>
        public RawKeyboard Keyboard
        {
            get { return _keyboard; }
        }

        /// <summary>
        /// Information on the HID device
        /// </summary>
        // ReSharper disable once InconsistentNaming
        public RawHID HID
        {
            get { return _hid; }
        }
    }
}