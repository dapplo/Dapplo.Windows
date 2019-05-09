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

namespace Dapplo.Windows.Input.Enums
{
    /// <summary>
    ///     The event-injected flags. An application can use the following values to test the flags.
    ///     Testing LLMHF_INJECTED (bit 0) will tell you whether the event was injected.
    ///     If it was, then testing LLMHF_LOWER_IL_INJECTED (bit 1) will tell you whether or not
    ///     the event was injected from a process running at lower integrity level.
    /// </summary>
    [Flags]
    public enum ExtendedMouseFlags : uint
    {
        /// <summary>
        ///     Test the event-injected (from any process) flag.
        /// </summary>
        Injected = 0x01,

        /// <summary>
        ///     Test the event-injected (from a process running at lower integrity level) flag.
        /// </summary>
        LowerIntegretyInjected = 0x02
    }
}