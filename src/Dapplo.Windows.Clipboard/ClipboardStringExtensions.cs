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

using System.Text;

namespace Dapplo.Windows.Clipboard
{
    /// <summary>
    /// These are extensions to work with the clipboard
    /// </summary>
    public static class ClipboardStringExtensions
    {
        /// <summary>
        /// Place string on the clipboard, this assumes you already locked the clipboard.
        /// It uses CF_UNICODETEXT by default, as all other formats are automatically generated from this by Windows.
        /// </summary>
        /// <param name="clipboard">IClipboardLock</param>
        /// <param name="text">string to place on the clipboard</param>
        /// <param name="format"></param>
        public static void SetAsUnicodeString(this IClipboard clipboard, string text, string format = "CF_UNICODETEXT")
        {
            var unicodeBytes = Encoding.Unicode.GetBytes(text + "\0");
            clipboard.SetAsBytes(unicodeBytes, format);
        }

        /// <summary>
        /// Get a string from the clipboard, this assumes you already locked the clipboard.
        /// This always takes the CF_UNICODETEXT format, as Windows automatically converts
        /// </summary>
        /// <param name="clipboard">IClipboardLock</param>
        /// <param name="format">string</param>
        /// <returns>string</returns>
        public static string GetAsUnicodeString(this IClipboard clipboard, string format = "CF_UNICODETEXT")
        {
            var bytes = clipboard.GetAsBytes(format);
            return Encoding.Unicode.GetString(bytes, 0, bytes.Length).TrimEnd('\0');
        }
    }
}
