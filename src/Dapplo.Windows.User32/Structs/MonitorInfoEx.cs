// Copyright (c) Dapplo and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;
using Dapplo.Windows.Common.Structs;
using Dapplo.Windows.User32.Enums;

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