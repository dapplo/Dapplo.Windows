//  Dapplo - building blocks for desktop applications
//  Copyright (C) 2017-2018  Dapplo
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

using System.IO;
using System.Runtime.InteropServices;
using Dapplo.Windows.Clipboard.Internals;

namespace Dapplo.Windows.Clipboard
{
    /// <summary>
    /// These are extensions to work with the clipboard
    /// </summary>
    public static class ClipboardByteExtensions
    {
        /// <summary>
        /// Retrieve the content for the specified format.
        /// You will need to "lock" (OpenClipboard) the clipboard before calling this.
        /// </summary>
        /// <param name="clipboard">IClipboardLock</param>
        /// <param name="format">the format to retrieve the content for</param>
        /// <returns>byte array</returns>
        public static byte[] GetAsBytes(this IClipboard clipboard, string format)
        {
            using (var readInfo = clipboard.ReadInfo(format))
            {
                var bytes = new byte[readInfo.Size];

                // Fill the memory stream
                Marshal.Copy(readInfo.MemoryPtr, bytes, 0, readInfo.Size);
                return bytes;
            }
        }

        /// <summary>
        /// Place byte[] on the clipboard, this assumes you already locked the clipboard.
        /// </summary>
        /// <param name="clipboard">IClipboardLock</param>
        /// <param name="bytes">bytes to place on the clipboard</param>
        /// <param name="format">format to place the bytes under</param>
        public static void SetAsBytes(this IClipboard clipboard, byte[] bytes, string format)
        {
            using (var writeInfo = clipboard.WriteInfo(format, bytes.Length))
            {
                unsafe
                {
                    using (var unsafeMemoryStream = new UnmanagedMemoryStream((byte*)writeInfo.MemoryPtr, bytes.Length, bytes.Length, FileAccess.Write))
                    {
                        unsafeMemoryStream.Write(bytes, 0, bytes.Length);
                    }
                }
            }
        }
    }
}
