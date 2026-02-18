// Copyright (c) Dapplo and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using Dapplo.Windows.Common.Structs;
using Dapplo.Windows.User32.Enums;

namespace Dapplo.Windows.Dpi;

/// <summary>
/// Provides convenient wrapper methods for DPI-aware Windows API calls
/// </summary>
public static class DpiApi
{
    /// <summary>
    /// Retrieves the value of one of the system metrics, taking into account the provided DPI value.
    /// This is a convenient wrapper around the GetSystemMetricsForDpi Win32 API.
    /// </summary>
    /// <param name="metric">The system metric or configuration setting to be retrieved.</param>
    /// <param name="dpi">The DPI to use for scaling the metric. If not provided, uses the system DPI.</param>
    /// <returns>The requested system metric or configuration setting scaled for the specified DPI.</returns>
    public static int GetSystemMetrics(SystemMetric metric, uint? dpi = null)
    {
        var effectiveDpi = dpi ?? NativeDpiMethods.GetDpiForSystem();
        return NativeDpiMethods.GetSystemMetricsForDpi(metric, effectiveDpi);
    }

    /// <summary>
    /// Retrieves the value of one of the system metrics for a specific window, taking into account the window's DPI.
    /// This is a convenient wrapper around the GetSystemMetricsForDpi Win32 API.
    /// </summary>
    /// <param name="metric">The system metric or configuration setting to be retrieved.</param>
    /// <param name="hWnd">Handle to the window. The DPI of this window will be used for scaling.</param>
    /// <returns>The requested system metric or configuration setting scaled for the window's DPI.</returns>
    public static int GetSystemMetricsForWindow(SystemMetric metric, IntPtr hWnd)
    {
        var dpi = (uint)NativeDpiMethods.GetDpi(hWnd);
        return NativeDpiMethods.GetSystemMetricsForDpi(metric, dpi);
    }

    /// <summary>
    /// Calculates the required size of the window rectangle, based on the desired size of the client rectangle and the provided DPI.
    /// This is a convenient wrapper around the AdjustWindowRectExForDpi Win32 API.
    /// </summary>
    /// <param name="clientRect">The desired client rectangle.</param>
    /// <param name="style">The window style of the window.</param>
    /// <param name="hasMenu">Indicates whether the window has a menu.</param>
    /// <param name="exStyle">The extended window style of the window.</param>
    /// <param name="dpi">The DPI to use for scaling. If not provided, uses the system DPI.</param>
    /// <returns>The calculated window rectangle, or null if the function fails.</returns>
    public static NativeRect? AdjustWindowRect(NativeRect clientRect, WindowStyleFlags style, bool hasMenu = false, ExtendedWindowStyleFlags exStyle = ExtendedWindowStyleFlags.WS_NONE, uint? dpi = null)
    {
        var effectiveDpi = dpi ?? NativeDpiMethods.GetDpiForSystem();
        var rect = clientRect;
        if (NativeDpiMethods.AdjustWindowRectExForDpi(ref rect, style, hasMenu, exStyle, effectiveDpi))
        {
            return rect;
        }
        return null;
    }

    /// <summary>
    /// Calculates the required size of the window rectangle for a specific window, based on the desired size of the client rectangle and the window's DPI.
    /// This is a convenient wrapper around the AdjustWindowRectExForDpi Win32 API.
    /// </summary>
    /// <param name="clientRect">The desired client rectangle.</param>
    /// <param name="style">The window style of the window.</param>
    /// <param name="hasMenu">Indicates whether the window has a menu.</param>
    /// <param name="exStyle">The extended window style of the window.</param>
    /// <param name="hWnd">Handle to the window. The DPI of this window will be used for scaling.</param>
    /// <returns>The calculated window rectangle, or null if the function fails.</returns>
    public static NativeRect? AdjustWindowRectForWindow(NativeRect clientRect, WindowStyleFlags style, bool hasMenu, ExtendedWindowStyleFlags exStyle, IntPtr hWnd)
    {
        var dpi = (uint)NativeDpiMethods.GetDpi(hWnd);
        var rect = clientRect;
        if (NativeDpiMethods.AdjustWindowRectExForDpi(ref rect, style, hasMenu, exStyle, dpi))
        {
            return rect;
        }
        return null;
    }

    /// <summary>
    /// Retrieves system parameters information for a specific DPI.
    /// This is a convenient wrapper around the SystemParametersInfoForDpi Win32 API.
    /// </summary>
    /// <typeparam name="T">The type of the parameter structure.</typeparam>
    /// <param name="action">The system parameter to query.</param>
    /// <param name="dpi">The DPI to use for scaling. If not provided, uses the system DPI.</param>
    /// <returns>The requested system parameter, or null if the function fails.</returns>
    public static T? GetSystemParametersInfo<T>(SystemParametersInfoActions action, uint? dpi = null) where T : struct
    {
        var effectiveDpi = dpi ?? NativeDpiMethods.GetDpiForSystem();
        return GetSystemParametersInfoInternal<T>(action, effectiveDpi);
    }

    /// <summary>
    /// Retrieves system parameters information for a specific window's DPI.
    /// This is a convenient wrapper around the SystemParametersInfoForDpi Win32 API.
    /// </summary>
    /// <typeparam name="T">The type of the parameter structure.</typeparam>
    /// <param name="action">The system parameter to query.</param>
    /// <param name="hWnd">Handle to the window. The DPI of this window will be used for scaling.</param>
    /// <returns>The requested system parameter, or null if the function fails.</returns>
    public static T? GetSystemParametersInfoForWindow<T>(SystemParametersInfoActions action, IntPtr hWnd) where T : struct
    {
        var dpi = (uint)NativeDpiMethods.GetDpi(hWnd);
        return GetSystemParametersInfoInternal<T>(action, dpi);
    }

    /// <summary>
    /// Internal helper method to retrieve system parameters information.
    /// </summary>
    private static T? GetSystemParametersInfoInternal<T>(SystemParametersInfoActions action, uint dpi) where T : struct
    {
        var size = System.Runtime.InteropServices.Marshal.SizeOf<T>();
        var ptr = System.Runtime.InteropServices.Marshal.AllocHGlobal(size);
        try
        {
            if (NativeDpiMethods.SystemParametersInfoForDpi(action, (uint)size, ptr, SystemParametersInfoBehaviors.None, dpi))
            {
                return System.Runtime.InteropServices.Marshal.PtrToStructure<T>(ptr);
            }
            return null;
        }
        finally
        {
            System.Runtime.InteropServices.Marshal.FreeHGlobal(ptr);
        }
    }
}
