// Copyright (c) Dapplo and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Linq;
using Dapplo.Windows.Common.Extensions;
using Dapplo.Windows.Common.Structs;
using System.Reactive.Linq;
using System.Threading;
using Dapplo.Windows.Messages;
using Dapplo.Windows.Messages.Enumerations;

namespace Dapplo.Windows.User32;

/// <summary>
///     The DisplayInfo class is like the Screen class, only not cached.
/// </summary>
public class DisplayInfo
{
    /// <summary>
    /// The cached displays and the bounds calculated from them, replaced as a whole so readers always see a consistent snapshot
    /// </summary>
    private sealed class DisplayCache
    {
        public DisplayCache(DisplayInfo[] displayInfos)
        {
            DisplayInfos = displayInfos;
            int left = 0, top = 0, bottom = 0, right = 0;
            foreach (var display in displayInfos)
            {
                var currentBounds = display.Bounds;
                left = Math.Min(left, currentBounds.X);
                top = Math.Min(top, currentBounds.Y);
                var screenAbsRight = currentBounds.X + currentBounds.Width;
                var screenAbsBottom = currentBounds.Y + currentBounds.Height;
                right = Math.Max(right, screenAbsRight);
                bottom = Math.Max(bottom, screenAbsBottom);
            }
            ScreenBounds = new NativeRect(left, top, right + Math.Abs(left), bottom + Math.Abs(top));
        }

        public DisplayInfo[] DisplayInfos { get; }

        public NativeRect ScreenBounds { get; }
    }

    private static DisplayCache _cache;
    private static int _cacheVersion;
    private static int _isListening;

    /// <summary>
    ///     Desktop working area
    /// </summary>
    public IntPtr MonitorHandle { get; set; }


    /// <summary>
    /// Index of the Display, as specified in the "control panel".
    /// </summary>
    public int? Index { get; set; }

    /// <summary>
    ///     Screen bounds
    /// </summary>
    public NativeRect Bounds { get; set; }

    /// <summary>
    ///     Device name
    /// </summary>
    public string DeviceName { get; set; }

    /// <summary>
    ///     Is this the primary monitor
    /// </summary>
    public bool IsPrimary { get; set; }

    /// <summary>
    ///     Height of  the screen
    /// </summary>
    public int ScreenHeight { get; set; }

    /// <summary>
    ///     Width of the screen
    /// </summary>
    public int ScreenWidth { get; set; }

    /// <summary>
    ///     Desktop working area
    /// </summary>
    public NativeRect WorkingArea { get; set; }

    /// <summary>
    /// Get the bounds of the complete screen
    /// </summary>
    public static NativeRect ScreenBounds => GetCache().ScreenBounds;

    /// <summary>
    ///     Return all DisplayInfo, this is never null.
    /// </summary>
    /// <remarks>
    ///     The information is cached, the cache is invalidated when the display configuration, the work area (e.g. taskbar moved or auto-hidden)
    ///     or the DPI changes. For this, the first call starts listening to the messages of the <see cref="SharedMessageWindow"/>.
    /// </remarks>
    /// <returns>array of DisplayInfo</returns>
    public static DisplayInfo[] AllDisplayInfos => GetCache().DisplayInfos;

    /// <summary>
    ///     Get the current cache, create it if there is none
    /// </summary>
    private static DisplayCache GetCache()
    {
        // Listen before enumerating the displays, so a change during the enumeration invalidates the result
        if (Interlocked.CompareExchange(ref _isListening, 1, 0) == 0)
        {
            try
            {
                SharedMessageWindow.Messages
                    .Where(m => m.Msg == WindowsMessages.WM_DISPLAYCHANGE || m.Msg == WindowsMessages.WM_SETTINGCHANGE || m.Msg == WindowsMessages.WM_DPICHANGED)
                    .Subscribe(_ =>
                    {
                        Interlocked.Increment(ref _cacheVersion);
                        Volatile.Write(ref _cache, null);
                    });
            }
            catch (Exception)
            {
                // Without the SharedMessageWindow the displays are not cached
                Volatile.Write(ref _isListening, 2);
            }
        }

        var cache = Volatile.Read(ref _cache);
        if (cache != null)
        {
            return cache;
        }
        var version = Volatile.Read(ref _cacheVersion);
        cache = new DisplayCache(User32Api.EnumDisplays()?.ToArray() ?? Array.Empty<DisplayInfo>());
        if (Volatile.Read(ref _isListening) == 1 && version == Volatile.Read(ref _cacheVersion))
        {
            Interlocked.CompareExchange(ref _cache, cache, null);
            if (version != Volatile.Read(ref _cacheVersion))
            {
                // Invalidated while storing, don't keep the possibly outdated information
                Interlocked.CompareExchange(ref _cache, null, cache);
            }
        }
        return cache;
    }

    /// <summary>
    ///     Implementation like <a href="https://msdn.microsoft.com/en-us/library/6d7ws9s4(v=vs.110).aspx">Screen.GetBounds</a>
    /// </summary>
    /// <param name="point">NativePoint</param>
    /// <returns>NativeRect</returns>
    public static NativeRect GetBounds(NativePoint point)
    {
        DisplayInfo returnValue = null;
        foreach (var display in AllDisplayInfos)
        {
            if (display.IsPrimary && returnValue == null)
            {
                returnValue = display;
            }
            if (display.Bounds.Contains(point))
            {
                returnValue = display;
            }
        }
        return returnValue?.Bounds ?? NativeRect.Empty;
    }
}