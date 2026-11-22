// Copyright (c) Dapplo and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Runtime.InteropServices;
using Dapplo.Windows.Shell32.Enums;

namespace Dapplo.Windows.Shell32;

/// <summary>
///     An API for the file and protocol associations of shlwapi
/// </summary>
public static class ShlwapiApi
{
    private const string ShlwapiDll = "shlwapi.dll";
    private const int SOk = 0;
    private const int SFalse = 1;
    private const int EPointer = unchecked((int)0x80004003);
    // A command line is limited to 32767 characters, an association string longer than that is an error
    private const int MaxLength = 32768;

    /// <summary>
    ///     Searches for and retrieves a file or protocol association-related string from the registry.
    ///     See <a href="https://learn.microsoft.com/windows/win32/api/shlwapi/nf-shlwapi-assocquerystringw">AssocQueryStringW function</a>.
    ///     Use <see cref="GetAssociationString"/>, which sizes the buffer.
    /// </summary>
    /// <param name="flags">AssocF, e.g. <see cref="AssocF.IsProtocol"/> when <paramref name="pszAssoc"/> is a protocol</param>
    /// <param name="str">AssocStr, the string to return</param>
    /// <param name="pszAssoc">The extension (".html"), the protocol ("https"), a ProgID or an application, depending on the <paramref name="flags"/></param>
    /// <param name="pszExtra">The verb, e.g. "open", for <see cref="AssocStr.Command"/> and <see cref="AssocStr.Executable"/>, can be null</param>
    /// <param name="pszOut">The buffer which receives the string, null to get the required size</param>
    /// <param name="pcchOut">On entry the size of <paramref name="pszOut"/> in characters, on exit the number of characters written or needed, including the terminating null</param>
    /// <returns>HRESULT: S_OK, S_FALSE when <paramref name="pszOut"/> is null and the size is returned, E_POINTER when the buffer is too small (with <see cref="AssocF.NoTruncate"/>),
    /// HRESULT_FROM_WIN32(ERROR_NO_ASSOCIATION) (0x80070483) when there is no association</returns>
    [DllImport(ShlwapiDll, CharSet = CharSet.Unicode, EntryPoint = "AssocQueryStringW", ExactSpelling = true)]
    public static extern int AssocQueryString(AssocF flags, AssocStr str, string pszAssoc, string pszExtra, [Out] char[] pszOut, ref uint pcchOut);

    /// <summary>
    ///     Returns a file or protocol association string, e.g. the command which opens https addresses:
    ///     <c>GetAssociationString(AssocStr.Command, "https", "open", AssocF.IsProtocol)</c>.
    ///     The buffer is sized to the string, it is never truncated.
    /// </summary>
    /// <param name="str">AssocStr, the string to return</param>
    /// <param name="assoc">The extension (".html"), the protocol ("https"), a ProgID or an application, depending on the <paramref name="flags"/></param>
    /// <param name="extra">The verb, default "open", used for <see cref="AssocStr.Command"/> and <see cref="AssocStr.Executable"/>, can be null</param>
    /// <param name="flags">AssocF, e.g. <see cref="AssocF.IsProtocol"/> for a protocol</param>
    /// <returns>string, null when there is no association (or no such string for it)</returns>
    public static string GetAssociationString(AssocStr str, string assoc, string extra = "open", AssocF flags = AssocF.None)
    {
        if (string.IsNullOrEmpty(assoc))
        {
            return null;
        }
        flags |= AssocF.NoTruncate;

        // Ask for the size first, the documented result is S_FALSE, E_POINTER is also handled
        uint size = 0;
        int hResult = AssocQueryString(flags, str, assoc, extra, null, ref size);
        if (hResult != SFalse && hResult != SOk && hResult != EPointer)
        {
            return null;
        }
        if (size == 0)
        {
            size = 260;
        }

        while (size <= MaxLength)
        {
            var buffer = new char[size];
            uint length = size;
            hResult = AssocQueryString(flags, str, assoc, extra, buffer, ref length);
            if (hResult == SOk)
            {
                // length includes the terminating null
                int textLength = 0;
                while (textLength < buffer.Length && buffer[textLength] != '\0')
                {
                    textLength++;
                }
                return textLength == 0 ? null : new string(buffer, 0, textLength);
            }
            if (hResult != EPointer)
            {
                return null;
            }
            // The string changed between the calls, or the reported size was too small
            size = length > size ? length : size * 2;
        }
        return null;
    }
}
