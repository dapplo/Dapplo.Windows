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

using Dapplo.Windows.Kernel32;
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
using Dapplo.Windows.Kernel32.Enums;

namespace Dapplo.Windows.Clipboard
{
    /// <summary>
    /// Provides low level access to the Windows clipboard
    /// </summary>
    public static class ClipboardNative
    {
        private const int SuccessError = 0;
        // Used to identify every clipboard change, if GetClipboardSequenceNumber doesn't work. 
        private static uint _globalSequenceNumber = 1;

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
        /// Empties the clipboard, this assumes that a lock has already been retrieved.
        /// </summary>
        public static void Clear()
        {
            NativeMethods.EmptyClipboard();
        }

        /// <summary>
        /// Retrieves the current owner
        /// </summary>
        public static IntPtr CurrentOwner => NativeMethods.GetClipboardOwner();

        /// <summary>
        /// Retrieves the current clipboard sequence number, either via GetClipboardSequenceNumber or internally
        /// </summary>
        public static uint SequenceNumber
        {
            get
            {
                _globalSequenceNumber++;
                var sequenceNumber = NativeMethods.GetClipboardSequenceNumber();
                return sequenceNumber > 0 ? sequenceNumber : _globalSequenceNumber;
            }
        }

        /// <summary>
        /// Place byte[] on the clipboard, this assumes you already locked the clipboard.
        /// </summary>
        /// <param name="bytes">bytes to place on the clipboard</param>
        /// <param name="format">format to place the bytes under</param>
        public static void SetAsBytes(byte[] bytes, string format)
        {
            using (var stream = new MemoryStream())
            {
                stream.Write(bytes, 0, bytes.Length);
                SetAsStream(format, stream);
            }
        }

        /// <summary>
        /// Place string on the clipboard, this assumes you already locked the clipboard.
        /// It uses CF_UNICODETEXT by default, as all other formats are automatically generated from this by Windows.
        /// </summary>
        /// <param name="text">string to place on the clipboard</param>
        /// <param name="format"></param>
        public static void SetAsUnicodeString(string text, string format = "CF_UNICODETEXT")
        {
            var unicodeBytes = Encoding.Unicode.GetBytes(text + "\0");
            SetAsBytes(unicodeBytes, format);
        }

        /// <summary>
        /// Get a string from the clipboard, this assumes you already locked the clipboard.
        /// This always takes the CF_UNICODETEXT format, as Windows automatically converts
        /// </summary>
        /// <returns>string</returns>
        public static string GetAsUnicodeString(string format = "CF_UNICODETEXT")
        {
            using (var textStream = GetAsStream(format))
            {
                return Encoding.Unicode.GetString(textStream.GetBuffer(), 0, (int) textStream.Length).TrimEnd('\0');
            }
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
                throw new ArgumentException($"{format} is not a known format, you might want to register it and call AvailableFormats afterwards.", nameof(format));
            }
            var hGlobal = NativeMethods.GetClipboardData(formatId);
            var memoryPtr = Kernel32Api.GlobalLock(hGlobal);
            try
            {
                if (memoryPtr == IntPtr.Zero)
                {
                    throw new Win32Exception();
                }
                var size = Kernel32Api.GlobalSize(hGlobal);
                var stream = new MemoryStream(size);
                stream.SetLength(size);
                // Fill the memory stream
                Marshal.Copy(memoryPtr, stream.GetBuffer(), 0, size);
                return stream;
            }
            finally
            {
                Kernel32Api.GlobalUnlock(hGlobal);
            }
        }

        /// <summary>
        /// Set the content for the specified format.
        /// You will need to "lock" (OpenClipboard) the clipboard before calling this.
        /// </summary>
        /// <param name="format">the format to set the content for</param>
        /// <param name="stream">MemoryStream with the content</param>
        public static void SetAsStream(string format, MemoryStream stream)
        {
            uint formatId;

            if (!Format2Id.TryGetValue(format, out formatId))
            {
                // TODO: Set format
                throw new ArgumentException($"{format} is not a known format, you might want to register it and call AvailableFormats afterwards.", nameof(format));
            }
            var length = stream.Length;
            var hGlobal = Kernel32Api.GlobalAlloc(GlobalMemorySettings.ZeroInit | GlobalMemorySettings.Movable , new UIntPtr((ulong)length));
            if (hGlobal == IntPtr.Zero)
            {
                throw new Win32Exception();
            }
            var memoryPtr = Kernel32Api.GlobalLock(hGlobal);
            try
            {
                if (memoryPtr == IntPtr.Zero)
                {
                    throw new Win32Exception();
                }
                // Fill the global memory
                Marshal.Copy(stream.GetBuffer(), 0, memoryPtr, (int)length);
            }
            finally
            {
                Kernel32Api.GlobalUnlock(hGlobal);
            }
            // Place the content on the clipboard
            NativeMethods.SetClipboardData(formatId, hGlobal);
        }

        /// <summary>
        /// Register the clipboard format, so we can use it
        /// </summary>
        /// <param name="format">string with the format to register</param>
        public static void RegisterFormat(string format)
        {
            if (Format2Id.ContainsKey(format))
            {
                // Format was already known
                return;
            }
            var clipboardFormatId = NativeMethods.RegisterClipboardFormat(format);

            // Make sure the format is known
            Id2Format[clipboardFormatId] = format;
            Format2Id[format] = clipboardFormatId;
        }

        /// <summary>
        ///     Enumerate through all formats on the clipboard, assumes the clipboard was already locked.
        /// </summary>
        /// <returns>IEnumerable with strings defining the format</returns>
        public static IEnumerable<string> AvailableFormats()
        {
            uint clipboardFormatId = 0;
            var clipboardFormatName = new StringBuilder(256);
            while (true)
            {
                clipboardFormatId = NativeMethods.EnumClipboardFormats(clipboardFormatId);
                if (clipboardFormatId == 0)
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
                if (Id2Format.TryGetValue(clipboardFormatId, out formatName))
                {
                    yield return formatName;
                    continue;
                }
                if (NativeMethods.GetClipboardFormatName(clipboardFormatId, clipboardFormatName, clipboardFormatName.Capacity) <= 0)
                {
                    // No name
                    continue;
                }
                formatName = clipboardFormatName.ToString();
                Id2Format[clipboardFormatId] = formatName;
                Format2Id[formatName] = clipboardFormatId;
                yield return clipboardFormatName.ToString();
            }
        }

        #region Native methods

        private static class NativeMethods
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
            internal static extern int GetClipboardFormatName(uint format, [Out] StringBuilder lpszFormatName, int cchMaxCount);

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
        }
        #endregion
    }
}
