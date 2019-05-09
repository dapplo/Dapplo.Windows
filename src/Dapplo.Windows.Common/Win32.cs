#region Copyright (C) 2016-2019 Dapplo
//  Dapplo - building blocks for desktop applications
//  Copyright (C) 2016-2019 Dapplo
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
#endregion

using System;
using System.Runtime.InteropServices;
using System.Text;
using Dapplo.Windows.Common.Enums;

namespace Dapplo.Windows.Common
{
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
        ///     Get the error code from the Win32Error
        /// </summary>
        /// <param name="errorCode"></param>
        /// <returns></returns>
        public static long GetHResult(Win32Error errorCode)
        {
            var error = (int)errorCode;

            if ((error & 0x80000000) == 0x80000000)
            {
                return error;
            }

            return 0x80070000 | (uint)(error & 0xffff);
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

        /// <summary>
        ///     Change the last error
        /// </summary>
        /// <param name="dwErrCode">error code to change to</param>
        [DllImport("kernel32")]
        public static extern void SetLastError(uint dwErrCode);
    }
}
