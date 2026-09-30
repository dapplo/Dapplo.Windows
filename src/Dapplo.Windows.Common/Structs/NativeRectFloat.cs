// Copyright (c) Dapplo and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.Diagnostics.Contracts;
using System.Drawing;
using System.Runtime.InteropServices;
using Dapplo.Windows.Common.TypeConverters;

namespace Dapplo.Windows.Common.Structs;

/// <summary>
///     NativeRect represents the native RECTF structure for calling native methods.
///     See <a href="https://msdn.microsoft.com/en-us/library/windows/desktop/ms534497(v=vs.85).aspx">RectF class</a>
///     It has conversions from and to System.Drawing.RectangleF (the conversions from and to the WPF types are extension methods in Dapplo.Windows.Wpf)
///
/// </summary>
[StructLayout(LayoutKind.Sequential)]
[Serializable]
[TypeConverter(typeof(NativeRectFloatTypeConverter))]
[SuppressMessage("ReSharper", "ConvertToAutoPropertyWithPrivateSetter")]
public readonly struct NativeRectFloat : IEquatable<NativeRectFloat>
{
    private readonly float _x;
    private readonly float _y;
    private readonly float _width;
    private readonly float _height;

    /// <summary>
    ///     Constructor from left, top, width, height
    /// </summary>
    /// <param name="left">float</param>
    /// <param name="top">float</param>
    /// <param name="width">float</param>
    /// <param name="height">float</param>
    public NativeRectFloat(float left, float top, float width, float height)
    {
        _x = left;
        _y = top;
        _width = width;
        _height = height;
    }

    /// <summary>
    ///     Constructor from x,y, (width,height)
    /// </summary>
    /// <param name="x">int</param>
    /// <param name="y">int</param>
    /// <param name="nativeSizeFloat">NativeSizeFloat</param>
    public NativeRectFloat(float x, float y, NativeSizeFloat nativeSizeFloat)
    {
        _x = x;
        _y = y;
        _width = nativeSizeFloat.Width;
        _height = nativeSizeFloat.Height;
    }

    /// <summary>
    ///     Constructor from top-left, bottom right
    /// </summary>
    /// <param name="topLeft">NativePointFloat</param>
    /// <param name="bottomRight">NativePointFloat</param>
    public NativeRectFloat(NativePointFloat topLeft, NativePointFloat bottomRight) : this(topLeft.X, topLeft.Y, bottomRight.X - topLeft.X, bottomRight.Y - topLeft.Y)
    {
    }

    /// <summary>
    ///     Constructor from location, size
    /// </summary>
    /// <param name="location">NativePoint</param>
    /// <param name="nativeSizeFloat">NativeSizeFloat</param>
    public NativeRectFloat(NativePointFloat location, NativeSizeFloat nativeSizeFloat)
    {
        _x = location.X;
        _y = location.Y;
        _width = nativeSizeFloat.Width;
        _height = nativeSizeFloat.Height;
    }

    /// <summary>
    ///     X value
    /// </summary>
    public float X => _x;

    /// <summary>
    ///     X location of the rectangle
    /// </summary>
    public float Y => _y;

    /// <summary>
    ///     Left value of the rectangle
    /// </summary>
    public float Left => _x;

    /// <summary>
    ///     Top of the rectangle
    /// </summary>
    public float Top => _y;

    /// <summary>
    ///     Right of the rectangle
    /// </summary>
    public float Right => _x + _width;

    /// <summary>
    ///     Bottom of the rectangle
    /// </summary>
    public float Bottom => _y + _height;

    /// <summary>
    ///     Heigh of the NativeRectFloat
    /// </summary>
    public float Height => _height;

    /// <summary>
    ///     Width of the NativeRectFloat
    /// </summary>
    public float Width => _width;

    /// <summary>
    ///     Coordinates of the bottom left
    /// </summary>
    public NativePointFloat BottomLeft => new NativePointFloat(X, Y + Height);

    /// <summary>
    ///     Coordinates of the top left
    /// </summary>
    public NativePointFloat TopLeft => new NativePointFloat(X, Y);

    /// <summary>
    ///     Coordinates of the bottom right
    /// </summary>
    public NativePointFloat BottomRight => new NativePointFloat(X + Width, Y + Height);

    /// <summary>
    ///     Coordinates of the top right
    /// </summary>
    public NativePointFloat TopRight => new NativePointFloat(X + Width, Y);

    /// <summary>
    ///     Location for this NativeRectFloat
    /// </summary>
    public NativePointFloat Location => new NativePointFloat(Left, Top);

    /// <summary>
    ///     Size for this NativeRectFloat
    /// </summary>
    public NativeSizeFloat Size => new NativeSizeFloat(Width, Height);

    /// <summary>
    ///     Cast NativeRect to NativeRectFloat
    /// </summary>
    /// <param name="rectangle">NativeRect</param>
    /// <returns>NativeRectFloat</returns>
    public static implicit operator NativeRectFloat(NativeRect rectangle)
    {
        return new NativeRectFloat(rectangle.Left, rectangle.Top, rectangle.Width, rectangle.Height);
    }

    /// <summary>
    ///     Cast RectangleF to NativeRectFloat
    /// </summary>
    /// <param name="rectangle">RectangleF</param>
    /// <returns>NativeRectFloat</returns>
    public static implicit operator NativeRectFloat(RectangleF rectangle)
    {
        return new NativeRectFloat(rectangle.Left, rectangle.Top, rectangle.Width, rectangle.Height);
    }

    /// <summary>
    ///     Cast Rectangle to NativeRectFloat
    /// </summary>
    /// <param name="rectangle">Rectangle</param>
    /// <returns>NativeRectFloat</returns>
    public static implicit operator NativeRectFloat(Rectangle rectangle)
    {
        return new NativeRectFloat(rectangle.Left, rectangle.Top, rectangle.Width, rectangle.Height);
    }

    /// <summary>
    ///     Calculate the smallest integer rectangle which contains this NativeRectFloat:
    ///     left and top are floored, right and bottom are rounded up. The result is normalized (no negative width or height).
    ///     This makes sure that Right does not drift, as it would when truncating left and width independently.
    /// </summary>
    /// <param name="left">int</param>
    /// <param name="top">int</param>
    /// <param name="width">int</param>
    /// <param name="height">int</param>
    public void GetContainingIntegerBounds(out int left, out int top, out int width, out int height)
    {
        left = (int)Math.Floor(Math.Min(Left, Right));
        top = (int)Math.Floor(Math.Min(Top, Bottom));
        width = (int)Math.Ceiling(Math.Max(Left, Right)) - left;
        height = (int)Math.Ceiling(Math.Max(Top, Bottom)) - top;
    }

    /// <summary>
    ///     Explicit (lossy) cast NativeRectFloat to NativeRect, this results in the smallest integer rectangle which contains the NativeRectFloat (see <see cref="GetContainingIntegerBounds"/>).
    ///     Use Round() to round the location and size instead.
    /// </summary>
    /// <param name="rectangle">NativeRectFloat</param>
    /// <returns>NativeRect</returns>
    public static explicit operator NativeRect(NativeRectFloat rectangle)
    {
        rectangle.GetContainingIntegerBounds(out var left, out var top, out var width, out var height);
        return new NativeRect(left, top, width, height);
    }

    /// <summary>
    ///     Cast NativeRectFloat to RectangleF
    /// </summary>
    /// <param name="rectangle">NativeRectFloat</param>
    /// <returns>RectangleF</returns>
    public static implicit operator RectangleF(NativeRectFloat rectangle)
    {
        return new RectangleF(rectangle.Left, rectangle.Top, rectangle.Width, rectangle.Height);
    }

    /// <summary>
    ///     Explicit (lossy) cast NativeRectFloat to Rectangle, this results in the smallest integer rectangle which contains the NativeRectFloat (see <see cref="GetContainingIntegerBounds"/>).
    /// </summary>
    /// <param name="rectangle">NativeRectFloat</param>
    /// <returns>Rectangle</returns>
    public static explicit operator Rectangle(NativeRectFloat rectangle)
    {
        rectangle.GetContainingIntegerBounds(out var left, out var top, out var width, out var height);
        return new Rectangle(left, top, width, height);
    }

    /// <summary>
    ///     Equals for NativeRectFloat
    /// </summary>
    /// <param name="rectangle1">NativeRectFloat</param>
    /// <param name="rectangle2">NativeRectFloat</param>
    /// <returns>bool true if they are equal</returns>
    public static bool operator ==(NativeRectFloat rectangle1, NativeRectFloat rectangle2)
    {
        return rectangle1.Equals(rectangle2);
    }

    /// <summary>
    ///     Not is operator
    /// </summary>
    /// <param name="rectangle1">NativeRectFloat</param>
    /// <param name="rectangle2">NativeRectFloat</param>
    /// <returns>bool</returns>
    public static bool operator !=(NativeRectFloat rectangle1, NativeRectFloat rectangle2)
    {
        return !rectangle1.Equals(rectangle2);
    }

    /// <inheritdoc />
    [Pure]
    public override string ToString()
    {
        return $"{{Left: {_x}; Top: {_y}; Width: {_width}; Height: {_height};}}";
    }

    /// <summary>
    ///     Equals
    /// </summary>
    /// <param name="rectangle">NativeRectFloat</param>
    /// <returns>bool</returns>
    [Pure]
    public bool Equals(NativeRectFloat rectangle)
    {
        return Math.Abs(rectangle._x - _x) < float.Epsilon
               && Math.Abs(rectangle._y - _y) < float.Epsilon
               && Math.Abs(rectangle._width - _width) < float.Epsilon
               && Math.Abs(rectangle._height - _height) < float.Epsilon;
    }

    /// <summary>
    ///     Checks if this NativeRectFloat is empty
    /// </summary>
    /// <returns>true when empty</returns>
    [Pure]
    public bool IsEmpty => Math.Abs(_width * _height) < float.Epsilon;

    /// <inheritdoc />
    [Pure]
    public override bool Equals(object obj)
    {
        switch (obj)
        {
            case NativeRectFloat f:
                return Equals(f);
            case RectangleF rectangleF:
                return Equals(rectangleF);
        }

        return false;
    }

    /// <inheritdoc />
    [Pure]
    public override int GetHashCode()
    {
        unchecked
        {
            var hashCode = _x.GetHashCode();
            hashCode = (hashCode * 397) ^ _x.GetHashCode();
            hashCode = (hashCode * 397) ^ _y.GetHashCode();
            hashCode = (hashCode * 397) ^ _width.GetHashCode();
            hashCode = (hashCode * 397) ^ _height.GetHashCode();
            return hashCode;
        }
    }

    /// <summary>
    ///     Test if this NativeRectFloat contains the specified NativePoint
    /// </summary>
    /// <param name="point">NativePoint</param>
    /// <returns>true if it contains</returns>
    [Pure]
    public bool Contains(NativePoint point)
    {
        return point.X >= Left && point.X <= Right && point.Y >= Top && point.Y <= Bottom;
    }

    /// <summary>
    /// Decontructor for tuples
    /// </summary>
    /// <param name="location">NativePointFloat</param>
    /// <param name="size">NativeSizeFloat</param>
    public void Deconstruct(out NativePointFloat location, out NativeSizeFloat size)
    {
        location = Location;
        size = Size;
    }

    /// <summary>
    ///     Empty NativeRectFloat
    /// </summary>
    public static NativeRectFloat Empty { get; } = new NativeRectFloat();
}