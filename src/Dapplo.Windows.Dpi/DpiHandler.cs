// Copyright (c) Dapplo and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Reactive.Subjects;
using System.Runtime.InteropServices;
using Dapplo.Log;
using Dapplo.Windows.Common;
using Dapplo.Windows.Common.Extensions;
using Dapplo.Windows.Common.Structs;
using Dapplo.Windows.Messages;
using Dapplo.Windows.Messages.Enums;
using Dapplo.Windows.User32;
using Dapplo.Windows.User32.Enums;

namespace Dapplo.Windows.Dpi;

/// <summary>
///     This handles DPI changes see 
///     <a href="https://msdn.microsoft.com/en-us/library/windows/desktop/dn469266.aspx">Writing DPI-Aware Desktop and Win32 Applications</a>
/// </summary>
public sealed class DpiHandler : IDisposable
{
    private static readonly LogSource Log = new LogSource();

    // Stores if the handler is running via a listener
    private bool _needsListenerWorkaround;

    // Via this the dpi values are published in details
    private readonly Subject<DpiChangeInfo> _onDpiChanged = new Subject<DpiChangeInfo>();

    // The DPI of the UI element, 0 as long as it's not known
    private int _dpi;

    private bool _isDisposed;

    /// <summary>
    ///     Create a DpiHandler.
    ///     This does not change the DPI awareness context of the thread, the DPI awareness of a window is decided when it's created.
    ///     Use the process DPI awareness (manifest or NativeDpiMethods.EnableDpiAware), or wrap the window creation with NativeDpiMethods.ScopedThreadDpiAwarenessContext (DpiAwareForm does this for you).
    /// </summary>
    public DpiHandler(bool needsListenerWorkaround = false)
    {
        _needsListenerWorkaround = needsListenerWorkaround;
    }

    /// <summary>
    ///     Retrieve the current DPI for the UI element which is related to this DpiHandler.
    ///     As long as the DPI is not known yet (see <see cref="IsDpiKnown"/>), this returns <see cref="DpiCalculator.DefaultScreenDpi"/>.
    /// </summary>
    public int Dpi => _dpi == 0 ? DpiCalculator.DefaultScreenDpi : _dpi;

    /// <summary>
    ///     True when the DPI of the UI element was determined (e.g. after WM_CREATE), before that <see cref="Dpi"/> returns the default of 96.
    /// </summary>
    public bool IsDpiKnown => _dpi != 0;

    /// <summary>
    ///     Specifies if the DpiHandler moves and resizes the window to the rectangle which Windows suggests with WM_DPICHANGED (default true).
    ///     When true, the WM_DPICHANGED message is reported as handled.
    ///     Set this to false when the UI framework applies the suggested rectangle itself, e.g. WinForms or WPF with Per-Monitor (v2) DPI support enabled.
    /// </summary>
    public bool ApplySuggestedWindowRect { get; set; } = true;

    /// <summary>
    ///     The subscription which feeds the window messages to this DpiHandler, e.g. set by the AttachDpiHandler extensions in Dapplo.Windows.Forms or Dapplo.Windows.Wpf.
    ///     It is disposed together with this DpiHandler.
    /// </summary>
    public IDisposable MessageHandler { get; set; }

    /// <summary>
    ///     This publishes whenever the DPI changes, with some details.
    ///     The first notification is published when the DPI is determined for the first time, the <see cref="DpiChangeInfo.PreviousDpi"/> is 0 in that case.
    ///     The sequence completes when this DpiHandler is disposed, a recreate of the window handle does not complete it.
    /// </summary>
    public IObservable<DpiChangeInfo> OnDpiChanged => _onDpiChanged;

    /// <summary>
    ///     Message handler of the Per_Monitor_DPI_Aware window.
    ///     The handles the WM_DPICHANGED message and adjusts window size, graphics and text based on the DPI of the monitor.
    ///     The window message provides the new window size (lparam) and new DPI (wparam)
    ///     See
    ///     <a href="https://msdn.microsoft.com/en-us/library/windows/desktop/dn312083(v=vs.85).aspx">WM_DPICHANGED message</a>
    /// </summary>
    /// <param name="hWnd">IntPtr with the hWnd</param>
    /// <param name="msg">The Windows message</param>
    /// <param name="wParam">IntPtr</param>
    /// <param name="lParam">IntPtr</param>
    /// <param name="handled">ref bool</param>
    /// <returns>IntPtr</returns>
    internal IntPtr HandleWindowMessages(IntPtr hWnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
    {
        if (HandleWindowMessages(new WindowMessage(hWnd, (WindowsMessages)msg, wParam, lParam)))
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
    /// <param name="windowMessage">WindowMessage</param>
    /// <returns>bool true if the message was handled</returns>
    public bool HandleWindowMessages(WindowMessage windowMessage)
    {
        bool handled = false;
        var currentDpi = DpiCalculator.DefaultScreenDpi;
        bool isDpiMessage = false;
        switch (windowMessage.Msg)
        {
            // Handle the WM_NCCREATE for Forms / controls, for WPF this is done differently
            case WindowsMessages.WM_NCCREATE:
                if (Log.IsVerboseEnabled())
                {
                    Log.Verbose().WriteLine("Processing {0} event, enabling DPI scaling for window {1}", windowMessage.Msg, windowMessage.Hwnd);
                }

                TryEnableNonClientDpiScaling(windowMessage.Hwnd);
                break;
            // Handle the WM_CREATE, this is where we can get the DPI via system calls
            case WindowsMessages.WM_CREATE:
                isDpiMessage = true;
                if (Log.IsVerboseEnabled())
                {
                    Log.Verbose().WriteLine("Processing {0} event, retrieving DPI for window {1}", windowMessage.Msg, windowMessage.Hwnd);
                }

                currentDpi = NativeDpiMethods.GetDpi(windowMessage.Hwnd);
                break;
            // Handle the DPI change message, this is where it's supplied
            case WindowsMessages.WM_DPICHANGED:
                isDpiMessage = true;
                if (Log.IsVerboseEnabled())
                {
                    Log.Verbose().WriteLine("Processing {0} event, resizing / positioning window {1}", windowMessage.Msg, windowMessage.Hwnd);
                }

                if (ApplySuggestedWindowRect)
                {
                    ApplySuggestedRect(windowMessage.Hwnd, windowMessage.LParam);
                    // specify that the message was handled
                    handled = true;
                }
                currentDpi = (int)windowMessage.WParam & 0xFFFF;
                break;
            case WindowsMessages.WM_PAINT:
                // This is a workaround for non DPI aware applications, these don't seem to get a WM_CREATE
                if (_dpi == 0)
                {
                    isDpiMessage = true;
                    currentDpi = NativeDpiMethods.GetDpi(windowMessage.Hwnd);
                }
                break;
            case WindowsMessages.WM_SETICON:
                // This is a workaround for handling WinProc outside of the class
                if (_needsListenerWorkaround)
                {
                    isDpiMessage = true;
                    // disable workaround
                    _needsListenerWorkaround = false;
                    currentDpi = NativeDpiMethods.GetDpi(windowMessage.Hwnd);
                }

                break;
            case WindowsMessages.WM_DPICHANGED_BEFOREPARENT:
                if (Log.IsVerboseEnabled())
                {
                    Log.Verbose().WriteLine("Dpi changed on {0} before parent", windowMessage.Hwnd);
                }
                break;
            case WindowsMessages.WM_DPICHANGED_AFTERPARENT:
                if (Log.IsVerboseEnabled())
                {
                    Log.Verbose().WriteLine("Dpi changed on {0} after parent", windowMessage.Hwnd);
                }
                break;
            // Note: WM_DESTROY doesn't complete anything, WinForms destroys and recreates handles (RecreateHandle) and the new handle is processed like the first.
        }

        // Check if the DPI was changed, if so call the action (if any)
        if (isDpiMessage)
        {
            UpdateDpi(currentDpi);
        }

        return handled;
    }

    /// <summary>
    ///     Move and resize the window to the rectangle which Windows suggested in the WM_DPICHANGED message
    /// </summary>
    /// <param name="hWnd">IntPtr with the window handle</param>
    /// <param name="suggestedRectPtr">IntPtr, the lParam of the WM_DPICHANGED, which points to a RECT</param>
    public static void ApplySuggestedRect(IntPtr hWnd, IntPtr suggestedRectPtr)
    {
        if (suggestedRectPtr == IntPtr.Zero)
        {
            return;
        }
        // Retrieve the advised location
        ApplySuggestedRect(hWnd, Marshal.PtrToStructure<NativeRect>(suggestedRectPtr));
    }

    /// <summary>
    ///     Move and resize the window to the rectangle which Windows suggested in the WM_DPICHANGED message
    /// </summary>
    /// <param name="hWnd">IntPtr with the window handle</param>
    /// <param name="suggestedRect">NativeRect, copied from the lParam of the WM_DPICHANGED</param>
    public static void ApplySuggestedRect(IntPtr hWnd, NativeRect suggestedRect)
    {
        // Move the window to it's location, and resize
        User32Api.SetWindowPos(hWnd,
            IntPtr.Zero,
            suggestedRect.Left,
            suggestedRect.Top,
            suggestedRect.Width,
            suggestedRect.Height,
            WindowPos.SWP_NOZORDER | WindowPos.SWP_NOOWNERZORDER | WindowPos.SWP_NOACTIVATE);
    }

    /// <summary>
    ///     Read the DPI of the specified window, and publish it when it changed.
    ///     Use this when the DpiHandler is attached to a window which already exists, and will not see a WM_CREATE.
    /// </summary>
    /// <param name="hWnd">IntPtr with the window handle</param>
    public void RefreshDpi(IntPtr hWnd)
    {
        UpdateDpi(NativeDpiMethods.GetDpi(hWnd));
    }

    /// <summary>
    ///     Store the new DPI, and publish the change (if any)
    /// </summary>
    /// <param name="newDpi">int</param>
    private void UpdateDpi(int newDpi)
    {
        if (newDpi <= 0 || _dpi == newDpi)
        {
            if (Log.IsVerboseEnabled())
            {
                Log.Verbose().WriteLine("DPI was unchanged from {0}", Dpi);
            }
            return;
        }

        var beforeDpi = _dpi;
        if (Log.IsVerboseEnabled())
        {
            Log.Verbose().WriteLine("Changing DPI from {0} to {1}", beforeDpi, newDpi);
        }
        // Update the value before publishing, so subscribers see the new value
        _dpi = newDpi;
        if (!_isDisposed)
        {
            _onDpiChanged.OnNext(new DpiChangeInfo(beforeDpi, newDpi));
        }
    }


    /// <summary>
    ///     Message handler of the DPI Aware ContextMenuStrip, this is simplified compared to the normal
    ///     The handles the WM_DPICHANGED message and adjusts window size, graphics and text based on the DPI of the monitor.
    ///     The window message provides the new window size (lparam) and new DPI (wparam)
    ///     See
    ///     <a href="https://msdn.microsoft.com/en-us/library/windows/desktop/dn312083(v=vs.85).aspx">WM_DPICHANGED message</a>
    /// </summary>
    /// <param name="windowMessage">WindowMessage</param>
    /// <returns>IntPtr</returns>
    public IntPtr HandleContextMenuMessages(WindowMessage windowMessage)
    {
        var currentDpi = DpiCalculator.DefaultScreenDpi;
        bool isDpiMessage = false;
        switch (windowMessage.Msg)
        {
            // Handle the WM_CREATE, this is where we can get the DPI via system calls
            case WindowsMessages.WM_SHOWWINDOW:
                isDpiMessage = true;
                if (Log.IsVerboseEnabled())
                {
                    Log.Verbose().WriteLine("Processing {0} event, retrieving DPI for ContextMenuStrip {1}", windowMessage.Msg, windowMessage.Hwnd);
                }

                currentDpi = NativeDpiMethods.GetDpi(windowMessage.Hwnd);
                break;
        }

        // Check if the DPI was changed, if so call the action (if any)
        if (isDpiMessage)
        {
            UpdateDpi(currentDpi);
        }

        return IntPtr.Zero;
    }

    /// <summary>
    ///     Scale the supplied number to the current dpi
    /// </summary>
    /// <param name="someNumber">double with e.g. a width like 16 for 16x16 images</param>
    /// <param name="scaleModifier">A function which can modify the scale factor</param>
    /// <returns>double with scaled number</returns>
    public double ScaleWithCurrentDpi(double someNumber, Func<float, float> scaleModifier = null)
    {
        return DpiCalculator.ScaleWithDpi(someNumber, Dpi, scaleModifier);
    }

    /// <summary>
    ///     Scale the supplied number to the current dpi
    /// </summary>
    /// <param name="someNumber">int with e.g. a width like 16 for 16x16 images</param>
    /// <param name="scaleModifier">A function which can modify the scale factor</param>
    /// <returns>int with scaled number</returns>
    public int ScaleWithCurrentDpi(int someNumber, Func<float, float> scaleModifier = null)
    {
        return DpiCalculator.ScaleWithDpi(someNumber, Dpi, scaleModifier);
    }

    /// <summary>
    ///     Scale the supplied NativeSize to the current dpi
    /// </summary>
    /// <param name="size">NativeSize to scale</param>
    /// <param name="scaleModifier">A function which can modify the scale factor</param>
    /// <returns>NativeSize scaled</returns>
    public NativeSize ScaleWithCurrentDpi(NativeSize size, Func<float, float> scaleModifier = null)
    {
        return DpiCalculator.ScaleWithDpi(size, Dpi, scaleModifier);
    }

    /// <summary>
    ///     Scale the supplied NativeSizeFloat to the current dpi
    /// </summary>
    /// <param name="size">NativeSizeFloat to scale</param>
    /// <param name="scaleModifier">A function which can modify the scale factor</param>
    /// <returns>NativeSizeFloat scaled</returns>
    public NativeSizeFloat ScaleWithCurrentDpi(NativeSizeFloat size, Func<float, float> scaleModifier = null)
    {
        return DpiCalculator.ScaleWithDpi(size, Dpi, scaleModifier);
    }

    /// <summary>
    ///     Scale the supplied NativePoint to the current dpi
    /// </summary>
    /// <param name="point">NativePoint to scale</param>
    /// <param name="scaleModifier">A function which can modify the scale factor</param>
    /// <returns>NativePoint scaled</returns>
    public NativePoint ScaleWithCurrentDpi(NativePoint point, Func<float, float> scaleModifier = null)
    {
        return DpiCalculator.ScaleWithDpi(point, Dpi, scaleModifier);
    }

    /// <summary>
    ///     Scale the supplied NativePointFloat to the current dpi
    /// </summary>
    /// <param name="point">NativePointFloat to scale</param>
    /// <param name="scaleModifier">A function which can modify the scale factor</param>
    /// <returns>NativePointFloat scaled</returns>
    public NativePointFloat ScaleWithCurrentDpi(NativePointFloat point, Func<float, float> scaleModifier = null)
    {
        return DpiCalculator.ScaleWithDpi(point, Dpi, scaleModifier);
    }

    /// <summary>
    ///     Unscale the supplied number to the current dpi
    /// </summary>
    /// <param name="someNumber">double with e.g. a width like 16 for 16x16 images</param>
    /// <param name="scaleModifier">A function which can modify the scale factor</param>
    /// <returns>double with unscaled number</returns>
    public double UnscaleWithCurrentDpi(double someNumber, Func<float, float> scaleModifier = null)
    {
        return DpiCalculator.UnscaleWithDpi(someNumber, Dpi, scaleModifier);
    }

    /// <summary>
    ///     Unscale the supplied number to the current dpi
    /// </summary>
    /// <param name="someNumber">int with e.g. a width like 16 for 16x16 images</param>
    /// <param name="scaleModifier">A function which can modify the scale factor</param>
    /// <returns>int with unscaled number</returns>
    public int UnscaleWithCurrentDpi(int someNumber, Func<float, float> scaleModifier = null)
    {
        return DpiCalculator.UnscaleWithDpi(someNumber, Dpi, scaleModifier);
    }

    /// <summary>
    ///     Unscale the supplied NativeSize to the current dpi
    /// </summary>
    /// <param name="size">NativeSize to unscale</param>
    /// <param name="scaleModifier">A function which can modify the scale factor</param>
    /// <returns>NativeSize unscaled</returns>
    public NativeSize UnscaleWithCurrentDpi(NativeSize size, Func<float, float> scaleModifier = null)
    {
        return DpiCalculator.UnscaleWithDpi(size, Dpi, scaleModifier);
    }

    /// <summary>
    ///     Unscale the supplied NativeSizeFloat to the current dpi
    /// </summary>
    /// <param name="size">NativeSizeFloat to unscale</param>
    /// <param name="scaleModifier">A function which can modify the scale factor</param>
    /// <returns>NativeSizeFloat unscaled</returns>
    public NativeSizeFloat UnscaleWithCurrentDpi(NativeSizeFloat size, Func<float, float> scaleModifier = null)
    {
        return DpiCalculator.UnscaleWithDpi(size, Dpi, scaleModifier);
    }

    /// <summary>
    ///     Unscale the supplied NativePoint to the current dpi
    /// </summary>
    /// <param name="point">NativePoint to unscale</param>
    /// <param name="scaleModifier">A function which can modify the scale factor</param>
    /// <returns>NativePoint unscaled</returns>
    public NativePoint UnscaleWithCurrentDpi(NativePoint point, Func<float, float> scaleModifier = null)
    {
        return DpiCalculator.UnscaleWithDpi(point, Dpi, scaleModifier);
    }

    /// <summary>
    ///     Unscale the supplied NativePointFloat to the current dpi
    /// </summary>
    /// <param name="point">NativePointFloat to unscale</param>
    /// <param name="scaleModifier">A function which can modify the scale factor</param>
    /// <returns>NativePointFloat unscaled</returns>
    public NativePointFloat UnscaleWithCurrentDpi(NativePointFloat point, Func<float, float> scaleModifier = null)
    {
        return DpiCalculator.UnscaleWithDpi(point, Dpi, scaleModifier);
    }

    /// <summary>
    /// public wrapper for EnableNonClientDpiScaling, this also checks if the function is available.
    /// </summary>
    /// <param name="hWnd">IntPtr</param>
    /// <returns>true if it worked</returns>
    public static bool TryEnableNonClientDpiScaling(IntPtr hWnd)
    {
        // EnableNonClientDpiScaling is only available on Windows 10 1607 (build 14393) and later
        if (!WindowsVersion.IsWindows10BuildOrLater(14393))
        {
            return false;
        }

        if (NativeDpiMethods.EnableNonClientDpiScaling(hWnd))
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

    /// <summary>
    ///     Stops the message processing (disposes the <see cref="MessageHandler"/>) and completes <see cref="OnDpiChanged"/>.
    /// </summary>
    public void Dispose()
    {
        if (_isDisposed)
        {
            return;
        }
        _isDisposed = true;
        MessageHandler?.Dispose();
        MessageHandler = null;
        _onDpiChanged.OnCompleted();
    }
}