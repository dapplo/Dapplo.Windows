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
    /// Create a BitmapFileHeader which needs a BitmapInfoHeader to calculate the values
    /// </summary>
    /// <param name="bitmapV5Header">BitmapV5Header</param>
    public static BitmapFileHeader Create(BitmapV5Header bitmapV5Header)
    {
        var bitmapFileHeaderSize = Marshal.SizeOf(typeof(BitmapFileHeader));
        return new BitmapFileHeader
        {
            // Fill with "BM"
            FileType = 0x4d42,
            // Size of the file, is the size of this, the size of a BitmapInfoHeader and the size of the image itself.
            Size = (int) (bitmapFileHeaderSize + bitmapV5Header.Size + bitmapV5Header.SizeImage),
            _reserved1 = 0,
            _reserved2 = 0,
            // Specify on what offset the bits are found
            OffsetToBitmapBits = (int) (bitmapFileHeaderSize + bitmapV5Header.Size + bitmapV5Header.ColorsUsed * 4)
        };
    }

    /// <summary>
    /// Create a BitmapFileHeader which needs a BitmapInfoHeader to calculate the values
    /// </summary>
    /// <param name="bitmapInfoHeader">BitmapInfoHeader</param>
    public static BitmapFileHeader Create(BitmapInfoHeader bitmapInfoHeader)
    {
        var bitmapFileHeaderSize = Marshal.SizeOf(typeof(BitmapFileHeader));
        return new BitmapFileHeader
        {
            // Fill with "BM"
            FileType = 0x4d42,
            // Size of the file, is the size of this, the size of a BitmapInfoHeader and the size of the image itself.
            Size = (int)(bitmapFileHeaderSize + bitmapInfoHeader.Size + bitmapInfoHeader.SizeImage),
            _reserved1 = 0,
            _reserved2 = 0,
            // Specify on what offset the bits are found
            OffsetToBitmapBits = (int)(bitmapFileHeaderSize + bitmapInfoHeader.Size + bitmapInfoHeader.ColorsUsed * 4)
        };
    }
}