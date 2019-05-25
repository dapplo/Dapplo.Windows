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

using System.Runtime.InteropServices;

namespace Dapplo.Windows.Advapi32.Structs
{
    /// <summary>
    /// The TOKEN_GROUPS structure contains information about the group security identifiers (SIDs) in an access token.
    /// </summary>
    [StructLayout(LayoutKind.Sequential)]
    public struct TokenGroups
    {
        /// <summary>
        /// Specifies the number of groups in the access token.
        /// </summary>
        public readonly int GroupCount;
        /// <summary>
        /// Specifies an array of SID_AND_ATTRIBUTES structures that contain a set of SIDs and corresponding attributes.
        /// The Attributes members of the SID_AND_ATTRIBUTES structures can have the following values.
        /// </summary>
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 1)]
        public SidAndAttributes[] Groups;
    }
}
