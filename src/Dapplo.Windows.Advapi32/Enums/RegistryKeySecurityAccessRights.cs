﻿// Copyright (c) Dapplo and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
using System;

namespace Dapplo.Windows.Advapi32.Enums;

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