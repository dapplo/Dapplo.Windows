//  Dapplo - building blocks for desktop applications
//  Copyright (C) 2017-2019  Dapplo
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

#endregion

namespace Dapplo.Windows.Input.Structs
{
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
}