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

using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using Dapplo.Windows.Clipboard.Internals;

namespace Dapplo.Windows.Clipboard
{
    /// <summary>
    /// These are extensions to work with the clipboard
    /// </summary>
    public static class ClipboardFormatExtensions
    {
        private const int SuccessError = 0;

        // Used for internal locking
        private static readonly object Lock = new object();
        // Cache for all the known clipboard format names
        private static readonly Dictionary<uint, string> Id2Format = new Dictionary<uint, string>();
        private static readonly Dictionary<string, uint> Format2Id = new Dictionary<string, uint>();

        /// <summary>
        /// Initialize the static data of the class
        /// </summary>
        static ClipboardFormatExtensions()
        {
            // Create an entry for every enum element which has a Display attribute
            foreach (var enumValue in typeof(StandardClipboardFormats).GetEnumValues().Cast<StandardClipboardFormats>())
            {
                var formatName = enumValue.AsString();
                if (string.IsNullOrEmpty(formatName))
                {
                    continue;
                }
                uint id = (uint)enumValue;
                Format2Id[formatName] = id;
                Id2Format[id] = formatName;
            }
        }

        /// <summary>
        /// Get the format string for the StandardClipboardFormats
        /// </summary>
        /// <param name="format">StandardClipboardFormats</param>
        /// <returns>string</returns>
        public static string AsString(this StandardClipboardFormats format)
        {
            var displayAttribute = format.GetType().GetMember(format.ToString()).FirstOrDefault()?.GetCustomAttributes<DisplayAttribute>().FirstOrDefault();
            return displayAttribute?.Name;
        }

        /// <summary>
        /// Method to map a clipboard format to an ID
        /// </summary>
        /// <param name="format">clipboard format</param>
        /// <returns>uint with the id</returns>
        public static uint MapFormatToId(string format)
        {
            if (!Format2Id.TryGetValue(format, out var formatId))
            {
                formatId = RegisterFormat(format);
            }

            return formatId;
        }

        /// <summary>
        /// Method to map a clipboard ID to a format name
        /// </summary>
        /// <param name="formatId">clipboard format ID</param>
        /// <returns>string with the format</returns>
        public static string MapIdToFormat(uint formatId)
        {
            if (Id2Format.TryGetValue(formatId, out var format))
            {
                return format;
            }

            unsafe
            {
                const int capacity = 256;
                var clipboardFormatName = stackalloc char[capacity];
                var nrCharacters = NativeMethods.GetClipboardFormatName(formatId, clipboardFormatName, capacity);
                if (nrCharacters <= 0)
                {
                    return null;
                }
                // No name
                format = new string(clipboardFormatName, 0, nrCharacters);
                Id2Format[formatId] = format;
                Format2Id[format] = formatId;
            }

            return format;
        }

        /// <summary>
        /// Register the clipboard format, so we can use it
        /// </summary>
        /// <param name="format">string with the format to register</param>
        /// <returns>uint for the format</returns>
        public static uint RegisterFormat(string format)
        {
            uint clipboardFormatId;
            lock (Lock)
            {
                if (Format2Id.TryGetValue(format, out clipboardFormatId))
                {
                    return clipboardFormatId;
                }

                clipboardFormatId = NativeMethods.RegisterClipboardFormat(format);

                // Make sure the format is known
                Id2Format[clipboardFormatId] = format;
                Format2Id[format] = clipboardFormatId;
            }

            return clipboardFormatId;
        }

        /// <summary>
        ///     Enumerate through all formats on the clipboard, assumes the clipboard was already locked.
        /// </summary>
        /// <returns>IEnumerable with strings defining the format</returns>
        public static IEnumerable<string> AvailableFormats(this IClipboardAccessToken clipboardAccessToken)
        {
            clipboardAccessToken.ThrowWhenNoAccess();

            return clipboardAccessToken.AvailableFormatIds().Select(MapIdToFormat).Where(format => !string.IsNullOrEmpty(format));
        }

        /// <summary>
        ///     Enumerate through all formats on the clipboard, assumes the clipboard was already locked.
        /// </summary>
        /// <returns>IEnumerable with strings defining the format</returns>
        public static IEnumerable<uint> AvailableFormatIds(this IClipboardAccessToken clipboardAccessToken)
        {
            clipboardAccessToken.ThrowWhenNoAccess();

            uint clipboardFormatId = 0;

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

                yield return clipboardFormatId;
            }
        }
    }
}
