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
using Dapplo.Windows.Input.Enums;

#endregion

namespace Dapplo.Windows.Input.Structs
{
    /// <summary>
    ///     This struct contains information about a simulated keyboard event.
    ///     See
    ///     <a href="https://msdn.microsoft.com/en-us/library/windows/desktop/ms646271.aspx">KEYBDINPUT structure</a>
    /// </summary>
    [StructLayout(LayoutKind.Sequential)]
    public struct KeyboardInput
    {
        private VirtualKeyCode _wVk;
        private ScanCodes _wScan;
        private KeyEventFlags _dwFlags;
        private uint _time;
        private UIntPtr _dwExtraInfo;

        /// <summary>
        ///     A virtual-key code. The code must be a value in the range 1 to 254.
        ///     If the dwFlags member specifies KEYEVENTF_UNICODE, wVk must be 0.
        /// </summary>
        public VirtualKeyCode VirtualKeyCode
        {
            get { return _wVk; }
            set { _wVk = value; }
        }

        /// <summary>
        ///     A hardware scan code for the key. If KeyEventFlags specifies Unicode, ScanCode specifies a Unicode character which
        ///     is to be sent to the foreground application.
        /// </summary>
        public ScanCodes ScanCode
        {
            get { return _wScan; }
            set { _wScan = value; }
        }

        /// <summary>
        ///     Specifies various aspects of a keystroke. This member can be certain combinations of the following values.
        /// </summary>
        public KeyEventFlags KeyEventFlags
        {
            get { return _dwFlags; }
            set { _dwFlags = value; }
        }

        /// <summary>
        ///     The time stamp for the event, in milliseconds. If this parameter is zero, the system will provide its own time
        ///     stamp.
        /// </summary>
        public uint Timestamp
        {
            get { return _time; }
            set { _time = value; }
        }

        /// <summary>
        ///     An additional value associated with the keystroke. Use the GetMessageExtraInfo function to obtain this information.
        /// </summary>
        public UIntPtr ExtraInfo
        {
            get { return _dwExtraInfo; }
            set { _dwExtraInfo = value; }
        }

        /// <summary>
        ///     Create a KeyboardInput for a key press (up / down)
        /// </summary>
        /// <param name="virtualKeyCode">Value from VirtualKeyCodes</param>
        /// <param name="timestamp">optional Timestamp</param>
        /// <returns>KeyboardInput[]</returns>
        public static KeyboardInput[] ForKeyPress(VirtualKeyCode virtualKeyCode, uint? timestamp = null)
        {
            return new[]
            {
                ForKeyDown(virtualKeyCode, timestamp),
                ForKeyUp(virtualKeyCode, timestamp)
            };
        }

        /// <summary>
        ///     Create a KeyboardInput for a key down
        /// </summary>
        /// <param name="virtualKeyCode">Value from VirtualKeyCodes</param>
        /// <param name="timestamp">optional Timestamp</param>
        /// <returns>KeyboardInput</returns>
        public static KeyboardInput ForKeyDown(VirtualKeyCode virtualKeyCode, uint? timestamp = null)
        {
            var messageTime = timestamp ?? (uint)Environment.TickCount;
            return new KeyboardInput
            {
                VirtualKeyCode = virtualKeyCode,
                Timestamp = messageTime
            };
        }

        /// <summary>
        ///     Create a KeyboardInput for a key up
        /// </summary>
        /// <param name="virtualKeyCode">Value from VirtualKeyCodes</param>
        /// <param name="timestamp">optional Timestamp</param>
        /// <returns>KeyboardInput</returns>
        public static KeyboardInput ForKeyUp(VirtualKeyCode virtualKeyCode, uint? timestamp = null)
        {
            var messageTime = timestamp ?? (uint)Environment.TickCount;
            return new KeyboardInput
            {
                VirtualKeyCode = virtualKeyCode,
                KeyEventFlags = KeyEventFlags.KeyUp,
                Timestamp = messageTime
            };
        }
    }
}