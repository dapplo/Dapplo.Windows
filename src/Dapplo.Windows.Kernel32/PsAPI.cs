//  Dapplo - building blocks for desktop applications
//  Copyright (C) 2017-2019  Dapplo
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
using System.Diagnostics;
using System.Runtime.InteropServices;

#endregion

namespace Dapplo.Windows.Kernel32
{
    /// <summary>
    ///     Description of PsAPI.
    /// </summary>
    public static class PsApi
    {
        /// <summary>
        /// Removes as many pages as possible from the working set of the specified process.
        /// </summary>
        /// <param name="hProcess">A handle to the process. The handle must have the PROCESS_QUERY_INFORMATION or PROCESS_QUERY_LIMITED_INFORMATION access right and the PROCESS_SET_QUOTA access right. For more information, see Process Security and Access Rights.</param>
        /// <returns>If the function succeeds, the return value is nonzero.
        /// If the function fails, the return value is zero. To get extended error information, call GetLastError.</returns>
        [DllImport("psapi", SetLastError = true)]
        private static extern int EmptyWorkingSet(IntPtr hProcess);

        /// <summary>
        ///     Removes as many pages as possible from the current process.
        /// </summary>
        public static void EmptyWorkingSet()
        {
            using (var currentProcess = Process.GetCurrentProcess())
            {
                EmptyWorkingSet(currentProcess.Handle);
            }
        }

        /// <summary>
        /// Retrieves the fully qualified path for the file containing the specified module.
        /// </summary>
        /// <param name="hProcess">IntPtr, A handle to the process that contains the module.</param>
        /// <param name="hModule">IntPtr A handle to the module. If this parameter is NULL, GetModuleFileNameEx returns the path of the executable file of the process specified in hProcess.</param>
        /// <returns>string</returns>
        public static string GetModuleFilename(IntPtr hProcess, IntPtr hModule)
        {
            unsafe
            {
                const int capacity = 512;
                var pathBuffer = stackalloc char[capacity];
                var nrCharacters = PsApi.GetModuleFileNameEx(hProcess, hModule, pathBuffer, capacity);
                if (nrCharacters > 0)
                {
                    return new string(pathBuffer, 0, nrCharacters);
                }

                return null;
            }
        }

        /// <summary>
        /// Retrieves the fully qualified path for the file containing the specified module.
        /// </summary>
        /// <param name="hProcess">IntPtr, A handle to the process that contains the module.</param>
       /// <returns>string</returns>
        public static string GetProcessImageFileName(IntPtr hProcess)
        {
            unsafe
            {
                const int capacity = 512;
                var pathBuffer = stackalloc char[capacity];
                var nrCharacters = GetProcessImageFileName(hProcess, pathBuffer, capacity);
                if (nrCharacters > 0)
                {
                    return new string(pathBuffer, 0, nrCharacters);
                }

                return null;
            }
        }

        /// <summary>
        /// Retrieves the fully qualified path for the file containing the specified module.
        /// </summary>
        /// <param name="hProcess">IntPtr, A handle to the process that contains the module.</param>
        /// <param name="hModule">IntPtr A handle to the module. If this parameter is NULL, GetModuleFileNameEx returns the path of the executable file of the process specified in hProcess.</param>
        /// <param name="lpFilename">char * that receives the full path to the executable file.</param>
        /// <param name="nSize">uint</param>
        /// <returns>uint If the function succeeds, the return value specifies the length of the string copied to the buffer.</returns>
        [DllImport("psapi", SetLastError = true, CharSet = CharSet.Unicode)]
        private static extern unsafe int GetModuleFileNameEx(IntPtr hProcess, IntPtr hModule, [Out] char * lpFilename, int nSize);

        /// <summary>
        /// Retrieves the name of the executable file for the specified process.
        /// </summary>
        /// <param name="hProcess">IntPtr, A handle to the process that contains the module.</param>
        /// <param name="lpImageFileName">char * that receives the full path to the executable file.</param>
        /// <param name="nSize">int</param>
        /// <returns>If the function succeeds, the return value specifies the length of the string copied to the buffer</returns>
        [DllImport("psapi", SetLastError = true, CharSet = CharSet.Unicode)]
        public static extern unsafe int GetProcessImageFileName(IntPtr hProcess, [Out] char * lpImageFileName, int nSize);
    }
}