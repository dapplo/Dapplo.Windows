// Copyright (c) Dapplo and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
using System;
using Dapplo.Windows.Input.Structs;

namespace Dapplo.Windows.Input
{
    /// <summary>
    ///     Raw Input information
    /// </summary>
    public class RawInputEventArgs : EventArgs
    {
        /// <summary>
        ///     If true the application was in the foreground
        /// </summary>
        public bool IsForeground { get; set; }

        /// <summary>
        ///     The actual raw input
        /// </summary>
        public RawInput RawInput { get; set; }
    }
}