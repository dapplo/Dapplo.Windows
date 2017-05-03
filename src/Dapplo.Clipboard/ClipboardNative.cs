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
using System.ComponentModel.DataAnnotations;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Dapplo.Windows.Clipboard
{
    /// <summary>
    /// Provides low level access to the Windows clipboard
    /// </summary>
    public static class ClipboardNative
    {
        private const int SuccessError = 0;

        // "Global" clipboard lock
        private static readonly ClipboardSemaphore ClipboardLock = new ClipboardSemaphore();

        // Cache for all the known clipboard format names
        private static readonly IDictionary<uint, string> Id2Format = new Dictionary<uint, string>();
        private static readonly IDictionary<string, uint> Format2Id = new Dictionary<string, uint>();

        /// <summary>
        /// Initialize the static data of the class
        /// </summary>
        static ClipboardNative()
        {
            // Create an entry for every enum element which has a Display attribute
            foreach (var enumValue in typeof(StandardClipboardFormats).GetEnumValues())
            {
                uint id = (uint) enumValue;
                var displayAttribute = enumValue.GetType().GetMember(enumValue.ToString()).FirstOrDefault()?.GetCustomAttributes<DisplayAttribute>()?.FirstOrDefault();
                var formatName = displayAttribute != null ? displayAttribute.Name : enumValue.ToString();
                Format2Id[formatName] = id;
                Id2Format[id] = formatName;
            }
        }

        /// <summary>
        /// Get a global lock to the clipboard
        /// </summary>
        /// <param name="hWnd">IntPtr with the windows handle</param>
        /// <param name="retries">int with the amount of lock attempts are made</param>
        /// <param name="retryInterval">Timespan between retries, default 200ms</param>
        /// <param name="timeout">Timeout for getting the lock</param>
        /// <returns>IDisposable, which will unlock when Dispose is called</returns>
        public static IDisposable Lock(IntPtr hWnd = default(IntPtr), int retries = 5, TimeSpan? retryInterval = null, TimeSpan? timeout = null)
        {
            return ClipboardLock.Lock(hWnd, retries, retryInterval, timeout);
        }


        /// <summary>
        /// Get a global lock to the clipboard
        /// </summary>
        /// <param name="hWnd">IntPtr with the windows handle</param>
        /// <param name="retries">int with the amount of lock attempts are made</param>
        /// <param name="retryInterval">Timespan between retries, default 200ms</param>
        /// <param name="cancellationToken">CancellationToken</param>
        /// <returns>IDisposable in a Task, which will unlock when Dispose is called</returns>
        public static Task<IDisposable> LockAsync(IntPtr hWnd = default(IntPtr), int retries = 5, TimeSpan? retryInterval = null, CancellationToken cancellationToken = default(CancellationToken))
        {
            return ClipboardLock.LockAsync(hWnd, retries, retryInterval, cancellationToken);
        }

        /// <summary>
        /// Place string on the clipboard, this assumes you already locked the clipboard
        /// </summary>
        /// <param name="text">string to place on the clipboard</param>
        /// <param name="format">Optional format, default is CF_UNICODETEXT</param>
        public static void Put(string text, string format = "CF_UNICODETEXT")
        {
            uint formatId;
            if (!Format2Id.TryGetValue(format, out formatId))
            {
                throw new ArgumentException($"{format} is not a known format.", nameof(format));
            }
            var hText = IntPtr.Zero;
            try
            {
                hText = Marshal.StringToHGlobalUni(text);
                SetClipboardData(formatId, hText);
            }
            finally
            {
                if (hText != IntPtr.Zero)
                {
                    Marshal.FreeHGlobal(hText);
                }
            }
        }

        /// <summary>
        /// Get a string from the clipboard, this assumes you already locked the clipboard
        /// </summary>
        /// <param name="format">Optional format, default is CF_UNICODETEXT</param>
        /// <returns>string</returns>
        public static string GetAsString(string format = "CF_UNICODETEXT")
        {
            uint formatId;
            if (!Format2Id.TryGetValue(format, out formatId))
            {
                throw new ArgumentException($"{format} is not a known format.", nameof(format));
            }
            var hText = GetClipboardData(formatId);
            return Marshal.PtrToStringUni(hText);
        }

        /// <summary>
        /// Retrieve the content for the specified format.
        /// You will need to "lock" (OpenClipboard) the clipboard before calling this.
        /// </summary>
        /// <param name="format">the format to retrieve the content for</param>
        /// <returns>MemoryStream</returns>
        public static MemoryStream GetAsStream(string format)
        {
            uint formatId;

            if (!Format2Id.TryGetValue(format, out formatId))
            {
                throw new ArgumentException($"{format} is not a known format.", nameof(format));
            }
            var hGlobal = GetClipboardData(formatId);
            var memoryPtr = GlobalLock(hGlobal);
            try
            {
                if (memoryPtr == IntPtr.Zero)
                {
                    throw new Win32Exception();
                }
                var size = GlobalSize(hGlobal);
                var stream = new MemoryStream(size);
                stream.SetLength(size);
                // Fill the memory stream
                Marshal.Copy(memoryPtr, stream.GetBuffer(), 0, size);
                return stream;
            }
            finally
            {
                GlobalUnlock(hGlobal);
            }
        }

        /// <summary>
        ///     Enumerate through all formats on the clipboard
        /// </summary>
        /// <param name="hWnd">IntPtr with the handle which wants to access the clipboard</param>
        /// <returns>IEnumerable with strings defining the format</returns>
        public static IEnumerable<string> AvailableFormats(IntPtr hWnd = default(IntPtr))
        {
            using (ClipboardLock.Lock(hWnd))
            {
                uint clipboardId = 0;
                var clipboardFormatName = new StringBuilder(256);
                while (true)
                {
                    clipboardId = EnumClipboardFormats(clipboardId);
                    if (clipboardId == 0)
                    {
                        // If GetLastWin32Error return SuccessError, this is the end
                        if (Marshal.GetLastWin32Error() == SuccessError)
                        {
                            yield break;
                        }
                        // GetLastWin32Error didn't return SuccessError, so throw exception
                        throw new Win32Exception();
                    }
                    string formatName;
                    clipboardFormatName.Length = 0;
                    if (Id2Format.TryGetValue(clipboardId, out formatName))
                    {
                        yield return formatName;
                        continue;
                    }
                    if (GetClipboardFormatName(clipboardId, clipboardFormatName, clipboardFormatName.Capacity) <= 0)
                    {
                        // No name
                        continue;
                    }
                    formatName = clipboardFormatName.ToString();
                    Id2Format[clipboardId] = formatName;
                    Format2Id[formatName] = clipboardId;
                    yield return clipboardFormatName.ToString();
                }
            }
        }

        #region Native methods

        /// <summary>
        /// Locks a global memory object and returns a pointer to the first byte of the object's memory block.
        /// </summary>
        /// <param name="hMem">IntPtr with a hGlobal, handle for a global memory blockk</param>
        /// <returns>IntPtr to the first byte of the global memory block</returns>
        [DllImport("kernel32", SetLastError = true)]
        private static extern IntPtr GlobalLock(IntPtr hMem);

        /// <summary>
        /// Decrements the lock count associated with a memory object that was allocated with GMEM_MOVEABLE. This function has no effect on memory objects allocated with GMEM_FIXED.
        /// If the memory object is still locked after decrementing the lock count, the return value is a nonzero value. If the memory object is unlocked after decrementing the lock count, the function returns zero and GetLastError returns NO_ERROR.
        /// If the function fails, the return value is zero and GetLastError returns a value other than NO_ERROR.
        /// </summary>
        /// <param name="hMem">IntPtr with a hGlobal, handle for a global memory block</param>
        /// <returns>bool if the unlock worked.</returns>
        [DllImport("kernel32", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool GlobalUnlock(IntPtr hMem);

        /// <summary>
        /// Retrieves the current size of the specified global memory object, in bytes.
        /// </summary>
        /// <param name="hMem">IntPtr with a hGlobal, handle for a global memory blockk</param>
        /// <returns>int with the size</returns>
        [DllImport("kernel32", SetLastError = true)]
        private static extern int GlobalSize(IntPtr hMem);

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
        private static extern uint EnumClipboardFormats(uint format);

        /// <summary>
        /// Determines whether the clipboard contains data in the specified format.
        /// </summary>
        /// <param name="format">uint for the format</param>
        /// <returns>bool</returns>
        [DllImport("user32", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool IsClipboardFormatAvailable(uint format);

        /// <summary>
        /// Empties the clipboard and frees handles to data in the clipboard. The function then assigns ownership of the clipboard to the window that currently has the clipboard open.
        /// </summary>
        /// <returns>bool</returns>
        [DllImport("user32", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool EmptyClipboard();

        /// <summary>
        /// Retrieves data from the clipboard in a specified format. The clipboard must have been opened previously.
        /// </summary>
        /// <param name="format">uint with the clipboard format.</param>
        /// <returns>IntPtr with a handle to the memory</returns>
        [DllImport("user32", SetLastError = true)]
        private static extern IntPtr GetClipboardData(uint format);

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
        private static extern int GetClipboardFormatName(uint format, [Out] StringBuilder lpszFormatName, int cchMaxCount);
        #endregion
    }
}
