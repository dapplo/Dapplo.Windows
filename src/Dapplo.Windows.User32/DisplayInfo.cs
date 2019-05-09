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
using System.Linq;
using System.Threading;
using Dapplo.Windows.Common.Extensions;
using Dapplo.Windows.Common.Structs;
using Dapplo.Windows.Messages;

#endregion

namespace Dapplo.Windows.User32
{
    /// <summary>
    ///     The DisplayInfo class is like the Screen class, only not cached.
    /// </summary>
    public class DisplayInfo
    {
        private static NativeRect? _screenBounds;
        private static DisplayInfo[] _allDisplayInfos;
        private static IDisposable _displayInfoUpdate;

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

                _allDisplayInfos = User32Api.EnumDisplays().ToArray();

#if !NETSTANDARD2_0

                IntPtr WinProcDisplayChangeHandler(IntPtr hWnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
                {
                    var windowsMessage = (WindowsMessages) msg;
                    if (windowsMessage != WindowsMessages.WM_DISPLAYCHANGE)
                    {
                        return IntPtr.Zero;
                    }

                    _allDisplayInfos = null;
                    _screenBounds = null;

                    return IntPtr.Zero;
                }

                // Subscribe to display changes, but only when we didn't yet
                if (_displayInfoUpdate == null && Thread.CurrentThread.GetApartmentState() == ApartmentState.STA)
                {
                    _displayInfoUpdate = WinProcHandler.Instance.Subscribe(WinProcDisplayChangeHandler);
                }
#endif
                return _allDisplayInfos;
            }
        }

        /// <summary>
        ///     Implementation like <a href="https://msdn.microsoft.com/en-us/library/6d7ws9s4(v=vs.110).aspx">Screen.GetBounds</a>
        /// </summary>
        /// <param name="point"></param>
        /// <returns></returns>
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
}