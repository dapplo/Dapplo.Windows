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

using System;

namespace Dapplo.Windows.Advapi32.Enums
{
    /// <summary>
    /// Used by RegOpenKeyEx
    /// </summary>
    [Flags]
    public enum RegistryKeySecurityAccessRights
    {
        /// <summary>
        /// Required to query the values of a registry key.
        /// </summary>
        QueryValue = 0x0001,
        /// <summary>
        /// Required to create, delete, or set a registry value.
        /// </summary>
        SetValue = 0x0002,
        /// <summary>
        /// Required to create a subkey of a registry key.
        /// </summary>
        CreateSubKey = 0x0004,
        /// <summary>
        /// Required to enumerate the subkeys of a registry key.
        /// </summary>
        EnumerateSubKeys = 0x0008,
        /// <summary>
        /// Required to request change notifications for a registry key or for subkeys of a registry key.
        /// </summary>
        Notify = 0x0010,
        /// <summary>
        /// Reserved for system use.
        /// </summary>
        CreateLink = 0x0020,
        /// <summary>
        /// Indicates that an application on 64-bit Windows should operate on the 32-bit registry view. This flag is ignored by 32-bit Windows. For more information, see Accessing an Alternate Registry View.
        /// This flag must be combined using the OR operator with the other flags in this table that either query or access registry values.
        /// Windows 2000:  This flag is not supported.
        /// </summary>
        WoW6432 = 0x0200,
        /// <summary>
        /// Indicates that an application on 64-bit Windows should operate on the 64-bit registry view. This flag is ignored by 32-bit Windows. For more information, see Accessing an Alternate Registry View.
        /// This flag must be combined using the OR operator with the other flags in this table that either query or access registry values.
        /// Windows 2000:  This flag is not supported.
        /// </summary>
        Wow6464 = 0x0100,
        /// <summary>
        /// StandardRightsRead
        /// </summary>
        StandardRightsRead = 0x20000,
        /// <summary>
        /// StandardRightsWrite
        /// </summary>
        StandardRightsWrite = 0x20000,
        /// <summary>
        /// Combines the STANDARD_RIGHTS_WRITE, KEY_SET_VALUE, and KEY_CREATE_SUB_KEY access rights.
        /// </summary>
        Write = StandardRightsRead | SetValue | CreateSubKey,
        /// <summary>
        /// Combines the STANDARD_RIGHTS_READ, KEY_QUERY_VALUE, KEY_ENUMERATE_SUB_KEYS, and KEY_NOTIFY values.
        /// </summary>
        Read = StandardRightsRead | QueryValue | EnumerateSubKeys | Notify,
        /// <summary>
        /// Equivalent to KEY_READ.
        /// </summary>
        Execute = Read,
        /// <summary>
        /// Combines the STANDARD_RIGHTS_REQUIRED, KEY_QUERY_VALUE, KEY_SET_VALUE, KEY_CREATE_SUB_KEY, KEY_ENUMERATE_SUB_KEYS, KEY_NOTIFY, and KEY_CREATE_LINK access rights.
        /// </summary>
        AllAccess = 0xF003F
    }
}
