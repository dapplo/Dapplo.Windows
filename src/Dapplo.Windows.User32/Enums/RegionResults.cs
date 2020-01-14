// Copyright (c) Dapplo and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
namespace Dapplo.Windows.User32.Enums
{
    /// <summary>
    ///     See See
    ///     <a href="https://msdn.microsoft.com/en-us/library/windows/desktop/dd144950(v=vs.85).aspx">GetWindowRgn function</a>
    /// </summary>
    public enum RegionResults
    {
        /// <summary>
        ///     The specified window does not have a region, or an error occurred while attempting to return the region.
        /// </summary>
        Error = 0,

        /// <summary>
        ///     The region is empty.
        /// </summary>
        NullRegion = 1,

        /// <summary>
        ///     The region is a single rectangle.
        /// </summary>
        SimpleRegion = 2,

        /// <summary>
        ///     The region is more than one rectangle.
        /// </summary>
        ComplexRegion = 3
    }
}