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

using Dapplo.Windows.Structs;

#endregion

namespace Dapplo.Windows.Extensions
{
	/// <summary>
	///     Helper method for the RECT struct
	/// </summary>
	public static class RectExensions
	{
		/// <summary>
		///     Test if this RECT contains the specified POINT
		/// </summary>
		/// <param name="rect"></param>
		/// <param name="point">POINT</param>
		/// <returns>true if it contains</returns>
		public static bool Contains(this RECT rect, POINT point)
		{
			return point.X >= rect.Left && point.X <= rect.Right && point.Y >= rect.Top && point.Y <= rect.Bottom;
		}

		/// <summary>
		///     True if small rectangle is entirely contained within the larger rectangle
		/// </summary>
		/// <param name="largerRectangle">The larger rectangle</param>
		/// <param name="smallerRectangle">The smaller rectangle</param>
		/// <returns>True if small rectangle is entirely contained within the larger rectangle, false otherwise</returns>
		public static bool Contains(this RECT largerRectangle, RECT smallerRectangle)
		{
			var topLeftCheck = largerRectangle.TopLeft.X <= smallerRectangle.TopLeft.X && largerRectangle.TopLeft.Y <= smallerRectangle.TopLeft.Y;
			var bottomLeftCheck = largerRectangle.BottomLeft.X <= smallerRectangle.BottomLeft.X && largerRectangle.BottomLeft.Y >= smallerRectangle.BottomLeft.Y;

			var topRightCheck = largerRectangle.TopRight.X >= smallerRectangle.TopRight.X && largerRectangle.TopRight.Y <= smallerRectangle.TopRight.Y;
			var bottomRightCheck = largerRectangle.BottomRight.X >= smallerRectangle.BottomRight.X && largerRectangle.BottomRight.Y >= smallerRectangle.BottomRight.Y;

			return topLeftCheck && bottomLeftCheck && topRightCheck && bottomRightCheck;
		}

		/// <summary>
		///     Check that two rectangles overlap with each other
		/// </summary>
		/// <param name="rect1">The first rectangle</param>
		/// <param name="rect2">The second rectangle</param>
		/// <returns>The rectangles overlap</returns>
		public static bool HasOverlap(this RECT rect1, RECT rect2)
		{
			if (rect1.IsAdjacent(rect2))
			{
				return true;
			}

			var leftOfRect1InsideRect2Width = IsBetween(rect1.X, rect2.X, rect2.X + rect2.Width);
			var leftOfRect2InsideRect1Width = IsBetween(rect2.X, rect1.X, rect1.X + rect1.Width);
			var xOverlap = leftOfRect1InsideRect2Width || leftOfRect2InsideRect1Width;

			var topOfRect1InsideRect2Height = IsBetween(rect1.Y, rect2.Y, rect2.Y + rect2.Height);
			var topOfRect2InsideRect1Height = IsBetween(rect2.Y, rect1.Y, rect1.Y + rect1.Height);
			var yOverlap = topOfRect1InsideRect2Height || topOfRect2InsideRect1Height;

			var rectanglesIntersect = xOverlap && yOverlap && !(rect1.Contains(rect2) || rect2.Contains(rect1));

			return rectanglesIntersect;
		}

		/// <summary>
		///     True if either rectangle is adjacent to the other rectangle
		/// </summary>
		/// <param name="rect1">The first rectangle</param>
		/// <param name="rect2">The second rectangle</param>
		/// <returns>At least one rectangle is adjacent to the other rectangle</returns>
		public static bool IsAdjacent(this RECT rect1, RECT rect2)
		{
			var overlapsWithRect2Left = rect1.Left.Equals(rect2.Left) || rect1.Right.Equals(rect2.Left);
			var overlapsWithRect2Right = rect1.Left.Equals(rect2.Right) || rect1.Right.Equals(rect2.Right);

			var overlapsWithRect2Top = rect1.Top.Equals(rect2.Top) || rect1.Bottom.Equals(rect2.Top);
			var overlapsWithRect2Bottom = rect1.Top.Equals(rect2.Bottom) || rect1.Bottom.Equals(rect2.Bottom);

			if (overlapsWithRect2Left || overlapsWithRect2Right)
			{
				var isWithinYAxis = IsBetween(rect1.Top, rect2.Top, rect2.Bottom) || IsBetween(rect1.Bottom, rect2.Top, rect2.Bottom);
				return isWithinYAxis;
			}

			if (overlapsWithRect2Bottom || overlapsWithRect2Top)
			{
				var isWithinXAxis = IsBetween(rect1.Left, rect2.Left, rect2.Right) || IsBetween(rect1.Right, rect2.Left, rect2.Right);
				return isWithinXAxis;
			}
			return false;
		}

		/// <summary>
		///     Simple helper to specify if value is inside min and max
		/// </summary>
		/// <param name="value">double to check</param>
		/// <param name="min">lowest allowed value</param>
		/// <param name="max">highest allowed value</param>
		/// <returns>bool true if the value is between</returns>
		private static bool IsBetween(double value, double min, double max)
		{
			return value >= min && value <= max;
		}

		/// <summary>
		///     Test if a RECT is docked to the left of another RECT
		/// </summary>
		/// <param name="rect1">RECT to test if it's docked</param>
		/// <param name="rect2">RECT rect to be docked to</param>
		/// <returns>bool with true if they are docked</returns>
		public static bool IsDockedToLeftOf(this RECT rect1, RECT rect2)
		{
			// Test if the right is one pixel to the left, and if top or bottom is within the rect2 height.
			return rect1.Right == rect2.Left - 1 && (IsBetween(rect1.Top, rect2.Top, rect2.Bottom) || IsBetween(rect1.Bottom, rect2.Top, rect2.Bottom));
		}

		/// <summary>
		///     Test if a RECT is docked to the right of another RECT
		/// </summary>
		/// <param name="rect1">RECT to test if it's docked</param>
		/// <param name="rect2">RECT rect to be docked to</param>
		/// <returns>bool with true if they are docked</returns>
		public static bool IsDockedToRightOf(this RECT rect1, RECT rect2)
		{
			// Test if the right is one pixel to the left, and if top or bottom is within the rect2 height.
			return rect1.Left == rect2.Right + 1 && (IsBetween(rect1.Top, rect2.Top, rect2.Bottom) || IsBetween(rect1.Bottom, rect2.Top, rect2.Bottom));
		}
	}
}