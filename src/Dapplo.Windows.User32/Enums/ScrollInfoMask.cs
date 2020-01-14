// Copyright (c) Dapplo and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
using System;

namespace Dapplo.Windows.User32.Enums
{
    /// <summary>
    ///     The ScrollInfoMask enum is used for retrieving the SCROLLINFO via the GetScrollInfo
    ///     See <a href="http://pinvoke.net/default.aspx/Enums/ScrollInfoMask.html">here</a>
    /// </summary>
    [Flags]
    public enum ScrollInfoMask
    {
        /// <summary>
        ///     Copies the scroll range to the Minimum and Maximum members of the SCROLLINFO structure pointed to by lpsi.
        /// </summary>
        Range = 0x1,

        /// <summary>
        ///     Copies the scroll page to the PageSize member of the SCROLLINFO structure pointed to by lpsi.
        /// </summary>
        Page = 0x2,

        /// <summary>
        ///     Copies the scroll position to the Position member of the SCROLLINFO structure pointed to by lpsi.
        /// </summary>
        Pos = 0x4,

        /// <summary>
        ///     Unknown
        /// </summary>
        DisableNoScroll = 0x8,

        /// <summary>
        ///     Copies the current scroll box tracking position to the TrackingPosition member of the SCROLLINFO structure pointed
        ///     to by
        ///     lpsi.
        /// </summary>
        Trackpos = 0x10,

        /// <summary>
        ///     All of the above
        /// </summary>
        All = Range | Page | Pos | Trackpos
    }
}