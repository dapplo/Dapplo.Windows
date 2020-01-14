// Copyright (c) Dapplo and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
using System;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;
using Dapplo.Windows.Input.Enums;

namespace Dapplo.Windows.Input.Structs
{
    /// <summary>
    ///     This struct contains information about a raw input device.
    ///     See <a href="https://msdn.microsoft.com/en-us/library/windows/desktop/ms645565.aspx">RAWINPUTDEVICE structure</a>
    /// Remarks:
    /// If RIDEV_NOLEGACY is set for a mouse or a keyboard, the system does not generate any legacy message for that device for the application.
    /// For example, if the mouse TLC is set with RIDEV_NOLEGACY, WM_LBUTTONDOWN and related legacy mouse messages are not generated.
    /// Likewise, if the keyboard TLC is set with RIDEV_NOLEGACY, WM_KEYDOWN and related legacy keyboard messages are not generated.
    /// If RIDEV_REMOVE is set and the hWndTarget member is not set to NULL, then parameter validation will fail.
    /// </summary>
    [StructLayout(LayoutKind.Sequential)]
    [SuppressMessage("ReSharper", "ConvertToAutoProperty")]
    [SuppressMessage("ReSharper", "ArrangeAccessorOwnerBody")]
    public struct RawInputDevice
    {
        private HidUsagePages _usUsagePage;
        private ushort _usUsage;
        private RawInputDeviceFlags _dwFlags;
        private IntPtr _hWndTarget;

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
            get { return _hWndTarget; }
            set { _hWndTarget = value; }
        }

        /// <inheritdoc />
        public override string ToString()
        {
            return $"{UsagePage}/{Usage}, flags: {Flags}, target window: {TargetHwnd}";
        }
    }
}