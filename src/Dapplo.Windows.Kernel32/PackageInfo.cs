// Copyright (c) Dapplo and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
using System.Runtime.InteropServices;
using Dapplo.Windows.Common;

namespace Dapplo.Windows.Kernel32
{
    /// <summary>
    ///     Kernel 32 functionality for app packages
    /// </summary>
    public static class PackageInfo
    {
        private const long AppModelErrorNoPackage = 15700L;

        /// <summary>
        /// 
        /// </summary>
        /// <param name="packageFullNameLength"></param>
        /// <param name="packageFullName"></param>
        /// <returns></returns>
        [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        private static extern unsafe int GetCurrentPackageFullName(ref int packageFullNameLength, char * packageFullName);

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
                unsafe
                {
                    var result = GetCurrentPackageFullName(ref length, null);
                    if (result == AppModelErrorNoPackage)
                    {
                        return null;
                    }
                    var packageName = stackalloc char[length];
                    result = GetCurrentPackageFullName(ref length, packageName);

                    return result == AppModelErrorNoPackage ? null : new string(packageName, 0, length);
                }
            }
        }

        /// <summary>
        /// Test if the current process is running as a UWP app ("on the UWP")
        /// </summary>
        public static bool IsRunningOnUwp => CurrentPackageFullName != null;
    }
}