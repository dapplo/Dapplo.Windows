// Copyright (c) Dapplo and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
using System;
using System.Diagnostics.CodeAnalysis;

namespace Dapplo.Windows.Kernel32.Enums
{
    /// <summary>
    ///     Any additional information about the system. This member can be one of the following values.
    ///     See
    ///     <a href="https://msdn.microsoft.com/en-us/library/windows/desktop/ms724833(v=vs.85).aspx">OSVERSIONINFOEX structure</a>
    /// </summary>
    [Flags]
    [SuppressMessage("ReSharper", "InconsistentNaming")]
    public enum WindowsProductTypes : byte
    {
        /// <summary>
        ///     The operating system is Windows 8, Windows 7, Windows Vista, Windows XP Professional, Windows XP Home Edition, or
        ///     Windows 2000 Professional.
        /// </summary>
        VER_NT_WORKSTATION = 0x00000001,

        /// <summary>
        ///     The system is a domain controller and the operating system is Windows Server 2012 , Windows Server 2008 R2, Windows
        ///     Server 2008, Windows Server 2003, or Windows 2000 Server
        /// </summary>
        VER_NT_DOMAIN_CONTROLLER = 0x00000002,

        /// <summary>
        ///     The operating system is Windows Server 2012, Windows Server 2008 R2, Windows Server 2008, Windows Server 2003, or
        ///     Windows 2000 Server.
        ///     Note that a server that is also a domain controller is reported as VER_NT_DOMAIN_CONTROLLER, not VER_NT_SERVER.
        /// </summary>
        VER_NT_SERVER = 0x00000003
    }
}