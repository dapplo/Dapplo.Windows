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
            get
            {
                return _fileType;
            }
            private set
            {
                _fileType = value;
            }
        }

        /// <summary>
        /// The size, in bytes, of the bitmap file.
        /// </summary>
        public int Size
        {
            get
            {
                return _size;
            }
            private set
            {
                _size = value;
            }
        }

        /// <summary>
        /// The offset, in bytes, from the beginning of the BITMAPFILEHEADER structure to the bitmap bits.
        /// </summary>
        public int OffsetToBitmapBits
        {
            get
            {
                return _offsetToBitmapBits;
            }
            private set
            {
                _offsetToBitmapBits = value;
            }
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
                Size = (int) (bitmapFileHeaderSize + bitmapInfoHeader.Size + bitmapInfoHeader.SizeImage),
                _reserved1 = 0,
                _reserved2 = 0,
                // Specify on what offset the bits are found
                OffsetToBitmapBits = (int) (bitmapFileHeaderSize + bitmapInfoHeader.Size + bitmapInfoHeader.ColorsUsed * 4)
            };
        }
    }
}