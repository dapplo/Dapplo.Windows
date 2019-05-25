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

#region using

using System;

#endregion

namespace Dapplo.Windows.Common
{
    /// <summary>
    ///     Extension methods to test the windows version
    /// </summary>
    public static class WindowsVersion
    {
        /// <summary>
        /// Get the current windows version
        /// </summary>
        public static Version WinVersion { get; } =
#if NET471
            Environment.OSVersion.Version;
#else
            new Version(Microsoft.DotNet.PlatformAbstractions.RuntimeEnvironment.OperatingSystemVersion);
#endif

        /// <summary>
        ///     Test if the current OS is Windows 10
        /// </summary>
        /// <returns>true if we are running on Windows 10</returns>
        public static bool IsWindows10 { get; } = WinVersion.Major == 10;

        /// <summary>
        ///     Test if the current OS is Windows 10 or later
        /// </summary>
        /// <returns>true if we are running on Windows 10 or later</returns>
        public static bool IsWindows10OrLater { get; } = WinVersion.Major >= 10;

        /// <summary>
        ///     Test if the current OS is Windows 7 or later
        /// </summary>
        /// <returns>true if we are running on Windows 7 or later</returns>
        public static bool IsWindows7OrLater { get; } = WinVersion.Major == 6 && WinVersion.Minor >= 1 || WinVersion.Major > 6;

        /// <summary>
        ///     Test if the current OS is Windows 8.0
        /// </summary>
        /// <returns>true if we are running on Windows 8.0</returns>
        public static bool IsWindows8 { get; } = WinVersion.Major == 6 && WinVersion.Minor == 2;

        /// <summary>
        ///     Test if the current OS is Windows 8(.1)
        /// </summary>
        /// <returns>true if we are running on Windows 8(.1)</returns>
        public static bool IsWindows81 { get; } = WinVersion.Major == 6 && WinVersion.Minor == 3;

        /// <summary>
        ///     Test if the current OS is Windows 8.0 or 8.1
        /// </summary>
        /// <returns>true if we are running on Windows 8.1 or 8.0</returns>
        public static bool IsWindows8X { get; } = IsWindows8 || IsWindows81;

        /// <summary>
        ///     Test if the current OS is Windows 8.1 or later
        /// </summary>
        /// <returns>true if we are running on Windows 8.1 or later</returns>
        public static bool IsWindows81OrLater { get; } = WinVersion.Major == 6 && WinVersion.Minor >= 3 || WinVersion.Major > 6;

        /// <summary>
        ///     Test if the current OS is Windows 8 or later
        /// </summary>
        /// <returns>true if we are running on Windows 8 or later</returns>
        public static bool IsWindows8OrLater { get; } = WinVersion.Major == 6 && WinVersion.Minor >= 2 || WinVersion.Major > 6;

        /// <summary>
        ///     Test if the current OS is Windows Vista
        /// </summary>
        /// <returns>true if we are running on Windows Vista or later</returns>
        public static bool IsWindowsVista { get; } = WinVersion.Major >= 6 && WinVersion.Minor == 0;

        /// <summary>
        ///     Test if the current OS is Windows Vista or later
        /// </summary>
        /// <returns>true if we are running on Windows Vista or later</returns>
        public static bool IsWindowsVistaOrLater { get; } = WinVersion.Major >= 6;

        /// <summary>
        ///     Test if the current OS is from before Windows Vista (e.g. Windows XP)
        /// </summary>
        /// <returns>true if we are running on Windows from before Vista</returns>
        public static bool IsWindowsBeforeVista { get; } = WinVersion.Major < 6;

        /// <summary>
        ///     Test if the current OS is Windows XP
        /// </summary>
        /// <returns>true if we are running on Windows XP or later</returns>
        public static bool IsWindowsXp { get; } = WinVersion.Major == 5 && WinVersion.Minor >= 1;

        /// <summary>
        ///     Test if the current OS is Windows XP or later
        /// </summary>
        /// <returns>true if we are running on Windows XP or later</returns>
        public static bool IsWindowsXpOrLater { get; } = WinVersion.Major >= 5 || WinVersion.Major == 5 && WinVersion.Minor >= 1;

        /// <summary>
        ///     Test if the current Windows version is 10 and the build number or later
        ///     See the build numbers <a href="https://en.wikipedia.org/wiki/Windows_10_version_history">here</a>
        /// </summary>
        /// <param name="minimalBuildNumber">int</param>
        /// <returns>bool</returns>
        public static bool IsWindows10BuildOrLater(int minimalBuildNumber)
        {
            return IsWindows10 && WinVersion.Build >= minimalBuildNumber;
        }
    }
}