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
using System.Linq;
using Dapplo.Windows.Clipboard.Internals;

namespace Dapplo.Windows.Clipboard
{
    /// <summary>
    /// These are extensions to work with the clipboard
    /// </summary>
    public static class ClipboardFileExtensions
    {
        /// <summary>
        /// Get a list of filenames on the clipboard
        /// </summary>
        /// <param name="clipboardAccessToken">IClipboard</param>
        /// <returns>IEnumerable of string</returns>
        public static IEnumerable<string> GetFilenames(this IClipboardAccessToken clipboardAccessToken)
        {
            clipboardAccessToken.ThrowWhenNoAccess();

            var hDrop = NativeMethods.GetClipboardData((uint)StandardClipboardFormats.Drop);

            unsafe
            {
                var files = NativeMethods.DragQueryFile(hDrop, uint.MaxValue, null, 0);
                if (files == 0)
                {
                    return Enumerable.Empty<string>();
                }

                var result = new List<string>();
                const int capacity = 260;
                var filename = stackalloc char[capacity];
                for (uint i = 0; i < files; i++)
                {
                    var nrCharacters = NativeMethods.DragQueryFile(hDrop, i, filename, capacity);
                    if (nrCharacters == 0)
                    {
                        continue;
                    }
                    result.Add(new string(filename, 0, nrCharacters));
                }

                return result;
            }
        }
    }
}
