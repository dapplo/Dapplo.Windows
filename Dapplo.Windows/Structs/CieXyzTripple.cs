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

using System.Runtime.InteropServices;

#endregion

namespace Dapplo.Windows.Structs
{
    /// <summary>
    ///     Color representation using CIEXYZ color components
    /// </summary>
    [StructLayout(LayoutKind.Sequential)]
    public struct CieXyzTripple
    {
        /// <summary>
        ///     A CIE XYZ 1931 color space for the red component
        /// </summary>
        public CieXyz CieXyzRed;

        /// <summary>
        ///     A CIE XYZ 1931 color space for the green component
        /// </summary>
        public CieXyz CieXyzGreen;

        /// <summary>
        ///     A CIE XYZ 1931 color space for the blue component
        /// </summary>
        public CieXyz CieXyzBlue;

        /// <summary>
        ///     Factory method
        /// </summary>
        /// <param name="red">CieXyz</param>
        /// <param name="green">CieXyz</param>
        /// <param name="blue">CieXyz</param>
        /// <returns>CieXyzTripple</returns>
        public static CieXyzTripple Create(CieXyz red, CieXyz green, CieXyz blue)
        {
            return new CieXyzTripple
            {
                CieXyzRed = red,
                CieXyzGreen = green,
                CieXyzBlue = blue
            };
        }
    }
}