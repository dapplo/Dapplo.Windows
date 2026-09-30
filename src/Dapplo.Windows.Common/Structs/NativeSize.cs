// Copyright (c) Dapplo and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.Diagnostics.Contracts;
using System.Runtime.InteropServices;
using Dapplo.Windows.Common.TypeConverters;

namespace Dapplo.Windows.Common.Structs;

/// <summary>
///     This structure should be used everywhere where native methods need a size struct.
/// </summary>
[StructLayout(LayoutKind.Sequential)]
[Serializable]
[SuppressMessage("ReSharper", "ConvertToAutoPropertyWithPrivateSetter")]
[TypeConverter(typeof(NativeSizeTypeConverter))]
public readonly struct NativeSize : IEquatable<NativeSize>, IComparable<NativeSize>
{
    private readonly int _width;
    private readonly int _height;

    /// <summary>
    ///     The Width of the size struct
    /// </summary>
    public int Width => _width;

    /// <summary>
    ///     The Width of the size struct
    /// </summary>
    public int Height => _height;

    /// <summary>
    ///     Returns an empty size
    /// </summary>
    public static NativeSize Empty { get; } = new NativeSize(0, 0);

    /// <summary>
    ///     Constructor from S.D.Size
    /// </summary>
    /// <param name="size"></param>
    public NativeSize(System.Drawing.Size size) : this(size.Width, size.Height)
    {
    }

    /// <summary>
    ///     Size contructor
    /// </summary>
    /// <param name="width">int</param>
    /// <param name="height">int</param>
    public NativeSize(int width, int height)
    {
        _width = width;
        _height = height;
    }

    /// <summary>
    ///     Implicit cast from NativeSize to System.Drawing.Size
    /// </summary>
    /// <param name="size">NativeSize</param>
    public static implicit operator System.Drawing.Size(NativeSize size) => new System.Drawing.Size(size.Width, size.Height);

    /// <summary>
    ///     Implicit cast from System.Drawing.Size to NativeSize
    /// </summary>
    /// <param name="size">System.Drawing.Size</param>
    public static implicit operator NativeSize(System.Drawing.Size size) => new NativeSize(size.Width, size.Height);

    /// <summary>
    /// Equals operator overloading
    /// </summary>
    /// <param name="size1">NativeSize</param>
    /// <param name="size2">NativeSize</param>
    /// <returns>bool</returns>
    public static bool operator ==(NativeSize size1, NativeSize size2) => size1.Equals(size2);

    /// <summary>
    /// Not Equals operator overloading
    /// </summary>
    /// <param name="size1">NativeSize</param>
    /// <param name="size2">NativeSize</param>
    /// <returns>bool</returns>
    public static bool operator !=(NativeSize size1, NativeSize size2) => !(size1 == size2);

    /// <summary>
    /// Equals operator overloading
    /// </summary>
    /// <param name="size1">NativeSize</param>
    /// <param name="size2">System.Drawing.Size</param>
    /// <returns>bool</returns>
    public static bool operator ==(NativeSize size1, System.Drawing.Size size2) => size1.Equals(size2);

    /// <summary>
    /// Not Equals operator overloading
    /// </summary>
    /// <param name="size1">NativeSize</param>
    /// <param name="size2">System.Drawing.Size</param>
    /// <returns>bool</returns>
    public static bool operator !=(NativeSize size1, System.Drawing.Size size2) => !(size1 == size2);

    /// <summary>
    /// Equals operator overloading
    /// </summary>
    /// <param name="size1">System.Drawing.Size</param>
    /// <param name="size2">NativeSize</param>
    /// <returns>bool</returns>
    public static bool operator ==(System.Drawing.Size size1, NativeSize size2) => size2.Equals(size1);

    /// <summary>
    /// Not Equals operator overloading
    /// </summary>
    /// <param name="size1">System.Drawing.Size</param>
    /// <param name="size2">NativeSize</param>
    /// <returns>bool</returns>
    public static bool operator !=(System.Drawing.Size size1, NativeSize size2) => !(size1 == size2);

    /// <summary>
    ///     Checks if the width * height are 0
    /// </summary>
    /// <returns>true if the size is empty</returns>
    [Pure]
    public bool IsEmpty => _width * _height == 0;

    /// <inheritdoc />
    /// <remarks>Sizes are ordered by area (width * height, calculated as long so it cannot overflow), smallest first.</remarks>
    [Pure]
    public int CompareTo(NativeSize other) => ((long)_width * _height).CompareTo((long)other._width * other._height);

    /// <inheritdoc />
    [Pure]
    public override string ToString() => $"{{Width: {_width}; Height: {_height};}}";

    /// <inheritdoc />
    [Pure]
    public override bool Equals(object obj)
    {
        switch (obj)
        {
            case NativeSize size:
                return Equals(size);
            case System.Drawing.Size size1:
                return Equals(size1);
        }

        return false;
    }

    /// <inheritdoc />
    [Pure]
    public bool Equals(NativeSize other) => _width == other._width && _height == other._height;

    /// <summary>
    /// Decontructor for tuples
    /// </summary>
    /// <param name="width">int</param>
    /// <param name="height">int</param>
    public void Deconstruct(out int width, out int height)
    {
        width = Width;
        height = Height;
    }

    /// <inheritdoc />
    [Pure]
    public override int GetHashCode()
    {
        unchecked
        {
            return (_width * 397) ^ _height;
        }
    }
}