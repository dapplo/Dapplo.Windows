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
using System.Runtime.InteropServices;

namespace Dapplo.Windows.Clipboard.Internals
{
    internal static class NativeMethods
    {
        /// <summary>
        ///     See
        ///     <a href="https://msdn.microsoft.com/en-us/library/windows/desktop/ms649038(v=vs.85).aspx">EnumClipboardFormats function</a>
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
        /// <returns>If the function succeeds, the return value is the clipboard format that follows the specified format, namely the next available clipboard format.
        ///     If the function fails, the return value is zero. To get extended error information, call GetLastError. If the clipboard is not open, the function fails.
        ///     If there are no more clipboard formats to enumerate, the return value is zero. In this case, the GetLastError function returns the value ERROR_SUCCESS.
        ///     This lets you distinguish between function failure and the end of enumeration.
        /// </returns>
        [DllImport("user32", SetLastError = true)]
        internal static extern uint EnumClipboardFormats(uint format);

        /// <summary>
        /// Determines whether the clipboard contains data in the specified format.
        /// </summary>
        /// <param name="format">uint for the format</param>
        /// <returns>bool</returns>
        [DllImport("user32", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static extern bool IsClipboardFormatAvailable(uint format);

        /// <summary>
        /// Empties the clipboard and frees handles to data in the clipboard. The function then assigns ownership of the clipboard to the window that currently has the clipboard open.
        /// </summary>
        /// <returns>bool</returns>
        [DllImport("user32", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static extern bool EmptyClipboard();

        /// <summary>
        /// Retrieves data from the clipboard in a specified format. The clipboard must have been opened previously.
        /// </summary>
        /// <param name="format">uint with the clipboard format.</param>
        /// <returns>IntPtr with a handle to the memory</returns>
        [DllImport("user32", SetLastError = true)]
        internal static extern IntPtr GetClipboardData(uint format);

        /// <summary>
        /// Places data on the clipboard in a specified clipboard format.
        /// The window must be the current clipboard owner, and the application must have called the OpenClipboard function.
        /// (When responding to the WM_RENDERFORMAT and WM_RENDERALLFORMATS messages, the clipboard owner must not call OpenClipboard before calling SetClipboardData.)
        /// </summary>
        /// <param name="format">uint</param>
        /// <param name="memory">IntPtr to the memory area</param>
        /// <returns></returns>
        [DllImport("user32", SetLastError = true)]
        internal static extern IntPtr SetClipboardData(uint format, IntPtr memory);

        /// <summary>
        ///     See
        ///     <a href="https://msdn.microsoft.com/en-us/library/windows/desktop/ms649040(v=vs.85).aspx">GetClipboardFormatName function</a>
        ///     Retrieves from the clipboard the name of the specified registered format.
        ///     The function copies the name to the specified buffer.
        /// </summary>
        /// <param name="format">uint with the id of the format</param>
        /// <param name="lpszFormatName">Name of the format</param>
        /// <param name="cchMaxCount">Maximum size of the output</param>
        /// <returns></returns>
        [DllImport("user32", SetLastError = true, CharSet = CharSet.Unicode)]
        internal static extern unsafe int GetClipboardFormatName(uint format, [Out] char* lpszFormatName, int cchMaxCount);

        /// <summary>
        /// Registers a new clipboard format. This format can then be used as a valid clipboard format.
        /// 
        /// If a registered format with the specified name already exists, a new format is not registered and the return value identifies the existing format. This enables more than one application to copy and paste data using the same registered clipboard format. Note that the format name comparison is case-insensitive.
        /// Registered clipboard formats are identified by values in the range 0xC000 through 0xFFFF.
        /// When registered clipboard formats are placed on or retrieved from the clipboard, they must be in the form of an HGLOBAL value.
        /// </summary>
        /// <param name="lpszFormat">The name of the new format.</param>
        /// <returns>
        /// If the function succeeds, the return value identifies the registered clipboard format.
        /// If the function fails, the return value is zero. To get extended error information, call GetLastError.
        /// </returns>
        [DllImport("user32", SetLastError = true, CharSet = CharSet.Unicode)]
        internal static extern uint RegisterClipboardFormat(string lpszFormat);

        /// <summary>
        /// Returns the hWnd of the owner of the clipboard content
        /// </summary>
        /// <returns>IntPtr with a hWnd</returns>
        [DllImport("user32", SetLastError = true)]
        internal static extern IntPtr GetClipboardOwner();

        /// <summary>
        /// Retrieves the sequence number of the clipboard
        /// </summary>
        /// <returns>sequence number or 0 if this cannot be retrieved</returns>
        [DllImport("user32", SetLastError = true)]
        internal static extern uint GetClipboardSequenceNumber();

        /// <summary>
        /// Retrieves the names of dropped files that result from a successful drag-and-drop operation.
        /// </summary>
        /// <param name="hDrop">Identifier of the structure that contains the file names of the dropped files.</param>
        /// <param name="iFile">Index of the file to query. If the value of this parameter is 0xFFFFFFFF, DragQueryFile returns a count of the files dropped. If the value of this parameter is between zero and the total number of files dropped, DragQueryFile copies the file name with the corresponding value to the buffer pointed to by the lpszFile parameter.</param>
        /// <param name="lpszFile">The address of a buffer that receives the file name of a dropped file when the function returns. This file name is a null-terminated string. If this parameter is NULL, DragQueryFile returns the required size, in characters, of this buffer.</param>
        /// <param name="cch">The size, in characters, of the lpszFile buffer.</param>
        /// <returns>
        /// A nonzero value indicates a successful call.
        /// When the function copies a file name to the buffer, the return value is a count of the characters copied, not including the terminating null character.
        /// If the index value is 0xFFFFFFFF, the return value is a count of the dropped files. Note that the index variable itself returns unchanged, and therefore remains 0xFFFFFFFF.
        /// If the index value is between zero and the total number of dropped files, and the lpszFile buffer address is NULL, the return value is the required size, in characters, of the buffer, not including the terminating null character.
        /// </returns>
        [DllImport("shell32")]
        internal static extern unsafe int DragQueryFile(IntPtr hDrop, uint iFile, [Out] char* lpszFile, int cch);

        /// <summary>
        ///     Add a window as a clipboard format listener
        ///     See
        ///     <a href="https://msdn.microsoft.com/en-us/library/windows/desktop/ms649033(v=vs.85).aspx">
        ///         AddClipboardFormatListener
        ///         function
        ///     </a>
        /// </summary>
        /// <param name="hWnd">IntPtr for the window to handle the messages</param>
        /// <returns>true if it worked, false if not; call GetLastError to see what was the problem</returns>
        [DllImport("user32", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static extern bool AddClipboardFormatListener(IntPtr hWnd);

        /// <summary>
        ///     Remove a window as a clipboard format listener
        ///     See
        ///     <a href="https://msdn.microsoft.com/en-us/library/windows/desktop/ms649050(v=vs.85).aspx">
        ///         RemoveClipboardFormatListener
        ///         function
        ///     </a>
        /// </summary>
        /// <param name="hWnd">IntPtr for the window to handle the messages</param>
        /// <returns>true if it worked, false if not; call GetLastError to see what was the problem</returns>
        [DllImport("user32", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static extern bool RemoveClipboardFormatListener(IntPtr hWnd);
    }
}
