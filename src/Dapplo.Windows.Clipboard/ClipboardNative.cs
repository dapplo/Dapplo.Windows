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
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using Dapplo.Windows.Clipboard.Internals;

namespace Dapplo.Windows.Clipboard
{
    /// <summary>
    /// Provides low level access to the Windows clipboard
    /// </summary>
    public static class ClipboardNative
    {
        private const int SuccessError = 0;

        // "Global" clipboard lock
        private static readonly ClipboardSemaphore ClipboardLockProvider = new ClipboardSemaphore();

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
                var displayAttribute = enumValue.GetType().GetMember(enumValue.ToString()).FirstOrDefault()?.GetCustomAttributes<DisplayAttribute>().FirstOrDefault();
                var formatName = displayAttribute != null ? displayAttribute.Name : enumValue.ToString();
                Format2Id[formatName] = id;
                Id2Format[id] = formatName;
            }
        }

        /// <summary>
        /// Get access, a global lock, to the clipboard
        /// </summary>
        /// <param name="hWnd">IntPtr with the windows handle</param>
        /// <param name="retries">int with the amount of lock attempts are made</param>
        /// <param name="retryInterval">Timespan between retries, default 200ms</param>
        /// <param name="timeout">Timeout for getting the lock</param>
        /// <returns>IClipboard, which will unlock when Dispose is called</returns>
        public static IClipboard AccessClipboard(IntPtr hWnd = default, int retries = 5, TimeSpan? retryInterval = null, TimeSpan? timeout = null)
        {
            return ClipboardLockProvider.Lock(hWnd, retries, retryInterval, timeout);
        }

        /// <summary>
        /// Get access, a global lock, to the clipboard
        /// </summary>
        /// <param name="hWnd">IntPtr with the windows handle</param>
        /// <param name="retries">int with the amount of lock attempts are made</param>
        /// <param name="retryInterval">Timespan between retries, default 200ms</param>
        /// <param name="cancellationToken">CancellationToken</param>
        /// <returns>IClipboard in a Task, which will unlock when Dispose is called</returns>
        public static Task<IClipboard> AccessClipboardAsync(IntPtr hWnd = default, int retries = 5, TimeSpan? retryInterval = null, CancellationToken cancellationToken = default)
        {
            return ClipboardLockProvider.LockAsync(hWnd, retries, retryInterval, cancellationToken);
        }

        /// <summary>
        /// Empties the clipboard, this assumes that a lock has already been retrieved.
        /// </summary>
        public static void ClearContents(this IClipboard clipboard)
        {
            clipboard.ThrowWhenNoAccess();
            NativeMethods.EmptyClipboard();
        }

        /// <summary>
        /// Retrieves the current owner
        /// </summary>
        public static IntPtr CurrentOwner => NativeMethods.GetClipboardOwner();

        /// <summary>
        /// Retrieves the current clipboard sequence number via GetClipboardSequenceNumber
        /// This returns 0 if there is no WINSTA_ACCESSCLIPBOARD
        /// </summary>
        public static uint SequenceNumber => NativeMethods.GetClipboardSequenceNumber();

        /// <summary>
        /// Method to map a clipboard format to an ID
        /// </summary>
        /// <param name="format">clipboard format</param>
        /// <returns>dtring with the id</returns>
        public static uint MapFormatToId(string format)
        {
            if (!Format2Id.TryGetValue(format, out var formatId))
            {
                formatId = RegisterFormat(format);
            }

            return formatId;
        }

        /// <summary>
        /// Register the clipboard format, so we can use it
        /// </summary>
        /// <param name="format">string with the format to register</param>
        /// <returns>uint for the format</returns>
        public static uint RegisterFormat(string format)
        {
            if (Format2Id.TryGetValue(format, out var clipboardFormatId))
            {
                // Format was already known
                return clipboardFormatId;
            }
            clipboardFormatId = NativeMethods.RegisterClipboardFormat(format);

            // Make sure the format is known
            Id2Format[clipboardFormatId] = format;
            Format2Id[format] = clipboardFormatId;

            return clipboardFormatId;
        }

        /// <summary>
        ///     Enumerate through all formats on the clipboard, assumes the clipboard was already locked.
        /// </summary>
        /// <returns>IEnumerable with strings defining the format</returns>
        public static IList<string> AvailableFormats(this IClipboard clipboard)
        {
            clipboard.ThrowWhenNoAccess();

            uint clipboardFormatId = 0;

            var result = new List<string>();
            unsafe
            {
                const int capacity = 256;
                var clipboardFormatName = stackalloc char[capacity];
                while (true)
                {
                    clipboardFormatId = NativeMethods.EnumClipboardFormats(clipboardFormatId);
                    if (clipboardFormatId == 0)
                    {
                        // If GetLastWin32Error return SuccessError, this is the end
                        if (Marshal.GetLastWin32Error() == SuccessError)
                        {
                            break;
                        }
                        // GetLastWin32Error didn't return SuccessError, so throw exception
                        throw new Win32Exception();
                    }

                    if (Id2Format.TryGetValue(clipboardFormatId, out var formatName))
                    {
                        result.Add(formatName);
                        continue;
                    }

                    var nrCharacters =  NativeMethods.GetClipboardFormatName(clipboardFormatId, clipboardFormatName, capacity);
                    if (nrCharacters <= 0)
                    {
                        // No name
                        continue;
                    }
                    formatName = new string(clipboardFormatName, 0, nrCharacters);
                    Id2Format[clipboardFormatId] = formatName;
                    Format2Id[formatName] = clipboardFormatId;
                    result.Add(formatName);
                }
            }

            return result;
        }
    }
}
