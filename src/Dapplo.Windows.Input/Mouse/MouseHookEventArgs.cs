// Copyright (c) Dapplo and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
using System;
using Dapplo.Windows.Common.Structs;
using Dapplo.Windows.Messages.Enumerations;

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