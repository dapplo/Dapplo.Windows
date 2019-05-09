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
using Dapplo.Windows.Common.Structs;
using Dapplo.Windows.Messages;

#endregion

namespace Dapplo.Windows.Input.Mouse
{
    /// <summary>
    ///     Information on mouse changes
    ///     TODO: Make the information a lot clearer, than processing WindowsMessages
    /// </summary>
    public class MouseHookEventArgs : EventArgs
    {
        /// <summary>
        ///     Set this to true if the event is handled, other event-handlers in the chain will not be called
        /// </summary>
        public bool Handled { get; set; }

        /// <summary>
        ///     The x- and y-coordinates of the cursor, in per-monitor-aware screen coordinates.
        /// </summary>
        public NativePoint Point { get; set; }

        /// <summary>
        ///     The mouse message
        /// </summary>
        public WindowsMessages WindowsMessage { get; set; }
    }
}