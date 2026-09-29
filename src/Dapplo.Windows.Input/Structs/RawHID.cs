// Copyright (c) Dapplo and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;

namespace Dapplo.Windows.Input.Structs;

/// <summary>
///     Describes the format of the raw input from a Human Interface Device (HID).
///     See <a href="https://msdn.microsoft.com/en-us/library/windows/desktop/ms645549.aspx">RAWHID structure</a>
///     This is only the fixed part of the structure, the HID reports (bRawData) follow it inline and have a variable length.
///     They are available as <see cref="RawInputEventArgs.HidData"/>, or via <see cref="RawInputApi.TryParseRawInput"/>.
/// </summary>
[StructLayout(LayoutKind.Sequential)]
[SuppressMessage("ReSharper", "ConvertToAutoProperty")]
[SuppressMessage("ReSharper", "ArrangeAccessorOwnerBody")]
// ReSharper disable once InconsistentNaming
public struct RawHID
{
    // The size, in bytes, of each HID input in bRawData.
    private readonly uint _dwSizeHid;
    // The number of HID inputs in bRawData.
    private readonly uint _dwCount;
    // bRawData, the variable length raw input data, follows here inline

    /// <summary>
    /// The size, in bytes, of each HID input report
    /// </summary>
    public uint SizeHid => _dwSizeHid;

    /// <summary>
    /// The number of HID input reports
    /// </summary>
    public uint Count => _dwCount;
}
