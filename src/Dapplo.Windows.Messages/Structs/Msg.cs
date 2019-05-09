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

using Dapplo.Windows.Common.Structs;
using System;
using System.Runtime.InteropServices;

namespace Dapplo.Windows.Messages.Structs
{
    /// <summary>
    /// This structure represents the message information of Windows
    /// See <a href="https://docs.microsoft.com/en-us/windows/desktop/api/winuser/ns-winuser-tagmsg">tagMSG structure</a>
    /// </summary>
    [StructLayout(LayoutKind.Sequential, Pack = 8)]
    public readonly struct Msg
    {
        private readonly IntPtr _hwnd;
        private readonly WindowsMessages _message;
        private readonly UIntPtr _wParam;
        private readonly UIntPtr _lParam;
        private readonly uint _time;
        private readonly NativePoint _pt;

        /// <summary>
        /// Identifies the window whose window procedure receives the message.
        /// </summary>
        public IntPtr Handle => _hwnd;

        /// <summary>
        /// The message identifier.
        /// </summary>
        public WindowsMessages Message => _message;

        /// <summary>
        /// Additional information about the message. The exact meaning depends on the value of the message member.
        /// </summary>
        public UIntPtr wParam => _wParam;

        /// <summary>
        /// Additional information about the message. The exact meaning depends on the value of the message member.
        /// </summary>
        public UIntPtr lParam => _lParam;

        /// <summary>
        /// Time of the message
        /// </summary>
        public DateTimeOffset Time => DateTimeOffset.Now.Subtract(TimeSpan.FromMilliseconds(Environment.TickCount - _time));

        /// <summary>
        /// The cursor position, in screen coordinates, when the message was posted.
        /// </summary>
        public NativePoint CursorPosition => _pt;

    }
}
