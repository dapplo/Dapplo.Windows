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
using System.Drawing;
using Dapplo.Windows.Common.Enums;
using Dapplo.Windows.Common.Structs;

#endregion

namespace Dapplo.Windows.Common.Extensions
{
    /// <summary>
    ///     Helper method for the NativeRectFloat struct
    /// </summary>
    public static class NativeRectFloatExensions
    {
        /// <summary>
        /// Create a new NativeRectFloat, from the supplied one, using the specified X coordinate
        /// </summary>
        /// <param name="rect">NativeRectFloat</param>
        /// <param name="x">float</param>
        /// <returns>NativeRectFloat</returns>
        [Pure]
        public static NativeRectFloat ChangeX(this NativeRectFloat rect, float x)
        {
            return new NativeRectFloat(rect.Location.ChangeX(x), rect.Size);
        }

        /// <summary>
        /// Create a new NativeRectFloat, from the supplied one, using the specified Y coordinate
        /// </summary>
        /// <param name="rect">NativeRectFloat</param>
        /// <param name="y">float</param>
        /// <returns>NativeRectFloat</returns>
        [Pure]
        public static NativeRectFloat ChangeY(this NativeRectFloat rect, float y)
        {
            return new NativeRectFloat(rect.Location.ChangeY(y), rect.Size);
        }

        /// <summary>
        /// Create a new NativeRectFloat, from the supplied one, using the specified width
        /// </summary>
        /// <param name="rect">NativeRectFloat</param>
        /// <param name="width">float</param>
        /// <returns>NativeRectFloat</returns>
        [Pure]
        public static NativeRectFloat ChangeWidth(this NativeRectFloat rect, float width)
        {
            return new NativeRectFloat(rect.Location, rect.Size.ChangeWidth(width));
        }

        /// <summary>
        /// Create a new NativeRectFloat, from the supplied one, using the specified height
        /// </summary>
        /// <param name="rect">NativeRectFloat</param>
        /// <param name="height">float</param>
        /// <returns>NativeRectFloat</returns>
        [Pure]
        public static NativeRectFloat ChangeHeight(this NativeRectFloat rect, float height)
        {
            return new NativeRectFloat(rect.Location, rect.Size.ChangeHeight(height));
        }

        /// <summary>
        ///     Test if this NativeRectFloat contains the specified NativePointFloat
        /// </summary>
        /// <param name="rect">NativeRectFloat</param>
        /// <param name="point">NativePointFloat</param>
        /// <returns>true if it contains</returns>
        [Pure]
        public static bool Contains(this NativeRectFloat rect, NativePointFloat point)
        {
            return IsBetween(point.X, rect.Left, rect.Right) && IsBetween(point.Y, rect.Top, rect.Bottom);
        }

        /// <summary>
        ///     Test if this NativeRectFloat contains the specified coordinates
        /// </summary>
        /// <param name="rect">NativeRectFloat</param>
        /// <param name="x">float</param>
        /// <param name="y">float</param>
        /// <returns>true if it contains</returns>
        [Pure]
        public static bool Contains(this NativeRectFloat rect, float x, float y)
        {
            return IsBetween(x, rect.Left, rect.Right) && IsBetween(y, rect.Top, rect.Bottom);
        }

        /// <summary>
        ///     True if small NativeRectFloat is entirely contained within the larger NativeRectFloat
        /// </summary>
        /// <param name="largerRectangle">NativeRectFloat, the larger rectangle</param>
        /// <param name="smallerRectangle">NativeRectFloat, the smaller rectangle</param>
        /// <returns>True if small rectangle is entirely contained within the larger rectangle, false otherwise</returns>
        [Pure]
        public static bool Contains(this NativeRectFloat largerRectangle, NativeRectFloat smallerRectangle)
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
        public static bool HasOverlap(this NativeRectFloat rect1, NativeRectFloat rect2)
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
        public static AdjacentTo IsAdjacent(this NativeRectFloat rect1, NativeRectFloat rect2)
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
        private static bool IsBetween(float value, float min, float max)
        {
            return value >= min && value < max;
        }

        /// <summary>
        ///     Test if a NativeRectFloat is docked to the left of another NativeRectFloat
        /// </summary>
        /// <param name="rect1">NativeRectFloat to test if it's docked</param>
        /// <param name="rect2">NativeRectFloat rect to be docked to</param>
        /// <returns>bool with true if they are docked</returns>
        [Pure]
        public static bool IsDockedToLeftOf(this NativeRectFloat rect1, NativeRectFloat rect2)
        {
            // Test if the right is one pixel to the left, and if top or bottom is within the rect2 height.
            return Math.Abs(rect1.Right - (rect2.Left - 1)) < float.Epsilon && (IsBetween(rect1.Top, rect2.Top, rect2.Bottom) || IsBetween(rect1.Bottom, rect2.Top, rect2.Bottom));
        }

        /// <summary>
        ///     Test if a NativeRectFloat is docked to the right of another NativeRectFloat
        /// </summary>
        /// <param name="rect1">NativeRectFloat to test if it's docked</param>
        /// <param name="rect2">NativeRectFloat rect to be docked to</param>
        /// <returns>bool with true if they are docked</returns>
        [Pure]
        public static bool IsDockedToRightOf(this NativeRectFloat rect1, NativeRectFloat rect2)
        {
            // Test if the right is one pixel to the left, and if top or bottom is within the rect2 height.
            return Math.Abs(rect1.Left - (rect2.Right + 1)) < float.Epsilon && (IsBetween(rect1.Top, rect2.Top, rect2.Bottom) || IsBetween(rect1.Bottom, rect2.Top, rect2.Bottom));
        }

        /// <summary>
        /// Creates a new NativeRectFloat which is the union of rect1 and rect2
        /// </summary>
        /// <param name="rect1">NativeRectFloat</param>
        /// <param name="rect2">NativeRectFloat</param>
        /// <returns>NativeRectFloat which is the union of rect1 and rect2</returns>
        [Pure]
        public static NativeRectFloat Union(this NativeRectFloat rect1, NativeRectFloat rect2)
        {
            // TODO: Replace logic with own code
            return RectangleF.Union(rect1, rect2);
        }

        /// <summary>
        /// Creates a new NativeRect which is the intersection of rect1 and rect2
        /// </summary>
        /// <param name="rect1">NativeRectFloat</param>
        /// <param name="rect2">NativeRectFloat</param>
        /// <returns>NativeRectFloat which is the intersection of rect1 and rect2</returns>
        [Pure]
        public static NativeRectFloat Intersect(this NativeRectFloat rect1, NativeRectFloat rect2)
        {
            // TODO: Replace logic with own code
            return RectangleF.Intersect(rect1, rect2);
        }

        /// <summary>
        /// Creates a new NativeRectFloat which is rect but inflated with the specified width and height
        /// </summary>
        /// <param name="rect">NativeRectFloat</param>
        /// <param name="width">int</param>
        /// <param name="height">int</param>
        /// <returns>NativeRectFloat</returns>
        [Pure]
        public static NativeRectFloat Inflate(this NativeRectFloat rect, int width, int height)
        {
            // TODO: Replace logic with own code
            return RectangleF.Inflate(rect, width, height);
        }

        /// <summary>
        /// Test if the current NativeRectFloat intersects with the specified.
        /// </summary>
        /// <param name="rect1">NativeRectFloat</param>
        /// <param name="rect2">NativeRectFloat</param>
        /// <returns>bool</returns>
        [Pure]
        public static bool IntersectsWith(this NativeRectFloat rect1, NativeRectFloat rect2)
        {
            return rect2.X < rect1.X + rect1.Width &&
                   rect1.X < rect2.X + rect2.Width &&
                   rect2.Y < rect1.Y + rect1.Height &&
                   rect1.Y < rect2.Y + rect2.Height;
        }

        /// <summary>
        /// Create a new NativeRect by offsetting the specified one
        /// </summary>
        /// <param name="rect">NativeRectFloat</param>
        /// <param name="offset">NativePointFloat</param>
        /// <returns>NativeRectFloat</returns>
        [Pure]
        public static NativeRectFloat Offset(this NativeRectFloat rect, NativePointFloat offset)
        {
            return new NativeRectFloat(rect.Location.Offset(offset), rect.Size);
        }

        /// <summary>
        /// Create a new NativeRectFloat by offsetting the specified one
        /// </summary>
        /// <param name="rect">NativeRectFloat</param>
        /// <param name="offsetX">float</param>
        /// <param name="offsetY">float</param>
        /// <returns>NativeRectFloat</returns>
        [Pure]
        public static NativeRectFloat Offset(this NativeRectFloat rect, float? offsetX = null, float? offsetY = null)
        {
            return new NativeRectFloat(rect.Location.Offset(offsetX, offsetY), rect.Size);
        }

        /// <summary>
        /// Create a new NativeRectFloat from the specified one, but on a different location
        /// </summary>
        /// <param name="rect">NativeRectFloat</param>
        /// <param name="location">NativePointFloat</param>
        /// <returns>NativeRectFloat</returns>
        [Pure]
        public static NativeRectFloat MoveTo(this NativeRectFloat rect, NativePointFloat location)
        {
            return new NativeRectFloat(location, rect.Size);
        }

        /// <summary>
        /// Create a new NativeRectFloat from the specified one, but on a different location
        /// </summary>
        /// <param name="rect">NativeRectFloat</param>
        /// <param name="x">int</param>
        /// <param name="y">int</param>
        /// <returns>NativeRectFloat</returns>
        [Pure]
        public static NativeRectFloat MoveTo(this NativeRectFloat rect, float? x = null, float? y = null)
        {
            return new NativeRectFloat(new NativePointFloat(x ?? rect.X,y ?? rect.Y), rect.Size);
        }

        /// <summary>
        /// Create a new NativeRectFloat from the specified one, but with a different size
        /// </summary>
        /// <param name="rect">NativeRectFloat</param>
        /// <param name="size">NativeSizeFloat</param>
        /// <returns>NativeRectFloat</returns>
        [Pure]
        public static NativeRectFloat Resize(this NativeRectFloat rect, NativeSizeFloat size)
        {
            return new NativeRectFloat(rect.Location, size);
        }

        /// <summary>
        /// Create a new NativeRectFloat from the specified one, but with a different size
        /// </summary>
        /// <param name="rect">NativeRectFloat</param>
        /// <param name="width">float</param>
        /// <param name="height">float</param>
        /// <returns>NativeRectFloat</returns>
        [Pure]
        public static NativeRectFloat Resize(this NativeRectFloat rect, float? width = null, float? height = null)
        {
            return rect.Resize(new NativeSizeFloat(width ?? rect.Width, height ?? rect.Height));
        }

        /// <summary>
        /// Create a NativeRect, using rounded values, from the specified NativeRectFloat
        /// </summary>
        /// <param name="rect">NativeRectFloat</param>
        /// <returns>NativeRect</returns>
        [Pure]
        public static NativeRect Round(this NativeRectFloat rect)
        {
            return new NativeRect((int)Math.Round(rect.X), (int)Math.Round(rect.Y), (int)Math.Round(rect.Width), (int)Math.Round(rect.Height));
        }

#if !NETSTANDARD2_0
        /// <summary>
        /// Transform the specified NativeRectFloat
        /// </summary>
        /// <param name="rect">NativeRectFloat</param>
        /// <param name="matrix">Matrix</param>
        /// <returns>NativeRectFloat</returns>
        [Pure]
        public static NativeRectFloat Transform(this NativeRectFloat rect, System.Windows.Media.Matrix matrix)
        {
            System.Windows.Point[] myPointArray = { rect.TopLeft, rect.BottomRight };
            matrix.Transform(myPointArray);
            NativePointFloat topLeft = myPointArray[0];
            NativePointFloat bottomRight = myPointArray[1];
            return new NativeRectFloat(topLeft, bottomRight);
        }
#endif

        /// <summary>
        /// Normalize the NativeRectFloat by making a negative width and or height absolute
        /// </summary>
        /// <param name="rect">NativeRectFloat</param>
        /// <returns>NativeRectFloat</returns>
        [Pure]
        public static NativeRectFloat Normalize(this NativeRectFloat rect)
        {
            float x = rect.X;
            float y = rect.Y;
            float width = rect.Width;
            float height = rect.Height;

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
            return new NativeRectFloat(x, y, new NativeSizeFloat(width, height));
        }

    }
}