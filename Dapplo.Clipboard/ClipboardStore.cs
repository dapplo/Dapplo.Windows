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
using System.ComponentModel;
using System.Reactive.Disposables;
using System.Runtime.InteropServices;
using System.Text;

namespace Dapplo.Clipboard
{
    /// <summary>
    /// Provides access to the Windows clipboard
    /// </summary>
    public static class ClipboardStore
    {
        /// <summary>
        /// Creates a lock on the clipboard and free this when the returned object is disposed.
        /// </summary>
        /// <param name="hWnd">optional IntPtr to the window</param>
        /// <returns>IDisposable</returns>
        private static IDisposable LockClipboard(IntPtr hWnd = default(IntPtr))
        {
            if (!OpenClipboard(hWnd))
            {
                throw new Win32Exception();
            }
            return Disposable.Create(() => CloseClipboard());
        }

        /// <summary>
        /// Place string on the clipboard
        /// </summary>
        /// <param name="hWnd">IntPtr with the handle which wants to access the clipboard</param>
        /// <param name="text">string to place on the clipboard</param>
        public static void Put(string text, IntPtr hWnd = default(IntPtr))
        {
            var hText = IntPtr.Zero;
            try
            {
                using (LockClipboard(hWnd))
                {
                    hText = Marshal.StringToHGlobalUni(text);
                    SetClipboardData(StandardClipboardFormats.UnicodeText, hText);
                }
            }
            finally
            {
                if (hText != IntPtr.Zero)
                {
                    Marshal.FreeHGlobal(hText);
                }
                CloseClipboard();
            }
        }

        /// <summary>
        /// Place string on the clipboard
        /// </summary>
        /// <param name="hWnd">IntPtr with the handle which wants to access the clipboard</param>
        /// <returns>string</returns>
        public static string GetText(IntPtr hWnd = default(IntPtr))
        {
            using (LockClipboard(hWnd))
            {
                var hText = GetClipboardData(StandardClipboardFormats.UnicodeText);
                return Marshal.PtrToStringUni(hText);
            }
        }

        /// <summary>
        ///     Enumerate through all formats on the clipboard
        /// </summary>
        /// <param name="hWnd">IntPtr with the handle which wants to access the clipboard</param>
        /// <returns>IEnumerable with strings defining the format</returns>
        public static IEnumerable<string> AvailableFormats(IntPtr hWnd)
        {
            using (LockClipboard(hWnd))
            {
                uint clipboardId = 0;
                var clipboardFormatName = new StringBuilder(256);
                do
                {
                    clipboardId = EnumClipboardFormats(clipboardId);
                    if (clipboardId == 0)
                    {
                        continue;
                    }
                    if (Enum.IsDefined(typeof(StandardClipboardFormats), clipboardId))
                    {
                        var clipboardFormat = (StandardClipboardFormats)clipboardId;
                        yield return clipboardFormat.ToString();
                    }
                    else
                    {
                        clipboardFormatName.Length = 0;
                        GetClipboardFormatName(clipboardId, clipboardFormatName, clipboardFormatName.Capacity);
                        yield return clipboardFormatName.ToString();
                    }
                } while (clipboardId != 0);
            }
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
        ///     <a href="https://msdn.microsoft.com/en-us/library/windows/desktop/ms649048(v=vs.85).aspx"></a>
        ///     Opens the clipboard for examination and prevents other applications from modifying the clipboard content.
        /// </summary>
        /// <param name="hWndNewOwner">IntPtr with the hWnd of the new owner. If this parameter is NULL, the open clipboard is associated with the current task.</param>
        /// <returns>true if the clipboard is opened</returns>
        [DllImport("user32", SetLastError = true)]
        private static extern bool OpenClipboard(IntPtr hWndNewOwner);

        /// <summary>
        ///     <a href="https://msdn.microsoft.com/en-us/library/windows/desktop/ms649048(v=vs.85).aspx"></a>
        ///     Opens the clipboard for examination and prevents other applications from modifying the clipboard content.
        /// </summary>
        /// <returns>true if the clipboard is closed</returns>
        [DllImport("user32", SetLastError = true)]
        private static extern bool CloseClipboard();

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
