// Copyright (c) Dapplo and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
using System;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;
using Dapplo.Windows.Input.Enums;

namespace Dapplo.Windows.Input.Structs;

/// <summary>
/// Contains information about a raw input device.
/// See <a href="https://msdn.microsoft.com/en-us/library/windows/desktop/ms645568.aspx">RAWINPUTDEVICELIST structure</a>
/// </summary>
[StructLayout(LayoutKind.Sequential)]
[SuppressMessage("ReSharper", "ConvertToAutoProperty")]
[SuppressMessage("ReSharper", "ArrangeAccessorOwnerBody")]
public struct RawInputDeviceList
{
    private readonly IntPtr _hDevice;
    private readonly RawInputDeviceTypes _dwType;

    /// <summary>
    /// A handle to the raw input device.
    /// </summary>
    public IntPtr Handle
    {
        get { return _hDevice; }
    }

    /// <summary>
    /// The type of device
    /// </summary>
    public RawInputDeviceTypes RawInputDeviceType
    {
        get { return _dwType; }
    }
}