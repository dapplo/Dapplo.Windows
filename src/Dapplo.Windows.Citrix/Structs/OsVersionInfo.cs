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

using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;

#endregion

namespace Dapplo.Windows.Citrix.Structs
{
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
}