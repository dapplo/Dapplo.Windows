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

namespace Dapplo.Windows.User32.Structs
{
    /// <summary>
    ///     Describes the animation effects associated with user actions. This structure is used with the SystemParametersInfo
    ///     function when the SPI_GETANIMATION or SPI_SETANIMATION action value is specified.
    /// </summary>
    [SuppressMessage("Sonar Code Smell", "S1450:Private fields only used as local variables in methods should become local variables", Justification = "Interop!")]
    [StructLayout(LayoutKind.Sequential)]
    public struct AnimationInfo
    {
        private uint _cbSize;
        private int _iMinAnimate;

        /// <summary>
        ///     Factory method to create AnimationInfo
        /// </summary>
        /// <param name="enableAnimations"></param>
        /// <returns></returns>
        public static AnimationInfo Create(bool enableAnimations = true)
        {
            return new AnimationInfo
            {
                _cbSize = (uint) Marshal.SizeOf(typeof(AnimationInfo)),
                _iMinAnimate = enableAnimations ? 1 : 0
            };
        }
    }
}