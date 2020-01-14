// Copyright (c) Dapplo and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
using System;

namespace Dapplo.Windows.User32.Enums
{
    /// <summary>
    ///     See <a href="https://msdn.microsoft.com/en-us/library/windows/desktop/ms644952(v=vs.85).aspx">SendMessageTimeout function</a>
    /// </summary>
    [Flags]
    public enum SendMessageTimeoutFlags : uint
    {
        /// <summary>
        ///     The calling thread is not prevented from processing other requests while waiting for the function to return.
        /// </summary>
        Normal = 0x0,

        /// <summary>
        ///     Prevents the calling thread from processing any other requests until the function returns.
        /// </summary>
        Block = 0x1,

        /// <summary>
        ///     The function returns without waiting for the time-out period to elapse if the receiving thread appears to not
        ///     respond or "hangs."
        /// </summary>
        AbortIfHung = 0x2,

        /// <summary>
        ///     The function does not enforce the time-out period as long as the receiving thread is processing messages.
        /// </summary>
        NoTimeoutIfNotHung = 0x8,

        /// <summary>
        ///     The function should return 0 if the receiving window is destroyed or its owning thread dies while the message is
        ///     being processed.
        /// </summary>
        ErrorOnExit = 0x20
    }
}