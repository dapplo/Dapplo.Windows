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

using Dapplo.Windows.Native;
using System.Windows;

namespace Dapplo.Windows.Desktop
{
	/// <summary>
	/// The DisplayInfo class is like the Screen class, only not cached.
	/// </summary>
	public class DisplayInfo
	{
		public bool IsPrimary
		{
			get;
			set;
		}

		public int ScreenHeight
		{
			get;
			set;
		}

		public int ScreenWidth
		{
			get;
			set;
		}

		public Rect Bounds
		{
			get;
			set;
		}

		public System.Drawing.Rectangle BoundsRectangle
		{
			get;
			set;
		}

		public Rect WorkingArea
		{
			get;
			set;
		}

		public System.Drawing.Rectangle WorkingAreaRectangle
		{
			get;
			set;
		}

		public string DeviceName
		{
			get;
			set;
		}

		/// <summary>
		/// Implementation like Screen.GetBounds
		/// https://msdn.microsoft.com/en-us/library/6d7ws9s4(v=vs.110).aspx
		/// </summary>
		/// <param name="point"></param>
		/// <returns></returns>
		public static Rect GetBounds(Point point)
		{
			DisplayInfo returnValue = null;
			foreach (var display in User32.AllDisplays())
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
			return returnValue.Bounds;
		}

		/// <summary>
		/// Implementation like Screen.GetBounds
		/// https://msdn.microsoft.com/en-us/library/6d7ws9s4(v=vs.110).aspx
		/// </summary>
		/// <param name="point">System.Drawing.Point</param>
		/// <returns>Rect</returns>
		public static System.Drawing.Rectangle GetBounds(System.Drawing.Point point)
		{
			var rect = GetBounds(new Point(point.X, point.Y));
			return new System.Drawing.Rectangle((int)rect.X, (int)rect.Y, (int)rect.Width, (int)rect.Height);
        }
	}
}
