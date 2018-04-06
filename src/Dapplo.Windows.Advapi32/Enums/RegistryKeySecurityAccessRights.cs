#region Copyright (C) 2016-2018 Dapplo
//  Dapplo - building blocks for desktop applications
//  Copyright (C) 2016-2018 Dapplo
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
    [Flags]
    public enum RegistryKeySecurityAccessRights
    {
        QueryValue = 0x0001,
        SetValue = 0x0002,
        CreateSubKey = 0x0004,
        EnumerateSubKeys = 0x0008,
        Notify = 0x0010,
        CreateLink = 0x0020,
        WoW6432 = 0x0200,
        Wow6464 = 0x0100,
        StandardRightsRead = 0x20000,
        StandardRightsWrite = 0x20000,
        Write = StandardRightsRead | SetValue | CreateSubKey,
        Read = StandardRightsRead | QueryValue | EnumerateSubKeys | Notify,
        Execute = 0x20019,
        AllAccess = 0xF003F
    }
}
