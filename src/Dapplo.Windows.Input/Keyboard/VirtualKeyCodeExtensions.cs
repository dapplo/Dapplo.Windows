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

using Dapplo.Windows.Input.Enums;

namespace Dapplo.Windows.Input.Keyboard
{
    /// <summary>
    /// Extensions for VirtualKeyCode
    /// </summary>
    public static class VirtualKeyCodeExtensions
    {
        /// <summary>
        /// Test if the VirtualKeyCode is a modifier key
        /// </summary>
        /// <param name="virtualKeyCode">VirtualKeyCode</param>
        /// <returns>bool</returns>
        public static bool IsModifier(this VirtualKeyCode virtualKeyCode)
        {
            bool isModifier = false;
            switch (virtualKeyCode)
            {
                case VirtualKeyCode.Capital:
                case VirtualKeyCode.NumLock:
                case VirtualKeyCode.Scroll:
                case VirtualKeyCode.LeftShift:
                case VirtualKeyCode.Shift:
                case VirtualKeyCode.RightShift:
                case VirtualKeyCode.Control:
                case VirtualKeyCode.LeftControl:
                case VirtualKeyCode.RightControl:
                case VirtualKeyCode.Menu:
                case VirtualKeyCode.LeftMenu:
                case VirtualKeyCode.RightMenu:
                case VirtualKeyCode.LeftWin:
                case VirtualKeyCode.RightWin:
                    isModifier = true;
                    break;
            }

            return isModifier;
        }
    }
}
