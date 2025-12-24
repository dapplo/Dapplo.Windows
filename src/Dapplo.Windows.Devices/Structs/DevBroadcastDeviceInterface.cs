// Copyright (c) Dapplo and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.ComponentModel;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;
using Dapplo.Windows.Common.Extensions;
using Dapplo.Windows.Devices.Enums;
using Microsoft.Win32;

namespace Dapplo.Windows.Devices.Structs;

/// <summary>
/// Contains information about a class of devices.
/// See <a href="https://www.pinvoke.net/default.aspx/Structures.DEV_BROADCAST_DEVICEINTERFACE">DEV_BROADCAST_DEVICEINTERFACE</a>
/// And <a href="https://docs.microsoft.com/en-us/windows/win32/api/dbt/ns-dbt-dev_broadcast_deviceinterface_w">DEV_BROADCAST_DEVICEINTERFACE_W structure</a>
/// </summary>
[StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
public struct DevBroadcastDeviceInterface
{
    private static readonly Regex VendorRegex = new Regex(@"V(ID|EN)_([0-9A-F]{4})", RegexOptions.Compiled | RegexOptions.IgnoreCase);
    private static readonly Regex DeviceIdRegex = new Regex(@"DEV_([0-9A-F]{4})", RegexOptions.Compiled | RegexOptions.IgnoreCase);
    private static readonly Regex ProductRegex = new Regex(@"PID_([0-9A-F]{4})", RegexOptions.Compiled | RegexOptions.IgnoreCase);
    private static readonly Regex DeviceTypeRegex = new Regex(@"\\\?\\([A-Z]+)#", RegexOptions.Compiled | RegexOptions.IgnoreCase);

    private int _size;
    // The device type, which determines the event-specific information that follows the first three members. 
    private DeviceBroadcastDeviceType _deviceType;
    private readonly int _reserved;
    private Guid _classGuid;
    [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 255)]
    private string _name;

    /// <summary>
    /// The GUID for the interface device class.
    /// This is the Device Interface Class GUID that comes from the device notification message.
    /// For the Device Setup Class GUID shown in Device Manager, use <see cref="DeviceSetupClassGuid"/> instead.
    /// </summary>
    public Guid DeviceClassGuid
    {
        get => _classGuid;
        set => _classGuid = value;
    }

    /// <summary>
    /// The name of the device.
    /// </summary>
    public string Name => _name;

    /// <summary>
    /// The name of the device.
    /// </summary>
    public string DisplayName {
        get
        {
            var displayName = _name.Substring(3);
            displayName = displayName.Substring(0,displayName.LastIndexOf("#{", StringComparison.Ordinal));
            displayName = displayName.Replace('#', '\\');

            return displayName;
        }
    }

    /// <summary>
    /// Factory for an empty, but initialized, DevBroadcastDeviceInterface
    /// </summary>
    /// <returns>DevBroadcastDeviceInterface</returns>
    public static DevBroadcastDeviceInterface Create()
    {
        return new DevBroadcastDeviceInterface
        {
            _deviceType = DeviceBroadcastDeviceType.DeviceInterface,
            _size = Marshal.SizeOf(typeof(DevBroadcastDeviceInterface))
        };
    }

    /// <summary>
    /// Helper method to parse the device name and open the corresponding registry key.
    /// Returns null if the device name format is invalid or the registry key cannot be opened.
    /// </summary>
    /// <param name="name">The device name to parse</param>
    /// <returns>RegistryKey or null. The caller is responsible for disposing the returned RegistryKey.</returns>
    private static RegistryKey TryOpenDeviceRegistryKey(string name)
    {
        if (string.IsNullOrEmpty(name))
        {
            return null;
        }

        string[] parts = name.Split('#');
        if (parts.Length < 3)
        {
            return null;
        }

        var startIndex = parts[0].IndexOf(@"?\", StringComparison.Ordinal);
        if (startIndex < 0 || startIndex + 2 >= parts[0].Length)
        {
            return null;
        }

        string devType = parts[0].Substring(startIndex + 2);
        string deviceInstanceId = parts[1];
        string deviceUniqueId = parts[2];
        
        // Validate that the parts don't contain invalid characters that could cause issues
        if (string.IsNullOrWhiteSpace(devType) || string.IsNullOrWhiteSpace(deviceInstanceId) || string.IsNullOrWhiteSpace(deviceUniqueId))
        {
            return null;
        }
        
        string regPath = $@"SYSTEM\CurrentControlSet\Enum\{devType}\{deviceInstanceId}\{deviceUniqueId}";
        
        return Registry.LocalMachine.OpenSubKey(regPath);
    }

    /// <summary>
    /// Returns a more friendly name for the device
    /// </summary>
    public string FriendlyDeviceName
    {
        get
        {
            using (var key = TryOpenDeviceRegistryKey(_name))
            {
                if (key == null)
                {
                    return _name;
                }

                if (key.GetValue("FriendlyName") is string result)
                {
                    return result;
                }
                result = key.GetValue("DeviceDesc") as string;
                if (result != null)
                {
                    // Example: @msclmd.inf,%scmspivcarddevicename%;Identifizierungsgerät (NIST SP 800-73 [PIV])
                    var semiColonIndex = result.LastIndexOf(';');
                    if (semiColonIndex >= 0)
                    {
                        return result.Substring(semiColonIndex + 1);
                    }
                    return result;
                }
            }
            return _name;

        }
    }

    /// <summary>
    /// Returns the Device Setup Class GUID from the Windows registry.
    /// This is the Class GUID shown in Device Manager (e.g., {4d36e968-e325-11ce-bfc1-08002be10318} for Display adapters).
    /// This is different from <see cref="DeviceClassGuid"/> which is the Device Interface Class GUID from the notification message.
    /// Returns null if the registry key cannot be accessed or the ClassGUID value is not found.
    /// </summary>
    public Guid? DeviceSetupClassGuid
    {
        get
        {
            using (var key = TryOpenDeviceRegistryKey(_name))
            {
                if (key == null)
                {
                    return null;
                }

                if (key.GetValue("ClassGUID") is string classGuidString)
                {
                    if (Guid.TryParse(classGuidString, out var classGuid))
                    {
                        return classGuid;
                    }
                }
            }
            return null;
        }
    }

    /// <summary>
    /// Returns the device type, e.g. USB or HID
    /// </summary>
    public string DeviceType
    {
        get
        {
            var match = DeviceTypeRegex.Match(_name);
            return match.Groups.Count != 2 ? null : match.Groups[1].Value;
        }
    }

    /// <summary>
    /// Is this a USB device?
    /// </summary>
    public bool IsUsb => "USB".Equals(DeviceType, StringComparison.OrdinalIgnoreCase);

    /// <summary>
    /// Is this a PCI device?
    /// </summary>
    public bool IsPci => "PCI".Equals(DeviceType, StringComparison.OrdinalIgnoreCase);

    /// <summary>
    /// Returns the Device ID of the device
    /// </summary>
    public string DeviceId
    {
        get
        {
            var match = DeviceIdRegex.Match(_name);
            return match.Groups.Count != 2 ? null : match.Groups[1].Value;
        }
    }

    /// <summary>
    /// Returns the Vendor ID of the device
    /// </summary>
    public string VendorId
    {
        get
        {
            var match = VendorRegex.Match(_name);
            return match.Groups.Count != 3 ? null : match.Groups[2].Value;
        }
    }

    /// <summary>
    /// Returns the Vendor ID of the device
    /// </summary>
    public string ProductId
    {
        get
        {
            var match = ProductRegex.Match(_name);
            return match.Groups.Count != 2 ? null : match.Groups[1].Value;
        }
    }

    /// <summary>
    /// Try to generate a Uri which might include more information about the device
    /// </summary>
    public Uri UsbDeviceInfoUri => new Uri($"https://www.the-sz.com/products/usbid/index.php?v=0x{VendorId}&p={ProductId}");

    /// <summary>
    /// Use an enum for handling the device class, instead of guid
    /// </summary>
    public DeviceInterfaceClass DeviceClass
    {
        get
        {
            var guidToFind = _classGuid.ToString();
            foreach (var deviceClass in Enum.GetValues(typeof(DeviceInterfaceClass)).Cast<DeviceInterfaceClass>())
            {
                var descriptionAttribute = deviceClass.GetAttributeOfType<DescriptionAttribute>();
                if (string.IsNullOrEmpty(descriptionAttribute?.Description) || !string.Equals(guidToFind, descriptionAttribute.Description, StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                return deviceClass;
            }

            return DeviceInterfaceClass.Unknown;
        }

        set
        {
            var descriptionAttribute = value.GetAttributeOfType<DescriptionAttribute>();
            if (!string.IsNullOrEmpty(descriptionAttribute?.Description))
            {
                _classGuid = Guid.Parse(descriptionAttribute.Description);
            }
        }

    }

    /// <summary>
    /// Used for testing
    /// </summary>
    /// <param name="deviceName">string</param>
    /// <param name="deviceClass">DeviceInterfaceClass</param>
    /// <returns>DevBroadcastDeviceInterface</returns>
    public static DevBroadcastDeviceInterface Test(string deviceName, DeviceInterfaceClass deviceClass = DeviceInterfaceClass.Unknown)
    {
        Guid deviceClassGuid = default;
        var descriptionAttribute = deviceClass.GetAttributeOfType<DescriptionAttribute>();
        if (!string.IsNullOrEmpty(descriptionAttribute?.Description))
        {
            deviceClassGuid = Guid.Parse(descriptionAttribute.Description);
        }

        return new DevBroadcastDeviceInterface
        {
            _name = deviceName,
            _deviceType = DeviceBroadcastDeviceType.DeviceInterface,
            _classGuid = deviceClassGuid
        };
    }
}