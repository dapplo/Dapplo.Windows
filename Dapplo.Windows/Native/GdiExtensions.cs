/*
 * dapplo - building blocks for desktop applications
 * Copyright (C) Dapplo 2015-2016
 * 
 * For more information see: http://dapplo.net/
 * dapplo repositories are hosted on GitHub: https://github.com/dapplo
 * 
 * This program is free software: you can redistribute it and/or modify
 * it under the terms of the GNU General Public License as published by
 * the Free Software Foundation, either version 1 of the License, or
 * (at your option) any later version.
 * 
 * This program is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 * GNU General Public License for more details.
 * 
 * You should have received a copy of the GNU General Public License
 * along with this program. If not, see <http://www.gnu.org/licenses/>.
 */

using Dapplo.Windows.SafeHandles;
using System.Drawing;

namespace Dapplo.Windows.Native
{
	public static class GdiExtensions
	{
		/// <summary>
		/// Check if all the corners of the rectangle are visible in the specified region.
		/// Not a perfect check, but this currently a workaround for checking if a window is completely visible
		/// </summary>
		/// <param name="region"></param>
		/// <param name="rectangle"></param>
		/// <returns></returns>
		public static bool AreRectangleCornersVisisble(this Region region, Rectangle rectangle)
		{
			Point topLeft = new Point(rectangle.X, rectangle.Y);
			Point topRight = new Point(rectangle.X + rectangle.Width, rectangle.Y);
			Point bottomLeft = new Point(rectangle.X, rectangle.Y + rectangle.Height);
			Point bottomRight = new Point(rectangle.X + rectangle.Width, rectangle.Y + rectangle.Height);
			bool topLeftVisible = region.IsVisible(topLeft);
			bool topRightVisible = region.IsVisible(topRight);
			bool bottomLeftVisible = region.IsVisible(bottomLeft);
			bool bottomRightVisible = region.IsVisible(bottomRight);

			return topLeftVisible && topRightVisible && bottomLeftVisible && bottomRightVisible;
		}

		/// <summary>
		/// Get a SafeHandle for the GetHdc, so one can use using to automatically cleanup the devicecontext
		/// </summary>
		/// <param name="graphics"></param>
		/// <returns>SafeDeviceContextHandle</returns>
		public static SafeDeviceContextHandle GetSafeDeviceContext(this Graphics graphics)
		{
			return SafeDeviceContextHandle.fromGraphics(graphics);
		}
	}
}
