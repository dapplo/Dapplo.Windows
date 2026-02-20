// Copyright (c) Dapplo and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
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

    /// <summary>
    /// Set a list of file-names on the clipboard in CF_HDROP (Drop) format.
    /// </summary>
    /// <param name="clipboardAccessToken">IClipboard</param>
    /// <param name="fileNames">IEnumerable of strings with the fully-qualified file names</param>
    public static void SetFileNames(this IClipboardAccessToken clipboardAccessToken, IEnumerable<string> fileNames)
    {
        clipboardAccessToken.ThrowWhenNoAccess();

        var files = fileNames.ToList();

        // DROPFILES header layout (total 20 bytes):
        // pFiles (uint, 4 bytes): offset in bytes from start of structure to the file name list
        // pt     (POINT, 8 bytes): drop point coordinates (unused for clipboard; set to 0)
        // fNC    (int,   4 bytes): non-client area flag (unused; set to 0)
        // fWide  (int,   4 bytes): 1 = Unicode file names
        const int dropFilesHeaderSize = 20;

        // Total size: header
        //           + each Unicode filename including its null terminator (f.Length + 1 chars × 2 bytes)
        //           + final null terminator (2 bytes) for the double-null that ends the file list
        int dataSize = dropFilesHeaderSize + files.Sum(f => (f.Length + 1) * sizeof(char)) + sizeof(char);

        using var writeInfo = clipboardAccessToken.WriteInfo((uint)StandardClipboardFormats.Drop, dataSize);

        // Write pFiles: offset to the file name list from start of DROPFILES structure
        Marshal.WriteInt32(writeInfo.MemoryPtr, 0, dropFilesHeaderSize);
        // pt.x (offset 4) and pt.y (offset 8) remain zero (already zeroed by GlobalAlloc with ZeroInit)
        // fNC (offset 12) remains zero
        // fWide (offset 16): 1 = Unicode
        Marshal.WriteInt32(writeInfo.MemoryPtr, 16, 1);

        // Write each file name as a null-terminated Unicode string
        var offset = dropFilesHeaderSize;
        foreach (var file in files)
        {
            var fileBytes = Encoding.Unicode.GetBytes(file + "\0");
            Marshal.Copy(fileBytes, 0, writeInfo.MemoryPtr + offset, fileBytes.Length);
            offset += fileBytes.Length;
        }
        // Final null terminator (2 bytes) is already zeroed by GlobalAlloc with ZeroInit
    }
}