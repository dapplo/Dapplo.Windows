// Copyright (c) Dapplo and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;

namespace Dapplo.Windows.Input.Structs
{
    /// <summary>
    ///     Contains the raw input from a device.
    ///     See <a href="https://msdn.microsoft.com/en-us/library/windows/desktop/ms645562.aspx">RAWINPUT structure</a>
    /// </summary>
    [StructLayout(LayoutKind.Sequential)]
    [SuppressMessage("ReSharper", "ConvertToAutoProperty")]
    [SuppressMessage("ReSharper", "ArrangeAccessorOwnerBody")]
    public struct RawInput
    {
        // 64 bit header size: 24  32 bit the header size: 16
        private RawInputHeader _header;
        // Creating the rest in a struct allows the header size to align correctly for 32/64 bit
        private RawDevice _device;

        /// <summary>
        /// The RawInput header
        /// </summary>
        public RawInputHeader Header
        {
            get { return _header; }
            set { _header = value; }
        }

        /// <summary>
        /// The device.
        /// </summary>
        public RawDevice Device
        {
            get { return _device; }
            set { _device = value; }
        }
    }
}