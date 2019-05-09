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

using System;

namespace Dapplo.Windows.Gdi32.Enums
{
    /// <summary>
    ///     A raster-operation code. These codes define how the color data for the source rectangle is to be combined with the
    ///     color data for the destination rectangle to achieve the final color.
    /// </summary>
	[Flags]
    public enum RasterOperations : uint
    {
        /// <summary>
        ///     The source area is copied directly to the destination area.
        ///     dest = source
        /// </summary>
        SourceCopy = 0x00CC0020,

        /// <summary>
        ///     The colors of the source and destination areas are combined using the Boolean OR operator.
        ///     dest = source OR dest
        /// </summary>
        SourcePaint = 0x00EE0086,

        /// <summary>
        ///     The colors of the source and destination areas are combined using the Boolean AND operator.
        ///     dest = source AND dest
        /// </summary>
        SourceAnd = 0x008800C6,

        /// <summary>
        ///     The colors of the source and destination areas are combined using the Boolean XOR operator.
        ///     dest = source XOR dest
        /// </summary>
        SourceInvert = 0x00660046,

        /// <summary>
        ///     The inverted colors of the destination area are combined with the colors of the source area using the Boolean AND
        ///     operator.
        ///     dest = source AND (NOT dest)
        /// </summary>
        SourceErase = 0x00440328,

        /// <summary>
        ///     The bitmap is not mirrored.
        /// </summary>
        NoMirrorBitmap = 0x80000000,

        /// <summary>
        ///     The inverted source area is copied to the destination.
        ///     dest = (NOT source)
        /// </summary>
        NotSourceCopy = 0x00330008,

        /// <summary>
        ///     The source and destination colors are combined using the Boolean OR operator, and then resultant color is then
        ///     inverted.
        ///     dest = (NOT src) AND (NOT dest)
        /// </summary>
        NotSourceErase = 0x001100A6,

        /// <summary>
        ///     The colors of the source area are merged with the colors of the selected brush of the destination device context
        ///     using the Boolean AND operator.
        ///     dest = (source AND pattern)
        /// </summary>
        MergeCopy = 0x00C000CA,

        /// <summary>
        ///     The colors of the inverted source area are merged with the colors of the destination area by using the Boolean OR
        ///     operator.
        ///     dest = (NOT source) OR dest
        /// </summary>
        MergePaint = 0x00BB0226,

        /// <summary>
        ///     The brush currently selected in the destination device context is copied to the destination bitmap.
        ///     dest = pattern
        /// </summary>
        PatternCopy = 0x00F00021,

        /// <summary>
        ///     The colors of the brush currently selected in the destination device context are combined with the colors of the
        ///     inverted source area using the Boolean OR operator.
        ///     The result of this operation is combined with the colors of the destination area using the Boolean OR operator.
        ///     dest = DPSnoo
        /// </summary>
        PatternPaint = 0x00FB0A09,

        /// <summary>
        ///     The colors of the brush currently selected in the destination device context are combined with the colors of the
        ///     destination are using the Boolean XOR operator.
        ///     dest = pattern XOR dest
        /// </summary>
        PatternInvert = 0x005A0049,

        /// <summary>
        ///     The destination area is inverted.
        ///     dest = (NOT dest)
        /// </summary>
        DestinationInvert = 0x00550009,

        /// <summary>
        ///     The destination area is filled by using the color associated with index 0 in the physical palette. (This color is
        ///     black for the default physical palette.)
        ///     dest = BLACK
        /// </summary>
        Blackness = 0x00000042,

        /// <summary>
        ///     The destination area is filled by using the color associated with index 1 in the physical palette. (This color is
        ///     white for the default physical palette.)
        ///     dest = WHITE
        /// </summary>
        Whiteness = 0x00FF0062,

        /// <summary>
        ///     Windows that are layered on top of your window are included in the resulting image.
        ///     By default, the image contains only your window.
        ///     Note that this generally cannot be used for printing device contexts.
        /// </summary>
        CaptureBlt = 0x40000000
    }
}