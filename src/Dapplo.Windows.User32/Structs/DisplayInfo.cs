//  Dapplo - building blocks for desktop applications
//  Copyright (C) 2016-2017 Dapplo
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
using System.Drawing;
using System.Windows;
using Dapplo.Windows.Common.Extensions;
using Dapplo.Windows.Common.Structs;

#endregion

namespace Dapplo.Windows.User32.Structs
{
    /// <summary>
    ///     The DisplayInfo class is like the Screen class, only not cached.
    /// </summary>
    public class DisplayInfo
    {
        /// <summary>
        ///     Screen bounds
        /// </summary>
        public RECT Bounds { get; set; }

        /// <summary>
        ///     Bounds as Rectangle
        /// </summary>
        public RECT BoundsRectangle { get; set; }

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
        public RECT WorkingArea { get; set; }

        /// <summary>
        ///     Desktop working area as Rectangle
        /// </summary>
        public Rectangle WorkingAreaRectangle { get; set; }

        /// <summary>
        ///     Get the bounds of the complete screen
        /// </summary>
        /// <returns></returns>
        public static RECT GetAllScreenBounds()
        {
            int left = 0, top = 0, bottom = 0, right = 0;
            foreach (var display in User32Api.AllDisplays())
            {
                left = Math.Min(left, display.Bounds.X);
                top = Math.Min(top, display.Bounds.Y);
                var screenAbsRight = display.Bounds.X + display.Bounds.Width;
                var screenAbsBottom = display.Bounds.Y + display.Bounds.Height;
                right = Math.Max(right, screenAbsRight);
                bottom = Math.Max(bottom, screenAbsBottom);
            }
            return new RECT(left, top, right + Math.Abs(left), bottom + Math.Abs(top));
        }

        /// <summary>
        ///     Implementation like <a href="https://msdn.microsoft.com/en-us/library/6d7ws9s4(v=vs.110).aspx">Screen.GetBounds</a>
        /// </summary>
        /// <param name="point"></param>
        /// <returns></returns>
        public static RECT GetBounds(POINT point)
        {
            DisplayInfo returnValue = null;
            foreach (var display in User32Api.AllDisplays())
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
            return returnValue?.Bounds ?? Rect.Empty;
        }
    }
}