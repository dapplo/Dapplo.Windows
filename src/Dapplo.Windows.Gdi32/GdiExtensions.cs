// Copyright (c) Dapplo and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
using System.Drawing;
using Dapplo.Windows.Common.Structs;
using Dapplo.Windows.Gdi32.SafeHandles;

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