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
using System.Runtime.InteropServices;

namespace Dapplo.Windows.Advapi32.Structs
{
    /// <summary>
    /// The SID_AND_ATTRIBUTES structure represents a security identifier (SID) and its attributes.
    /// SIDs are used to uniquely identify users or groups.
    /// </summary>
    [StructLayout(LayoutKind.Sequential)]
    public struct SidAndAttributes
    {
        /// <summary>
        /// A pointer to a SID structure.
        /// </summary>
        public IntPtr Sid;
        /// <summary>
        /// Specifies attributes of the SID. This value contains up to 32 one-bit flags. Its meaning depends on the definition and use of the SID.
        /// </summary>
        public readonly uint Attributes;
    }
}
