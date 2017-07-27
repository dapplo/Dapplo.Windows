//  Dapplo - building blocks for desktop applications
//  Copyright (C) 2016-2017 Dapplo
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
using Dapplo.Log;
using Microsoft.Win32;

namespace Dapplo.Windows.Input
{
    /// <summary>
    /// Functionality to use the RawInput API
    /// </summary>
    public static class RawInput
    {
        private static readonly LogSource Log = new LogSource();

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
                    return CreateRawInputDevice(hWnd, 0x01, 0x01, flags);
                case RawInputDevices.Mouse:
                    return CreateRawInputDevice(hWnd, 0x01, 0x02, flags);
                case RawInputDevices.Joystick:
                    return CreateRawInputDevice(hWnd, 0x01, 0x04, flags);
                case RawInputDevices.GamePad:
                    return CreateRawInputDevice(hWnd, 0x01, 0x05, flags);
                case RawInputDevices.Keyboard:
                    return CreateRawInputDevice(hWnd, 0x01, 0x06, flags);
                case RawInputDevices.Keypad:
                    return CreateRawInputDevice(hWnd, 0x01, 0x07, flags);
                case RawInputDevices.SystemControl:
                    return CreateRawInputDevice(hWnd, 0x01, 0x80, flags);
                case RawInputDevices.ConsumerAudioControl:
                    return CreateRawInputDevice(hWnd, 0xc0, 0x01, flags);
                default:
                    throw new NotSupportedException($"Unknown RawInputDevices: {device}");
            }
        }

        /// <summary>
        /// Create RawInputDevice, to use with RegisterRawInput
        /// </summary>
        /// <param name="hWnd">IntPtr with the window handle which handles the messages</param>
        /// <param name="usagePage">Top level collection Usage page for the raw input device.</param>
        /// <param name="usage">Top level collection Usage for the raw input device.</param>
        /// <param name="flags">RawInputDeviceFlags</param>
        /// <returns>RawInputDevice filleds</returns>
        public static RawInputDevice CreateRawInputDevice(IntPtr hWnd, ushort usagePage, ushort usage, RawInputDeviceFlags flags = RawInputDeviceFlags.InputSink)
        {
            return new RawInputDevice
            {
                TargetHwnd = hWnd,
                Flags = flags,
                UsagePage = usagePage,
                Usage = usage
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
        /// A convenient function for getting all raw input devices.
        /// This method will get all devices, including virtual devices-
        /// For remote desktop and any other device driver that's registered as such a device.
        /// </summary>
        /// <returns></returns>
        public static IEnumerable<RawInputDeviceInfo> GetAllDevices()
        {
            uint deviceCount = 0;
            uint dwSize = (uint)Marshal.SizeOf(typeof(RawInputDeviceList));

            // First call the system routine with a null pointer
            // for the array to get the size needed for the list
            uint retValue = GetRawInputDeviceList(null, ref deviceCount, dwSize);

            // If anything but zero is returned, the call failed, so return a null list
            if (0 != retValue)
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
                uint pcbSize = 0;
                uint returnValue = GetRawInputDeviceInfo(rawInputDeviceList.Device, RawInputDeviceInfoCommands.DeviceName, IntPtr.Zero, ref pcbSize);
                if (returnValue == uint.MaxValue)
                {
                    throw new Win32Exception("Exception when calling GetRawInputDeviceInfo");
                }
                if (pcbSize > 0)
                {
                    // Allocate the characters * 2 (Unicode!!)
                    var deviceNamePtr = Marshal.AllocHGlobal((int)pcbSize*2);
                    try
                    {
                        returnValue = GetRawInputDeviceInfo(rawInputDeviceList.Device, RawInputDeviceInfoCommands.DeviceName, deviceNamePtr, ref pcbSize);
                        if (returnValue == uint.MaxValue)
                        {
                            throw new Win32Exception("Exception when calling GetRawInputDeviceInfo");
                        }
                        string deviceName = Marshal.PtrToStringUni(deviceNamePtr);
                        if (deviceName != null)
                        {
                            Log.Debug().WriteLine("Retrieving information on {0} with name {1}", rawInputDeviceList.RawInputDeviceType, deviceName);

                            string[] split = deviceName.Substring(4).Split('#');
                            string classCode = split[0];
                            string subclassCode = split[1];
                            string protocolCode = split[2];
                            using (var registryKey = Registry.LocalMachine.OpenSubKey($@"System\CurrentControlSet\Enum\{classCode}\{subclassCode}\{protocolCode}"))
                            {
                                var deviceDescription = (string)registryKey?.GetValue("DeviceDesc");
                                if (deviceDescription != null)
                                {
                                    Log.Debug().WriteLine("{0} has Device Description {1}", rawInputDeviceList.RawInputDeviceType, deviceDescription);
                                }
                            }
                        }
                    }
                    finally
                    {
                        Marshal.FreeHGlobal(deviceNamePtr);
                    }
                }

                returnValue = GetRawInputDeviceInfo(rawInputDeviceList.Device, RawInputDeviceInfoCommands.DeviceInfo, IntPtr.Zero, ref pcbSize);
                if (returnValue == uint.MaxValue)
                {
                    throw new Win32Exception("Exception when calling GetRawInputDeviceInfo");
                }
                var deviceInfoPtr = Marshal.AllocHGlobal((int)pcbSize);
                try
                {
                    returnValue = GetRawInputDeviceInfo(rawInputDeviceList.Device, RawInputDeviceInfoCommands.DeviceInfo, deviceInfoPtr, ref pcbSize);
                    if (returnValue == uint.MaxValue)
                    {
                        throw new Win32Exception("Exception when calling GetRawInputDeviceInfo");
                    }
                    var deviceInfo = (RawInputDeviceInfo)Marshal.PtrToStructure(deviceInfoPtr, typeof(RawInputDeviceInfo));
                    yield return deviceInfo;
                }
                finally
                {
                    Marshal.FreeHGlobal(deviceInfoPtr);
                }
            }
        }

        #region DllImports
        [DllImport("user32", SetLastError = true)]
        private static extern uint GetRawInputDeviceList([In, Out] RawInputDeviceList[] rawInputDeviceList, ref uint numDevices, uint size);

        [DllImport("user32", SetLastError = true, CharSet = CharSet.Unicode)]
        private static extern uint GetRawInputDeviceInfo(IntPtr deviceHandle, RawInputDeviceInfoCommands command, IntPtr hDeviceName, ref uint dataSize);

        [DllImport("user32")]
        private static extern bool RegisterRawInputDevices([MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 0)] RawInputDevice[] pRawInputDevices, int uiNumDevices, int cbSize);

        #endregion
    }
}
