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
using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;
using Dapplo.Windows.Input.Enums;

#endregion

namespace Dapplo.Windows.Input.Structs
{
    /// <summary>
    ///     This struct contains information about a raw input device.
    ///     See <a href="https://msdn.microsoft.com/en-us/library/windows/desktop/ms645565.aspx">RAWINPUTDEVICE structure</a>
    /// Remarks:
    /// If RIDEV_NOLEGACY is set for a mouse or a keyboard, the system does not generate any legacy message for that device for the application.
    /// For example, if the mouse TLC is set with RIDEV_NOLEGACY, WM_LBUTTONDOWN and related legacy mouse messages are not generated.
    /// Likewise, if the keyboard TLC is set with RIDEV_NOLEGACY, WM_KEYDOWN and related legacy keyboard messages are not generated.
    /// If RIDEV_REMOVE is set and the hwndTarget member is not set to NULL, then parameter validation will fail.
    /// </summary>
    [StructLayout(LayoutKind.Sequential)]
    [SuppressMessage("ReSharper", "ConvertToAutoProperty")]
    [SuppressMessage("ReSharper", "ArrangeAccessorOwnerBody")]
    public struct RawInputDevice
    {
        private HidUsagePages _usUsagePage;
        private ushort _usUsage;
        private RawInputDeviceFlags _dwFlags;
        private IntPtr _hwndTarget;

        /// <summary>
        /// Top level collection Usage page for the raw input device.
        /// </summary>
        public HidUsagePages UsagePage
        {
            get { return _usUsagePage; }
            set { _usUsagePage = value; }
        }

        /// <summary>
        /// Top level collection Usage for the raw input device.
        /// </summary>
        public ushort Usage
        {
            get { return _usUsage; }
            set { _usUsage = value; }
        }

        /// <summary>
        /// Mode flag that specifies how to interpret the information provided by usUsagePage and usUsage.
        /// It can be zero (the default) or one of the following values.
        /// By default, the operating system sends raw input from devices with the specified top level collection (TLC) to the registered application as long as it has the window focus.
        /// </summary>
        public RawInputDeviceFlags Flags
        {
            get { return _dwFlags; }
            set { _dwFlags = value; }
        }

        /// <summary>
        /// A handle to the target window. If NULL it follows the keyboard focus.
        /// </summary>
        public IntPtr TargetHwnd
        {
            get { return _hwndTarget; }
            set { _hwndTarget = value; }
        }

        /// <inheritdoc />
        public override string ToString()
        {
            return $"{UsagePage}/{Usage}, flags: {Flags}, target window: {TargetHwnd}";
        }
    }
}