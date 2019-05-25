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

#endregion

namespace Dapplo.Windows.Input.Structs
{
    /// <summary>
    ///     A struct used by SendInput to store information for synthesizing input events such as keystrokes, mouse movement,
    ///     and mouse clicks.
    ///     See <a href="https://msdn.microsoft.com/en-us/library/windows/desktop/ms646272(v=vs.85).aspx">LASTINPUTINFO structure</a>
    /// </summary>
    [StructLayout(LayoutKind.Sequential)]
    public struct LastInputInfo
    {
        private uint _cbSize;
        private readonly uint _dwTime;

        /// <summary>
        /// The tick count for the last registered input 
        /// </summary>
        public uint TickCountLastInput => _dwTime;

        /// <summary>
        /// The timespan for how long ago the last input was
        /// </summary>
        public TimeSpan LastInputTimeSpan => TimeSpan.FromMilliseconds(Environment.TickCount - _dwTime);

        /// <summary>
        /// Returns the DateTimeOffset for the tick count of the last input
        /// </summary>
        public DateTimeOffset LastInputDateTime => DateTimeOffset.Now.Subtract(LastInputTimeSpan);

        /// <summary>
        ///     A factory method to simplify creating the LastInputInfo struct
        /// </summary>
        /// <returns>LastInputInfo</returns>
        public static LastInputInfo Create()
        {
            return new LastInputInfo
            {
                _cbSize = (uint)Marshal.SizeOf(typeof(LastInputInfo))
            };
        }
    }
}