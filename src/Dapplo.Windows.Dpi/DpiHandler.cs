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

#region using

using System;
using System.Diagnostics;
using System.Reactive.Subjects;
using System.Runtime.InteropServices;
using Dapplo.Log;
using Dapplo.Windows.Common;
using Dapplo.Windows.Common.Enums;
using Dapplo.Windows.Common.Extensions;
using Dapplo.Windows.Common.Structs;
using Dapplo.Windows.Dpi.Enums;
using Dapplo.Windows.Gdi32;
using Dapplo.Windows.Gdi32.Enums;
using Dapplo.Windows.Gdi32.SafeHandles;
using Dapplo.Windows.Messages;
using Dapplo.Windows.User32;
using Dapplo.Windows.User32.Enums;

#endregion

namespace Dapplo.Windows.Dpi
{
    /// <summary>
    ///     This handles DPI changes see u.a.
    ///     <a href="https://msdn.microsoft.com/en-us/library/windows/desktop/dn469266(v=vs.85).aspx">Writing DPI-Aware Desktop and Win32 Applications</a>
    /// </summary>
    public sealed class DpiHandler : IDisposable
    {
        /// <summary>
        ///     This is the default DPI for the screen
        /// </summary>
        public const double DefaultScreenDpi = 96d;
        private static readonly LogSource Log = new LogSource();

        // Stores if the handler is runing via a listener
        private bool _needsListenerWorkaround;
        // Via this the dpi values are published
        private readonly ISubject<double> _onDpiChanged = new Subject<double>();

        // Via this the dpi values are published in details
        private readonly ISubject<DpiChangeInfo> _onDpiChangeInfo = new Subject<DpiChangeInfo>();

        /// <summary>
        ///     Create a DpiHandler
        /// </summary>
        public DpiHandler(bool needsListenerWorkaround = false)
        {
            _needsListenerWorkaround = needsListenerWorkaround;
            EnableDpiAwareness();
        }

        /// <summary>
        ///     Retrieve the current DPI for the window
        /// </summary>
        public double Dpi { get; private set; }

        /// <summary>
        /// Turn on the Dpi awareness
        /// </summary>
        /// <returns>bool if this is enabled</returns>
        public static bool EnableDpiAwareness()
        {
            if (IsDpiAware)
            {
                // Nothing to do
                return true;
            }
            // Can we enable the DpiAwareness?
            if (!WindowsVersion.IsWindows81OrLater)
            {
                if (Log.IsVerboseEnabled())
                {
                    Log.Verbose().WriteLine("The DPI handler will only do one initial Dpi change event, on Window creation, when the DPI settings are different from the default.");
                }
                return false;
            }
            // Try setting the DpiAwareness
            if (SetProcessDpiAwareness(DpiAwareness.PerMonitorAware).Succeeded())
            {
                return true;
            }
            Log.Warn().WriteLine("Couldn't enable Dpi Awareness.");
            return false;
        }

        /// <summary>
        ///     Check if the process is DPI Aware, an DpiHandler doesn't make sense if not.
        /// </summary>
        public static bool IsDpiAware
        {
            get
            {
                // We can only test this for Windows 8.1 or later
                if (!WindowsVersion.IsWindows81OrLater)
                {
                    Log.Verbose().WriteLine("An application can only be DPI aware starting with Window 8.1 and later.");
                    return false;
                }
                using (var process = Process.GetCurrentProcess())
                {
                    GetProcessDpiAwareness(process.Handle, out var dpiAwareness);
                    if (Log.IsVerboseEnabled())
                    {
                        Log.Verbose().WriteLine("Process {0} has a Dpi awareness {1}", process.ProcessName, dpiAwareness);
                    }

                    return dpiAwareness != DpiAwareness.Unaware && dpiAwareness != DpiAwareness.Invalid;
                }
            }
        }

        /// <summary>
        ///     This is that which handles the windows messages, and needs to be disposed
        /// </summary>
        internal IDisposable MessageHandler { get; set; }


        /// <summary>
        ///     This subject publishes whenever the dpi settings are changed
        /// </summary>
        public IObservable<DpiChangeInfo> OnDpiChangeInfo => _onDpiChangeInfo;

        /// <summary>
        ///     This subject publishes whenever the dpi settings are changed
        /// </summary>
        public IObservable<double> OnDpiChanged => _onDpiChanged;

        /// <summary>
        ///     Retrieve the DPI value for the supplied window handle
        /// </summary>
        /// <param name="hWnd">IntPtr</param>
        /// <returns>dpi value</returns>
        public static int GetDpi(IntPtr hWnd)
        {
            // Use the easiest method, but this only works for Windows 10
            if (WindowsVersion.IsWindows10OrLater)
            {
                return GetDpiForWindow(hWnd);
            }

            // Use the second easiest method, but this only works for Windows 8.1 or later
            if (WindowsVersion.IsWindows81OrLater)
            {
                var hMonitor = User32Api.MonitorFromWindow(hWnd, MonitorFrom.DefaultToNearest);
                // ReSharper disable once UnusedVariable
                if (GetDpiForMonitor(hMonitor, MonitorDpiType.EffectiveDpi, out var dpiX, out var dpiY))
                {
                    return (int) dpiX;
                }
            }

            // Fallback to the global DPI settings
            using (var hdc = SafeDeviceContextHandle.FromHWnd(hWnd))
            {
                return Gdi32Api.GetDeviceCaps(hdc, DeviceCaps.LOGPIXELSX);
            }
        }

        /// <summary>
        ///     Message handler of the Per_Monitor_DPI_Aware window.
        ///     The handles the WM_DPICHANGED message and adjusts window size, graphics and text based on the DPI of the monitor.
        ///     The window message provides the new window size (lparam) and new DPI (wparam)
        ///     See
        ///     <a href="https://msdn.microsoft.com/en-us/library/windows/desktop/dn312083(v=vs.85).aspx">WM_DPICHANGED message</a>
        /// </summary>
        /// <param name="hwnd">IntPtr with the hWnd</param>
        /// <param name="msg">The Windows message</param>
        /// <param name="wParam">IntPtr</param>
        /// <param name="lParam">IntPtr</param>
        /// <param name="handled">ref bool</param>
        /// <returns>IntPtr</returns>
        internal IntPtr HandleWindowMessages(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
        {
            if (HandleWindowMessages(WindowMessageInfo.Create(hwnd, msg, wParam, lParam)))
            {
                handled = true;
            }
            return IntPtr.Zero;
        }

        /// <summary>
        ///     Message handler of the Per_Monitor_DPI_Aware window.
        ///     The handles the WM_DPICHANGED message and adjusts window size, graphics and text based on the DPI of the monitor.
        ///     The window message provides the new window size (lparam) and new DPI (wparam)
        ///     See
        ///     <a href="https://msdn.microsoft.com/en-us/library/windows/desktop/dn312083(v=vs.85).aspx">WM_DPICHANGED message</a>
        /// </summary>
        /// <param name="windowMessageInfo">WindowMessageInfo</param>
        /// <returns>IntPtr</returns>
        internal bool HandleWindowMessages(WindowMessageInfo windowMessageInfo)
        {
            bool handled = false;
            var currentDpi = (int) DefaultScreenDpi;
            bool isDpiMessage = false;
            switch (windowMessageInfo.Message)
            {
                // Handle the WM_NCCREATE for Forms / controls, for WPF this is done differently
                case WindowsMessages.WM_NCCREATE:
                    if (Log.IsVerboseEnabled())
                    {
                        Log.Verbose().WriteLine("Processing {0} event, enabling DPI scaling for window {1}", windowMessageInfo.Message, windowMessageInfo.Handle);
                    }

                    TryEnableNonClientDpiScaling(windowMessageInfo.Handle);
                    break;
                // Handle the WM_CREATE, this is where we can get the DPI via system calls
                case WindowsMessages.WM_CREATE:
                    isDpiMessage = true;
                    if (Log.IsVerboseEnabled())
                    {
                        Log.Verbose().WriteLine("Processing {0} event, retrieving DPI for window {1}", windowMessageInfo.Message, windowMessageInfo.Handle);
                    }

                    currentDpi = GetDpi(windowMessageInfo.Handle);
                    break;
                // Handle the DPI change message, this is where it's supplied
                case WindowsMessages.WM_DPICHANGED:
                    isDpiMessage = true;
                    if (Log.IsVerboseEnabled())
                    {
                        Log.Verbose().WriteLine("Processing {0} event, resizing / positioning window {1}", windowMessageInfo.Message, windowMessageInfo.Handle);
                    }

                    // Retrieve the adviced location
                    var lprNewRect = (NativeRect) Marshal.PtrToStructure(windowMessageInfo.LongParam, typeof(NativeRect));
                    // Move the window to it's location, and resize
                    User32Api.SetWindowPos(windowMessageInfo.Handle,
                        IntPtr.Zero,
                        lprNewRect.Left,
                        lprNewRect.Top,
                        lprNewRect.Width,
                        lprNewRect.Height,
                        WindowPos.SWP_NOZORDER | WindowPos.SWP_NOOWNERZORDER | WindowPos.SWP_NOACTIVATE);
                    currentDpi = windowMessageInfo.WordParam.ToInt32() & 0xFFFF;
                    // specify that the message was handled
                    handled = true;
                    break;
                case WindowsMessages.WM_PAINT:
                    // This is a workaround for non DPI aware applications, these don't seem to get a WM_CREATE
                    if (Math.Abs(Dpi) < double.Epsilon && !IsDpiAware)
                    {
                        isDpiMessage = true;
                        currentDpi = GetDpi(windowMessageInfo.Handle);
                    }
                    break;
                case WindowsMessages.WM_SETICON:
                    // This is a workaround for handling WinProc outside of the class
                    if (_needsListenerWorkaround)
                    {
                        isDpiMessage = true;
                        // disable workaround
                        _needsListenerWorkaround = false;
                        currentDpi = GetDpi(windowMessageInfo.Handle);
                    }
                    break;
                case WindowsMessages.WM_DESTROY:
                    if (Log.IsVerboseEnabled())
                    {
                        Log.Verbose().WriteLine("Completing the observable for {0}", windowMessageInfo.Handle);
                    }

                    // If the window is destroyed, we complete the subject
                    _onDpiChanged.OnCompleted();
                    // Dispose all resources
                    Dispose();
                    break;
            }
            // Check if the DPI was changed, if so call the action (if any)
            if (!isDpiMessage)
            {
                return false;
            }
            if (!IsEqual(Dpi, currentDpi))
            {
                var beforeDpi = Dpi;
                if (Log.IsVerboseEnabled())
                {
                    Log.Verbose().WriteLine("Changing DPI from {0} to {1}", beforeDpi, currentDpi);
                }

                _onDpiChangeInfo.OnNext(new DpiChangeInfo(DpiChangeEventTypes.Before, beforeDpi, currentDpi));
                Dpi = currentDpi;
                _onDpiChanged.OnNext(Dpi);
                _onDpiChangeInfo.OnNext(new DpiChangeInfo(DpiChangeEventTypes.Change, beforeDpi, currentDpi));
                _onDpiChangeInfo.OnNext(new DpiChangeInfo(DpiChangeEventTypes.After, beforeDpi, currentDpi));
            }
            else if(Log.IsVerboseEnabled())
            {
                Log.Verbose().WriteLine("DPI was unchanged from {0}", Dpi);
            }

            return handled;
        }


        /// <summary>
        ///     Message handler of the DPI Aware ContextMenuStrip, this is simplified compared to the normal
        ///     The handles the WM_DPICHANGED message and adjusts window size, graphics and text based on the DPI of the monitor.
        ///     The window message provides the new window size (lparam) and new DPI (wparam)
        ///     See
        ///     <a href="https://msdn.microsoft.com/en-us/library/windows/desktop/dn312083(v=vs.85).aspx">WM_DPICHANGED message</a>
        /// </summary>
        /// <param name="windowMessageInfo">WindowMessageInfo</param>
        /// <returns>IntPtr</returns>
        internal IntPtr HandleContextMenuMessages(WindowMessageInfo windowMessageInfo)
        {
            var currentDpi = (int)DefaultScreenDpi;
            bool isDpiMessage = false;
            switch (windowMessageInfo.Message)
            {
                // Handle the WM_CREATE, this is where we can get the DPI via system calls
                case WindowsMessages.WM_SHOWWINDOW:
                    isDpiMessage = true;
                    if (Log.IsVerboseEnabled())
                    {
                        Log.Verbose().WriteLine("Processing {0} event, retrieving DPI for ContextMenuStrip {1}", windowMessageInfo.Message, windowMessageInfo.Handle);
                    }

                    currentDpi = GetDpi(windowMessageInfo.Handle);
                    break;
                case WindowsMessages.WM_DESTROY:
                    // If the window is destroyed, we complete the subject
                    _onDpiChanged.OnCompleted();
                    break;
            }
            // Check if the DPI was changed, if so call the action (if any)
            if (!isDpiMessage)
            {
                return IntPtr.Zero;
            }
            if (!IsEqual(Dpi, currentDpi))
            {
                var beforeDpi = Dpi;
                if (Log.IsVerboseEnabled())
                {
                    Log.Verbose().WriteLine("Changing DPI from {0} to {1}", beforeDpi, currentDpi);
                }

                _onDpiChangeInfo.OnNext(new DpiChangeInfo(DpiChangeEventTypes.Before, beforeDpi, currentDpi));
                Dpi = currentDpi;
                _onDpiChanged.OnNext(Dpi);
                _onDpiChangeInfo.OnNext(new DpiChangeInfo(DpiChangeEventTypes.Change, beforeDpi, currentDpi));
                _onDpiChangeInfo.OnNext(new DpiChangeInfo(DpiChangeEventTypes.After, beforeDpi, currentDpi));
            }
            else if(Log.IsVerboseEnabled())
            {
                Log.Verbose().WriteLine("DPI was unchanged from {0}", Dpi);
            }

            return IntPtr.Zero;
        }

        /// <summary>
        ///     Compare helper for doubles
        /// </summary>
        /// <param name="d1">double</param>
        /// <param name="d2">double</param>
        /// <param name="precisionFactor">uint</param>
        /// <returns>bool</returns>
        private static bool IsEqual(double d1, double d2, uint precisionFactor = 2)
        {
            return Math.Abs(d1 - d2) < precisionFactor * double.Epsilon;
        }

        /// <summary>
        ///     Scale the supplied base width according to the supplied dpi
        /// </summary>
        /// <param name="baseWidth">int with e.g. 16 for 16x16 images</param>
        /// <param name="dpi">current dpi, normal is 96.</param>
        /// <returns>Scaled width</returns>
        public static int ScaleWithDpi(int baseWidth, double dpi)
        {
            var scaleFactor = dpi / DefaultScreenDpi;
            var width = (int) (scaleFactor * baseWidth);
            return width;
        }

        /// <summary>
        /// public wrapper for EnableNonClientDpiScaling, this also checks if the function is available.
        /// </summary>
        /// <param name="hWnd">IntPtr</param>
        /// <returns>true if it worked</returns>
        public static bool TryEnableNonClientDpiScaling(IntPtr hWnd)
        {
            // EnableNonClientDpiScaling is only available on Windows 10 and later
            if (!WindowsVersion.IsWindows10OrLater)
            {
                return false;
            }
            var result = EnableNonClientDpiScaling(hWnd);
            if (result.Succeeded())
            {
                return true;
            }
            var error = Win32.GetLastErrorCode();
            if (Log.IsVerboseEnabled())
            {
                Log.Verbose().WriteLine("Error enabling non client dpi scaling : {0}", Win32.GetMessage(error));
            }

            return false;
        }

        #region DllImports

        [DllImport("shcore")]
        private static extern int GetProcessDpiAwareness(IntPtr processHandle, out DpiAwareness value);

        [DllImport(User32Api.User32)]
        private static extern int GetDpiForWindow(IntPtr hWnd);

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
        private static extern bool GetDpiForMonitor(IntPtr hMonitor, MonitorDpiType dpiType, out uint dpiX, out uint dpiY);

        /// <summary>
        ///     See <a href="https://msdn.microsoft.com/en-us/library/windows/desktop/mt748621(v=vs.85).aspx">EnableNonClientDpiScaling function</a>
        /// </summary>
        /// <param name="hWnd">IntPtr</param>
        /// <returns>bool</returns>
        [DllImport(User32Api.User32, SetLastError = true)]
        private static extern HResult EnableNonClientDpiScaling(IntPtr hWnd);

        /// <summary>
        /// Sets the current process to a specified dots per inch (dpi) awareness level. The DPI awareness levels are from the PROCESS_DPI_AWARENESS enumeration.
        /// See <a href="https://msdn.microsoft.com/en-us/library/windows/desktop/dn302122(v=vs.85).aspx">SetProcessDpiAwareness function</a>
        /// </summary>
        /// <param name="dpiAwareness">DpiAwareness</param>
        /// <returns>HResult</returns>
        [DllImport("shcore")]
        private static extern HResult SetProcessDpiAwareness(DpiAwareness dpiAwareness);

        //[DllImport("shcore")]
        //private static extern int GetDpiForSystem();

        //[DllImport("shcore")]
        //private static extern DpiAwarenessContext GetThreadDpiAwarenessContext();

        //[DllImport("shcore")]
        //private static extern DpiAwarenessContext SetThreadDpiAwarenessContext(DpiAwarenessContext dpiAwarenessContext);

        //[DllImport("shcore")]
        //private static extern DpiAwareness GetAwarenessFromDpiAwarenessContext(DpiAwarenessContext dpiAwarenessContext);

        /// <inheritdoc />
        public void Dispose()
        {
            MessageHandler?.Dispose();
            MessageHandler = null;
        }

        #endregion
    }
}