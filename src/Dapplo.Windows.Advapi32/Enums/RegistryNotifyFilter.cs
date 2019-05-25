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
}
