// Copyright (c) Dapplo and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
using System;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;
using Dapplo.Windows.Input.Enums;

namespace Dapplo.Windows.Input.Structs
{
    /// <summary>
    ///     Contains the header information that is part of the raw input data.
    ///     See <a href="https://msdn.microsoft.com/en-us/library/windows/desktop/ms645571.aspx">RAWINPUTHEADER structure</a>
    /// </summary>
    [StructLayout(LayoutKind.Sequential)]
    [SuppressMessage("ReSharper", "ConvertToAutoProperty")]
    [SuppressMessage("ReSharper", "ArrangeAccessorOwnerBody")]
    public struct RawInputHeader
    {
        private RawInputDeviceTypes _dwType;
        private uint _dwSize;                     // Size in bytes of the entire input packet of data. This includes RAWINPUT plus possible extra input reports in the RAWHID variable length array. 
        private IntPtr _hDevice;                  // A handle to the device generating the raw input data. 
        private IntPtr _wParam;                   // RIM_INPUT 0 if input occurred while application was in the foreground else RIM_INPUTSINK 1 if it was not.


        /// <summary>
        /// Type of raw input (RIM_TYPEHID 2, RIM_TYPEKEYBOARD 1, RIM_TYPEMOUSE 0)
        /// </summary>
        public RawInputDeviceTypes Type
        {
            get { return _dwType; }
            set { _dwType = value; }
        }

        /// <summary>
        /// A handle to the Device.
        /// </summary>
        public IntPtr DeviceHandle
        {
            get { return _hDevice; }
            set { _hDevice = value; }
        }
    }
}