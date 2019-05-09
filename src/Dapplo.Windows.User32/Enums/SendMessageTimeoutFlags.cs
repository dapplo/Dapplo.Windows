//  Dapplo - building blocks for desktop applications
//  Copyright (C) 2017-2019  Dapplo
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

#region using

using System;

#endregion

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