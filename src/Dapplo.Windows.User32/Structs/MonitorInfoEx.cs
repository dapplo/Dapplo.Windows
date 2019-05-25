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

#region Usings

using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;
using Dapplo.Windows.Common.Structs;
using Dapplo.Windows.User32.Enums;

#endregion

namespace Dapplo.Windows.User32.Structs
{
    /// <summary>
    ///     The MONITORINFOEX structure contains information about a display monitor.
    ///     The GetMonitorInfo function stores information into a MONITORINFOEX structure or a MONITORINFO structure.
    ///     The MONITORINFOEX structure is a superset of the MONITORINFO structure.
    ///     The MONITORINFOEX structure adds a string member to contain a name for the display monitor.
    /// </summary>
    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
    [SuppressMessage("Sonar Code Smell", "S1450:Private fields only used as local variables in methods should become local variables", Justification = "Interop!")]
    [SuppressMessage("Sonar Code Smell", "S3459:Unassigned members should be removed", Justification = "Interop!")]
    [SuppressMessage("ReSharper", "ConvertToAutoProperty")]
    public unsafe struct MonitorInfoEx
    {
        /// <summary>
        ///     The size, in bytes, of the structure. Set this member to sizeof(MONITORINFOEX) (72) before calling the
        ///     GetMonitorInfo function.
        ///     Doing so lets the function determine the type of structure you are passing to it.
        /// </summary>
        private int _cbSize;

        private readonly NativeRect _monitor;
        private readonly NativeRect _workArea;
        private readonly MonitorInfoFlags _flags;
        private fixed char _deviceName[32];

        /// <summary>
        ///     A RECT structure that specifies the display monitor rectangle, expressed in virtual-screen coordinates.
        ///     Note that if the monitor is not the primary display monitor, some of the rectangle's coordinates may be negative
        ///     values.
        /// </summary>
        public NativeRect Monitor => _monitor;

        /// <summary>
        ///     A RECT structure that specifies the work area rectangle of the display monitor that can be used by applications,
        ///     expressed in virtual-screen coordinates. Windows uses this rectangle to maximize an application on the monitor.
        ///     The rest of the area in rcMonitor contains system windows such as the task bar and side bars.
        ///     Note that if the monitor is not the primary display monitor, some of the rectangle's coordinates may be negative
        ///     values.
        /// </summary>
        public NativeRect WorkArea => _workArea;

        /// <summary>
        ///     The attributes of the display monitor.
        /// </summary>
        public MonitorInfoFlags Flags => _flags;

        /// <summary>
        ///     A string that specifies the device name of the monitor being used.
        ///     Most applications have no use for a display monitor name,
        ///     and so can save some bytes by using a MONITORINFO structure.
        /// </summary>
        public string DeviceName
        {
            get
            {
                fixed (char* deviceName = _deviceName)
                {
                    return new string(deviceName);
                }

            }
        }

        /// <summary>
        ///     Create a MonitorInfoEx with defaults
        /// </summary>
        public static MonitorInfoEx Create()
        {
            return new MonitorInfoEx
            {
                _cbSize = Marshal.SizeOf(typeof(MonitorInfoEx))
            };
        }
    }
}