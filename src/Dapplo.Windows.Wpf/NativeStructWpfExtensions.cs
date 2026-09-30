// Copyright (c) Dapplo and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Diagnostics.Contracts;
using System.Windows;
using System.Windows.Media;
using Dapplo.Windows.Common.Structs;

namespace Dapplo.Windows.Wpf;

/// <summary>
///     Conversions between the native structs of Dapplo.Windows.Common and the WPF types (System.Windows.Point, Size, Rect, Int32Rect and Matrix transformations)
/// </summary>
public static class NativeStructWpfExtensions
{
    /// <summary>
    ///     Convert a NativePoint to a System.Windows.Point
    /// </summary>
    /// <param name="point">NativePoint</param>
    /// <returns>System.Windows.Point</returns>
    [Pure]
    public static Point ToPoint(this NativePoint point) => new Point(point.X, point.Y);

    /// <summary>
    ///     Convert a NativePointFloat to a System.Windows.Point
    /// </summary>
    /// <param name="point">NativePointFloat</param>
    /// <returns>System.Windows.Point</returns>
    [Pure]
    public static Point ToPoint(this NativePointFloat point) => new Point(point.X, point.Y);

    /// <summary>
    ///     Convert a System.Windows.Point to a NativePointFloat
    /// </summary>
    /// <param name="point">System.Windows.Point</param>
    /// <returns>NativePointFloat</returns>
    [Pure]
    public static NativePointFloat ToNativePointFloat(this Point point) => new NativePointFloat((float)point.X, (float)point.Y);

    /// <summary>
    ///     Convert a NativeSize to a System.Windows.Size
    /// </summary>
    /// <param name="size">NativeSize</param>
    /// <returns>System.Windows.Size</returns>
    [Pure]
    public static Size ToSize(this NativeSize size) => new Size(size.Width, size.Height);

    /// <summary>
    ///     Convert a NativeSizeFloat to a System.Windows.Size
    /// </summary>
    /// <param name="size">NativeSizeFloat</param>
    /// <returns>System.Windows.Size</returns>
    [Pure]
    public static Size ToSize(this NativeSizeFloat size) => new Size(size.Width, size.Height);

    /// <summary>
    ///     Convert a System.Windows.Size to a NativeSize (lossy), the width and height are rounded up to the smallest integer size which contains the size.
    ///     System.Windows.Size.Empty (which has negative infinite dimensions) results in an empty NativeSize.
    /// </summary>
    /// <param name="size">System.Windows.Size</param>
    /// <returns>NativeSize</returns>
    [Pure]
    public static NativeSize ToNativeSize(this Size size) =>
        size.IsEmpty ? NativeSize.Empty : new NativeSize((int)Math.Ceiling(size.Width), (int)Math.Ceiling(size.Height));

    /// <summary>
    ///     Convert a System.Windows.Size to a NativeSizeFloat
    /// </summary>
    /// <param name="size">System.Windows.Size</param>
    /// <returns>NativeSizeFloat</returns>
    [Pure]
    public static NativeSizeFloat ToNativeSizeFloat(this Size size) => new NativeSizeFloat((float)size.Width, (float)size.Height);

    /// <summary>
    ///     Convert a NativeRect to a System.Windows.Rect.
    ///     A System.Windows.Rect cannot have a negative width or height, a not normalized NativeRect is normalized.
    /// </summary>
    /// <param name="rectangle">NativeRect</param>
    /// <returns>System.Windows.Rect</returns>
    [Pure]
    public static Rect ToRect(this NativeRect rectangle) =>
        // The Rect(Point, Point) constructor normalizes, Rect(x, y, width, height) would throw an ArgumentException for a negative width or height
        new Rect(new Point(rectangle.Left, rectangle.Top), new Point(rectangle.Right, rectangle.Bottom));

    /// <summary>
    ///     Convert a NativeRectFloat to a System.Windows.Rect.
    ///     A System.Windows.Rect cannot have a negative width or height, a not normalized NativeRectFloat is normalized.
    /// </summary>
    /// <param name="rectangle">NativeRectFloat</param>
    /// <returns>System.Windows.Rect</returns>
    [Pure]
    public static Rect ToRect(this NativeRectFloat rectangle) =>
        new Rect(new Point(rectangle.Left, rectangle.Top), new Point(rectangle.Right, rectangle.Bottom));

    /// <summary>
    ///     Convert a System.Windows.Rect to a NativeRectFloat
    /// </summary>
    /// <param name="rectangle">System.Windows.Rect</param>
    /// <returns>NativeRectFloat</returns>
    [Pure]
    public static NativeRectFloat ToNativeRectFloat(this Rect rectangle) =>
        new NativeRectFloat((float)rectangle.Left, (float)rectangle.Top, (float)rectangle.Width, (float)rectangle.Height);

    /// <summary>
    ///     Convert a NativeRect to an Int32Rect
    /// </summary>
    /// <param name="rectangle">NativeRect</param>
    /// <returns>Int32Rect</returns>
    [Pure]
    public static Int32Rect ToInt32Rect(this NativeRect rectangle) => new Int32Rect(rectangle.Left, rectangle.Top, rectangle.Width, rectangle.Height);

    /// <summary>
    ///     Convert a NativeRectFloat to an Int32Rect (lossy), this results in the smallest integer rectangle which contains the NativeRectFloat (see <see cref="NativeRectFloat.GetContainingIntegerBounds"/>).
    /// </summary>
    /// <param name="rectangle">NativeRectFloat</param>
    /// <returns>Int32Rect</returns>
    [Pure]
    public static Int32Rect ToInt32Rect(this NativeRectFloat rectangle)
    {
        rectangle.GetContainingIntegerBounds(out var left, out var top, out var width, out var height);
        return new Int32Rect(left, top, width, height);
    }

    /// <summary>
    ///     Convert an Int32Rect to a NativeRect
    /// </summary>
    /// <param name="rectangle">Int32Rect</param>
    /// <returns>NativeRect</returns>
    [Pure]
    public static NativeRect ToNativeRect(this Int32Rect rectangle) => new NativeRect(rectangle.X, rectangle.Y, rectangle.Width, rectangle.Height);

    /// <summary>
    ///     Convert an Int32Rect to a NativeRectFloat
    /// </summary>
    /// <param name="rectangle">Int32Rect</param>
    /// <returns>NativeRectFloat</returns>
    [Pure]
    public static NativeRectFloat ToNativeRectFloat(this Int32Rect rectangle) => new NativeRectFloat(rectangle.X, rectangle.Y, rectangle.Width, rectangle.Height);

    /// <summary>
    ///     Transform the specified NativeRect, the result is the smallest integer rectangle which contains the transformed rectangle
    /// </summary>
    /// <param name="rect">NativeRect</param>
    /// <param name="matrix">Matrix</param>
    /// <returns>NativeRect</returns>
    [Pure]
    public static NativeRect Transform(this NativeRect rect, Matrix matrix)
    {
        Point[] points = { rect.TopLeft.ToPoint(), rect.BottomRight.ToPoint() };
        matrix.Transform(points);
        return (NativeRect)new NativeRectFloat(points[0].ToNativePointFloat(), points[1].ToNativePointFloat());
    }

    /// <summary>
    ///     Transform the specified NativeRectFloat
    /// </summary>
    /// <param name="rect">NativeRectFloat</param>
    /// <param name="matrix">Matrix</param>
    /// <returns>NativeRectFloat</returns>
    [Pure]
    public static NativeRectFloat Transform(this NativeRectFloat rect, Matrix matrix)
    {
        Point[] points = { rect.TopLeft.ToPoint(), rect.BottomRight.ToPoint() };
        matrix.Transform(points);
        return new NativeRectFloat(points[0].ToNativePointFloat(), points[1].ToNativePointFloat());
    }
}
