//  Dapplo - building blocks for desktop applications
//  Copyright (C) 2016 Dapplo
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
using System.Windows;
using Dapplo.Windows.Native;
using Point = System.Windows.Point;

#endregion

namespace Dapplo.Windows.Desktop
{
	/// <summary>
	///     The DisplayInfo class is like the Screen class, only not cached.
	/// </summary>
	public class DisplayInfo
	{
		/// <summary>
		///     Screen bounds
		/// </summary>
		public Rect Bounds { get; set; }

		/// <summary>
		///     Bounds as Rectangle
		/// </summary>
		public Rectangle BoundsRectangle { get; set; }

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
		public Rect WorkingArea { get; set; }

		/// <summary>
		///     Desktop working area as Rectangle
		/// </summary>
		public Rectangle WorkingAreaRectangle { get; set; }

		/// <summary>
		///     Implementation like Screen.GetBounds
		///     https://msdn.microsoft.com/en-us/library/6d7ws9s4(v=vs.110).aspx
		/// </summary>
		/// <param name="point"></param>
		/// <returns></returns>
		public static Rect GetBounds(Point point)
		{
			DisplayInfo returnValue = null;
			foreach (var display in User32.AllDisplays())
			{
				if (display.IsPrimary && (returnValue == null))
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

		/// <summary>
		///     Implementation like Screen.GetBounds
		///     https://msdn.microsoft.com/en-us/library/6d7ws9s4(v=vs.110).aspx
		/// </summary>
		/// <param name="point">System.Drawing.Point</param>
		/// <returns>Rect</returns>
		public static Rectangle GetBounds(System.Drawing.Point point)
		{
			var rect = GetBounds(new Point(point.X, point.Y));
			return new Rectangle((int) rect.X, (int) rect.Y, (int) rect.Width, (int) rect.Height);
		}
	}
}