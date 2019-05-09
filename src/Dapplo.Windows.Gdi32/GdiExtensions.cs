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

using System.Drawing;
using Dapplo.Windows.Common.Structs;
using Dapplo.Windows.Gdi32.SafeHandles;

#endregion

namespace Dapplo.Windows.Gdi32
{
    /// <summary>
    ///     Some extensions for GDI stuff
    /// </summary>
    public static class GdiExtensions
    {
        /// <summary>
        ///     Check if all the corners of the rectangle are visible in the specified region.
        ///     Not a perfect check, but this currently a workaround for checking if a window is completely visible
        /// </summary>
        /// <param name="region"></param>
        /// <param name="rectangle"></param>
        /// <returns></returns>
        public static bool AreRectangleCornersVisisble(this Region region, NativeRect rectangle)
        {
            var topLeft = new Point(rectangle.X, rectangle.Y);
            var topRight = new Point(rectangle.X + rectangle.Width, rectangle.Y);
            var bottomLeft = new Point(rectangle.X, rectangle.Y + rectangle.Height);
            var bottomRight = new Point(rectangle.X + rectangle.Width, rectangle.Y + rectangle.Height);
            var topLeftVisible = region.IsVisible(topLeft);
            var topRightVisible = region.IsVisible(topRight);
            var bottomLeftVisible = region.IsVisible(bottomLeft);
            var bottomRightVisible = region.IsVisible(bottomRight);

            return topLeftVisible && topRightVisible && bottomLeftVisible && bottomRightVisible;
        }

        /// <summary>
        ///     Get a SafeHandle for the GetHdc, so one can use using to automatically cleanup the devicecontext
        /// </summary>
        /// <param name="graphics">Graphics</param>
        /// <returns>SafeGraphicsDcHandle</returns>
        public static SafeGraphicsDcHandle GetSafeDeviceContext(this Graphics graphics)
        {
            return SafeGraphicsDcHandle.FromGraphics(graphics);
        }

        /// <summary>
        ///     Get a SafeHBitmapHandle so one can use using to automatically cleanup the HBitmap
        /// </summary>
        /// <param name="bitmap">Bitmap</param>
        /// <returns>SafeHBitmapHandle</returns>
        public static SafeHBitmapHandle GetSafeHBitmapHandle(this Bitmap bitmap)
        {
            return new SafeHBitmapHandle(bitmap);
        }
    }
}