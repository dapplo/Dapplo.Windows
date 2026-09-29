// Copyright (c) Dapplo and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Runtime.InteropServices;

namespace Dapplo.Windows.Common;

/// <summary>
///     Extension methods to test the windows version.
///     The properties named IsWindowsXyz are only true for exactly that version, IsWindowsXyzOrLater also for all later versions.
/// </summary>
public static class WindowsVersion
{
    /// <summary>
    /// Get the current windows version (Major.Minor.Build.0).
    /// This is retrieved with RtlGetVersion, so it is correct even when the host executable has no supportedOS entries in its manifest
    /// (in that case Environment.OSVersion reports 6.2 on Windows 8.1 and later, on .NET Framework).
    /// </summary>
    public static Version WinVersion { get; } = GetWindowsVersion();

    /// <summary>
    ///     Test if the current OS is Windows 10, this is false on Windows 11
    /// </summary>
    /// <returns>true if we are running on Windows 10</returns>
    public static bool IsWindows10 { get; } = WinVersion.Major == 10 && WinVersion.Build < 22000;

    /// <summary>
    ///     Test if the current OS is Windows 11 or later
    /// </summary>
    /// <returns>true if we are running on Windows 11 or later</returns>
    public static bool IsWindows11OrLater { get; } = WinVersion.Major >= 10 && WinVersion.Build >= 22000;

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
    ///     Test if the current OS is Windows Vista (6.0)
    /// </summary>
    /// <returns>true if we are running on Windows Vista</returns>
    public static bool IsWindowsVista { get; } = WinVersion.Major == 6 && WinVersion.Minor == 0;

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
    /// <returns>true if we are running on Windows XP (5.1, or 5.2 for XP x64)</returns>
    public static bool IsWindowsXp { get; } = WinVersion.Major == 5 && WinVersion.Minor >= 1;

    /// <summary>
    ///     Test if the current OS is Windows XP or later
    /// </summary>
    /// <returns>true if we are running on Windows XP or later</returns>
    public static bool IsWindowsXpOrLater { get; } = WinVersion.Major > 5 || WinVersion.Major == 5 && WinVersion.Minor >= 1;

    /// <summary>
    ///     Test if the current Windows version is 10 (or later, e.g. Windows 11) and has the specified build number or later
    ///     See the build numbers <a href="https://en.wikipedia.org/wiki/Windows_10_version_history">here</a>
    /// </summary>
    /// <param name="minimalBuildNumber">int</param>
    /// <returns>bool</returns>
    public static bool IsWindows10BuildOrLater(int minimalBuildNumber)
    {
        return IsWindows10OrLater && WinVersion.Build >= minimalBuildNumber;
    }

    /// <summary>
    ///     Retrieve the real windows version via RtlGetVersion, this is not affected by the application manifest.
    ///     Falls back to Environment.OSVersion if this is not possible (e.g. not running on Windows).
    /// </summary>
    /// <returns>Version</returns>
    private static Version GetWindowsVersion()
    {
        try
        {
            var osVersionInfo = new RtlOsVersionInfo
            {
                OsVersionInfoSize = Marshal.SizeOf(typeof(RtlOsVersionInfo))
            };
            if (RtlGetVersion(ref osVersionInfo) == 0)
            {
                return new Version(osVersionInfo.MajorVersion, osVersionInfo.MinorVersion, osVersionInfo.BuildNumber, 0);
            }
        }
        catch (Exception)
        {
            // Ignore, e.g. DllNotFoundException or EntryPointNotFoundException when not running on Windows
        }
        return Environment.OSVersion.Version;
    }

    /// <summary>
    ///     See <a href="https://learn.microsoft.com/en-us/windows-hardware/drivers/ddi/wdm/nf-wdm-rtlgetversion">RtlGetVersion function</a>
    /// </summary>
    /// <param name="versionInformation">RtlOsVersionInfo</param>
    /// <returns>NTSTATUS, 0 (STATUS_SUCCESS) if it worked</returns>
    [DllImport("ntdll.dll", ExactSpelling = true)]
    private static extern int RtlGetVersion(ref RtlOsVersionInfo versionInformation);

    /// <summary>
    ///     The RTL_OSVERSIONINFOW structure
    /// </summary>
    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    private struct RtlOsVersionInfo
    {
        public int OsVersionInfoSize;
        public int MajorVersion;
        public int MinorVersion;
        public int BuildNumber;
        public int PlatformId;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 128)]
        public string CsdVersion;
    }
}