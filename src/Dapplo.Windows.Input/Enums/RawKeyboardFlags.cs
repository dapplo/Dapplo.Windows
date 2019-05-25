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

using System;

namespace Dapplo.Windows.Input.Enums
{
    /// <summary>
    /// Enumeration containing flags for raw keyboard input.
    /// </summary>
    [Flags]
    public enum RawKeyboardFlags : ushort
    {
        /// <summary>
        /// The key is down.
        /// </summary>
        Make = 0,
        /// <summary>
        /// The key is up.
        /// </summary>
        Break = 1,
        /// <summary>
        /// The scan code has the E0 prefix.
        /// </summary>
        E0 = 2,
        /// <summary>
        /// The scan code has the E1 prefix.
        /// </summary>
        E1 = 4,
        /// <summary>
        /// No clue
        /// </summary>
        // ReSharper disable once InconsistentNaming
        TerminalServerSetLED = 8,
        /// <summary>
        /// No clue
        /// </summary>
        TerminalServerShadow = 0x10,
        /// <summary>
        /// No clue
        /// </summary>
        TerminalServerVkPacket = 0x20
    }
}
