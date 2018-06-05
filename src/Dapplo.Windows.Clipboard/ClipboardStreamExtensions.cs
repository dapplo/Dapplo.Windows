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

using System;
using System.ComponentModel;
using System.IO;
using Dapplo.Windows.Clipboard.Internals;
using Dapplo.Windows.Kernel32;
using Dapplo.Windows.Kernel32.Enums;

namespace Dapplo.Windows.Clipboard
{
    /// <summary>
    /// These are extensions to work with the clipboard
    /// </summary>
    public static class ClipboardStreamExtensions
    {
        /// <summary>
        /// Set the content for the specified format.
        /// You will need to "lock" (OpenClipboard) the clipboard before calling this.
        /// </summary>
        /// <param name="clipboard">IClipboardLock</param>
        /// <param name="format">the format to set the content for</param>
        /// <param name="stream">MemoryStream with the content</param>
        /// <param name="size">long with the size, if the stream is not seekable</param>
        public static void SetAsStream(this IClipboard clipboard, string format, Stream stream, long? size = null)
        {
            clipboard.ThrowWhenNoAccess();

            if (!stream.CanRead)
            {
                throw new NotSupportedException("Can't read stream");
            }

            bool needsDispose = false;
            long length;
            if (stream.CanSeek)
            {
                // Calculate the rest left
                length = stream.Length - stream.Position;
                if (length <= 0)
                {
                    throw new NotSupportedException($"Cannot write {length} length stream.");
                }
            }
            else if (size.HasValue)
            {
                length = size.Value;
            }
            else
            {
                var bufferStream = new MemoryStream();
                needsDispose = true;
                stream.CopyTo(bufferStream);
                length = bufferStream.Length;
                stream = bufferStream;
            }

            using (var writeInfo = clipboard.WriteInfo(format, length))
            {
                unsafe
                {
                    using (var unsafeMemoryStream = new UnmanagedMemoryStream((byte*)writeInfo.MemoryPtr, length, length, FileAccess.Write))
                    {
                        stream.CopyTo(unsafeMemoryStream);
                    }
                    if (needsDispose)
                    {
                        stream.Dispose();
                    }
                }
            }
        }

        /// <summary>
        /// Retrieve the content for the specified format.
        /// You will need to "lock" (OpenClipboard) the clipboard before calling this.
        /// </summary>
        /// <param name="clipboard">IClipboardLock</param>
        /// <param name="format">the format to retrieve the content for</param>
        /// <returns>MemoryStream</returns>
        public static Stream GetAsStream(this IClipboard clipboard, string format)
        {
            using (var readInfo = clipboard.ReadInfo(format))
            {
                // return the memory stream, the global unlock is done when the UnmanagedMemoryStreamWrapper is disposed
                unsafe
                {
                    var result = new UnmanagedMemoryStreamWrapper((byte*)readInfo.MemoryPtr, readInfo.Size, readInfo.Size, FileAccess.Read);
                    // Make sure the readinfo is disposed when needed
                    result.SetDisposable(readInfo);
                    return result;
                }
            }
        }
    }
}
