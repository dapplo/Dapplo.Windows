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
using Dapplo.Windows.Kernel32.Enums;

#endregion

namespace Dapplo.Windows.Kernel32.Structs
{
    /// <summary>
    ///     See <a href="https://msdn.microsoft.com/en-us/library/windows/desktop/ms724833(v=vs.85).aspx">OSVERSIONINFOEX structure</a>
    /// </summary>
    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    [SuppressMessage("Sonar Code Smell", "S3459:Unassigned members should be removed", Justification = "Interop!")]
    [SuppressMessage("Sonar Code Smell", "S1144:Unused private types or members should be removed", Justification = "Interop!")]
    [SuppressMessage("Sonar Code Smell", "S1450:Trivial properties should be auto-implementedPrivate fields only used as local variables in methods should become local variables", Justification = "Interop!")]
    [SuppressMessage("ReSharper", "ConvertToAutoProperty")]
    [SuppressMessage("ReSharper", "UnusedMember.Global")]
    public unsafe struct OsVersionInfoEx
    {
        /// <summary>
        ///     The size of this data structure, in bytes. Set this member to sizeof(OSVERSIONINFOEX).
        /// </summary>
        private int _dwOSVersionInfoSize;
        private readonly int _dwMajorVersion;
        private readonly int _dwMinorVersion;
        private readonly int _dwBuildNumber;
        private readonly int _dwPlatformId;
        private fixed char _szCSDVersion[128];
        private readonly short _wServicePackMajor;
        private readonly short _wServicePackMinor;
        private readonly WindowsSuites _wSuiteMask;
        private readonly WindowsProductTypes _wProductType;
        private readonly byte _wReserved;

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
                fixed(char * servicePackVersion = _szCSDVersion)
                {
                    return new string(servicePackVersion);
                }
                
            }
        }

        /// <summary>
        ///     The major version number of the latest Service Pack installed on the system. For example, for Service Pack 3, the
        ///     major version number is 3.
        ///     If no Service Pack has been installed, the value is zero.
        /// </summary>
        public short ServicePackMajor => _wServicePackMajor;

        /// <summary>
        ///     The minor version number of the latest Service Pack installed on the system. For example, for Service Pack 3, the
        ///     minor version number is 0.
        /// </summary>
        public short ServicePackMinor => _wServicePackMinor;

        /// <summary>
        ///     A bit mask that identifies the product suites available on the system. This member can be a combination of the
        ///     following values.
        /// </summary>
        public WindowsSuites SuiteMask => _wSuiteMask;

        /// <summary>
        ///     Any additional information about the system.
        /// </summary>
        public WindowsProductTypes ProductType => _wProductType;

        /// <summary>
        /// Factory for an empty OsVersionInfoEx
        /// </summary>
        /// <returns></returns>
        public static OsVersionInfoEx Create()
        {
            return new OsVersionInfoEx
            {
                _dwOSVersionInfoSize = Marshal.SizeOf(typeof(OsVersionInfoEx))
            };
        }
    }
}