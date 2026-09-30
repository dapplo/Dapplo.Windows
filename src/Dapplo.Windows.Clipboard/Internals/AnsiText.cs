// Copyright (c) Dapplo and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
using System;

namespace Dapplo.Windows.Clipboard.Internals;

/// <summary>
/// Decodes CF_TEXT and CF_OEMTEXT like Windows does when it synthesizes CF_UNICODETEXT on the clipboard.
/// MultiByteToWideChar is used, so every Windows code page works on .NET Framework and .NET, without System.Text.Encoding.CodePages.
/// </summary>
internal static class AnsiText
{
    // CP_ACP and CP_OEMCP
    internal const uint AnsiCodePage = 0;
    internal const uint OemCodePage = 1;
    // LOCALE_IDEFAULTANSICODEPAGE | LOCALE_RETURN_NUMBER
    private const uint LocaleDefaultAnsiCodePageNumber = 0x1004 | 0x20000000;

    /// <summary>
    /// The ANSI code page of the locale in CF_LOCALE (an LCID), like Windows uses it to convert CF_TEXT
    /// </summary>
    /// <param name="locale">bytes of CF_LOCALE, null when there is none</param>
    /// <returns>the code page, or CP_ACP when it's unknown</returns>
    internal static uint CodePageOfLocale(byte[] locale)
    {
        if (locale == null || locale.Length < 4)
        {
            return AnsiCodePage;
        }
        var lcid = BitConverter.ToUInt32(locale, 0);
        // Locales without an ANSI code page (Unicode only) return 0, which is CP_ACP
        return NativeMethods.GetLocaleInfoNumber(lcid, LocaleDefaultAnsiCodePageNumber, out var codePage, 2) > 0 ? codePage : AnsiCodePage;
    }

    /// <summary>
    /// Decode bytes up to the first NUL with the code page, falling back to CP_ACP when the code page isn't installed
    /// </summary>
    internal static string Decode(byte[] bytes, uint codePage)
    {
        if (bytes == null)
        {
            return null;
        }
        // A NUL can't be part of a multi-byte character in a Windows code page, so the first one ends the text
        var length = Array.IndexOf(bytes, (byte)0);
        if (length < 0)
        {
            length = bytes.Length;
        }
        if (length == 0)
        {
            return string.Empty;
        }
        return TryDecode(bytes, length, codePage, out var text) || (codePage != AnsiCodePage && TryDecode(bytes, length, AnsiCodePage, out text))
            ? text
            : null;
    }

    private static unsafe bool TryDecode(byte[] bytes, int length, uint codePage, out string text)
    {
        text = null;
        fixed (byte* source = bytes)
        {
            var characters = NativeMethods.MultiByteToWideChar(codePage, 0, source, length, null, 0);
            if (characters <= 0)
            {
                return false;
            }
            var buffer = new char[characters];
            fixed (char* target = buffer)
            {
                characters = NativeMethods.MultiByteToWideChar(codePage, 0, source, length, target, buffer.Length);
            }
            if (characters <= 0)
            {
                return false;
            }
            text = new string(buffer, 0, characters);
            return true;
        }
    }
}
