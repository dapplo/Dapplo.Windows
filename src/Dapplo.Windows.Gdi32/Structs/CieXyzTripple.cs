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

namespace Dapplo.Windows.Gdi32.Structs
{
    /// <summary>
    ///     Color representation using CIEXYZ color components
    /// </summary>
    [StructLayout(LayoutKind.Sequential)]
    [SuppressMessage("ReSharper", "ConvertToAutoPropertyWhenPossible")]
    [SuppressMessage("Sonar Code Smell", "S2292:Trivial properties should be auto-implemented", Justification = "Interop!")]
    public struct CieXyzTripple
    {
        private CieXyz _cieXyzRed;
        private CieXyz _cieXyzGreen;
        private CieXyz _cieXyzBlue;

        /// <summary>
        ///     A CIE XYZ 1931 color space for the red component
        /// </summary>
        public CieXyz Red {
            get { return _cieXyzRed; }
            set { _cieXyzRed = value; }
        }

        /// <summary>
        ///     A CIE XYZ 1931 color space for the green component
        /// </summary>
        public CieXyz Green
        {
            get { return _cieXyzGreen; }
            set { _cieXyzGreen = value; }
        }

        /// <summary>
        ///     A CIE XYZ 1931 color space for the blue component
        /// </summary>
        public CieXyz Blue
        {
            get { return _cieXyzBlue; }
            set { _cieXyzBlue = value; }
        }

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
                _cieXyzRed = red,
                _cieXyzGreen = green,
                _cieXyzBlue = blue
            };
        }
    }
}