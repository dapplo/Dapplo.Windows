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
using Dapplo.Windows.Messages;

#endregion

namespace Dapplo.Windows.Input.Structs
{
    /// <summary>
    ///     Contains information about the state of the keyboard.
    ///     See <a href="https://msdn.microsoft.com/en-us/library/windows/desktop/ms645575.aspx">RAWKEYBOARD structure</a>
    /// </summary>
    [StructLayout(LayoutKind.Sequential)]
    [SuppressMessage("ReSharper", "ConvertToAutoProperty")]
    [SuppressMessage("ReSharper", "ArrangeAccessorOwnerBody")]
    public struct RawKeyboard
    {
        // Scan code from the key depression
        private readonly ushort _makecode;
        // One or more of RI_KEY_MAKE, RI_KEY_BREAK, RI_KEY_E0, RI_KEY_E1
        private readonly RawKeyboardFlags _flags;
        // Always 0
        private readonly ushort _reserved;
        // Virtual Key Code
        private readonly VirtualKeyCode _vkey;
        // Corresponding Windows message for exmaple (WM_KEYDOWN, WM_SYASKEYDOWN etc)
        private readonly WindowsMessages _message;
        // The device-specific addition information for the event (seems to always be zero for keyboards)
        private readonly uint _extraInformation;

        /// <summary>
        /// The virtual key code
        /// </summary>
        public VirtualKeyCode VirtualKey
        {
            get { return _vkey; }
        }

        /// <summary>
        /// Scan code flags
        /// </summary>
        public RawKeyboardFlags Flags
        {
            get { return _flags; }
        }

        /// <summary>
        /// The scan code
        /// </summary>
        public ushort ScanCode
        {
            get { return _makecode; }
        }

        /// <inheritdoc />
        public override string ToString()
        {
            return string.Format("Rawkeyboard\n Makecode: {0}\n Makecode(hex) : {0:X}\n Flags: {1}\n Reserved: {2}\n VKeyName: {3}\n Message: {4}\n ExtraInformation {5}\n",
                ScanCode, Flags, _reserved, _vkey, _message, _extraInformation);
        }
    }
}