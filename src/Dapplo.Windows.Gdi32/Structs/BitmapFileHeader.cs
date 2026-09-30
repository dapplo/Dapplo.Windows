// Copyright (c) Dapplo and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;

namespace Dapplo.Windows.Gdi32.Structs;

/// <summary>
/// The BITMAPFILEHEADER structure contains information about the type, size, and layout of a file that contains a DIB.
/// See <a href="https://msdn.microsoft.com/en-us/library/windows/desktop/dd183374(v=vs.85).aspx">BITMAPFILEHEADER structure</a>
/// </summary>
[StructLayout(LayoutKind.Sequential, Pack = 2)]
[SuppressMessage("Sonar Code Smell", "S1450:Trivial properties should be auto-implementedPrivate fields only used as local variables in methods should become local variables", Justification = "Interop!")]
[SuppressMessage("ReSharper", "ConvertToAutoProperty")]
public struct BitmapFileHeader
{
    private short _fileType;
    private int _size;
    private short _reserved1;
    private short _reserved2;
    private int _offsetToBitmapBits;

    /// <summary>
    /// The file type; must be BM.
    /// </summary>
    public short FileType
    {
        get => _fileType;
        private set => _fileType = value;
    }

    /// <summary>
    /// The size, in bytes, of the bitmap file.
    /// </summary>
    public int Size
    {
        get => _size;
        private set => _size = value;
    }

    /// <summary>
    /// The offset, in bytes, from the beginning of the BITMAPFILEHEADER structure to the bitmap bits.
    /// </summary>
    public int OffsetToBitmapBits
    {
        get => _offsetToBitmapBits;
        private set => _offsetToBitmapBits = value;
    }

    /// <summary>
    /// Create a BitmapFileHeader for a DIB with the specified header, color masks and color table sizes
    /// </summary>
    /// <param name="offsetToPixels">uint with the offset from the start of the info header to the pixels, see e.g. BitmapInfoHeader.OffsetToPixels</param>
    /// <param name="sizeImage">uint with the size of the pixels in bytes</param>
    /// <returns>BitmapFileHeader</returns>
    private static BitmapFileHeader Create(uint offsetToPixels, uint sizeImage)
    {
        var bitmapFileHeaderSize = (uint)Marshal.SizeOf(typeof(BitmapFileHeader));
        var offsetToBitmapBits = bitmapFileHeaderSize + offsetToPixels;
        return new BitmapFileHeader
        {
            // Fill with "BM"
            FileType = 0x4d42,
            // Size of the file: this header, the info header, the color masks, the color table and the image itself.
            Size = checked((int)(offsetToBitmapBits + sizeImage)),
            _reserved1 = 0,
            _reserved2 = 0,
            // Specify on what offset the bits are found
            OffsetToBitmapBits = checked((int)offsetToBitmapBits)
        };
    }

    /// <summary>
    /// Create a BitmapFileHeader which needs a BitmapV5Header to calculate the values
    /// </summary>
    /// <param name="bitmapV5Header">BitmapV5Header</param>
    public static BitmapFileHeader Create(BitmapV5Header bitmapV5Header)
    {
        var sizeImage = bitmapV5Header.SizeImage;
        if (sizeImage == 0)
        {
            // May be 0 for BI_RGB bitmaps, calculate it
            sizeImage = BitmapInfoHeader.CalculateImageSize(bitmapV5Header.Width, bitmapV5Header.Height, bitmapV5Header.BitCount);
        }
        return Create(bitmapV5Header.OffsetToPixels, sizeImage);
    }

    /// <summary>
    /// Create a BitmapFileHeader which needs a BitmapInfoHeader to calculate the values
    /// </summary>
    /// <param name="bitmapInfoHeader">BitmapInfoHeader</param>
    public static BitmapFileHeader Create(BitmapInfoHeader bitmapInfoHeader)
    {
        var sizeImage = bitmapInfoHeader.SizeImage;
        if (sizeImage == 0)
        {
            // May be 0 for BI_RGB bitmaps, calculate it
            sizeImage = BitmapInfoHeader.CalculateImageSize(bitmapInfoHeader.Width, bitmapInfoHeader.Height, bitmapInfoHeader.BitCount);
        }
        return Create(bitmapInfoHeader.OffsetToPixels, sizeImage);
    }
}