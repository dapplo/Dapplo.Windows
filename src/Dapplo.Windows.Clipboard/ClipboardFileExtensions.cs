// Copyright (c) Dapplo and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
using System.Collections.Generic;
using System.Linq;
using Dapplo.Windows.Clipboard.Internals;

namespace Dapplo.Windows.Clipboard;

/// <summary>
/// These are extensions to work with the clipboard
/// </summary>
public static class ClipboardFileExtensions
{
    /// <summary>
    /// Get a list of file-names on the clipboard
    /// </summary>
    /// <param name="clipboardAccessToken">IClipboard</param>
    /// <returns>IEnumerable of string</returns>
    public static IEnumerable<string> GetFileNames(this IClipboardAccessToken clipboardAccessToken)
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