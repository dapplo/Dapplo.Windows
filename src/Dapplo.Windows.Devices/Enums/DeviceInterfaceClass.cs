// Copyright (c) Dapplo and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.ComponentModel;

namespace Dapplo.Windows.Devices.Enums;

/// <summary>
/// This enum contains the device interface classes (GUID)
/// See <a href="https://docs.microsoft.com/en-us/windows-hardware/drivers/install/overview-of-device-interface-classes">Overview of Device Interface Classes</a>
/// The classes themselves can be found under <a href="https://docs.microsoft.com/en-us/previous-versions/ff553412(v=vs.85)">System-Defined Device Interface Classes</a>
/// </summary>
public enum DeviceInterfaceClass
{
    /// <summary>
    /// Don't know what class / GUID
    /// </summary>
    Unknown,

    /// <summary>The BUS1394_CLASS_GUID device interface class is defined for 1394 bus devices.</summary>
    [Description("6BDD1FC1-810F-11d0-BEC7-08002BE2092F")]
    Bus1394,

    /// <summary>
    /// The GUID_61883_CLASS device interface class is defined for devices in the 61883 device setup class.
    /// The 61883 device interface class includes IEEE 1394 devices that support the IEC-61883 protocol. 
    /// </summary>
    [Description("7EBEFBC0-3200-11d2-B4C2-00A0C9697D07")]
    Iec61883,

    /// <summary>
    ///     The GUID_DEVICE_APPLICATIONLAUNCH_BUTTON device interface class is defined for Advanced Configuration and
    ///     Power Interface (ACPI) application start buttons.
    /// </summary>
    [Description("629758EE-986E-4D9E-8E47-DE27F8AB054D")]
    ApplicationLaunchButton,

    /// <summary>The GUID_DEVICE_BATTERY device interface class is defined for battery devices.</summary>
    [Description("72631E54-78A4-11D0-BCF7-00AA00B7B32A")]
    Battery,

    /// <summary>
    ///     The GUID_DEVICE_LID device interface class is defined for Advanced Configuration and Power Interface (ACPI)
    ///     lid devices.
    /// </summary>
    [Description("4AFA3D52-74A7-11d0-be5e-00A0C9062857")]
    Lid,

    /// <summary>
    ///     The GUID_DEVICE_MEMORY device interface class is defined for Advanced Configuration and Power Interface (ACPI)
    ///     memory devices.
    /// </summary>
    [Description("3FD0F03D-92E0-45FB-B75C-5ED8FFB01021")]
    Memory,

    /// <summary>
    ///     The GUID_DEVICE_MESSAGE_INDICATOR device interface class is defined for Advanced Configuration and Power
    ///     Interface (ACPI) message indicator devices.
    /// </summary>
    [Description("CD48A365-FA94-4CE2-A232-A1B764E5D8B4")]
    MessageIndicator,

    /// <summary>
    ///     The GUID_DEVICE_PROCESSOR device interface class is defined for Advanced Configuration and Power Interface
    ///     (ACPI) processor devices.
    /// </summary>
    [Description("97FADB10-4E33-40AE-359C-8BEF029DBDD0")]
    Processor,

    /// <summary>
    ///     The GUID_DEVICE_SYS_BUTTON device interface classis defined for Advanced Configuration and Power Interface
    ///     (ACPI) system power button devices.
    /// </summary>
    [Description("4AFA3D53-74A7-11d0-be5e-00A0C9062857")]
    SysButton,

    /// <summary>
    ///     The GUID_DEVICE_THERMAL_ZONE device interface class is defined for Advanced Configuration and Power Interface
    ///     (ACPI) thermal zone devices.
    /// </summary>
    [Description("4AFA3D51-74A7-11d0-be5e-00A0C9062857")]
    ThermalZone,

    /// <summary>The GUID_BTHPORT_DEVICE_INTERFACE device interface class is defined for Bluetooth radios.</summary>
    [Description("0850302A-B344-4fda-9BE9-90576B8D46F0")]
    BluetoothRadio,

    /// <summary>The GUID_BTH_DEVICE_INTERFACE device interface class is defined for Bluetooth devices.</summary>
    [Description("00F40965-E89D-4487-9890-87C3ABB211F4")]
    BluetoothDevice,

    /// <summary>The GUID_BLUETOOTHLE_DEVICE_INTERFACE device interface class is defined for Bluetooth LE devices.</summary>
    [Description("00F40965-E89D-4487-9890-87C3ABB211F4")]
    BluetoothLeDevice,

    /// <summary>
    ///     The GUID_DEVINTERFACE_BRIGHTNESS device interface class is defined for display adapter drivers that operate in
    ///     the context of the Windows Vista Display Driver Model and support brightness control of monitor child devices.
    /// </summary>
    [Description("FDE5BBA4-B3F9-46FB-BDAA-0728CE3100B4")]
    Brightness,

    /// <summary>
    ///     The GUID_DEVINTERFACE_DISPLAY_ADAPTER device interface class is defined for display views that are supported
    ///     by display adapters.
    /// </summary>
    [Description("5B45201D-F2F2-4F3B-85BB-30FF1F953599")]
    DisplayAdapter,

    /// <summary>
    ///     The GUID_DEVINTERFACE_I2C device interface class is defined for display adapter drivers that operate in the
    ///     context of the Windows Vista Display Driver Model and perform I2C transactions with monitor child devices.
    /// </summary>
    [Description("2564AA4F-DDDB-4495-B497-6AD4A84163D7")]
    I2C,

    /// <summary>
    ///     The GUID_DEVINTERFACE_IMAGE device interface class is defined for WIA devices and Still Image (STI) devices,
    ///     including digital cameras and scanners.
    /// </summary>
    [Description("6BDD1FC6-810F-11D0-BEC7-08002BE2092F")]
    StillImage,

    /// <summary>The GUID_DEVINTERFACE_MONITOR device interface class is defined for monitor devices.</summary>
    [Description("E6F07B5F-EE97-4a90-B076-33F57BF4EAA7")]
    Monitor,

    /// <summary>
    ///     The GUID_DEVINTERFACE_OPM device interface class is defined for display adapter drivers that operate in the
    ///     context of the Windows Vista Display Driver Model and support output protection management (OPM) for monitor child
    ///     devices.
    /// </summary>
    [Description("BF4672DE-6B4E-4BE4-A325-68A91EA49C09")]
    OutputProtectionManagement,

    /// <summary>
    ///     The GUID_DEVINTERFACE_VIDEO_OUTPUT_ARRIVAL device interface class is defined for child devices of display
    ///     devices.
    /// </summary>
    [Description("1AD9E4F0-F88D-4360-BAB9-4C2D55E564CD")]
    VideoOutputArrival,

    /// <summary>The GUID_DISPLAY_DEVICE_ARRIVAL device interface class is defined for display adapters.</summary>
    [Description("1CA05180-A699-450A-9A0C-DE4FBE3DDD89")]
    DisplayDeviceArrival,

    /// <summary>The GUID_DEVINTERFACE_HID device interface class is defined for HID collections.</summary>
    [Description("4D1E55B2-F16F-11CF-88CB-001111000030")]
    Hid,

    /// <summary>The GUID_DEVINTERFACE_KEYBOARD device interface class is defined for keyboard devices.</summary>
    [Description("4D1E55B2-F16F-11CF-88CB-001111000030")]
    Keyboard,

    /// <summary>The GUID_DEVINTERFACE_MOUSE device interface class is defined for mouse devices.</summary>
    [Description("378DE44C-56EF-11D1-BC8C-00A0C91405DD")]
    Mouse,

    /// <summary>The GUID_DEVINTERFACE_MODEM device interface class is defined for modem devices.</summary>
    [Description("2C7089AA-2E0E-11D1-B114-00C04FC2AAE4")]
    Modem,


    /// <summary>The GUID_DEVINTERFACE_NET device interface class is defined for network devices.</summary>
    [Description("CAC88484-7515-4C03-82E6-71A87ABAC361")]
    Network,

    /// <summary>The GUID_DEVINTERFACE_COMPORT device interface class is defined for COM ports.</summary>
    [Description("86E0D1E0-8089-11D0-9CE4-08003E301F73")]
    ComPort,

    /// <summary>
    ///     The GUID_DEVINTERFACE_PARALLEL device interface class is defined for parallel ports that support an IEEE
    ///     1284-compatible hardware interface.
    /// </summary>
    [Description("97F76EF0-F883-11D0-AF1F-0000F800845C")]
    Parallel,

    /// <summary>
    ///     The GUID_DEVINTERFACE_PARCLASS device interface class is defined for devices that are attached to a parallel
    ///     port.
    /// </summary>
    [Description("811FC6A5-F728-11D0-A537-0000F8753ED1")]
    ParallelClass,

    /// <summary>
    ///     The GUID_DEVINTERFACE_SERENUM_BUS_ENUMERATOR device interface class is defined for Plug and Play (PnP) serial
    ///     ports.
    /// </summary>
    [Description("4D36E978-E325-11CE-BFC1-08002BE10318")]
    SerialEnumBusEnumerator,

    /// <summary>The GUID_DEVINTERFACE_CDCHANGER device interface class is defined for CD-ROM changer devices.</summary>
    [Description("53F56312-B6BF-11D0-94F2-00A0C91EFB8B")]
    CdromChanger,

    /// <summary>The GUID_DEVINTERFACE_CDROM device interface class is defined for CD-ROM storage devices.</summary>
    [Description("53F56308-B6BF-11D0-94F2-00A0C91EFB8B")]
    Cdrom,

    /// <summary>The GUID_DEVINTERFACE_DISK device interface class is defined for hard disk storage devices.</summary>
    [Description("53F56307-B6BF-11D0-94F2-00A0C91EFB8B")]
    Disk,

    /// <summary>The GUID_DEVINTERFACE_FLOPPY device interface class is defined for floppy disk storage devices.</summary>
    [Description("53F56311-B6BF-11D0-94F2-00A0C91EFB8B")]
    Floppy,

    /// <summary>The GUID_DEVINTERFACE_MEDIUMCHANGER device interface class is defined for medium changer devices.</summary>
    [Description("53F56310-B6BF-11D0-94F2-00A0C91EFB8B")]
    MediumChanger,

    /// <summary>The GUID_DEVINTERFACE_PARTITION device interface class is defined for partition devices.</summary>
    [Description("53F5630A-B6BF-11D0-94F2-00A0C91EFB8B")]
    Partition,

    /// <summary>The GUID_DEVINTERFACE_STORAGEPORT device interface class is defined for storage port devices.</summary>
    [Description("2ACCFE60-C130-11D2-B082-00A0C91EFB8B")]
    StoragePort,

    /// <summary>The GUID_DEVINTERFACE_TAPE device interface class is defined for tape storage devices.</summary>
    [Description("53F5630B-B6BF-11D0-94F2-00A0C91EFB8B")]
    Tape,

    /// <summary>The GUID_DEVINTERFACE_VOLUME device interface class is defined for volume devices.</summary>
    [Description("53F5630D-B6BF-11D0-94F2-00A0C91EFB8B")]
    Volume,

    /// <summary>The GUID_DEVINTERFACE_WRITEONCEDISK device interface class is defined for write-once disk devices.</summary>
    [Description("53F5630C-B6BF-11D0-94F2-00A0C91EFB8B")]
    WriteOnceDisk,

    /// <summary>
    ///     The GUID_DEVINTERFACE_USB_DEVICE device interface class is defined for USB devices that are attached to a USB
    ///     hub.
    /// </summary>
    [Description("A5DCBF10-6530-11D2-901F-00C04FB951ED")]
    UsbDevice,

    /// <summary>The GUID_DEVINTERFACE_USB_HOST_CONTROLLER device interface class is defined for USB host controller devices.</summary>
    [Description("3ABF6F2D-71C4-462A-8A92-1E6861E6AF27")]
    UsbHostController,

    /// <summary>The GUID_DEVINTERFACE_USB_HUB device interface class is defined for USB hub devices.</summary>
    [Description("F18A0E88-C30C-11D0-8815-00A0C906BED8")]
    UsbHub,

    /// <summary>Smart card</summary>
    [Description("084F9363-E31A-4BDE-9CA5-6FA0D86E009F")]
    SmartCard,

    /// <summary>
    /// SmartCard reader
    /// </summary>
    [Description("50dd5230-ba8a-11d1-bf5d-0000f805f530")]
    SmartCardReader,

    /// <summary>
    /// SmartCard filter
    /// </summary>
    [Description("D86354CC-A2AC-4223-95B9-2E48CE154434")]
    SmartCardFilter,

    /// <summary>The GUID_DEVINTERFACE_WPD device interface class is defined for Windows Portable Devices (WPD).</summary>
    [Description("6AC27878-A6FA-4155-BA85-F98F491D4F33")]
    WindowsPortableDevices,

    /// <summary>The GUID_DEVINTERFACE_WPD_PRIVATE device interface class is defined for specialized Windows Portable Devices (WPD).</summary>
    [Description("BA0C718F-4DED-49B7-BDD3-FABE28661211")]
    PrivateWindowsPortableDevices,

    /// <summary>The GUID_VIRTUAL_AVC_CLASS device interface class is defined for virtual audio video control (AV/C) devices that are supported by the AVStream architecture.</summary>
    [Description("616EF4D0-23CE-446D-A568-C31EB01913D0")]
    VirtualAudioVideoControl,

    /// <summary>The GUID_DEVINTERFACE_SIDESHOW device interface class is defined for Windows SideShow devices.</summary>
    [Description("152E5811-FEB9-4B00-90F4-D32947AE1681")]
    SideShow
}