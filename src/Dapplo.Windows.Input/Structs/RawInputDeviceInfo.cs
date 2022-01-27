// Copyright (c) Dapplo and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
using System;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;
using Dapplo.Windows.Input.Enums;

namespace Dapplo.Windows.Input.Structs;

/// <summary>
///     This structdefines the raw input data coming from any device.
///     See <a href="https://msdn.microsoft.com/en-us/library/windows/desktop/ms645581(v=vs.85).aspx">RID_DEVICE_INFO structure</a>
/// Remarks:
/// </summary>
[StructLayout(LayoutKind.Explicit)]
[SuppressMessage("ReSharper", "ConvertToAutoProperty")]
[SuppressMessage("ReSharper", "ArrangeAccessorOwnerBody")]
public struct RawInputDeviceInfo
{
    [FieldOffset(0)]
    private readonly int _cbSize;
    [FieldOffset(4)]
    private readonly RawInputDeviceTypes _dwType;
    [FieldOffset(8)]
    private readonly RawInputDeviceInfoMouse _mouse;
    [FieldOffset(8)]
    private readonly RawInputDeviceInfoKeyboard _keyboard;
    [FieldOffset(8)]
    private readonly RawInputDeviceInfoHID _hid;

    /// <summary>
    /// The type RawInput device
    /// </summary>
    public RawInputDeviceTypes Type => _dwType;

    /// <summary>
    /// Information on the mouse device
    /// </summary>
    public RawInputDeviceInfoMouse Mouse
    {
        get
        {
            if (_dwType != RawInputDeviceTypes.Mouse)
            {
                throw new NotSupportedException($"The RawInputDeviceInfo contains info on {_dwType} and not on mouse.");
            }
            return _mouse;
        }
    }
    /// <summary>
    /// Information on the keyboard device
    /// </summary>
    public RawInputDeviceInfoKeyboard Keyboard
    {
        get
        {
            if (_dwType != RawInputDeviceTypes.Keyboard)
            {
                throw new NotSupportedException($"The RawInputDeviceInfo contains info on {_dwType} and not on keyboard.");
            }
            return _keyboard;
        }
    }
    /// <summary>
    /// Information on the HID device
    /// </summary>
    // ReSharper disable once InconsistentNaming
    public RawInputDeviceInfoHID HID
    {
        get
        {
            if (_dwType != RawInputDeviceTypes.HID)
            {
                throw new NotSupportedException($"The RawInputDeviceInfo contains info on {_dwType} and not on HID.");
            }
            return _hid;
        }
    }
}