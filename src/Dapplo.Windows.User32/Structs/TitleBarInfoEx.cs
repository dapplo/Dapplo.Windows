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
using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;
using Dapplo.Windows.Common.Structs;
using Dapplo.Windows.User32.Enums;

#endregion

namespace Dapplo.Windows.User32.Structs
{
    /// <summary>
    ///     The structure for the TITLEBARINFOEX
    ///     See
    ///     <a href="https://msdn.microsoft.com/en-us/library/windows/desktop/aa969233(v=vs.85).aspx">TITLEBARINFOEX struct</a>
    /// </summary>
    [Serializable]
    [StructLayout(LayoutKind.Sequential)]
    [SuppressMessage("Sonar Code Smell", "S1450:Trivial properties should be auto-implementedPrivate fields only used as local variables in methods should become local variables", Justification = "Interop!")]
    public struct TitleBarInfoEx
    {
        /// <summary>
        ///     The size of the structure, in bytes.
        ///     The caller must set this member to sizeof(TITLEBARINFOEX).
        /// </summary>
        private uint _cbSize;

        private NativeRect _rcTitleBar;

        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 6)]
        private ObjectStates[] _rgState;

        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 6)]
        private NativeRect[] _rgRect;

        /// <summary>
        /// The coordinates of the title bar. These coordinates include all title-bar elements except the window menu.
        /// </summary>
        public NativeRect Bounds => _rcTitleBar;

        /// <summary>
        /// Returns the ObjectState of the specified element
        /// </summary>
        /// <param name="titleBarInfoIndex">TitleBarInfoIndexes used to specify the element</param>
        /// <returns>ObjectStates</returns>
        public ObjectStates ElementState(TitleBarInfoIndexes titleBarInfoIndex) => _rgState[(int)titleBarInfoIndex];

        /// <summary>
        /// Returns the Bounds of the specified element
        /// </summary>
        /// <param name="titleBarInfoIndex">TitleBarInfoIndexes used to specify the element</param>
        /// <returns>RECT</returns>
        public NativeRect ElementBounds(TitleBarInfoIndexes titleBarInfoIndex) => _rgRect[(int)titleBarInfoIndex];

        /// <summary>
        ///     Factory method for a default TitleBarInfoEx.
        /// </summary>
        public static TitleBarInfoEx Create()
        {
            return new TitleBarInfoEx
            {
                _cbSize = (uint) Marshal.SizeOf(typeof(TitleBarInfoEx)),
                _rgState = new ObjectStates[6],
                _rgRect = new NativeRect[6],
                _rcTitleBar = NativeRect.Empty
            };
        }
    }
}