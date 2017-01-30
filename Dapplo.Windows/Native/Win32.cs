//  Dapplo - building blocks for desktop applications
//  Copyright (C) 2016 Dapplo
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

#region using

using System;
using System.Runtime.InteropServices;
using System.Text;
using Dapplo.Windows.Enums;

#endregion

namespace Dapplo.Windows.Native
{
	/// <summary>
	///     Kernel 32 methods
	/// </summary>
	public static class Win32
	{
		[DllImport("kernel32.dll", CharSet = CharSet.Unicode)]
		private static extern uint FormatMessage(uint dwFlags, IntPtr lpSource, uint dwMessageId, uint dwLanguageId, [Out] StringBuilder lpBuffer, int nSize, IntPtr arguments);

		/// <summary>
		///     Get the error code from the Win32Error
		/// </summary>
		/// <param name="errorCode"></param>
		/// <returns></returns>
		public static long GetHResult(Win32Error errorCode)
		{
			var error = (int) errorCode;

			if ((error & 0x80000000) == 0x80000000)
			{
				return error;
			}

			return 0x80070000 | (uint) (error & 0xffff);
		}

		/// <summary>
		///     Get the last Win32 error as an exception
		/// </summary>
		/// <returns></returns>
		public static Win32Error GetLastErrorCode()
		{
			return (Win32Error) Marshal.GetLastWin32Error();
		}

		/// <summary>
		///     Get the message for a Win32 error
		/// </summary>
		/// <param name="errorCode">Win32Error</param>
		/// <param name="languageId">uint with language ID, see <a href="https://msdn.microsoft.com/en-us/library/dd318693.aspx">here</a></param>
		/// <returns>string with the message</returns>
		public static string GetMessage(Win32Error errorCode, uint languageId = 0)
		{
			var buffer = new StringBuilder(0x100);

			if (FormatMessage(0x3200, IntPtr.Zero, (uint) errorCode, languageId, buffer, buffer.Capacity, IntPtr.Zero) == 0)
			{
				return "Unknown error (0x" + ((int) errorCode).ToString("x") + ")";
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
		/// <param name="dwErrCode"></param>
		[DllImport("kernel32.dll")]
		public static extern void SetLastError(uint dwErrCode);
	}
}