// Copyright (c) Dapplo and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Runtime.InteropServices;

namespace Dapplo.Windows.Icons.Structs
{
    /// <summary>
    /// See the IconDir for more information, but this contains the separate icon / cursor definitions
    /// </summary>
    [StructLayout(LayoutKind.Explicit)]
    public struct IconDirEntry
    {
        /// <summary>
        /// Specifies image width in pixels. Can be any number between 0 and 255. Value 0 means image width is 256 pixels.
        /// </summary>
        [FieldOffset(0)] public byte ImageWidth;
        /// <summary>
        /// Specifies image height in pixels. Can be any number between 0 and 255. Value 0 means image width is 256 pixels.
        /// </summary>
        [FieldOffset(1)] public byte ImageHeight;
        /// <summary>
        /// Specifies number of colors in the color palette. Should be 0 if the image does not use a color palette.
        /// </summary>
        [FieldOffset(2)] public byte ImageColorCount;

        /// <summary>
        /// Reserved, should be 0
        ///
        /// Although Microsoft's technical documentation states that this value must be zero, the icon encoder built into .NET (System.Drawing.Icon.Save) sets this value to 255.
        /// It appears that the operating system ignores this value altogether.
        /// </summary>
        [FieldOffset(3)] private readonly byte _reserved;

        /// <summary>
        /// For .ico files! Specifies color planes. Should be 0 or 1.
        /// Setting the color planes to 0 or 1 is treated equivalently by the operating system, but if the color planes are set higher than 1, this value should be multiplied by the bits per pixel to determine the final color depth of the image. It is unknown if the various Windows operating system versions are resilient to different color plane values.
        /// </summary>
        [FieldOffset(4)] public short IconColorPlanes;

        /// <summary>
        /// For .cur files! Specifies the horizontal coordinates of the hotspot in number of pixels from the left.
        /// </summary>
        [FieldOffset(4)] public short CursorHotspotX;

        /// <summary>
        /// For .cur files! Specifies the horizontal coordinates of the hotspot in number of pixels from the top.
        /// </summary>
        [FieldOffset(6)] public short CursorHotspotY;

        /// <summary>
        /// For .ico files! Specifies bits per pixel.
        /// The bits per pixel might be set to zero, but can be inferred from the other data; specifically, if the bitmap is not PNG compressed, then the bits per pixel can be calculated based on the length of the bitmap data relative to the size of the image. If the bitmap is PNG compressed, the bits per pixel are stored within the PNG data. It is unknown if the various Windows operating system versions contain logic to infer the bit depth for all possibilities if this value is set to zero.
        /// </summary>
        [FieldOffset(6)] public short IconBitCount;

        /// <summary>
        /// Specifies the size of the image's data in bytes
        /// </summary>
        [FieldOffset(8)] public int ImageBytes;

        /// <summary>
        /// Specifies the offset of BMP or PNG data from the beginning of the ICO/CUR file.
        /// All image data referenced by entries in the image directory proceed directly after the image directory. It is customary practice to store them in the same order as defined in the image directory.
        /// Recall that if an image is stored in BMP format, it must exclude the opening BITMAPFILEHEADER structure, whereas if it is stored in PNG format, it must be stored in its entirety.
        ///
        /// Note that the height of the BMP image must be twice the height declared in the image directory.
        /// The second half of the bitmap should be an AND mask for the existing screen pixels, with the output pixels given by the formula Output = (Existing AND Mask) XOR Image.
        /// Set the mask to be zero everywhere for a clean overwrite.
        /// </summary>
        [FieldOffset(12)] public int ImageOffset;

        /// <summary>
        /// Specialized width which will get and set with a trick
        /// </summary>
        public int Width
        {
            get => ImageWidth == 0 ? 256 : ImageWidth;
            set => ImageWidth = (byte)(value == 256 ? 0 : value);
        }

        /// <summary>
        /// Specialized height which will get and set with a trick
        /// </summary>
        public int Height
        {
            get => ImageHeight == 0 ? 256 : ImageHeight;
            set => ImageHeight = (byte)(value == 256 ? 0 : value);
        }
    }
}
