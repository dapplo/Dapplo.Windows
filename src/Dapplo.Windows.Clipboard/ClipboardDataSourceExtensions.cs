// Copyright (c) Dapplo and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using Dapplo.Windows.Clipboard.Internals;

namespace Dapplo.Windows.Clipboard;

/// <summary>
/// Read helpers for every <see cref="IClipboardDataSource"/>: the open clipboard, a <see cref="ClipboardSnapshot"/>, drag and drop data, …
/// </summary>
public static class ClipboardDataSourceExtensions
{
    /// <summary>
    /// Use the open clipboard as <see cref="IClipboardDataSource"/>, so code written for a <see cref="ClipboardSnapshot"/> also reads the clipboard directly.
    /// The data source is only usable while the token is open, and only on the thread which opened the clipboard.
    /// </summary>
    /// <param name="clipboardAccessToken">IClipboardAccessToken</param>
    /// <returns>IClipboardDataSource</returns>
    public static IClipboardDataSource AsDataSource(this IClipboardAccessToken clipboardAccessToken)
    {
        if (clipboardAccessToken == null)
        {
            throw new ArgumentNullException(nameof(clipboardAccessToken));
        }
        return new ClipboardTokenDataSource(clipboardAccessToken);
    }

    /// <summary>
    /// Check if the standard format is available
    /// </summary>
    /// <param name="source">IClipboardDataSource</param>
    /// <param name="format">StandardClipboardFormats</param>
    /// <returns>bool</returns>
    public static bool HasFormat(this IClipboardDataSource source, StandardClipboardFormats format) => source.HasFormat(format.AsString());

    /// <summary>
    /// Try to get the data of a format as bytes
    /// </summary>
    /// <param name="source">IClipboardDataSource</param>
    /// <param name="format">string with the format name</param>
    /// <param name="bytes">byte array with a copy of the data</param>
    /// <returns>true when the format is available</returns>
    public static bool TryGetAsBytes(this IClipboardDataSource source, string format, out byte[] bytes)
    {
        if (source == null)
        {
            throw new ArgumentNullException(nameof(source));
        }
        bytes = null;
        if (!source.TryGetStream(format, out var stream) || stream == null)
        {
            return false;
        }
        using (stream)
        {
            if (stream is MemoryStream memoryStream)
            {
                bytes = memoryStream.ToArray();
                return true;
            }
            using var copy = new MemoryStream();
            stream.CopyTo(copy);
            bytes = copy.ToArray();
        }
        return true;
    }

    /// <summary>
    /// Get the data of a format as bytes
    /// </summary>
    /// <param name="source">IClipboardDataSource</param>
    /// <param name="format">string with the format name</param>
    /// <returns>byte array with a copy of the data, or null when the format isn't available</returns>
    public static byte[] GetAsBytes(this IClipboardDataSource source, string format) => source.TryGetAsBytes(format, out var bytes) ? bytes : null;

    /// <summary>
    /// Try to get a UTF-16 string, like CF_UNICODETEXT. The string ends at the first NUL character.
    /// </summary>
    /// <param name="source">IClipboardDataSource</param>
    /// <param name="text">string</param>
    /// <param name="format">string with the format name, default CF_UNICODETEXT</param>
    /// <returns>true when the format is available</returns>
    public static bool TryGetAsUnicodeString(this IClipboardDataSource source, out string text, string format = null)
    {
        text = null;
        if (!source.TryGetAsBytes(format ?? StandardClipboardFormats.UnicodeText.AsString(), out var bytes))
        {
            return false;
        }
        text = DecodeNullTerminated(Encoding.Unicode.GetString(bytes));
        return true;
    }

    /// <summary>
    /// Get a UTF-16 string, like CF_UNICODETEXT
    /// </summary>
    /// <param name="source">IClipboardDataSource</param>
    /// <param name="format">string with the format name, default CF_UNICODETEXT</param>
    /// <returns>string, or null when the format isn't available</returns>
    public static string GetAsUnicodeString(this IClipboardDataSource source, string format = null) => source.TryGetAsUnicodeString(out var text, format) ? text : null;

    /// <summary>
    /// Try to get a UTF-8 string, e.g. for formats like "text/plain;charset=utf-8" or your own formats. The string ends at the first NUL character.
    /// </summary>
    /// <param name="source">IClipboardDataSource</param>
    /// <param name="format">string with the format name</param>
    /// <param name="text">string</param>
    /// <returns>true when the format is available</returns>
    public static bool TryGetAsUtf8String(this IClipboardDataSource source, string format, out string text)
    {
        text = null;
        if (!source.TryGetAsBytes(format, out var bytes))
        {
            return false;
        }
        text = DecodeNullTerminated(Encoding.UTF8.GetString(bytes));
        return true;
    }

    /// <summary>
    /// Get the file names of CF_HDROP (e.g. files copied in Explorer)
    /// </summary>
    /// <param name="source">IClipboardDataSource</param>
    /// <returns>list with the file names, empty when the format isn't available</returns>
    public static IReadOnlyList<string> GetFileNames(this IClipboardDataSource source)
    {
        if (!source.TryGetAsBytes(StandardClipboardFormats.Drop.AsString(), out var bytes))
        {
            return Array.Empty<string>();
        }
        return ParseDropFiles(bytes);
    }

    /// <summary>
    /// Parse a DROPFILES structure with the file list which follows it
    /// </summary>
    /// <param name="bytes">byte array</param>
    /// <returns>list with the file names</returns>
    internal static IReadOnlyList<string> ParseDropFiles(byte[] bytes)
    {
        // DROPFILES: DWORD pFiles, POINT pt, BOOL fNC, BOOL fWide
        const int headerSize = 20;
        var result = new List<string>();
        if (bytes == null || bytes.Length < headerSize)
        {
            return result;
        }
        var offset = BitConverter.ToInt32(bytes, 0);
        var isWide = BitConverter.ToInt32(bytes, 16) != 0;
        if (offset < headerSize || offset >= bytes.Length)
        {
            return result;
        }
        if (isWide)
        {
            var text = Encoding.Unicode.GetString(bytes, offset, (bytes.Length - offset) & ~1);
            AddNames(text, result);
            return result;
        }
        // ANSI file names, decoded with the system code page
        var handle = GCHandle.Alloc(bytes, GCHandleType.Pinned);
        try
        {
            var pointer = handle.AddrOfPinnedObject() + offset;
            var end = bytes.Length;
            var position = offset;
            while (position < end && bytes[position] != 0)
            {
                var length = 0;
                while (position + length < end && bytes[position + length] != 0)
                {
                    length++;
                }
                result.Add(Marshal.PtrToStringAnsi(pointer + (position - offset), length));
                position += length + 1;
            }
        }
        finally
        {
            handle.Free();
        }
        return result;
    }

    private static void AddNames(string text, List<string> result)
    {
        var start = 0;
        while (start < text.Length && text[start] != '\0')
        {
            var end = text.IndexOf('\0', start);
            if (end < 0)
            {
                end = text.Length;
            }
            result.Add(text.Substring(start, end - start));
            start = end + 1;
        }
    }

    private static string DecodeNullTerminated(string text)
    {
        var terminatorIndex = text.IndexOf('\0');
        return terminatorIndex >= 0 ? text.Substring(0, terminatorIndex) : text;
    }
}
