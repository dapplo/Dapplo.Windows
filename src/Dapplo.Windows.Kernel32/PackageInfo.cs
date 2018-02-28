//  Dapplo - building blocks for desktop applications
//  Copyright (C) 2017-2018  Dapplo
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

using System.Runtime.InteropServices;
using System.Text;
using Dapplo.Windows.Common;

#endregion

namespace Dapplo.Windows.Kernel32
{
    /// <summary>
    ///     Kernel 32 functionality for app packages
    /// </summary>
    public static class PackageInfo
    {
        private const long AppModelErrorNoPackage = 15700L;

        [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        private static extern int GetCurrentPackageFullName(ref int packageFullNameLength, StringBuilder packageFullName);

        /// <summary>
        /// Get the current package fullname
        /// </summary>
        /// <returns>string</returns>
        public static string CurrentPackageFullName
        {
            get
            {
                if (!WindowsVersion.IsWindows8OrLater)
                {
                    return null;
                }

                int length = 0;
                GetCurrentPackageFullName(ref length, null);
                var sb = new StringBuilder(length);
                var result = GetCurrentPackageFullName(ref length, sb);

                return result == AppModelErrorNoPackage ? null : sb.ToString();
            }
        }

        /// <summary>
        /// Test if the current process is running as a UWP app ("on the UWP")
        /// </summary>
        public static bool IsRunningOnUwp => CurrentPackageFullName != null;
    }
}