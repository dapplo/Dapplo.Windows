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
        [DllImport("kernel32", CharSet = CharSet.Unicode)]
        private static extern uint FormatMessage(uint dwFlags, IntPtr lpSource, uint dwMessageId, uint dwLanguageId, [Out] StringBuilder lpBuffer, int nSize, IntPtr arguments);

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
            var buffer = new StringBuilder(0x100);

            if (FormatMessage(0x3200, IntPtr.Zero, (uint)errorCode, languageId, buffer, buffer.Capacity, IntPtr.Zero) == 0)
            {
                return "Unknown error (0x" + ((int)errorCode).ToString("x") + ")";
            }

            var result = new StringBuilder();
            var i = 0;

            while (i < buffer.Length)
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

        /// <summary>
        ///     Change the last error
        /// </summary>
        /// <param name="dwErrCode">error code to change to</param>
        [DllImport("kernel32")]
        public static extern void SetLastError(uint dwErrCode);
    }
}
