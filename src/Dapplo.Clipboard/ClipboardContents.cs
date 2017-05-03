//  Dapplo - building blocks for desktop applications
//  Copyright (C) 2016-2017 Dapplo
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
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;

namespace Dapplo.Windows.Clipboard
{
    /// <summary>
    /// Content of the Clipboards
    /// </summary>
    public sealed class ClipboardContents : IDisposable
    {
        // Used to identify every clipboard change
        private static uint _globalSequenceNumber = 0;

        // Contents of the clipboard
        private readonly IDictionary<string, Lazy<MemoryStream>> _contents = new Dictionary<string, Lazy<MemoryStream>>();

        /// <summary>
        /// A unique ID, given as sequence.
        /// If this number doesn't match, with the global counter, the clipboard content already changed.
        /// </summary>
        public uint Id { get; }

        /// <summary>
        /// The handle of the window which owns the clipboard content
        /// </summary>
        public IntPtr OwnerHandle { get; }

        /// <summary>
        /// The formats in this clipboard contents
        /// </summary>
        public IEnumerable<string> Formats { get; }

        /// <summary>
        /// This initializes some properties
        /// </summary>
        public ClipboardContents(IntPtr hWnd)
        {
            Id = _globalSequenceNumber++;
            // Try to get the real one.
            var windowsSequenceNumber = GetClipboardSequenceNumber();
            if (windowsSequenceNumber > 0)
            {
                Id = windowsSequenceNumber;
            }
            OwnerHandle = GetClipboardOwner();

            Formats = ClipboardNative.AvailableFormats(hWnd).ToList();

            // Create Lazy for all available formats.
            foreach (var format in Formats)
            {
                _contents[format] = new Lazy<MemoryStream>(() =>
                {
                    using (ClipboardNative.Lock(hWnd))
                    {
                        return ClipboardNative.GetAsStream(format);
                    }
                });
            }
        }

        /// <summary>
        /// Returns the content for a format
        /// </summary>
        /// <param name="format"></param>
        /// <returns>MemoryStream</returns>
        public MemoryStream this[string format] => _contents[format].Value;

        /// <summary>
        /// Dispose all content
        /// </summary>
        public void Dispose()
        {
            
        }

        #region Native methods
        /// <summary>
        ///     See
        ///     <a href="https://msdn.microsoft.com/en-us/library/windows/desktop/ms649038(v=vs.85).aspx">
        ///         EnumClipboardFormats
        ///         function
        ///     </a>
        ///     Enumerates the data formats currently available on the clipboard.
        ///     Clipboard data formats are stored in an ordered list. To perform an enumeration of clipboard data formats, you make
        ///     a series of calls to the EnumClipboardFormats function. For each call, the format parameter specifies an available
        ///     clipboard format, and the function returns the next available clipboard format.
        /// </summary>
        /// <param name="format">
        ///     To start an enumeration of clipboard formats, set format to zero. When format is zero, the
        ///     function retrieves the first available clipboard format. For subsequent calls during an enumeration, set format to
        ///     the result of the previous EnumClipboardFormats call.
        /// </param>
        /// <returns>uint with clipboard format id</returns>
        [DllImport("user32", SetLastError = true)]
        private static extern uint EnumClipboardFormats(uint format);

        /// <summary>
        /// Retrieves data from the clipboard in a specified format. The clipboard must have been opened previously.
        /// </summary>
        /// <param name="format">uint with the clipboard format.</param>
        /// <returns>IntPtr with a handle to the memory</returns>
        [DllImport("user32", SetLastError = true)]
        private static extern IntPtr GetClipboardData(uint format);

        /// <summary>
        /// Returns the hWnd of the owner of the clipboard content
        /// </summary>
        /// <returns>IntPtr with a hWnd</returns>
        [DllImport("user32", SetLastError = true)]
        private static extern IntPtr GetClipboardOwner();

        /// <summary>
        /// Retrieves the sequence number of the clipboard
        /// </summary>
        /// <returns>sequence number or 0 if this cannot be retrieved</returns>
        [DllImport("user32", SetLastError = true)]
        private static extern uint GetClipboardSequenceNumber();

        /// <summary>
        /// Retrieves data from the clipboard in a specified format. The clipboard must have been opened previously.
        /// </summary>
        /// <param name="format">StandardClipboardFormats with the clipboard format.</param>
        /// <returns>IntPtr with a handle to the memory</returns>
        [DllImport("user32", SetLastError = true)]
        private static extern IntPtr GetClipboardData(StandardClipboardFormats format);

        /// <summary>
        /// Places data on the clipboard in a specified clipboard format.
        /// The window must be the current clipboard owner, and the application must have called the OpenClipboard function.
        /// (When responding to the WM_RENDERFORMAT and WM_RENDERALLFORMATS messages, the clipboard owner must not call OpenClipboard before calling SetClipboardData.)
        /// </summary>
        /// <param name="format">uint</param>
        /// <param name="memory">IntPtr to the memory area</param>
        /// <returns></returns>
        [DllImport("user32", SetLastError = true)]
        private static extern IntPtr SetClipboardData(uint format, IntPtr memory);

        /// <summary>
        /// Places data on the clipboard in a specified clipboard format.
        /// The window must be the current clipboard owner, and the application must have called the OpenClipboard function.
        /// (When responding to the WM_RENDERFORMAT and WM_RENDERALLFORMATS messages, the clipboard owner must not call OpenClipboard before calling SetClipboardData.)
        /// </summary>
        /// <param name="format">StandardClipboardFormats can be used</param>
        /// <param name="memory">IntPtr to the memory area</param>
        /// <returns></returns>
        [DllImport("user32", SetLastError = true)]
        private static extern IntPtr SetClipboardData(StandardClipboardFormats format, IntPtr memory);

        /// <summary>
        ///     See
        ///     <a href="https://msdn.microsoft.com/en-us/library/windows/desktop/ms649040(v=vs.85).aspx">
        ///         GetClipboardFormatName
        ///         function
        ///     </a>
        ///     Retrieves from the clipboard the name of the specified registered format.
        ///     The function copies the name to the specified buffer.
        /// </summary>
        /// <param name="format">uint with the id of the format</param>
        /// <param name="lpszFormatName">Name of the format</param>
        /// <param name="cchMaxCount">Maximum size of the output</param>
        /// <returns></returns>
        [DllImport("user32", SetLastError = true, CharSet = CharSet.Unicode)]
        private static extern int GetClipboardFormatName(uint format, [Out] StringBuilder lpszFormatName, int cchMaxCount);
        #endregion
    }
}
