// Copyright (c) Dapplo and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
using System.Text;
using Dapplo.Windows.Clipboard.Internals;

namespace Dapplo.Windows.Clipboard;

/// <summary>
/// These are extensions to work with the clipboard
/// </summary>
public static class ClipboardStringExtensions
{
    /// <summary>
    /// Place string on the clipboard, this assumes you already locked the clipboard.
    /// </summary>
    /// <param name="clipboardAccessToken">IClipboardLock</param>
    /// <param name="text">string to place on the clipboard</param>
    /// <param name="format">StandardClipboardFormats with the clipboard format to use</param>
    public static void SetAsUnicodeString(this IClipboardAccessToken clipboardAccessToken, string text, StandardClipboardFormats format)
    {
        clipboardAccessToken.SetAsUnicodeString(text, (uint)format);
    }

    /// <summary>
    /// Place string on the clipboard, this assumes you already locked the clipboard.
    /// </summary>
    /// <param name="clipboardAccessToken">IClipboardLock</param>
    /// <param name="text">string to place on the clipboard</param>
    /// <param name="format">string with the clipboard format to use</param>
    public static void SetAsUnicodeString(this IClipboardAccessToken clipboardAccessToken, string text, string format)
    {
        clipboardAccessToken.SetAsUnicodeString(text, ClipboardFormatExtensions.MapFormatToId(format));
    }

    /// <summary>
    /// Place string on the clipboard, this assumes you already locked the clipboard.
    /// It uses Unicode (CF_UNICODETEXT) by default, as all other formats are automatically generated from this by Windows.
    /// </summary>
    /// <param name="clipboardAccessToken">IClipboardLock</param>
    /// <param name="text">string to place on the clipboard</param>
    /// <param name="formatId">uint with the clipboard format id</param>
    public static void SetAsUnicodeString(this IClipboardAccessToken clipboardAccessToken, string text, uint formatId = (uint)StandardClipboardFormats.UnicodeText)
    {
        text ??= string.Empty;
        // Encode directly into the clipboard memory, instead of creating a copy of the string with the terminator and a byte array.
        // UTF-16 needs 2 bytes per char, a lone surrogate is replaced (U+FFFD) with the same size. The memory is zero initialized: the terminator.
        var byteCount = (text.Length + 1) * 2L;
        using var writeInfo = clipboardAccessToken.WriteInfo(formatId, byteCount);
        if (text.Length > 0)
        {
            unsafe
            {
                fixed (char* chars = text)
                {
                    Encoding.Unicode.GetBytes(chars, text.Length, (byte*)writeInfo.MemoryPtr, (int)byteCount);
                }
            }
        }
        writeInfo.Commit();
    }

    /// <summary>
    /// Get a string from the clipboard, this assumes you already locked the clipboard.
    /// </summary>
    /// <param name="clipboardAccessToken">IClipboardLock</param>
    /// <param name="format">StandardClipboardFormats with the clipboard format</param>
    /// <returns>string</returns>
    public static string GetAsUnicodeString(this IClipboardAccessToken clipboardAccessToken, StandardClipboardFormats format)
    {
        return clipboardAccessToken.GetAsUnicodeString((uint)format);
    }

    /// <summary>
    /// Get a string from the clipboard, this assumes you already locked the clipboard.
    /// This always takes the CF_UNICODETEXT format, as Windows automatically converts
    /// </summary>
    /// <param name="clipboardAccessToken">IClipboardLock</param>
    /// <param name="format">string with the clipboard format</param>
    /// <returns>string</returns>
    public static string GetAsUnicodeString(this IClipboardAccessToken clipboardAccessToken, string format)
    {
        return clipboardAccessToken.GetAsUnicodeString(ClipboardFormatExtensions.MapFormatToId(format));
    }

    /// <summary>
    /// Get a string from the clipboard, this assumes you already locked the clipboard.
    /// This by default takes the CF_UNICODETEXT format, as Windows automatically converts
    /// </summary>
    /// <param name="clipboardAccessToken">IClipboardLock</param>
    /// <param name="formatId">uint with the clipboard format</param>
    /// <returns>string</returns>
    public static string GetAsUnicodeString(this IClipboardAccessToken clipboardAccessToken, uint formatId = (uint)StandardClipboardFormats.UnicodeText)
    {
        // Decoded directly from the clipboard memory. The memory allocation can be larger than the string, and it's not always zero
        // initialized: the string ends at the first NUL
        using var readInfo = clipboardAccessToken.ReadInfo(formatId);
        return ClipboardSpanReading.Read(readInfo, ClipboardSpanReading.DecodeUnicode);
    }
}