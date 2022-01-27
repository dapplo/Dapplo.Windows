// Copyright (c) Dapplo and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
using System.Text;

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
        var unicodeBytes = Encoding.Unicode.GetBytes(text + "\0");
        clipboardAccessToken.SetAsBytes(unicodeBytes, formatId);
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
        var bytes = clipboardAccessToken.GetAsBytes(formatId);
        return Encoding.Unicode.GetString(bytes, 0, bytes.Length).TrimEnd('\0');
    }
}