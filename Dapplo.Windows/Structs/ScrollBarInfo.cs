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

using System;
using System.Runtime.InteropServices;
using Dapplo.Windows.Enums;

#endregion

namespace Dapplo.Windows.Structs
{
    /// <summary>
    ///     See
    ///     <a href="https://msdn.microsoft.com/en-us/library/windows/desktop/bb787535(v=vs.85).aspx">SCROLLBARINFO structure</a>
    /// </summary>
    [Serializable]
    [StructLayout(LayoutKind.Sequential)]
    public struct ScrollBarInfo
    {
        /// <summary>
        ///     Size of this struct
        /// </summary>
        private uint _cbSize;

        private int _reserved;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 6)] private ObjectStates[] _states;

        /// <summary>
        ///     Coordinates of the scroll bar as specified in a RECT structure.
        /// </summary>
        public RECT Bounds { get; private set; }

        /// <summary>
        ///     Height or width of the thumb.
        /// </summary>
        public int ThumbSize { get; private set; }

        /// <summary>
        ///     Position of the bottom or right of the thumb.
        /// </summary>
        public int ThumbBottom { get; private set; }

        /// <summary>
        ///     Position of the top or left of the thumb.
        /// </summary>
        public int ThumbTop { get; private set; }

        /// <summary>
        ///     An array of object states.
        ///     Each element indicates the state of a scroll bar component, the element is specified via the ScrollBarStateIndexes.
        /// </summary>
        public ObjectStates[] States => _states;

        /// <inheritdoc />
        public override string ToString()
        {
            var statesString = string.Join(",", States);
            return $"{{Bounds = {Bounds}; ThumbSize = {ThumbSize};ThumbBottom = {ThumbBottom};ThumbTop = {ThumbTop};States = {statesString};}}";
        }

        /// <summary>
        ///     Create a ScrollBarInfo struct
        /// </summary>
        public static ScrollBarInfo Create()
        {
            return new ScrollBarInfo
            {
                _cbSize = (uint) Marshal.SizeOf(typeof(ScrollBarInfo)),
                _states = new ObjectStates[6],
                Bounds = new RECT(),
                ThumbSize = 0,
                ThumbBottom = 0,
                ThumbTop = 0,
                _reserved = 0
            };
        }
    }
}