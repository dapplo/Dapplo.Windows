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

namespace Dapplo.Windows.Gdi32.Enums
{
    /// <summary>
    ///     The DIBColors enumeration defines how to interpret the values in the color table of a DIB.
    /// </summary>
    public enum DibColors : uint
    {
        /// <summary>
        ///     The color table contains literal RGB values.
        /// </summary>
        RgbColors = 0x00,
        /// <summary>
        /// The color table consists of an array of 16-bit indexes into the LogPalette object (section 2.2.17) that is currently defined in the playback device context.
        /// </summary>
        PalColors = 0x01,
        /// <summary>
        /// No color table exists. The pixels in the DIB are indices into the current logical palette in the playback device context.
        /// </summary>
        PalIndices = 0x02
    }
}