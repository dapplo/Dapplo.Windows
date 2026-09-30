// Copyright (c) Dapplo and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Runtime.InteropServices;
using System.Text;
using Dapplo.Windows.Common.Enums;

namespace Dapplo.Windows.Common;

/// <summary>
/// Helper class for Win32 errors
/// </summary>
public static class Win32
{
    /// <summary>
    /// Formats a message string.
    /// The function requires a message definition as input.
    /// The message definition can come from a buffer passed into the function.
    /// It can come from a message table resource in an already-loaded module.
    /// Or the caller can ask the function to search the system's message table resource(s) for the message definition.
    /// The function finds the message definition in a message table resource based on a message identifier and a language identifier.
    /// The function copies the formatted message text to an output buffer, processing any embedded insert sequences if requested.
    /// </summary>
    /// <param name="dwFlags"></param>
    /// <param name="lpSource"></param>
    /// <param name="dwMessageId"></param>
    /// <param name="dwLanguageId"></param>
    /// <param name="lpBuffer"></param>
    /// <param name="nSize"></param>
    /// <param name="arguments"></param>
    /// <returns>If the function succeeds, the return value is the number of TCHARs stored in the output buffer, excluding the terminating null character.</returns>
    [DllImport("kernel32", CharSet = CharSet.Unicode)]
    private static extern unsafe int FormatMessage(uint dwFlags, IntPtr lpSource, uint dwMessageId, uint dwLanguageId, [Out] char* lpBuffer, int nSize, IntPtr arguments);

    /// <summary>
    ///     Convert a Win32Error to an HResult, this is the equivalent of the HRESULT_FROM_WIN32 macro.
    ///     Success (0) maps to S_OK, values which already are an HRESULT (negative when interpreted as int) are returned unchanged,
    ///     all other values are mapped to 0x8007xxxx (FACILITY_WIN32 with the severity bit set).
    /// </summary>
    /// <param name="errorCode">Win32Error</param>
    /// <returns>HResult</returns>
    public static HResult GetHResult(Win32Error errorCode)
    {
        var error = unchecked((int)errorCode);

        if (error <= 0)
        {
            return (HResult)error;
        }

        return (HResult)unchecked((int)(0x80070000u | ((uint)error & 0xffffu)));
    }

    /// <summary>
    ///     Get the last Win32 error as an exception
    /// </summary>
    /// <returns></returns>
    public static Win32Error GetLastErrorCode()
    {
        return (Win32Error)Marshal.GetLastWin32Error();
    }

    /// <summary>
    ///     Get the message for a Win32 error
    /// </summary>
    /// <param name="errorCode">Win32Error</param>
    /// <param name="languageId">
    ///     uint with language ID, see
    ///     <a href="https://msdn.microsoft.com/en-us/library/dd318693.aspx">here</a>
    /// </param>
    /// <returns>string with the message</returns>
    public static string GetMessage(Win32Error errorCode, uint languageId = 0)
    {
        unsafe
        {
            const int capacity = 256;
            var buffer = stackalloc char[capacity];
            var nrCharacters = FormatMessage(0x3200, IntPtr.Zero, (uint) errorCode, languageId, buffer, capacity, IntPtr.Zero);
            if (nrCharacters == 0)
            {
                return "Unknown error (0x" + ((int)errorCode).ToString("x") + ")";
            }
            var result = new StringBuilder();
            var i = 0;

            while (i < nrCharacters)
            {
                if (!char.IsLetterOrDigit(buffer[i]) && !char.IsPunctuation(buffer[i]) && !char.IsSymbol(buffer[i]) && !char.IsWhiteSpace(buffer[i]))
                {
                    break;
                }

                result.Append(buffer[i]);
                i++;
            }

            return result.ToString().Replace("\r\n", "");
        }
    }
}