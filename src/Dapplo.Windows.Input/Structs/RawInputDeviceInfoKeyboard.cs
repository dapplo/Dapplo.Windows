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
    ///     This struct defines the raw input data coming from the specified keyboard.
    ///     See <a href="https://msdn.microsoft.com/en-us/library/windows/desktop/ms645587.aspx">RID_DEVICE_INFO_KEYBOARD structure</a>
    /// Remarks:
    /// For the keyboard, the Usage Page is 1 and the Usage is 6.
    /// </summary>
    [StructLayout(LayoutKind.Sequential)]
    [SuppressMessage("ReSharper", "ConvertToAutoProperty")]
    [SuppressMessage("ReSharper", "ArrangeAccessorOwnerBody")]
    public struct RawInputDeviceInfoKeyboard
    {
        private readonly int _dwType;
        private readonly int _dwSubType;
        private readonly int _dwKeyboardMode;
        private readonly int _dwNumberOfFunctionKeys;
        private readonly int _dwNumberOfIndicators;
        private readonly int _dwNumberOfKeysTotal;

        /// <summary>
        /// The type of the keyboard
        /// </summary>
        public int Type => _dwType;

        /// <summary>
        /// The type of the keyboard
        /// </summary>
        public int SubType => _dwSubType;

        /// <summary>
        /// The scan code mode.
        /// </summary>
        public int KeyboardMode => _dwKeyboardMode;

        /// <summary>
        /// The number of function keys on the keyboard.
        /// </summary>
        public int NumberOfFunctionKeys => _dwNumberOfFunctionKeys;

        /// <summary>
        /// The number of LED indicators on the keyboard.
        /// </summary>
        public int NumberOfIndicators => _dwNumberOfIndicators;

        /// <summary>
        /// The total number of keys on the keyboard.
        /// </summary>
        public int NumberOfKeysTotal => _dwNumberOfKeysTotal;

    }
}