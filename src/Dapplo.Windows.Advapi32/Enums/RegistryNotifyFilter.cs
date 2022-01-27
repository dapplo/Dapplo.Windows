// Copyright (c) Dapplo and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;

namespace Dapplo.Windows.Advapi32.Enums;

/// <summary>
/// A value that indicates the changes that should be reported
/// </summary>
[Flags]
public enum RegistryNotifyFilter
{
    /// <summary>Notify the caller if a subkey is added or deleted.</summary>
    ChangeName = 1,
    /// <summary>Notify the caller of changes to the attributes of the key,
    /// such as the security descriptor information.</summary>
    ChangeAttributes = 2,
    /// <summary>Notify the caller of changes to a value of the key. This can
    /// include adding or deleting a value, or changing an existing value.</summary>
    ChangeLastSet = 4,
    /// <summary>Notify the caller of changes to the security descriptor
    /// of the key.</summary>
    ChangeSecurity = 8,
    /// <summary>
    /// Indicates that the lifetime of the registration must not be tied to the lifetime of the thread issuing the RegNotifyChangeKeyValue call.
    /// Note  This flag value is only supported in Windows 8 and later.
    /// </summary>
    ThreadAgnostic = 0x10000000
}