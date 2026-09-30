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
    /// <remarks>
    /// Windows synthesizes CF_UNICODETEXT from CF_TEXT and CF_OEMTEXT on the clipboard, but other sources (e.g. a <see cref="DataObjectReader"/>
    /// for a drop) may only have CF_TEXT. For CF_UNICODETEXT such sources fall back like Windows: CF_TEXT decoded with the ANSI code page of
    /// CF_LOCALE when present, else the system ANSI code page; then CF_OEMTEXT with the OEM code page. The text ends at the first NUL.
    /// The open clipboard, a <see cref="ClipboardSnapshot"/> and the OLE clipboard (<see cref="ClipboardNative.GetOleDataObject"/>) don't
    /// use the fallback: the clipboard synthesizes CF_UNICODETEXT itself, and a snapshot has exactly the formats which were requested.
    /// </remarks>
    /// <param name="source">IClipboardDataSource</param>
    /// <param name="text">string</param>
    /// <param name="format">string with the format name, default CF_UNICODETEXT</param>
    /// <returns>true when the format is available</returns>
    public static bool TryGetAsUnicodeString(this IClipboardDataSource source, out string text, string format = null)
    {
        text = null;
        var unicodeTextFormat = StandardClipboardFormats.UnicodeText.AsString();
        format ??= unicodeTextFormat;
        if (source.TryGetAsBytes(format, out var bytes))
        {
            text = DecodeNullTerminated(Encoding.Unicode.GetString(bytes));
            return true;
        }
        return string.Equals(format, unicodeTextFormat, StringComparison.OrdinalIgnoreCase)
               && !HasSynthesizedFormats(source)
               && TryGetAnsiTextAsUnicode(source, out text);
    }

    /// <summary>
    /// True for the sources which represent the clipboard: Windows synthesizes the text formats there, and a snapshot must only
    /// return what was read from the clipboard
    /// </summary>
    private static bool HasSynthesizedFormats(IClipboardDataSource source) =>
        source is ClipboardSnapshot or ClipboardTokenDataSource or DataObjectReader { IsFromClipboard: true };

    /// <summary>
    /// CF_TEXT with the code page of CF_LOCALE (or CP_ACP), else CF_OEMTEXT with CP_OEMCP, like Windows synthesizes CF_UNICODETEXT
    /// </summary>
    private static bool TryGetAnsiTextAsUnicode(IClipboardDataSource source, out string text)
    {
        text = null;
        if (source.TryGetAsBytes(StandardClipboardFormats.Text.AsString(), out var ansi) && ansi != null)
        {
            source.TryGetAsBytes(StandardClipboardFormats.Locale.AsString(), out var locale);
            text = AnsiText.Decode(ansi, AnsiText.CodePageOfLocale(locale));
            return text != null;
        }
        if (source.TryGetAsBytes(StandardClipboardFormats.OemText.AsString(), out var oem) && oem != null)
        {
            text = AnsiText.Decode(oem, AnsiText.OemCodePage);
            return text != null;
        }
        return false;
    }

    /// <summary>
    /// Get a UTF-16 string, like CF_UNICODETEXT. For CF_UNICODETEXT, sources other than the clipboard fall back to CF_TEXT / CF_OEMTEXT,
    /// see <see cref="TryGetAsUnicodeString"/>.
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
    /// Check if the source has virtual files (FileGroupDescriptorW or the ANSI FileGroupDescriptor), e.g. Outlook attachments.
    /// The descriptor isn't read. Read the files with <see cref="DataObjectReader.GetVirtualFiles"/>, for a snapshot of the clipboard with
    /// <see cref="ClipboardSnapshot.TryUseVirtualFiles{T}"/>; <see cref="ClipboardNative.HasVirtualFiles"/> checks the clipboard itself.
    /// </summary>
    /// <param name="source">IClipboardDataSource</param>
    /// <returns>bool</returns>
    public static bool HasVirtualFiles(this IClipboardDataSource source)
    {
        if (source == null)
        {
            throw new ArgumentNullException(nameof(source));
        }
        return source.HasFormat(DataObjectReader.FileGroupDescriptorWFormat) || source.HasFormat(DataObjectReader.FileGroupDescriptorFormat);
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
