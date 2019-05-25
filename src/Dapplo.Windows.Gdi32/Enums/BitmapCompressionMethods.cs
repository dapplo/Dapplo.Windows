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

using System.Diagnostics.CodeAnalysis;

namespace Dapplo.Windows.Gdi32.Enums
{
    /// <summary>
    ///     Type of compression used for the bitmap in the BitmapInfoHeader
    /// </summary>
    [SuppressMessage("ReSharper", "InconsistentNaming")]
    public enum BitmapCompressionMethods : uint
    {
        /// <summary>
        ///     No compression
        /// </summary>
        BI_RGB = 0,

        /// <summary>
        ///     RLE 8BPP
        /// </summary>
        BI_RLE8 = 1,

        /// <summary>
        ///     RLE 4BPP
        /// </summary>
        BI_RLE4 = 2,

        /// <summary>
        ///     Specifies that the bitmap is not compressed and that the color table consists of three DWORD color masks that
        ///     specify the
        ///     red, green, and blue components, respectively, of each pixel. This is valid when used with 16- and 32-bpp bitmaps.
        /// </summary>
        BI_BITFIELDS = 3,

        /// <summary>
        ///     Indicates that the image is a JPEG image.
        /// </summary>
        BI_JPEG = 4,

        /// <summary>
        ///     Indicates that the image is a PNG image.
        /// </summary>
        BI_PNG = 5
    }
}