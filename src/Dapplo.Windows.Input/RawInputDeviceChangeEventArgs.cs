// Copyright (c) Dapplo and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
using System;

namespace Dapplo.Windows.Input
{
    /// <summary>
    ///     Information on RawInput device changes
    /// </summary>
    public class RawInputDeviceChangeEventArgs : EventArgs
    {
        /// <summary>
        ///     If true it was added, if false it was removed
        /// </summary>
        public bool Added { get; set; }

        /// <summary>
        ///     The device which was added or removed
        /// </summary>
        public RawInputDeviceInformation DeviceInformation { get; set; }
    }
}