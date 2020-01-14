// Copyright (c) Dapplo and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
namespace Dapplo.Windows.User32.Enums
{
    /// <summary>
    ///     See <a href="https://msdn.microsoft.com/en-us/library/windows/desktop/bb787535(v=vs.85).aspx">here</a>
    /// </summary>
    public enum ScrollBarStateIndexes : uint
    {
        /// <summary>
        ///     The scroll bar itself.
        /// </summary>
        Scrollbar,

        /// <summary>
        ///     The top or right arrow button.
        /// </summary>
        TopOrRightArrow,

        /// <summary>
        ///     The page up or page right region.
        /// </summary>
        PageUpOrRightRegion,

        /// <summary>
        ///     The scroll box (thumb).
        /// </summary>
        ScrollBox,

        /// <summary>
        ///     The page down or page left region.
        /// </summary>
        PageDownOrLeftRegion,

        /// <summary>
        ///     The bottom or left arrow button.
        /// </summary>
        ButtonOrLeftArrow
    }
}