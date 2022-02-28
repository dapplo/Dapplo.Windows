// Copyright (c) Dapplo and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Runtime.InteropServices;
using Dapplo.Windows.Icons.Enums;

namespace Dapplo.Windows.Icons.Structs
{
    /// <summary>
    /// This is put together from the following sources: <a href="https://devblogs.microsoft.com/oldnewthing/20101018-00/?p=12513">The evolution of the ICO file format from Raymond Chen</a> and <a href="https://en.wikipedia.org/wiki/ICO_(file_format)">Wikipedia</a>
    ///
    /// 
    /// An ICO or CUR file is made up of an ICONDIR ("Icon directory") structure, containing an ICONDIRENTRY structure for each image in the file, followed by a contiguous block of all image bitmap data (which may be in either Windows BMP format, excluding the BITMAPFILEHEADER structure, or in PNG format, stored in its entirety).
    /// 
    /// Images with less than 32 bits of color depth follow a particular format: the image is encoded as a single image consisting of a color mask (the "XOR mask") together with an opacity mask (the "AND mask").
    /// The XOR mask must precede the AND mask inside the bitmap data; if the image is stored in bottom-up order (which it most likely is), the XOR mask would be drawn below the AND mask.
    /// The AND mask is 1 bit per pixel, regardless of the color depth specified by the BMP header, and specifies which pixels are fully transparent and which are fully opaque.
    /// The XOR mask conforms to the bit depth specified in the BMP header and specifies the numerical color or palette value for each pixel.
    /// Together, the AND mask and XOR mask make for a non-transparent image representing an image with 1-bit transparency; they also allow for inversion of the background.
    /// The height for the image in the ICONDIRENTRY structure of the ICO/CUR file takes on that of the intended image dimensions (after the masks are composited), whereas the height in the BMP header takes on that of the two mask images combined (before they are composited).
    /// Therefore, the masks must each be of the same dimensions, and the height specified in the BMP header must be exactly twice the height specified in the ICONDIRENTRY structure.
    /// 
    /// 32-bit images (including 32-bit BITMAPINFOHEADER-format BMP images) are specifically a 24-bit image with the addition of an 8-bit channel for alpha compositing.
    /// Thus, in 32-bit images, the AND mask is not required, but recommended for consideration.
    /// Windows XP and higher will use a 32-bit image in less than True color mode by constructing an AND mask based on the alpha channel (if one does not reside with the image already) if no 24-bit version of the image is supplied in the ICO/CUR file.
    /// However, earlier versions of Windows interpret all pixels with 100% opacity unless an AND mask is supplied with the image.
    /// Supplying a custom AND mask will also allow for tweaking and hinting by the icon author.
    /// Even if the AND mask is not supplied, if the image is in Windows BMP format, the BMP header must still specify a doubled height.
    /// </summary>
    [StructLayout(LayoutKind.Sequential)]
    public struct IconDir
    {
        /// <summary>
        /// Reserved. Must always be 0.
        /// </summary>
        private readonly short _reserved;
        /// <summary>
        /// Specifies image type: 1 for icon (.ICO) image, 2 for cursor (.CUR) image. Other values are invalid.
        /// </summary>
        public IconDirTypes IconDirType;
        /// <summary>
        /// Specifies number of images in the file.
        /// </summary>
        public short ImageCount;
    }
}
