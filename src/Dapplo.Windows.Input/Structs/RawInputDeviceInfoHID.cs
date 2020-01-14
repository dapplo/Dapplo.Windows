// Copyright (c) Dapplo and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;

namespace Dapplo.Windows.Input.Structs
{
    /// <summary>
    ///     This struct defines the raw input data coming from the specified keyboard.
    ///     See <a href="https://msdn.microsoft.com/en-us/library/windows/desktop/ms645587.aspx">RID_DEVICE_INFO_KEYBOARD structure</a>
    /// Remarks:
    /// For the keyboard, the Usage Page is 1 and the Usage is 6.
    /// </summary>
    [StructLayout(LayoutKind.Sequential)]
    [SuppressMessage("ReSharper", "ConvertToAutoProperty")]
    [SuppressMessage("ReSharper", "ArrangeAccessorOwnerBody")]
    // ReSharper disable once InconsistentNaming
    public struct RawInputDeviceInfoHID
    {
        private readonly int _dwVendorId;
        private readonly int _dwProductId;
        private readonly int _dwVersionNumber;
        private readonly ushort _usUsagePage;
        private readonly ushort _usUsage;

        /// <summary>
        /// The vendor identifier for the HID.
        /// </summary>
        public int VendorId => _dwVendorId;

        /// <summary>
        /// The product identifier for the HID.
        /// </summary>
        public int ProductId => _dwProductId;

        /// <summary>
        /// The version number for the HID.
        /// </summary>
        public int VersionNumber => _dwVersionNumber;

        /// <summary>
        /// The top-level collection Usage Page for the device.
        /// </summary>
        public ushort UsagePage => _usUsagePage;

        /// <summary>
        /// The top-level collection Usage for the device.
        /// </summary>
        public ushort Usage => _usUsage;
    }
}