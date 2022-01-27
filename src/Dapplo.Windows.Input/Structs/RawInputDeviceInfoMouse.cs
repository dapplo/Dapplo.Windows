// Copyright (c) Dapplo and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;

namespace Dapplo.Windows.Input.Structs;

/// <summary>
///     This struct defines the raw input data coming from the specified mouse.
///     See <a href="https://msdn.microsoft.com/en-us/library/windows/desktop/ms645589.aspx">RID_DEVICE_INFO_MOUSE structure</a>
/// Remarks:
/// For the mouse, the Usage Page is 1 and the Usage is 2.
/// </summary>
[StructLayout(LayoutKind.Sequential)]
[SuppressMessage("ReSharper", "ConvertToAutoProperty")]
[SuppressMessage("ReSharper", "ArrangeAccessorOwnerBody")]
public struct RawInputDeviceInfoMouse
{
    private readonly int _dwId;
    private readonly int _dwNumberOfButtons;
    private readonly int _dwSampleRate;
    private readonly bool _fHasHorizontalWheel;

    /// <summary>
    /// The identifier of the mouse device.
    /// </summary>
    public int Id => _dwId;

    /// <summary>
    /// The number of buttons for the mouse.
    /// </summary>
    public int NumberOfButtons => _dwNumberOfButtons;

    /// <summary>
    /// The number of data points per second. This information may not be applicable for every mouse device.
    /// </summary>
    public int SampleRate => _dwSampleRate;

    /// <summary>
    /// TRUE if the mouse has a wheel for horizontal scrolling; otherwise, FALSE.
    /// Windows XP:  This member is only supported starting with Windows Vista.
    /// </summary>
    public bool HasHorizontalWheel => _fHasHorizontalWheel;
}