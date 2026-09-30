// Copyright (c) Dapplo and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
using System.Runtime.InteropServices;
using Dapplo.Windows.Common;

namespace Dapplo.Windows.Kernel32;

/// <summary>
///     Kernel 32 functionality for app packages
/// </summary>
public static class PackageInfo
{
    private const int ErrorSuccess = 0;
    private const int ErrorInsufficientBuffer = 122;

    /// <summary>
    /// See <a href="https://learn.microsoft.com/en-us/windows/win32/api/appmodel/nf-appmodel-getcurrentpackagefullname">GetCurrentPackageFullName function</a>
    /// </summary>
    /// <param name="packageFullNameLength">On input the size of the buffer in characters, on output the size of the package full name including the null-terminator</param>
    /// <param name="packageFullName">char * to the buffer, or null to query the length</param>
    /// <returns>int with the Win32 error code: ERROR_SUCCESS, APPMODEL_ERROR_NO_PACKAGE (15700) or ERROR_INSUFFICIENT_BUFFER (122)</returns>
    [DllImport("kernel32.dll", CharSet = CharSet.Unicode)]
    private static extern unsafe int GetCurrentPackageFullName(ref int packageFullNameLength, char * packageFullName);

    /// <summary>
    /// Get the full name of the package of the current process
    /// </summary>
    /// <returns>string with the package full name, or null when the process has no package identity (or it couldn't be retrieved)</returns>
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
                // When there is a package, the first call reports the needed length (including the terminator) with ERROR_INSUFFICIENT_BUFFER
                var result = GetCurrentPackageFullName(ref length, null);
                if (result != ErrorInsufficientBuffer || length <= 0)
                {
                    // e.g. APPMODEL_ERROR_NO_PACKAGE
                    return null;
                }
                var packageName = stackalloc char[length];
                result = GetCurrentPackageFullName(ref length, packageName);
                if (result != ErrorSuccess || length <= 0)
                {
                    return null;
                }

                // The length includes the null-terminator
                return new string(packageName, 0, length - 1);
            }
        }
    }

    /// <summary>
    /// Test if the current process has package identity, e.g. it's installed via MSIX / the desktop bridge (or is a UWP app)
    /// </summary>
    public static bool HasPackageIdentity => CurrentPackageFullName != null;
}
