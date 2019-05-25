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

using System;
using System.Reactive.Subjects;
using System.Runtime.InteropServices;
using Dapplo.Log;
using Dapplo.Windows.Common;
using Dapplo.Windows.Common.Extensions;
using Dapplo.Windows.Common.Structs;
using Dapplo.Windows.Messages;
using Dapplo.Windows.User32;
using Dapplo.Windows.User32.Enums;

#endregion

namespace Dapplo.Windows.Dpi
{
    /// <summary>
    ///     This handles DPI changes see 
    ///     <a href="https://msdn.microsoft.com/en-us/library/windows/desktop/dn469266.aspx">Writing DPI-Aware Desktop and Win32 Applications</a>
    /// </summary>
    public sealed class DpiHandler : IDisposable
    {
        private static readonly LogSource Log = new LogSource();

        /// <summary>
        ///     This is the default DPI for the screen
        /// </summary>
        public const uint DefaultScreenDpi = 96;

        // Stores if the handler is runing via a listener
        private bool _needsListenerWorkaround;

        // Via this the dpi values are published in details
        private readonly ISubject<DpiChangeInfo> _onDpiChanged = new Subject<DpiChangeInfo>();

        private readonly IDisposable _scopedThreadDpiAwarenessContext;
        /// <summary>
        ///     Create a DpiHandler
        /// </summary>
        public DpiHandler(bool needsListenerWorkaround = false)
        {
            _needsListenerWorkaround = needsListenerWorkaround;
            _scopedThreadDpiAwarenessContext = NativeDpiMethods.DefaultScopedThreadDpiAwarenessContext();
        }

        /// <summary>
        ///     Retrieve the current DPI for the UI element whic is related to this DpiHandler
        /// </summary>
        public uint Dpi { get; private set; }

        /// <summary>
        ///     This is that which handles the windows messages, and needs to be disposed
        /// </summary>
        internal IDisposable MessageHandler { get; set; }

        /// <summary>
        ///     This subject publishes whenever the dpi settings are changed, with some details
        /// </summary>
        public IObservable<DpiChangeInfo> OnDpiChanged => _onDpiChanged;

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
            var currentDpi = DefaultScreenDpi;
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

                    currentDpi = NativeDpiMethods.GetDpi(windowMessageInfo.Handle);
                    _scopedThreadDpiAwarenessContext.Dispose();
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
                    currentDpi = (uint)windowMessageInfo.WordParam & 0xFFFF;
                    // specify that the message was handled
                    handled = true;
                    break;
                case WindowsMessages.WM_PAINT:
                    // This is a workaround for non DPI aware applications, these don't seem to get a WM_CREATE
                    if (Dpi == 0)
                    {
                        isDpiMessage = true;
                        currentDpi = NativeDpiMethods.GetDpi(windowMessageInfo.Handle);
                    }
                    break;
                case WindowsMessages.WM_SETICON:
                    // This is a workaround for handling WinProc outside of the class
                    if (_needsListenerWorkaround)
                    {
                        isDpiMessage = true;
                        // disable workaround
                        _needsListenerWorkaround = false;
                        currentDpi = NativeDpiMethods.GetDpi(windowMessageInfo.Handle);
                    }

                    break;
                case WindowsMessages.WM_DPICHANGED_BEFOREPARENT:
                    if (Log.IsVerboseEnabled())
                    {
                        Log.Verbose().WriteLine("Dpi changed on {0} before parent", windowMessageInfo.Handle);
                    }
                    break;
                case WindowsMessages.WM_DPICHANGED_AFTERPARENT:
                    if (Log.IsVerboseEnabled())
                    {
                        Log.Verbose().WriteLine("Dpi changed on {0} after parent", windowMessageInfo.Handle);
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

            if (Dpi != currentDpi)
            {
                var beforeDpi = Dpi;
                if (Log.IsVerboseEnabled())
                {
                    Log.Verbose().WriteLine("Changing DPI from {0} to {1}", beforeDpi, currentDpi);
                }
                Dpi = currentDpi;
                _onDpiChanged.OnNext(new DpiChangeInfo(beforeDpi, currentDpi));
            }
            else if (Log.IsVerboseEnabled())
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
            var currentDpi = DefaultScreenDpi;
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

                    currentDpi = NativeDpiMethods.GetDpi(windowMessageInfo.Handle);
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

            if (Dpi != currentDpi)
            {
                var beforeDpi = Dpi;
                if (Log.IsVerboseEnabled())
                {
                    Log.Verbose().WriteLine("DPI changed from {0} to {1}", beforeDpi, currentDpi);
                }

                _onDpiChanged.OnNext(new DpiChangeInfo(beforeDpi, currentDpi));
                Dpi = currentDpi;
            }
            else if (Log.IsVerboseEnabled())
            {
                Log.Verbose().WriteLine("DPI was unchanged from {0}", Dpi);
            }

            return IntPtr.Zero;
        }

        #region Scale
        /// <summary>
        /// Calculate a DPI scale factor
        /// </summary>
        /// <param name="dpi">uint</param>
        /// <returns>double</returns>
        public static double DpiScaleFactor(uint dpi)
        {
            return (double) dpi / DefaultScreenDpi;
        }

        /// <summary>
        ///     Scale the supplied number according to the supplied dpi
        /// </summary>
        /// <param name="someNumber">double with e.g. the width 16 for 16x16 images</param>
        /// <param name="dpi">current dpi, normal is 96.</param>
        /// <param name="scaleModifier">A function which can modify the scale factor</param>
        /// <returns>double with the scaled number</returns>
        public static double ScaleWithDpi(double someNumber, uint dpi, Func<double, double> scaleModifier = null)
        {
            var dpiScaleFactor = DpiScaleFactor(dpi);
            if (scaleModifier != null)
            {
                dpiScaleFactor = scaleModifier(dpiScaleFactor);
            }
            return dpiScaleFactor * someNumber;
        }

        /// <summary>
        ///     Scale the supplied number according to the supplied dpi
        /// </summary>
        /// <param name="number">int with e.g. 16 for 16x16 images</param>
        /// <param name="dpi">current dpi, normal is 96.</param>
        /// <param name="scaleModifier">A function which can modify the scale factor</param>
        /// <returns>Scaled width</returns>
        public static int ScaleWithDpi(int number, uint dpi, Func<double, double> scaleModifier = null)
        {
            var dpiScaleFactor = DpiScaleFactor(dpi);
            if (scaleModifier != null)
            {
                dpiScaleFactor = scaleModifier(dpiScaleFactor);
            }
            return (int)(dpiScaleFactor * number);
        }

        /// <summary>
        ///     Scale the supplied NativeSize according to the supplied dpi
        /// </summary>
        /// <param name="size">NativeSize to resize</param>
        /// <param name="dpi">current dpi, normal is 96.</param>
        /// <param name="scaleModifier">A function which can modify the scale factor</param>
        /// <returns>NativeSize scaled</returns>
        public static NativeSize ScaleWithDpi(NativeSize size, uint dpi, Func<double, double> scaleModifier = null)
        {
            var dpiScaleFactor = DpiScaleFactor(dpi);
            if (scaleModifier != null)
            {
                dpiScaleFactor = scaleModifier(dpiScaleFactor);
            }
            return new NativeSize((int)(dpiScaleFactor * size.Width), (int)(dpiScaleFactor * size.Height));
        }

        /// <summary>
        ///     Scale the supplied NativePoint according to the supplied dpi
        /// </summary>
        /// <param name="size">NativePoint to resize</param>
        /// <param name="dpi">current dpi, normal is 96.</param>
        /// <param name="scaleModifier">A function which can modify the scale factor</param>
        /// <returns>NativePoint scaled</returns>
        public static NativePoint ScaleWithDpi(NativePoint size, uint dpi, Func<double, double> scaleModifier = null)
        {
            var dpiScaleFactor = DpiScaleFactor(dpi);
            if (scaleModifier != null)
            {
                dpiScaleFactor = scaleModifier(dpiScaleFactor);
            }
            return new NativePoint((int)(dpiScaleFactor * size.X), (int)(dpiScaleFactor * size.Y));
        }

        /// <summary>
        ///     Scale the supplied NativeSizeFloat according to the supplied dpi
        /// </summary>
        /// <param name="size">NativeSizeFloat to resize</param>
        /// <param name="dpi">current dpi, normal is 96.</param>
        /// <param name="scaleModifier">A function which can modify the scale factor</param>
        /// <returns>NativeSize scaled</returns>
        public static NativeSizeFloat ScaleWithDpi(NativeSizeFloat size, uint dpi, Func<double, double> scaleModifier = null)
        {
            var dpiScaleFactor = DpiScaleFactor(dpi);
            if (scaleModifier != null)
            {
                dpiScaleFactor = scaleModifier(dpiScaleFactor);
            }
            return new NativeSizeFloat(dpiScaleFactor * size.Width, dpiScaleFactor * size.Height);
        }

        /// <summary>
        ///     Scale the supplied number to the current dpi
        /// </summary>
        /// <param name="someNumber">double with e.g. a width like 16 for 16x16 images</param>
        /// <param name="scaleModifier">A function which can modify the scale factor</param>
        /// <returns>double with scaled number</returns>
        public double ScaleWithCurrentDpi(double someNumber, Func<double, double> scaleModifier = null)
        {
            return ScaleWithDpi(someNumber, Dpi, scaleModifier);
        }

        /// <summary>
        ///     Scale the supplied number to the current dpi
        /// </summary>
        /// <param name="someNumber">int with e.g. a width like 16 for 16x16 images</param>
        /// <param name="scaleModifier">A function which can modify the scale factor</param>
        /// <returns>int with scaled number</returns>
        public int ScaleWithCurrentDpi(int someNumber, Func<double, double> scaleModifier = null)
        {
            return ScaleWithDpi(someNumber, Dpi, scaleModifier);
        }

        /// <summary>
        ///     Scale the supplied NativeSize to the current dpi
        /// </summary>
        /// <param name="size">NativeSize to scale</param>
        /// <param name="scaleModifier">A function which can modify the scale factor</param>
        /// <returns>NativeSize scaled</returns>
        public NativeSize ScaleWithCurrentDpi(NativeSize size, Func<double, double> scaleModifier = null)
        {
            return ScaleWithDpi(size, Dpi, scaleModifier);
        }

        /// <summary>
        ///     Scale the supplied NativeSizeFloat to the current dpi
        /// </summary>
        /// <param name="size">NativeSizeFloat to scale</param>
        /// <param name="scaleModifier">A function which can modify the scale factor</param>
        /// <returns>NativeSizeFloat scaled</returns>
        public NativeSizeFloat ScaleWithCurrentDpi(NativeSizeFloat size, Func<double, double> scaleModifier = null)
        {
            return ScaleWithDpi(size, Dpi, scaleModifier);
        }

        /// <summary>
        ///     Scale the supplied NativePoint to the current dpi
        /// </summary>
        /// <param name="point">NativePoint to scale</param>
        /// <param name="scaleModifier">A function which can modify the scale factor</param>
        /// <returns>NativePoint scaled</returns>
        public NativePoint ScaleWithCurrentDpi(NativePoint point, Func<double, double> scaleModifier = null)
        {
            return ScaleWithDpi(point, Dpi, scaleModifier);
        }

        /// <summary>
        ///     Scale the supplied NativePointFloat to the current dpi
        /// </summary>
        /// <param name="point">NativePointFloat to scale</param>
        /// <param name="scaleModifier">A function which can modify the scale factor</param>
        /// <returns>NativePointFloat scaled</returns>
        public NativePointFloat ScaleWithCurrentDpi(NativePointFloat point, Func<double, double> scaleModifier = null)
        {
            return ScaleWithDpi(point, Dpi, scaleModifier);
        }
        #endregion

        #region unscale
        /// <summary>
        /// Calculate a DPI unscale factor
        /// </summary>
        /// <param name="dpi">uint</param>
        /// <returns>double</returns>
        public static double DpiUnscaleFactor(uint dpi)
        {
            return (double)DefaultScreenDpi / dpi;
        }

        /// <summary>
        ///     Unscale the supplied number according to the supplied dpi
        /// </summary>
        /// <param name="someNumber">double with e.g. the scaled width</param>
        /// <param name="dpi">current dpi, normal is 96.</param>
        /// <param name="scaleModifier">A function which can modify the scale factor</param>
        /// <returns>double with the unscaled number</returns>
        public static double UnscaleWithDpi(double someNumber, uint dpi, Func<double, double> scaleModifier = null)
        {
            var dpiUnscaleFactor = DpiUnscaleFactor(dpi);
            if (scaleModifier != null)
            {
                dpiUnscaleFactor = scaleModifier(dpiUnscaleFactor);
            }
            return dpiUnscaleFactor * someNumber;
        }

        /// <summary>
        ///    Unscale the supplied number according to the supplied dpi
        /// </summary>
        /// <param name="number">int with a scaled width</param>
        /// <param name="dpi">current dpi, normal is 96.</param>
        /// <param name="scaleModifier">A function which can modify the scale factor</param>
        /// <returns>Unscaled width</returns>
        public static int UnscaleWithDpi(int number, uint dpi, Func<double, double> scaleModifier = null)
        {
            var dpiUnscaleFactor = DpiUnscaleFactor(dpi);
            if (scaleModifier != null)
            {
                dpiUnscaleFactor = scaleModifier(dpiUnscaleFactor);
            }
            return (int)(dpiUnscaleFactor * number);
        }

        /// <summary>
        ///     Unscale the supplied NativeSize according to the supplied dpi
        /// </summary>
        /// <param name="size">NativeSize to unscale</param>
        /// <param name="dpi">current dpi, normal is 96.</param>
        /// <param name="scaleModifier">A function which can modify the scale factor</param>
        /// <returns>NativeSize unscaled</returns>
        public static NativeSize UnscaleWithDpi(NativeSize size, uint dpi, Func<double, double> scaleModifier = null)
        {
            var dpiUnscaleFactor = DpiUnscaleFactor(dpi);
            if (scaleModifier != null)
            {
                dpiUnscaleFactor = scaleModifier(dpiUnscaleFactor);
            }
            return new NativeSize((int)(dpiUnscaleFactor * size.Width), (int)(dpiUnscaleFactor * size.Height));
        }

        /// <summary>
        ///     Unscale the supplied NativePoint according to the supplied dpi
        /// </summary>
        /// <param name="size">NativePoint to unscale</param>
        /// <param name="dpi">current dpi, normal is 96.</param>
        /// <param name="scaleModifier">A function which can modify the scale factor</param>
        /// <returns>NativePoint unscaled</returns>
        public static NativePoint UnscaleWithDpi(NativePoint size, uint dpi, Func<double, double> scaleModifier = null)
        {
            var dpiUnscaleFactor = DpiUnscaleFactor(dpi);
            if (scaleModifier != null)
            {
                dpiUnscaleFactor = scaleModifier(dpiUnscaleFactor);
            }
            return new NativePoint((int)(dpiUnscaleFactor * size.X), (int)(dpiUnscaleFactor * size.Y));
        }

        /// <summary>
        ///     unscale the supplied NativeSizeFloat according to the supplied dpi
        /// </summary>
        /// <param name="size">NativeSizeFloat to resize</param>
        /// <param name="dpi">current dpi, normal is 96.</param>
        /// <param name="scaleModifier">A function which can modify the scale factor</param>
        /// <returns>NativeSize unscaled</returns>
        public static NativeSizeFloat UnscaleWithDpi(NativeSizeFloat size, uint dpi, Func<double, double> scaleModifier = null)
        {
            var dpiUnscaleFactor = DpiUnscaleFactor(dpi);
            if (scaleModifier != null)
            {
                dpiUnscaleFactor = scaleModifier(dpiUnscaleFactor);
            }
            return new NativeSizeFloat(dpiUnscaleFactor * size.Width, dpiUnscaleFactor * size.Height);
        }

        /// <summary>
        ///     Unscale the supplied number to the current dpi
        /// </summary>
        /// <param name="someNumber">double with e.g. a width like 16 for 16x16 images</param>
        /// <param name="scaleModifier">A function which can modify the scale factor</param>
        /// <returns>double with unscaled number</returns>
        public double UnscaleWithCurrentDpi(double someNumber, Func<double, double> scaleModifier = null)
        {
            return UnscaleWithDpi(someNumber, Dpi, scaleModifier);
        }

        /// <summary>
        ///     Unscale the supplied number to the current dpi
        /// </summary>
        /// <param name="someNumber">int with e.g. a width like 16 for 16x16 images</param>
        /// <param name="scaleModifier">A function which can modify the scale factor</param>
        /// <returns>int with unscaled number</returns>
        public int UnscaleWithCurrentDpi(int someNumber, Func<double, double> scaleModifier = null)
        {
            return UnscaleWithDpi(someNumber, Dpi, scaleModifier);
        }

        /// <summary>
        ///     Unscale the supplied NativeSize to the current dpi
        /// </summary>
        /// <param name="size">NativeSize to unscale</param>
        /// <param name="scaleModifier">A function which can modify the scale factor</param>
        /// <returns>NativeSize unscaled</returns>
        public NativeSize UnscaleWithCurrentDpi(NativeSize size, Func<double, double> scaleModifier = null)
        {
            return UnscaleWithDpi(size, Dpi, scaleModifier);
        }

        /// <summary>
        ///     Unscale the supplied NativeSizeFloat to the current dpi
        /// </summary>
        /// <param name="size">NativeSizeFloat to unscale</param>
        /// <param name="scaleModifier">A function which can modify the scale factor</param>
        /// <returns>NativeSizeFloat unscaled</returns>
        public NativeSizeFloat UnscaleWithCurrentDpi(NativeSizeFloat size, Func<double, double> scaleModifier = null)
        {
            return UnscaleWithDpi(size, Dpi, scaleModifier);
        }

        /// <summary>
        ///     Unscale the supplied NativePoint to the current dpi
        /// </summary>
        /// <param name="point">NativePoint to unscale</param>
        /// <param name="scaleModifier">A function which can modify the scale factor</param>
        /// <returns>NativePoint unscaled</returns>
        public NativePoint UnscaleWithCurrentDpi(NativePoint point, Func<double, double> scaleModifier = null)
        {
            return UnscaleWithDpi(point, Dpi, scaleModifier);
        }

        /// <summary>
        ///     Unscale the supplied NativePointFloat to the current dpi
        /// </summary>
        /// <param name="point">NativePointFloat to unscale</param>
        /// <param name="scaleModifier">A function which can modify the scale factor</param>
        /// <returns>NativePointFloat unscaled</returns>
        public NativePointFloat UnscaleWithCurrentDpi(NativePointFloat point, Func<double, double> scaleModifier = null)
        {
            return ScaleWithDpi(point, Dpi, scaleModifier);
        }
        #endregion

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

            var result = NativeDpiMethods.EnableNonClientDpiScaling(hWnd);
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

        /// <inheritdoc />
        public void Dispose()
        {
            MessageHandler?.Dispose();
            MessageHandler = null;
        }
    }
}