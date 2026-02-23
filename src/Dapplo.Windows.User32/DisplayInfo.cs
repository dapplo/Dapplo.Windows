// Copyright (c) Dapplo and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Linq;
using Dapplo.Windows.Common.Extensions;
using Dapplo.Windows.Common.Structs;
using System.Reactive.Linq;
using Dapplo.Windows.Messages;

#if !NETSTANDARD2_0
using System.Threading;
using Dapplo.Windows.Messages.Enumerations;
#endif

namespace Dapplo.Windows.User32;

/// <summary>
///     The DisplayInfo class is like the Screen class, only not cached.
/// </summary>
public class DisplayInfo
{
    private static NativeRect? _screenBounds;
    private static DisplayInfo[] _allDisplayInfos;
#if !NETSTANDARD2_0
    private static IDisposable _displayInfoUpdate;
#endif

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
    public static NativeRect ScreenBounds
    {
        get
        {
            if (_screenBounds.HasValue)
            {
                return _screenBounds.Value;
            }

            int left = 0, top = 0, bottom = 0, right = 0;
            foreach (var display in AllDisplayInfos)
            {
                var currentBounds = display.Bounds;
                left = Math.Min(left, currentBounds.X);
                top = Math.Min(top, currentBounds.Y);
                var screenAbsRight = currentBounds.X + currentBounds.Width;
                var screenAbsBottom = currentBounds.Y + currentBounds.Height;
                right = Math.Max(right, screenAbsRight);
                bottom = Math.Max(bottom, screenAbsBottom);
            }
            _screenBounds = new NativeRect(left, top, right + Math.Abs(left), bottom + Math.Abs(top));

            return _screenBounds.Value;
        }
    }

    /// <summary>
    ///     Return all DisplayInfo
    /// </summary>
    /// <returns>array of DisplayInfo</returns>
    public static DisplayInfo[] AllDisplayInfos
    {
        get
        {
            if (_allDisplayInfos != null)
            {
                return _allDisplayInfos;
            }

            _allDisplayInfos = User32Api.EnumDisplays()?.ToArray();

#if !NETSTANDARD2_0

            // Subscribe to display changes, but only when we didn't yet
            _displayInfoUpdate ??= SharedMessageWindow.Listen().Where(m => m.Msg == WindowsMessages.WM_DISPLAYCHANGE)
                        .Subscribe(m =>
                        {
                            _allDisplayInfos = null;
                            _screenBounds = null;
                        });
#endif
            return _allDisplayInfos;
        }
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