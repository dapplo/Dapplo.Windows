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
using System.Diagnostics.Contracts;
using Dapplo.Windows.Common.Enums;
using Dapplo.Windows.Common.Structs;

#endregion

namespace Dapplo.Windows.Common.Extensions
{
    /// <summary>
    ///     Helper method for the NativeRect struct
    /// </summary>
    public static class NativeRectExensions
    {
        /// <summary>
        /// Create a new NativeRect, from the supplied one, using the specified X coordinate
        /// </summary>
        /// <param name="rect">NativeRect</param>
        /// <param name="x">int</param>
        /// <returns>NativeRect</returns>
        [Pure]
        public static NativeRect ChangeX(this NativeRect rect, int x)
        {
            return new NativeRect(rect.Location.ChangeX(x), rect.Size);
        }

        /// <summary>
        /// Create a new NativeRect, from the supplied one, using the specified Y coordinate
        /// </summary>
        /// <param name="rect">NativeRect</param>
        /// <param name="y">int</param>
        /// <returns>NativeRect</returns>
        [Pure]
        public static NativeRect ChangeY(this NativeRect rect, int y)
        {
            return new NativeRect(rect.Location.ChangeY(y), rect.Size);
        }

        /// <summary>
        /// Create a new NativeRect, from the supplied one, using the specified width
        /// </summary>
        /// <param name="rect">NativeRect</param>
        /// <param name="width">int</param>
        /// <returns>NativeRect</returns>
        [Pure]
        public static NativeRect ChangeWidth(this NativeRect rect, int width)
        {
            return new NativeRect(rect.Location, rect.Size.ChangeWidth(width));
        }

        /// <summary>
        /// Create a new NativeRect, from the supplied one, using the specified height
        /// </summary>
        /// <param name="rect">NativeRect</param>
        /// <param name="height">int</param>
        /// <returns>NativeRect</returns>
        [Pure]
        public static NativeRect ChangeHeight(this NativeRect rect, int height)
        {
            return new NativeRect(rect.Location, rect.Size.ChangeHeight(height));
        }


        /// <summary>
        ///     Test if this NativeRect contains the specified NativePoint
        /// </summary>
        /// <param name="rect"></param>
        /// <param name="point">NativePoint</param>
        /// <returns>true if it contains</returns>
        [Pure]
        public static bool Contains(this NativeRect rect, NativePoint point)
        {
            return IsBetween(point.X, rect.Left, rect.Right) && IsBetween(point.Y, rect.Top, rect.Bottom);
        }

        /// <summary>
        ///     Test if this NativeRect contains the specified coordinates
        /// </summary>
        /// <param name="rect"></param>
        /// <param name="x">int</param>
        /// <param name="y">int</param>
        /// <returns>true if it contains</returns>
        [Pure]
        public static bool Contains(this NativeRect rect, int x, int y)
        {
            return IsBetween(x, rect.Left, rect.Right) && IsBetween(y, rect.Top, rect.Bottom);
        }

        /// <summary>
        ///     True if small NativeRect is entirely contained within the larger NativeRect
        /// </summary>
        /// <param name="largerRectangle">NativeRect, the larger rectangle</param>
        /// <param name="smallerRectangle">NativeRect, the smaller rectangle</param>
        /// <returns>True if small NativeRect is entirely contained within the larger NativeRect, false otherwise</returns>
        [Pure]
        public static bool Contains(this NativeRect largerRectangle, NativeRect smallerRectangle)
        {
            return
                largerRectangle.Left <= smallerRectangle.Left &&
                smallerRectangle.Right <= largerRectangle.Right &&
                largerRectangle.Top <= smallerRectangle.Top &&
                smallerRectangle.Bottom <= largerRectangle.Bottom;
        }

        /// <summary>
        ///     Check that two rectangles overlap with each other
        /// </summary>
        /// <param name="rect1">The first rectangle</param>
        /// <param name="rect2">The second rectangle</param>
        /// <returns>The rectangles overlap</returns>
        [Pure]
        public static bool HasOverlap(this NativeRect rect1, NativeRect rect2)
        {
            if (rect1.IsAdjacent(rect2) != AdjacentTo.None)
            {
                // If it's adjacent than there is no overlap?
                return true;
            }

            var leftOfRect1InsideRect2Width = IsBetween(rect1.X, rect2.Left, rect2.Right);
            var leftOfRect2InsideRect1Width = IsBetween(rect2.X, rect1.Left, rect1.Right);
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
        [Pure]
        public static AdjacentTo IsAdjacent(this NativeRect rect1, NativeRect rect2)
        {
            if (rect1.Left.Equals(rect2.Right) && (IsBetween(rect1.Top, rect2.Top, rect2.Bottom) || IsBetween(rect2.Top, rect1.Top, rect1.Bottom)))
            {
                return AdjacentTo.Left;
            }
            if (rect1.Right.Equals(rect2.Left) && (IsBetween(rect1.Top, rect2.Top, rect2.Bottom) || IsBetween(rect2.Top, rect1.Top, rect1.Bottom)))
            {
                return AdjacentTo.Right;
            }
            if (rect1.Top.Equals(rect2.Bottom) && (IsBetween(rect1.Left, rect2.Left, rect2.Right) || IsBetween(rect2.Left, rect1.Left, rect1.Right)))
            {
                return AdjacentTo.Top;
            }
            if (rect1.Bottom.Equals(rect2.Top) && (IsBetween(rect1.Left, rect2.Left, rect2.Right) || IsBetween(rect2.Left, rect1.Left, rect1.Right)))
            {
                return AdjacentTo.Bottom;
            }
            return AdjacentTo.None;
        }

        /// <summary>
        ///     Simple helper to specify if value is inside min and max
        /// </summary>
        /// <param name="value">double to check</param>
        /// <param name="min">lowest allowed value</param>
        /// <param name="max">highest allowed value</param>
        /// <returns>bool true if the value is between</returns>
        [Pure]
        private static bool IsBetween(int value, int min, int max)
        {
            return value >= min && value < max;
        }

        /// <summary>
        ///     Test if a NativeRect is docked to the left of another NativeRect
        /// </summary>
        /// <param name="rect1">NativeRect to test if it's docked</param>
        /// <param name="rect2">NativeRect rect to be docked to</param>
        /// <returns>bool with true if they are docked</returns>
        [Pure]
        public static bool IsDockedToLeftOf(this NativeRect rect1, NativeRect rect2)
        {
            // Test if the right is one pixel to the left, and if top or bottom is within the rect2 height.
            return rect1.Right == rect2.Left - 1 && (IsBetween(rect1.Top, rect2.Top, rect2.Bottom) || IsBetween(rect1.Bottom, rect2.Top, rect2.Bottom));
        }

        /// <summary>
        ///     Test if a NativeRect is docked to the right of another NativeRect
        /// </summary>
        /// <param name="rect1">NativeRect to test if it's docked</param>
        /// <param name="rect2">NativeRect rect to be docked to</param>
        /// <returns>bool with true if they are docked</returns>
        [Pure]
        public static bool IsDockedToRightOf(this NativeRect rect1, NativeRect rect2)
        {
            // Test if the right is one pixel to the left, and if top or bottom is within the rect2 height.
            return rect1.Left == rect2.Right + 1 && (IsBetween(rect1.Top, rect2.Top, rect2.Bottom) || IsBetween(rect1.Bottom, rect2.Top, rect2.Bottom));
        }

        /// <summary>
        /// Creates a new NativeRect which is the union of rect1 and rect2
        /// </summary>
        /// <param name="rect1">NativeRect</param>
        /// <param name="rect2">NativeRect</param>
        /// <returns>NativeRect which is the intersection of rect1 and rect2</returns>
        [Pure]
        public static NativeRect Union(this NativeRect rect1, NativeRect rect2)
        {
            // TODO: Replace logic with own code
            return System.Drawing.Rectangle.Union(rect1, rect2);
        }

        /// <summary>
        /// Creates a new NativeRect which is the intersection of rect1 and rect2
        /// </summary>
        /// <param name="rect1">NativeRect</param>
        /// <param name="rect2">NativeRect</param>
        /// <returns>NativeRect which is the intersection of rect1 and rect2</returns>
        [Pure]
        public static NativeRect Intersect(this NativeRect rect1, NativeRect rect2)
        {
            // TODO: Replace logic with own code
            return System.Drawing.Rectangle.Intersect(rect1, rect2);
        }

        /// <summary>
        /// Creates a new NativeRect which is rect but inflated with the specified width and height
        /// </summary>
        /// <param name="rect">NativeRect</param>
        /// <param name="width">int</param>
        /// <param name="height">int</param>
        /// <returns>NativeRect</returns>
        [Pure]
        public static NativeRect Inflate(this NativeRect rect, int width, int height)
        {
            // TODO: Replace logic with own code
            return System.Drawing.Rectangle.Inflate(rect, width, height);
        }

        /// <summary>
        /// Test if the current rectangle intersects with the specified.
        /// </summary>
        /// <param name="rect1">NativeRect</param>
        /// <param name="rect2">NativeRect</param>
        /// <returns>bool</returns>
        [Pure]
        public static bool IntersectsWith(this NativeRect rect1, NativeRect rect2)
        {
            return rect2.X < rect1.X + rect1.Width &&
                   rect1.X < rect2.X + rect2.Width &&
                   rect2.Y < rect1.Y + rect1.Height &&
                   rect1.Y < rect2.Y + rect2.Height;
        }

        /// <summary>
        /// Create a new NativeRect by offsetting the specified one
        /// </summary>
        /// <param name="rect">NativeRect</param>
        /// <param name="offset">NativePoint</param>
        /// <returns>NativeRect</returns>
        [Pure]
        public static NativeRect Offset(this NativeRect rect, NativePoint offset)
        {
            return new NativeRect(rect.Location.Offset(offset), rect.Size);
        }

        /// <summary>
        /// Create a new NativeRect by offsetting the specified one
        /// </summary>
        /// <param name="rect">NativeRect</param>
        /// <param name="offsetX">int</param>
        /// <param name="offsetY">int</param>
        /// <returns>NativeRect</returns>
        [Pure]
        public static NativeRect Offset(this NativeRect rect, int? offsetX = null, int? offsetY = null)
        {
            return rect.Offset(new NativePoint(offsetX ?? 0, offsetY ?? 0));
        }

        /// <summary>
        /// Create a new NativeRect from the specified one, but on a different location
        /// </summary>
        /// <param name="rect">NativeRect</param>
        /// <param name="location">NativePoint</param>
        /// <returns>NativeRect</returns>
        [Pure]
        public static NativeRect MoveTo(this NativeRect rect, NativePoint location)
        {
            return new NativeRect(location, rect.Size);
        }

        /// <summary>
        /// Create a new NativeRect from the specified one, but on a different location
        /// </summary>
        /// <param name="rect">NativeRect</param>
        /// <param name="x">int</param>
        /// <param name="y">int</param>
        /// <returns>NativeRect</returns>
        [Pure]
        public static NativeRect MoveTo(this NativeRect rect, int? x = null, int? y = null)
        {
            return rect.MoveTo(new NativePoint(x ?? rect.X,y ?? rect.Y));
        }

        /// <summary>
        /// Create a new NativeRect from the specified one, but with a different size
        /// </summary>
        /// <param name="rect">NativeRect</param>
        /// <param name="size">NativeSize</param>
        /// <returns>NativeRect</returns>
        [Pure]
        public static NativeRect Resize(this NativeRect rect, NativeSize size)
        {
            return new NativeRect(rect.Location, size);
        }

        /// <summary>
        /// Create a new NativeRect from the specified one, but with a different size
        /// </summary>
        /// <param name="rect">NativeRect</param>
        /// <param name="width">int</param>
        /// <param name="height">int</param>
        /// <returns>NativeRect</returns>
        [Pure]
        public static NativeRect Resize(this NativeRect rect, int? width = null, int? height = null)
        {
            return rect.Resize(new NativeSize(width ?? rect.Width, height ?? rect.Height));
        }

#if !NETSTANDARD2_0
        /// <summary>
        /// Transform the specified NativeRect
        /// </summary>
        /// <param name="rect">NativeRect</param>
        /// <param name="matrix">Matrix</param>
        /// <returns>NativeRect</returns>
        [Pure]
        public static NativeRect Transform(this NativeRect rect, System.Windows.Media.Matrix matrix)
        {
            System.Windows.Point[] myPointArray = {rect.TopLeft, rect.BottomRight};
            matrix.Transform(myPointArray);
            NativePointFloat topLeft = myPointArray[0];
            NativePointFloat bottomRight = myPointArray[1];
            return new NativeRect(topLeft, bottomRight);
        }
#endif

        /// <summary>
        /// Normalize the NativeRect by making a negative width and or height absolute
        /// </summary>
        /// <param name="rect">NativeRect</param>
        /// <returns>NativeRect</returns>
        [Pure]
        public static NativeRect Normalize(this NativeRect rect)
        {
            int x = rect.X;
            int y = rect.Y;
            int width = rect.Width;
            int height = rect.Height;

            if (width < 0)
            {
                x += width;
                width = Math.Abs(width);
            }
            if (height < 0)
            {
                y += height;
                height = Math.Abs(height);
            }
            return new NativeRect(x,y, new NativeSize(width,height));
        }
    }
}