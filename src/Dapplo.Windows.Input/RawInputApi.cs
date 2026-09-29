// Copyright (c) Dapplo and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
using System;
using System.ComponentModel;
using System.Runtime.InteropServices;
using Dapplo.Windows.Input.Enums;
using Dapplo.Windows.Input.Structs;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Microsoft.Win32;

namespace Dapplo.Windows.Input;

/// <summary>
/// Functionality to use the RawInput API
/// </summary>
public static class RawInputApi
{
    private const int ErrorInsufficientBuffer = 122;

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
    /// <returns>RawInputDevice filled</returns>
    public static RawInputDevice CreateRawInputDevice(IntPtr hWnd, RawInputDevices device, RawInputDeviceFlags flags = RawInputDeviceFlags.InputSink) =>
        device switch
        {
            RawInputDevices.Pointer => CreateRawInputDevice(hWnd, HidUsagesGeneric.Pointer, flags),
            RawInputDevices.Mouse => CreateRawInputDevice(hWnd, HidUsagesGeneric.Mouse, flags),
            RawInputDevices.Joystick => CreateRawInputDevice(hWnd, HidUsagesGeneric.Joystick, flags),
            RawInputDevices.GamePad => CreateRawInputDevice(hWnd, HidUsagesGeneric.Gamepad, flags),
            RawInputDevices.Keyboard => CreateRawInputDevice(hWnd, HidUsagesGeneric.Keyboard, flags),
            RawInputDevices.Keypad => CreateRawInputDevice(hWnd, HidUsagesGeneric.Keypad, flags),
            RawInputDevices.SystemControl => CreateRawInputDevice(hWnd, HidUsagesGeneric.SystemControl, flags),
            RawInputDevices.ConsumerAudioControl => CreateRawInputDevice(hWnd, HidUsagesConsumer.ConsumerControl, flags),
            _ => throw new NotSupportedException($"Unknown RawInputDevices: {device}")
        };

    /// <summary>
    /// Create RawInputDevice, to use with RegisterRawInput
    /// </summary>
    /// <param name="hWnd">IntPtr with the window handle which handles the messages</param>
    /// <param name="usage">Generic Usage for the raw input device.</param>
    /// <param name="flags">RawInputDeviceFlags</param>
    /// <returns>RawInputDevice filled</returns>
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
    /// <returns>RawInputDevice filled</returns>
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
    /// If a RAWINPUTDEVICE structure has the RIDEV_REMOVE flag set and the hWndTarget parameter is not set to NULL, then parameter validation will fail.
    /// </summary>
    /// <param name="rawInputDevices">RawInputDevice(s) specifying what to register</param>
    /// <exception cref="Win32Exception">Win32Exception when the registration failed</exception>
    public static void RegisterRawInput(params RawInputDevice[] rawInputDevices)
    {
        if (!RegisterRawInputDevices(rawInputDevices, (uint)rawInputDevices.Length, (uint)Marshal.SizeOf<RawInputDevice>()))
        {
            var errorCode = Marshal.GetLastWin32Error();
            throw new Win32Exception(errorCode, $"RegisterRawInputDevices failed for {string.Join("; ", rawInputDevices)}: {new Win32Exception(errorCode).Message}");
        }
    }

    /// <summary>
    /// Get the raw input devices which are registered for the current process, see <a href="https://learn.microsoft.com/en-us/windows/win32/api/winuser/nf-winuser-getregisteredrawinputdevices">GetRegisteredRawInputDevices</a>
    /// </summary>
    /// <returns>RawInputDevice array</returns>
    /// <exception cref="Win32Exception">Win32Exception when the call failed</exception>
    public static RawInputDevice[] GetRegisteredDevices()
    {
        var size = (uint)Marshal.SizeOf<RawInputDevice>();
        while (true)
        {
            uint numberOfDevices = 0;
            if (GetRegisteredRawInputDevices(null, ref numberOfDevices, size) == uint.MaxValue)
            {
                var errorCode = Marshal.GetLastWin32Error();
                if (errorCode != ErrorInsufficientBuffer)
                {
                    throw new Win32Exception(errorCode, "GetRegisteredRawInputDevices failed");
                }
            }
            if (numberOfDevices == 0)
            {
                return [];
            }
            var devices = new RawInputDevice[numberOfDevices];
            var result = GetRegisteredRawInputDevices(devices, ref numberOfDevices, size);
            if (result == uint.MaxValue)
            {
                var errorCode = Marshal.GetLastWin32Error();
                if (errorCode == ErrorInsufficientBuffer)
                {
                    // A registration was added in the mean time
                    continue;
                }
                throw new Win32Exception(errorCode, "GetRegisteredRawInputDevices failed");
            }
            return devices.Take((int)result).ToArray();
        }
    }

    /// <summary>
    /// Read the raw input of a WM_INPUT message, including the variable length data of a HID device.
    /// </summary>
    /// <param name="hRawInput">IntPtr, the lParam of the WM_INPUT message</param>
    /// <param name="rawInput">RawInput with the header and the mouse, keyboard or fixed HID information</param>
    /// <param name="hidData">byte array with the HID input reports, null for a mouse or keyboard</param>
    /// <returns>bool true if the raw input could be read</returns>
    public static bool TryGetRawInputData(IntPtr hRawInput, out RawInput rawInput, out byte[] hidData)
    {
        rawInput = default;
        hidData = null;
        var headerSize = (uint)Marshal.SizeOf<RawInputHeader>();
        uint size = 0;
        // Query the size first, a HID report can be larger than the RawInput struct
        if (GetRawInputData(hRawInput, RawInputDataCommands.Input, IntPtr.Zero, ref size, headerSize) != 0 || size == 0)
        {
            return false;
        }

        var data = new byte[size];
        unsafe
        {
            fixed (byte* dataPointer = data)
            {
                var result = GetRawInputData(hRawInput, RawInputDataCommands.Input, (IntPtr)dataPointer, ref size, headerSize);
                if (result == uint.MaxValue)
                {
                    return false;
                }
            }
        }
        return TryParseRawInput(data, out rawInput, out hidData);
    }

    /// <summary>
    /// Parse the RAWINPUT data, as returned by GetRawInputData with RID_INPUT, including the variable length data of a HID device.
    /// </summary>
    /// <param name="data">byte array with the RAWINPUT, as the current process (32 or 64 bit) gets it</param>
    /// <param name="rawInput">RawInput with the header and the mouse, keyboard or fixed HID information</param>
    /// <param name="hidData">byte array with the HID input reports (Count * SizeHid bytes), null for a mouse or keyboard</param>
    /// <returns>bool true if the data could be parsed</returns>
    public static bool TryParseRawInput(byte[] data, out RawInput rawInput, out byte[] hidData)
    {
        rawInput = default;
        hidData = null;
        var headerSize = Marshal.SizeOf<RawInputHeader>();
        if (data == null || data.Length < headerSize)
        {
            return false;
        }

        // The data for a keyboard is smaller than the RawInput struct (the size of the union is the size of the largest member), pad it for PtrToStructure
        var rawInputSize = Marshal.SizeOf<RawInput>();
        var buffer = data;
        if (buffer.Length < rawInputSize)
        {
            buffer = new byte[rawInputSize];
            Array.Copy(data, buffer, data.Length);
        }
        unsafe
        {
            fixed (byte* bufferPointer = buffer)
            {
                rawInput = Marshal.PtrToStructure<RawInput>((IntPtr)bufferPointer);
            }
        }

        if (rawInput.Header.Type != RawInputDeviceTypes.HID)
        {
            return true;
        }

        // RAWHID: DWORD dwSizeHid; DWORD dwCount; BYTE bRawData[dwSizeHid * dwCount], inline directly after the header
        var hidHeaderSize = Marshal.SizeOf<RawHID>();
        if (data.Length < headerSize + hidHeaderSize)
        {
            return false;
        }
        var hidDataLength = (long)rawInput.Device.HID.SizeHid * rawInput.Device.HID.Count;
        var available = data.Length - headerSize - hidHeaderSize;
        if (hidDataLength > available)
        {
            return false;
        }
        hidData = new byte[hidDataLength];
        Array.Copy(data, headerSize + hidHeaderSize, hidData, 0, (int)hidDataLength);
        return true;
    }

    /// <summary>
    /// Get the RawInputDeviceTypes which a RawInputDevices produces
    /// </summary>
    /// <param name="device">RawInputDevices</param>
    /// <returns>RawInputDeviceTypes</returns>
    internal static RawInputDeviceTypes GetDeviceType(RawInputDevices device) =>
        device switch
        {
            RawInputDevices.Pointer => RawInputDeviceTypes.Mouse,
            RawInputDevices.Mouse => RawInputDeviceTypes.Mouse,
            RawInputDevices.Keyboard => RawInputDeviceTypes.Keyboard,
            RawInputDevices.Keypad => RawInputDeviceTypes.Keyboard,
            _ => RawInputDeviceTypes.HID
        };

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
                    if (split.Length > 2) {
                        string classCode = split[0];
                        string subclassCode = split[1];
                        string protocolCode = split[2];
                        using (var registryKey = Registry.LocalMachine.OpenSubKey($@"System\CurrentControlSet\Enum\{classCode}\{subclassCode}\{protocolCode}"))
                        {
                            var deviceDescription = (string)registryKey?.GetValue("DeviceDesc");
                            var startOfDisplayName = deviceDescription?.LastIndexOf(";", StringComparison.Ordinal);
                            if (startOfDisplayName >= 0)
                            {
                                result.DisplayName = deviceDescription.Substring(startOfDisplayName.Value + 1);
                            }
                        }
                    } else {
                        result.DisplayName = result.DeviceName;
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
        var deviceInfoSize = Math.Max((int)pcbSize, Marshal.SizeOf<RawInputDeviceInfo>());
        var deviceInfoPtr = Marshal.AllocHGlobal(deviceInfoSize);
        try
        {
            // RID_DEVICE_INFO.cbSize must be set before the call
            Marshal.WriteInt32(deviceInfoPtr, 0, (int)pcbSize);
            returnValue = GetRawInputDeviceInfo(handle, RawInputDeviceInfoCommands.DeviceInfo, deviceInfoPtr, ref pcbSize);
            if (returnValue == uint.MaxValue)
            {
                throw new Win32Exception("Calling GetRawInputDeviceInfo");
            }
            result.DeviceInfo = Marshal.PtrToStructure<RawInputDeviceInfo>(deviceInfoPtr);
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
    /// Devices which are removed while they are queried are skipped.
    /// </summary>
    /// <returns>IEnumerable with RawInputDeviceInformation</returns>
    public static IEnumerable<RawInputDeviceInformation> GetAllDevices()
    {
        uint dwSize = (uint)Marshal.SizeOf<RawInputDeviceList>();
        RawInputDeviceList[] deviceList;
        uint numberOfDevices;
        while (true)
        {
            uint deviceCount = 0;
            // First call the system routine with a null pointer for the array to get the size needed for the list
            if (GetRawInputDeviceList(null, ref deviceCount, dwSize) == uint.MaxValue)
            {
                throw new Win32Exception(Marshal.GetLastWin32Error(), "Exception when calling GetRawInputDeviceList");
            }
            if (deviceCount == 0)
            {
                yield break;
            }

            deviceList = new RawInputDeviceList[deviceCount];
            numberOfDevices = GetRawInputDeviceList(deviceList, ref deviceCount, dwSize);
            if (numberOfDevices != uint.MaxValue)
            {
                break;
            }
            var errorCode = Marshal.GetLastWin32Error();
            if (errorCode != ErrorInsufficientBuffer)
            {
                throw new Win32Exception(errorCode, "Exception when calling GetRawInputDeviceList");
            }
            // A device arrived between the two calls, try again
        }

        for (var i = 0; i < numberOfDevices && i < deviceList.Length; i++)
        {
            RawInputDeviceInformation deviceInformation;
            try
            {
                deviceInformation = GetDeviceInformation(deviceList[i].Handle);
            }
            catch (Win32Exception ex)
            {
                // The device was most likely removed in the mean time
                Trace.TraceWarning("Dapplo.Windows.Input.RawInputApi: skipping raw input device {0}: {1}", deviceList[i].Handle, ex.Message);
                continue;
            }
            yield return deviceInformation;
        }
    }

    [DllImport("user32", SetLastError = true)]
    private static extern uint GetRawInputDeviceList([In, Out] RawInputDeviceList[] rawInputDeviceList, ref uint numDevices, uint size);

    /// <summary>
    /// See <a href="https://docs.microsoft.com/en-us/windows/win32/api/winuser/nf-winuser-getrawinputdeviceinfow">GetRawInputDeviceInfoW function</a>
    /// Retrieves information about the raw input device.
    /// </summary>
    /// <param name="deviceHandle">IntPtr A handle to the raw input device. This comes from the hDevice member of RAWINPUTHEADER or from GetRawInputDeviceList.</param>
    /// <param name="command">RawInputDeviceInfoCommands</param>
    /// <param name="hDeviceName">IntPtr</param>
    /// <param name="dataSize">uint</param>
    /// <returns>uint gt 0 if success</returns>
    [DllImport("user32", SetLastError = true, CharSet = CharSet.Unicode)]
    private static extern uint GetRawInputDeviceInfo(IntPtr deviceHandle, RawInputDeviceInfoCommands command, IntPtr hDeviceName, ref uint dataSize);

    /// <summary>
    /// See <a href="https://docs.microsoft.com/en-us/windows/win32/api/winuser/nf-winuser-registerrawinputdevices">RegisterRawInputDevices function</a>
    /// Registers the devices that supply the raw input data.
    /// </summary>
    /// <param name="pRawInputDevices">RawInputDevice array</param>
    /// <param name="uiNumDevices">int</param>
    /// <param name="cbSize">int</param>
    /// <returns>true if registration works</returns>
    [DllImport("user32", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool RegisterRawInputDevices([In] RawInputDevice[] pRawInputDevices, uint uiNumDevices, uint cbSize);

    /// <summary>
    /// See <a href="https://learn.microsoft.com/en-us/windows/win32/api/winuser/nf-winuser-getregisteredrawinputdevices">GetRegisteredRawInputDevices function</a>
    /// </summary>
    [DllImport("user32", SetLastError = true)]
    private static extern uint GetRegisteredRawInputDevices([In, Out] RawInputDevice[] pRawInputDevices, ref uint puiNumDevices, uint cbSize);

    /// <summary>
    /// GetRawInputData function
    /// Retrieves the raw input from the specified device.
    /// See <a href="https://msdn.microsoft.com/en-us/library/windows/desktop/ms645596.aspx">GetRawInputData function</a>
    /// </summary>
    /// <param name="hRawInput">IntPtr to a RawInput struct</param>
    /// <param name="uiCommand">RawInputDataCommands</param>
    /// <param name="pData">IntPtr to the buffer, IntPtr.Zero to query the size</param>
    /// <param name="pcbSize">uint with the size of the buffer</param>
    /// <param name="cbSizeHeader">uint with the size of the RAWINPUTHEADER</param>
    /// <returns>uint with the number of bytes copied, or uint.MaxValue on error</returns>
    [DllImport("user32", SetLastError = true)]
    private static extern uint GetRawInputData(IntPtr hRawInput, RawInputDataCommands uiCommand, IntPtr pData, ref uint pcbSize, uint cbSizeHeader);
}