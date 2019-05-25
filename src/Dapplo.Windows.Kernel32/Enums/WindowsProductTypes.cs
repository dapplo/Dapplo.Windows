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
using System.Diagnostics.CodeAnalysis;

#endregion

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