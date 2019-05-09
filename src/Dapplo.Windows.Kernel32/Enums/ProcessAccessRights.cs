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

#endregion

namespace Dapplo.Windows.Kernel32.Enums
{
    /// <summary>
    ///     <a href="https://msdn.microsoft.com/en-us/library/windows/desktop/ms684880(v=vs.85).aspx">Process Security and Access Rights</a>
    /// </summary>
    [Flags]
    public enum ProcessAccessRights : uint
    {
        /// <summary>
        ///     Combined value for access all
        /// </summary>
        All = 0x001F0FFF,

        /// <summary>
        ///     Enables usage of the process handle in the TerminateProcess function to terminate the process.
        /// </summary>
        Terminate = 0x00000001,

        /// <summary>
        ///     Enables usage of the process handle in the CreateRemoteThread function to create a thread in the process.
        /// </summary>
        CreateThread = 0x00000002,

        /// <summary>
        ///     Enables usage of the process handle in the VirtualProtectEx and WriteProcessMemory functions to modify the virtual
        ///     memory of the process.
        /// </summary>
        VirtualMemoryOperation = 0x00000008,

        /// <summary>
        ///     Enables usage of the process handle in the ReadProcessMemory function to' read from the virtual memory of the
        ///     process.
        /// </summary>
        VirtualMemoryRead = 0x00000010,

        /// <summary>
        ///     Enables usage of the process handle in the WriteProcessMemory function to write to the virtual memory of the
        ///     process.
        /// </summary>
        VirtualMemoryWrite = 0x00000020,

        /// <summary>
        ///     Enables usage of the process handle as either the source or target process in the DuplicateHandle function to
        ///     duplicate a handle.
        /// </summary>
        DuplicateHandle = 0x00000040,

        /// <summary>
        ///     Enables usage of the process handle in the SetPriorityClass function to set the priority class of the process.
        /// </summary>
        SetInformation = 0x00000200,

        /// <summary>
        ///     Enables usage of the process handle in the GetExitCodeProcess and GetPriorityClass functions to read information
        ///     from the process object.
        /// </summary>
        QueryInformation = 0x00000400,

        /// <summary>
        ///     Required to retrieve certain information about a process (see GetExitCodeProcess, GetPriorityClass, IsProcessInJob, QueryFullProcessImageName). A handle that has the PROCESS_QUERY_INFORMATION access right is automatically granted PROCESS_QUERY_LIMITED_INFORMATION.
        ///     Windows Server 2003 and Windows XP:  This access right is not supported.
        /// </summary>
        QueryLimitedInformation = 0x00000400,

        /// <summary>
        ///     The right to use the object for synchronization.
        ///     This enables a thread to wait until the object is in the signaled state.
        /// </summary>
        Synchronize = 0x00100000
    }
}