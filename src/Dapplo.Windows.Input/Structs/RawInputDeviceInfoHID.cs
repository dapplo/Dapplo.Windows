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

using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;

#endregion

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