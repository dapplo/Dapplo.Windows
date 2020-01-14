// Copyright (c) Dapplo and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

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
