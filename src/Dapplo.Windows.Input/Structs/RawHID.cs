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

#region using

using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;
using Dapplo.Windows.Input.Enums;

#endregion

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
        private readonly byte _bRawData;

        /// <inheritdoc />
        public override string ToString()
        {
            return $"Rawhib\n dwSizeHid : {_dwSizeHid}\n dwCount : {_dwCount}\n bRawData : {_bRawData}\n";
        }
    }
}