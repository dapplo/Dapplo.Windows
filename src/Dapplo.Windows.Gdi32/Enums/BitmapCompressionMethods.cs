// Copyright (c) Dapplo and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
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