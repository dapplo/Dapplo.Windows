// Copyright (c) Dapplo and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
using System;
using System.Diagnostics;
using System.Linq;
using Dapplo.Windows.App;
using Dapplo.Windows.Desktop;
using Dapplo.Windows.Kernel32;
using Dapplo.Windows.User32;

namespace Dapplo.Windows.Icons;

/// <summary>
/// Icon extensions for the <see cref="IInteropWindow"/>, these live in Dapplo.Windows so Dapplo.Windows.Icons doesn't need to depend on Dapplo.Windows
/// </summary>
public static class InteropWindowIconExtensions
{
    /// <summary>
    ///     Get the icon for a hWnd
    /// </summary>
    /// <typeparam name="TIcon">The return type for the icon, can be Icon or Bitmap (Dapplo.Windows.Wpf has ToBitmapSource() to convert these to a WPF BitmapSource)</typeparam>
    /// <param name="window">IInteropWindow</param>
    /// <param name="useLargeIcons">true to try to get a big icon first</param>
    /// <returns>TIcon</returns>
    /// <exception cref="NotSupportedException">when TIcon is not Icon or Bitmap</exception>
    public static TIcon GetIcon<TIcon>(this IInteropWindow window, bool useLargeIcons = false) where TIcon : class
    {
        IconHelper.ThrowIfUnsupportedIconType<TIcon>();
        if (window.IsApp())
        {
            return window.GetAppLogo<TIcon>();
        }
        var icon = GetIconFromWindow<TIcon>(window, useLargeIcons);
        if (icon != null)
        {
            return icon;
        }
        var processId = window.GetProcessId();
        // Try to get the icon from the process file itself, if we can query the path
        var processPath = Kernel32Api.GetProcessPath(processId);
        if (processPath != null)
        {
            return IconHelper.ExtractAssociatedIcon<TIcon>(processPath, useLargeIcon: useLargeIcons);
        }
        // Look at the windows of the other similar named processes
        using (var process = Process.GetProcessById(processId))
        {
            var processName = process.ProcessName;
            var similarProcesses = Process.GetProcessesByName(processName);
            try
            {
                foreach (var possibleParentProcess in similarProcesses)
                {
                    var parentProcessWindow = InteropWindowFactory.CreateFor(possibleParentProcess.MainWindowHandle);
                    icon = GetIconFromWindow<TIcon>(parentProcessWindow, useLargeIcons);
                    if (icon != null)
                    {
                        return icon;
                    }
                }
            }
            finally
            {
                foreach (var similarProcess in similarProcesses)
                {
                    similarProcess.Dispose();
                }
            }
        }
        // Try to find another window, which belongs to the same process, and get the icon from there
        foreach(var otherWindow in InteropWindowQuery.GetTopWindows().Where(interopWindow => interopWindow.GetProcessId() == processId))
        {
            if (otherWindow.Handle == window.Handle)
            {
                continue;
            }
            icon = GetIconFromWindow<TIcon>(otherWindow, useLargeIcons);
            if (icon != null)
            {
                return icon;
            }

        }
        // Nothing found, REALLY!
        return default;
    }

    /// <summary>
    ///     Get the icon for an IInteropWindow
    /// </summary>
    /// <typeparam name="TIcon">The return type for the icon, can be Icon or Bitmap (Dapplo.Windows.Wpf has ToBitmapSource() to convert these to a WPF BitmapSource)</typeparam>
    /// <param name="window">IInteropWindow</param>
    /// <param name="useLargeIcons">true to try to get a big icon first</param>
    /// <returns>TIcon</returns>
    public static TIcon GetIconFromWindow<TIcon>(this IInteropWindow window, bool useLargeIcons = false) where TIcon : class
    {
        return IconExtensions.GetIconForWindowHandle<TIcon>(window.Handle, useLargeIcons);
    }

    /// <summary>
    /// Get the app logo from the AppxManifest of the modern (UWP) app which the window belongs to
    /// </summary>
    /// <typeparam name="TBitmap">Type for the Bitmap, only Bitmap is supported (Dapplo.Windows.Wpf has ToBitmapSource() to convert these to a WPF BitmapSource)</typeparam>
    /// <param name="interopWindow">IInteropWindow</param>
    /// <param name="scale">int with scale, 100 is default</param>
    /// <returns>instance of TBitmap or null if nothing found</returns>
    /// <exception cref="NotSupportedException">when TBitmap is not Icon or Bitmap</exception>
    public static TBitmap GetAppLogo<TBitmap>(this IInteropWindow interopWindow, int scale = 100) where TBitmap : class
    {
        IconHelper.ThrowIfUnsupportedIconType<TBitmap>();
        return IconHelper.GetAppLogo<TBitmap>(GetAppProcessPath(interopWindow), scale);
    }

    /// <summary>
    /// Get the path for the real modern app process belonging to the window
    /// </summary>
    /// <param name="interopWindow">IInteropWindow</param>
    /// <returns>string or null</returns>
    private static string GetAppProcessPath(IInteropWindow interopWindow)
    {
        User32Api.GetWindowThreadProcessId(interopWindow.Handle, out var pid);
        if (string.Equals(interopWindow.GetClassname(), AppQuery.AppFrameWindowClass))
        {
            // EnumChildWindows (GetChildren) doesn't return the CoreWindow, which belongs to the process of the app
            pid = interopWindow.GetCoreWindow()?.GetProcessId() ?? 0;
        }
        if (pid <= 0)
        {
            return null;
        }

        return Kernel32Api.GetProcessPath(pid);
    }
}
