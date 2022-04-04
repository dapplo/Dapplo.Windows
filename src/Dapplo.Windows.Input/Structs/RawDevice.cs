// Copyright (c) Dapplo and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;

namespace Dapplo.Windows.Input.Structs;

/// <summary>
///     This is used to similate a union in the RawInput struct, were cannot use Explicit due to 32/64 bit
/// </summary>
[StructLayout(LayoutKind.Explicit)]
[SuppressMessage("ReSharper", "ConvertToAutoProperty")]
[SuppressMessage("ReSharper", "ArrangeAccessorOwnerBody")]
public struct RawDevice
{
    [FieldOffset(0)] private readonly RawMouse _mouse;
    [FieldOffset(0)] private readonly RawKeyboard _keyboard;
    [FieldOffset(0)] private readonly RawHID _hid;

    /// <summary>
    /// Information on the mouse
    /// </summary>
    public RawMouse Mouse
    {
        get { return _mouse; }
    }

    /// <summary>
    /// Information on the keyboard
    /// </summary>
    public RawKeyboard Keyboard
    {
        get { return _keyboard; }
    }

    /// <summary>
    /// Information on the HID device
    /// </summary>
    // ReSharper disable once InconsistentNaming
    public RawHID HID
    {
        get { return _hid; }
    }
}