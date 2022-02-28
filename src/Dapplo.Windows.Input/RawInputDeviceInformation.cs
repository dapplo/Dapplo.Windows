// Copyright (c) Dapplo and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using Dapplo.Windows.Input.Structs;

namespace Dapplo.Windows.Input
{
    /// <summary>
    /// Describes a RawInput device
    /// </summary>
    public class RawInputDeviceInformation
    {
        /// <summary>
        /// The handle to the raw input device
        /// </summary>
        public IntPtr DeviceHandle { get; internal set; }

        /// <summary>
        /// The cryptic device name
        /// </summary>
        public string DeviceName { get; internal set; }

        /// <summary>
        /// A name which can be used to display to a user
        /// </summary>
        public string DisplayName { get; internal set; }

        /// <summary>
        /// The actual device information
        /// </summary>
        public RawInputDeviceInfo DeviceInfo { get; internal set; }
    }
}
