// Copyright (c) Dapplo and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;

namespace Dapplo.Windows.Citrix.Structs;

/// <summary>
///     This structure is returned when WFQuerySessionInformation is called with WFInfoClasses.Version
/// </summary>
[StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
[SuppressMessage("Sonar Code Smell", "S3459:Unassigned members should be removed", Justification = "Interop!")]
[SuppressMessage("Sonar Code Smell", "S1144:Unused private types or members should be removed", Justification = "Interop!")]
[SuppressMessage("Sonar Code Smell", "S1450:Trivial properties should be auto-implementedPrivate fields only used as local variables in methods should become local variables", Justification = "Interop!")]
[SuppressMessage("ReSharper", "ConvertToAutoProperty")]
[SuppressMessage("ReSharper", "UnusedMember.Global")]
public unsafe struct OsVersionInfo
{
    /// <summary>
    ///     The size of this data structure, in bytes. Set this member to sizeof(OSVERSIONINFO).
    /// </summary>
    private int _dwOSVersionInfoSize;
    private readonly int _dwMajorVersion;
    private readonly int _dwMinorVersion;
    private readonly int _dwBuildNumber;
    private readonly int _dwPlatformId;
    private fixed char _szCSDVersion[128];

    /// <summary>
    ///     The major version number of the operating system.
    /// </summary>
    public int MajorVersion => _dwMajorVersion;

    /// <summary>
    ///     The minor version number of the operating system.
    /// </summary>
    public int MinorVersion => _dwMinorVersion;

    /// <summary>
    ///     The build number of the operating system.
    /// </summary>
    public int BuildNumber => _dwBuildNumber;

    /// <summary>
    ///     The operating system platform. This member can be VER_PLATFORM_WIN32_NT (2).
    /// </summary>
    public int PlatformId => _dwPlatformId;

    /// <summary>
    ///     A null-terminated string, such as "Service Pack 3", that indicates the latest Service Pack installed on the system.
    ///     If no Service Pack has been installed, the string is empty.
    /// </summary>
    public string ServicePackVersion
    {
        get
        {
            fixed (char* servicePackVersion = _szCSDVersion)
            {
                return new string(servicePackVersion);
            }

        }
    }

    /// <summary>
    /// Factory for an empty OsVersionInfo
    /// </summary>
    /// <returns></returns>
    public static OsVersionInfo Create()
    {
        return new OsVersionInfo
        {
            _dwOSVersionInfoSize = Marshal.SizeOf(typeof(OsVersionInfo))
        };
    }
}