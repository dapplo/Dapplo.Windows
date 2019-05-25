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

using System;
using System.ComponentModel;
using System.Runtime.InteropServices;
using Dapplo.Windows.Input.Enums;
using Dapplo.Windows.Input.Structs;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Win32;

namespace Dapplo.Windows.Input
{
    /// <summary>
    /// Functionality to use the RawInput API
    /// </summary>
    public static class RawInputApi
    {
        /// <summary>
        /// Register the specified window to receive raw input, coming from the specified device
        /// </summary>
        /// <param name="hWnd">IntPtr for the window to receive the events</param>
        /// <param name="flags">RawInputDeviceFlags</param>
        /// <param name="devices">one or more RawInputDevices</param>
        public static void RegisterRawInput(IntPtr hWnd, RawInputDeviceFlags flags, params RawInputDevices[] devices)
        {
            RegisterRawInput(devices.Select(device => CreateRawInputDevice(hWnd, device, flags)).ToArray());
        }

        /// <summary>
        /// Create RawInputDevice
        /// </summary>
        /// <param name="hWnd">IntPtr with the window handle which handles the messages</param>
        /// <param name="device">RawInputDevices</param>
        /// <param name="flags">RawInputDeviceFlags</param>
        /// <returns>RawInputDevice filleds</returns>
        public static RawInputDevice CreateRawInputDevice(IntPtr hWnd, RawInputDevices device, RawInputDeviceFlags flags = RawInputDeviceFlags.InputSink)
        {
            switch (device)
            {
                case RawInputDevices.Pointer:
                    return CreateRawInputDevice(hWnd, HidUsagesGeneric.Pointer, flags);
                case RawInputDevices.Mouse:
                    return CreateRawInputDevice(hWnd, HidUsagesGeneric.Mouse, flags);
                case RawInputDevices.Joystick:
                    return CreateRawInputDevice(hWnd, HidUsagesGeneric.Joystick, flags);
                case RawInputDevices.GamePad:
                    return CreateRawInputDevice(hWnd, HidUsagesGeneric.Gamepad, flags);
                case RawInputDevices.Keyboard:
                    return CreateRawInputDevice(hWnd, HidUsagesGeneric.Keyboard, flags);
                case RawInputDevices.Keypad:
                    return CreateRawInputDevice(hWnd, HidUsagesGeneric.Keypad, flags);
                case RawInputDevices.SystemControl:
                    return CreateRawInputDevice(hWnd, HidUsagesGeneric.SystemControl, flags);
                case RawInputDevices.ConsumerAudioControl:
                    return CreateRawInputDevice(hWnd, HidUsagesConsumer.ConsumerControl, flags);
                default:
                    throw new NotSupportedException($"Unknown RawInputDevices: {device}");
            }
        }

        /// <summary>
        /// Create RawInputDevice, to use with RegisterRawInput
        /// </summary>
        /// <param name="hWnd">IntPtr with the window handle which handles the messages</param>
        /// <param name="usage">Generic Usage for the raw input device.</param>
        /// <param name="flags">RawInputDeviceFlags</param>
        /// <returns>RawInputDevice filleds</returns>
        public static RawInputDevice CreateRawInputDevice(IntPtr hWnd, HidUsagesGeneric usage, RawInputDeviceFlags flags = RawInputDeviceFlags.InputSink)
        {
            return new RawInputDevice
            {
                TargetHwnd = hWnd,
                Flags = flags,
                UsagePage = HidUsagePages.Generic,
                Usage = (ushort)usage
            };
        }

        /// <summary>
        /// Create RawInputDevice, to use with RegisterRawInput
        /// </summary>
        /// <param name="hWnd">IntPtr with the window handle which handles the messages</param>
        /// <param name="usage">Consumer Usage for the raw input device.</param>
        /// <param name="flags">RawInputDeviceFlags</param>
        /// <returns>RawInputDevice filleds</returns>
        public static RawInputDevice CreateRawInputDevice(IntPtr hWnd, HidUsagesConsumer usage, RawInputDeviceFlags flags = RawInputDeviceFlags.InputSink)
        {
            return new RawInputDevice
            {
                TargetHwnd = hWnd,
                Flags = flags,
                UsagePage = HidUsagePages.Consumer,
                Usage = (ushort)usage
            };
        }

        /// <summary>
        /// Register to handle RawInput events
        /// Note:
        /// To receive WM_INPUT messages, an application must first register the raw input devices using RegisterRawInputDevices. By default, an application does not receive raw input.
        /// To receive WM_INPUT_DEVICE_CHANGE messages, an application must specify the RIDEV_DEVNOTIFY flag for each device class that is specified by the usUsagePage and usUsage fields of the RAWINPUTDEVICE structure . By default, an application does not receive WM_INPUT_DEVICE_CHANGE notifications for raw input device arrival and removal.
        /// If a RAWINPUTDEVICE structure has the RIDEV_REMOVE flag set and the hwndTarget parameter is not set to NULL, then parameter validation will fail.
        /// </summary>
        /// <param name="rawInputDevices">RawInputDevice(s) specifying what to register</param>
        /// <exception cref="Win32Exception">Win32Exception when the registration failed</exception>
        public static void RegisterRawInput(params RawInputDevice[] rawInputDevices)
        {
            if (!RegisterRawInputDevices(rawInputDevices, rawInputDevices.Length, Marshal.SizeOf(typeof(RawInputDevice))))
            {
                throw new Win32Exception();
            }
        }

        /// <summary>
        /// Retrieve RawInputDeviceInformation on the by the handle specified RawInput device
        /// This is used when calling GetAllDevices, but can also be called when getting a WM_INPUT_DEVICE_CHANGE message
        /// </summary>
        /// <param name="handle">IntPtr handle to the raw input device</param>
        /// <returns>RawInputDeviceInformation</returns>
        public static RawInputDeviceInformation GetDeviceInformation(IntPtr handle)
        {
            var result = new RawInputDeviceInformation
            {
                Handle = handle
            };
            uint pcbSize = 0;
            uint returnValue = GetRawInputDeviceInfo(handle, RawInputDeviceInfoCommands.DeviceName, IntPtr.Zero, ref pcbSize);
            if (returnValue == uint.MaxValue)
            {
                throw new Win32Exception("Calling GetRawInputDeviceInfo");
            }
            if (pcbSize > 0)
            {
                // Allocate the characters * 2 (Unicode!!)
                var deviceNamePtr = Marshal.AllocHGlobal((int)pcbSize * 2);
                try
                {
                    returnValue = GetRawInputDeviceInfo(handle, RawInputDeviceInfoCommands.DeviceName, deviceNamePtr, ref pcbSize);
                    if (returnValue == uint.MaxValue)
                    {
                        throw new Win32Exception("Calling GetRawInputDeviceInfo");
                    }
                    result.DeviceName = Marshal.PtrToStringUni(deviceNamePtr);

                    // Use the devicename to find information in the registry
                    if (!string.IsNullOrEmpty(result.DeviceName) && result.DeviceName.Length > 4)
                    {
                        string[] split = result.DeviceName.Substring(4).Split('#');
                        string classCode = split[0];
                        string subclassCode = split[1];
                        string protocolCode = split[2];
                        using (var registryKey = Registry.LocalMachine.OpenSubKey($@"System\CurrentControlSet\Enum\{classCode}\{subclassCode}\{protocolCode}"))
                        {
                            var deviceDescription = (string)registryKey?.GetValue("DeviceDesc");
                            var startOfDisplayName = deviceDescription?.LastIndexOf(";", StringComparison.Ordinal);
                            if (startOfDisplayName >= 0)
                            {
                                result.DisplayName = deviceDescription.Substring(startOfDisplayName.Value +1);
                            }
                        }
                    }
                }
                finally
                {
                    Marshal.FreeHGlobal(deviceNamePtr);
                }
            }

            returnValue = GetRawInputDeviceInfo(handle, RawInputDeviceInfoCommands.DeviceInfo, IntPtr.Zero, ref pcbSize);
            if (returnValue == uint.MaxValue)
            {
                throw new Win32Exception("Calling GetRawInputDeviceInfo");
            }
            var deviceInfoPtr = Marshal.AllocHGlobal((int)pcbSize);
            try
            {
                returnValue = GetRawInputDeviceInfo(handle, RawInputDeviceInfoCommands.DeviceInfo, deviceInfoPtr, ref pcbSize);
                if (returnValue == uint.MaxValue)
                {
                    throw new Win32Exception("Calling GetRawInputDeviceInfo");
                }
                result.DeviceInfo = (RawInputDeviceInfo)Marshal.PtrToStructure(deviceInfoPtr, typeof(RawInputDeviceInfo));
                return result;
            }
            finally
            {
                Marshal.FreeHGlobal(deviceInfoPtr);
            }
        }

        /// <summary>
        /// A convenient function for getting all raw input devices.
        /// This method will get all devices, including virtual devices-
        /// For remote desktop and any other device driver that's registered as such a device.
        /// </summary>
        /// <returns></returns>
        public static IEnumerable<RawInputDeviceInformation> GetAllDevices()
        {
            uint deviceCount = 0;
            uint dwSize = (uint)Marshal.SizeOf(typeof(RawInputDeviceList));

            // First call the system routine with a null pointer
            // for the array to get the size needed for the list
            uint retValue = GetRawInputDeviceList(null, ref deviceCount, dwSize);

            // If anything but zero is returned, the call failed, so return a null list
            if (0 != retValue || deviceCount <= 0)
            {
                yield break;
            }
            // Now allocate an array of the specified number of entries
            RawInputDeviceList[] deviceList = new RawInputDeviceList[deviceCount];
            // Now make the call again, using the array
            retValue = GetRawInputDeviceList(deviceList, ref deviceCount, dwSize);
            if (retValue == uint.MaxValue)
            {
                throw new Win32Exception("Exception when calling GetRawInputDeviceList");
            }
            foreach (var rawInputDeviceList in deviceList)
            {
                yield return GetDeviceInformation(rawInputDeviceList.Handle);
            }
        }

        #region DllImports
        [DllImport("user32", SetLastError = true)]
        private static extern uint GetRawInputDeviceList([In, Out] RawInputDeviceList[] rawInputDeviceList, ref uint numDevices, uint size);

        [DllImport("user32", SetLastError = true, CharSet = CharSet.Unicode)]
        private static extern uint GetRawInputDeviceInfo(IntPtr deviceHandle, RawInputDeviceInfoCommands command, IntPtr hDeviceName, ref uint dataSize);

        [DllImport("user32")]
        private static extern bool RegisterRawInputDevices([MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 0)] RawInputDevice[] pRawInputDevices, int uiNumDevices, int cbSize);

        /// <summary>
        /// GetRawInputData function
        /// Retrieves the raw input from the specified device.
        /// See <a href="https://msdn.microsoft.com/en-us/library/windows/desktop/ms645596.aspx">GetRawInputData function</a>
        /// </summary>
        /// <param name="hRawInput">IntPtr to a RawInput struct</param>
        /// <param name="uiCommand">RawInputDataCommands</param>
        /// <param name="pData"></param>
        /// <param name="pcbSize">int</param>
        /// <param name="cbSizeHeader">int</param>
        /// <returns></returns>
        [DllImport("user32")]
        public static extern int GetRawInputData(IntPtr hRawInput, RawInputDataCommands uiCommand, out RawInput pData, ref int pcbSize, int cbSizeHeader);
        #endregion
    }
}
