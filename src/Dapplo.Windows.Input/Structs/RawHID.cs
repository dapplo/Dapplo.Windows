// Copyright (c) Dapplo and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
using System;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;

namespace Dapplo.Windows.Input.Structs
{
    /// <summary>
    ///     Describes the format of the raw input from a Human Interface Device (HID).
    ///     See <a href="https://msdn.microsoft.com/en-us/library/windows/desktop/ms645549.aspx">RAWHID structure</a>
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
        // The raw input data, as an array of bytes.
        private readonly IntPtr _bRawData;

        /// <summary>
        /// Returns the raw input data, as an array of bytes.
        /// </summary>
        public byte[] GetData()
        {
            var data = new byte[_dwSizeHid * _dwCount];
            Marshal.Copy(_bRawData, data, 0, data.Length);
            return data;
        }
    }
}