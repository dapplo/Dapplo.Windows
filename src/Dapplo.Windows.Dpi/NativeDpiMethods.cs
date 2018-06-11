//  Dapplo - building blocks for desktop applications
//  Copyright (C) 2017-2018  Dapplo
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

using System;
using System.Runtime.InteropServices;
using Dapplo.Windows.Common.Enums;
using Dapplo.Windows.Common.Structs;
using Dapplo.Windows.Dpi.Enums;
using Dapplo.Windows.User32;

namespace Dapplo.Windows.Dpi
{
    /// <summary>
    /// Some of the native DPI related Win32 API methods
    /// Not all have been implemented yet, see <a href="https://msdn.microsoft.com/en-us/library/windows/desktop/hh447398.aspx">High DPI reference</a>
    /// </summary>
    public static class NativeDpiMethods
    {
        /// <summary>
        /// See details <a hef="https://msdn.microsoft.com/en-us/library/windows/desktop/dn302113(v=vs.85).aspx">GetProcessDpiAwareness function</a>
        /// Retrieves the dots per inch (dpi) awareness of the specified process.
        /// </summary>
        /// <param name="processHandle">IntPtr with handle of the process that is being queried. If this parameter is NULL, the current process is queried.</param>
        /// <param name="value">out DpiAwareness - The DPI awareness of the specified process. Possible values are from the PROCESS_DPI_AWARENESS enumeration.</param>
        /// <returns>HResult</returns>
        [DllImport("shcore")]
        public static extern HResult GetProcessDpiAwareness(IntPtr processHandle, out DpiAwareness value);

        /// <summary>
        /// See more at <a href="https://msdn.microsoft.com/en-us/library/windows/desktop/mt748624(v=vs.85).aspx">GetDpiForWindow function</a>
        /// Returns the dots per inch (dpi) value for the associated window.
        /// </summary>
        /// <param name="hWnd">IntPtr</param>
        /// <returns>uint with dpi</returns>
        [DllImport(User32Api.User32)]
        public static extern uint GetDpiForWindow(IntPtr hWnd);

        /// <summary>
        ///     See
        ///     <a href="https://msdn.microsoft.com/en-us/library/windows/desktop/dn280510(v=vs.85).aspx">GetDpiForMonitor function</a>
        ///     Queries the dots per inch (dpi) of a display.
        /// </summary>
        /// <param name="hMonitor">IntPtr</param>
        /// <param name="dpiType">MonitorDpiType</param>
        /// <param name="dpiX">out int for the horizontal dpi</param>
        /// <param name="dpiY">out int for the vertical dpi</param>
        /// <returns>true if all okay</returns>
        [DllImport("shcore")]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool GetDpiForMonitor(IntPtr hMonitor, MonitorDpiType dpiType, out uint dpiX, out uint dpiY);

        /// <summary>
        ///     See <a href="https://msdn.microsoft.com/en-us/library/windows/desktop/mt748621(v=vs.85).aspx">EnableNonClientDpiScaling function</a>
        /// </summary>
        /// <param name="hWnd">IntPtr</param>
        /// <returns>bool</returns>
        [DllImport(User32Api.User32, SetLastError = true)]
        public static extern HResult EnableNonClientDpiScaling(IntPtr hWnd);

        /// <summary>
        /// Sets the current process to a specified dots per inch (dpi) awareness level. The DPI awareness levels are from the PROCESS_DPI_AWARENESS enumeration.
        /// See <a href="https://msdn.microsoft.com/en-us/library/windows/desktop/dn302122(v=vs.85).aspx">SetProcessDpiAwareness function</a>
        /// </summary>
        /// <param name="dpiAwareness">DpiAwareness</param>
        /// <returns>HResult</returns>
        [DllImport("shcore")]
        public static extern HResult SetProcessDpiAwareness(DpiAwareness dpiAwareness);

        /// <summary>
        /// See <a href="https://msdn.microsoft.com/en-us/library/windows/desktop/mt748623(v=vs.85).aspx">GetDpiForSystem function</a>
        /// Returns the system DPI.
        /// </summary>
        /// <returns>uint with the system DPI</returns>
        [DllImport(User32Api.User32)]
        public static extern uint GetDpiForSystem();

        /// <summary>
        /// Converts a point in a window from logical coordinates into physical coordinates, regardless of the dots per inch (dpi) awareness of the caller. For more information about DPI awareness levels, see PROCESS_DPI_AWARENESS.
        /// See more at <a href="https://msdn.microsoft.com/en-us/library/windows/desktop/dn384110(v=vs.85).aspx">LogicalToPhysicalPointForPerMonitorDPI function</a>
        /// </summary>
        /// <param name="hWnd">IntPtr A handle to the window whose transform is used for the conversion.</param>
        /// <param name="point">A pointer to a POINT structure that specifies the logical coordinates to be converted. The new physical coordinates are copied into this structure if the function succeeds.</param>
        /// <returns>bool</returns>
        [DllImport(User32Api.User32)]
        public static extern bool LogicalToPhysicalPointForPerMonitorDPI(IntPtr hWnd, ref NativePoint point);

        /// <summary>
        /// Converts a point in a window from logical coordinates into physical coordinates, regardless of the dots per inch (dpi) awareness of the caller. For more information about DPI awareness levels, see PROCESS_DPI_AWARENESS.
        /// See more at <a href="https://msdn.microsoft.com/en-us/library/windows/desktop/dn384112(v=vs.85).aspx">PhysicalToLogicalPointForPerMonitorDPI function</a>
        /// </summary>
        /// <param name="hWnd">IntPtr A handle to the window whose transform is used for the conversion.</param>
        /// <param name="point">NativePoint A pointer to a POINT structure that specifies the physical/screen coordinates to be converted. The new logical coordinates are copied into this structure if the function succeeds.</param>
        /// <returns>bool</returns>
        [DllImport(User32Api.User32)]
        public static extern bool PhysicalToLogicalPointForPerMonitorDPI(IntPtr hWnd, ref NativePoint point);

        /// <summary>
        /// See <a href="https://msdn.microsoft.com/en-us/library/windows/desktop/mt748626(v=vs.85).aspx">GetThreadDpiAwarenessContext function</a>
        /// Gets the DPI_AWARENESS_CONTEXT for the current thread.
        ///
        /// This method will return the latest DPI_AWARENESS_CONTEXT sent to SetThreadDpiAwarenessContext. If SetThreadDpiAwarenessContext was never called for this thread, then the return value will equal the default DPI_AWARENESS_CONTEXT for the process.
        /// </summary>
        /// <returns>DpiAwarenessContext</returns>
        [DllImport(User32Api.User32)]
        public static extern DpiAwarenessContext GetThreadDpiAwarenessContext();

        /// <summary>
        /// Set the DPI awareness for the current thread to the provided value.
        /// </summary>
        /// <param name="dpiAwarenessContext">DpiAwarenessContext the new value for the current thread</param>
        /// <returns>DpiAwarenessContext previous value</returns>
        [DllImport(User32Api.User32)]
        public static extern DpiAwarenessContext SetThreadDpiAwarenessContext(DpiAwarenessContext dpiAwarenessContext);

        /// <summary>
        /// Retrieves the DpiAwareness value from a DpiAwarenessContext.
        /// </summary>
        /// <param name="dpiAwarenessContext">DpiAwarenessContext</param>
        /// <returns>DpiAwareness</returns>
        [DllImport(User32Api.User32)]
        public static extern DpiAwareness GetAwarenessFromDpiAwarenessContext(DpiAwarenessContext dpiAwarenessContext);

        /// <summary>
        /// Retrieves the DPI from a given DPI_AWARENESS_CONTEXT handle. This enables you to determine the DPI of a thread without needed to examine a window created within that thread.
        /// </summary>
        /// <param name="dpiAwarenessContext">DpiAwarenessContext</param>
        /// <returns>uint with dpi value</returns>
        [DllImport(User32Api.User32)]
        public static extern uint GetDpiFromDpiAwarenessContext(DpiAwarenessContext dpiAwarenessContext);

        /// <summary>
        /// Determines if a specified DPI_AWARENESS_CONTEXT is valid and supported by the current system.
        /// </summary>
        /// <param name="dpiAwarenessContext">DpiAwarenessContext The context that you want to determine if it is supported.</param>
        /// <returns>bool true if supported otherwise false</returns>
        [DllImport(User32Api.User32)]
        public static extern bool IsValidDpiAwarenessContext(DpiAwarenessContext dpiAwarenessContext);

        /// <summary>
        /// Returns the DPI_HOSTING_BEHAVIOR of the specified window.
        ///
        /// This API allows you to examine the hosting behavior of a window after it has been created. A window's hosting behavior is the hosting behavior of the thread in which the window was created, as set by a call to SetThreadDpiHostingBehavior. This is a permanent value and cannot be changed after the window is created, even if the thread's hosting behavior is changed.
        /// </summary>
        /// <returns>DpiHostingBehavior</returns>
        [DllImport(User32Api.User32)]
        public static extern DpiHostingBehavior GetWindowDpiHostingBehavior();

        /// <summary>
        /// See more at <a href="https://msdn.microsoft.com/en-us/library/windows/desktop/mt845775.aspx">SetThreadDpiHostingBehavior function</a>
        /// Sets the thread's DPI_HOSTING_BEHAVIOR. This behavior allows windows created in the thread to host child windows with a different DPI_AWARENESS_CONTEXT.
        ///
        /// DPI_HOSTING_BEHAVIOR enables a mixed content hosting behavior, which allows parent windows created in the thread to host child windows with a different DPI_AWARENESS_CONTEXT value. This property only effects new windows created within this thread while the mixed hosting behavior is active. A parent window with this hosting behavior is able to host child windows with different DPI_AWARENESS_CONTEXT values, regardless of whether the child windows have mixed hosting behavior enabled.
        /// 
        /// This hosting behavior does not allow for windows with per-monitor DPI_AWARENESS_CONTEXT values to be hosted until windows with DPI_AWARENESS_CONTEXT values of system or unaware.
        /// 
        /// To avoid unexpected outcomes, a thread's DPI_HOSTING_BEHAVIOR should be changed to support mixed hosting behaviors only when creating a new window which needs to support those behaviors. Once that window is created, the hosting behavior should be switched back to its default value.
        /// 
        /// This API is used to change the thread's DPI_HOSTING_BEHAVIOR from its default value. This is only necessary if your app needs to host child windows from plugins and third-party components that do not support per-monitor-aware context. This is most likely to occur if you are updating complex applications to support per-monitor DPI_AWARENESS_CONTEXT behaviors.
        /// 
        /// Enabling mixed hosting behavior will not automatically adjust the thread's DPI_AWARENESS_CONTEXT to be compatible with legacy content. The thread's awareness context must still be manually changed before new windows are created to host such content.
        /// </summary>
        /// <param name="dpiHostingBehavior">DpiHostingBehavior</param>
        /// <returns>previous DpiHostingBehavior</returns>
        [DllImport(User32Api.User32)]
        public static extern DpiHostingBehavior SetThreadDpiHostingBehavior(DpiHostingBehavior dpiHostingBehavior);

        /// <summary>
        ///Retrieves the DPI_HOSTING_BEHAVIOR from the current thread.
        /// </summary>
        /// <returns>DpiHostingBehavior</returns>
        [DllImport(User32Api.User32)]
        public static extern DpiHostingBehavior GetThreadDpiHostingBehavior();

        /// <summary>
        /// Overrides the default per-monitor DPI scaling behavior of a child window in a dialog.
        /// This function returns TRUE if the operation was successful, and FALSE otherwise. To get extended error information, call GetLastError.
        /// 
        /// Possible errors are ERROR_INVALID_HANDLE if passed an invalid HWND, and ERROR_ACCESS_DENIED if the windows belongs to another process.
        ///
        /// The behaviors are specified as values from the DIALOG_CONTROL_DPI_CHANGE_BEHAVIORS enum. This function follows the typical two-parameter approach to setting flags, where a mask specifies the subset of the flags to be changed.
        /// 
        /// It is valid to set these behaviors on any window. It does not matter if the window is currently a child of a dialog at the point in time that SetDialogControlDpiChangeBehavior is called. The behaviors are retained and will take effect only when the window is an immediate child of a dialog that has per-monitor DPI scaling enabled.
        /// 
        /// This API influences individual controls within dialogs. The dialog-wide per-monitor DPI scaling behavior is controlled by SetDialogDpiChangeBehavior.
        /// </summary>
        /// <param name="hWnd">IntPtr A handle for the window whose behavior will be modified.</param>
        /// <param name="mask">DialogScalingBehaviors A mask specifying the subset of flags to be changed.</param>
        /// <param name="values">DialogScalingBehaviors The desired value to be set for the specified subset of flags.</param>
        /// <returns>bool</returns>
        [DllImport(User32Api.User32)]
        public static extern bool SetDialogControlDpiChangeBehavior(IntPtr hWnd, DialogScalingBehaviors mask, DialogScalingBehaviors values);

        /// <summary>
        /// Retrieves and per-monitor DPI scaling behavior overrides of a child window in a dialog.
        /// The flags set on the given window. If passed an invalid handle, this function will return zero, and set its last error to ERROR_INVALID_HANDLE.
        /// </summary>
        /// <param name="hWnd">IntPtr A handle for the window whose behavior will be modified.</param>
        /// <returns>DialogScalingBehaviors</returns>
        [DllImport(User32Api.User32)]
        public static extern DialogScalingBehaviors GetDialogControlDpiChangeBehavior(IntPtr hWnd);
    }
}
