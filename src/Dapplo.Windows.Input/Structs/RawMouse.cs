//  Dapplo - building blocks for desktop applications
//  Copyright (C) 2016-2017 Dapplo
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
        [FieldOffset(0)] private MouseStates _usFlags;
        // reserved
        [FieldOffset(4)] private uint _ulButtons;
        [FieldOffset(4)] private ushort _usButtonFlags;
        [FieldOffset(6)] private ushort _usButtonData;
        [FieldOffset(8)] private uint _ulRawButtons;
        [FieldOffset(12)] private int _lLastX;
        [FieldOffset(16)] private int _lLastY;
        [FieldOffset(20)] private uint _ulExtraInformation;

        /// <summary>
        /// The mouse state.
        /// </summary>
        public MouseStates State
        {
            get { return _usFlags; }
            set { _usFlags = value; }
        }
    }
}