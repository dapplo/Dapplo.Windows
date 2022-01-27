// Copyright (c) Dapplo and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
using Dapplo.Windows.Common.Structs;
using System;
using System.Diagnostics.Contracts;

namespace Dapplo.Windows.Common.Extensions;

/// <summary>
///     Helper method for the NativePointFloat struct
/// </summary>
public static class NativePointFloatExensions
{
    /// <summary>
    /// Create a new NativePointFloat, from the supplied one, using the specified X coordinate
    /// </summary>
    /// <param name="point">NativePointFloat</param>
    /// <param name="x">float</param>
    /// <returns>NativePointFloat</returns>
    [Pure]
    public static NativePointFloat ChangeX(this NativePointFloat point, float x)
    {
        return new NativePointFloat(x, point.Y);
    }

    /// <summary>
    /// Create a new NativePointFloat, from the supplied one, using the specified Y coordinate
    /// </summary>
    /// <param name="point">NativePointFloat</param>
    /// <param name="y">float</param>
    /// <returns>NativePointFloat</returns>
    [Pure]
    public static NativePointFloat ChangeY(this NativePointFloat point, float y)
    {
        return new NativePointFloat(point.X, y);
    }

    /// <summary>
    /// Create a new NativePointFloat, from the supplied one, using the specified offset
    /// </summary>
    /// <param name="point">NativePointFloat</param>
    /// <param name="offsetX">float</param>
    /// <param name="offsetY">float</param>
    /// <returns>NativePointFloat</returns>
    [Pure]
    public static NativePointFloat Offset(this NativePointFloat point, float? offsetX = null, float? offsetY = null)
    {
        return new NativePointFloat(point.X + offsetX ?? 0, point.Y + offsetY ?? 0);
    }

    /// <summary>
    /// Create a new NativePointFloat, from the supplied one, using the specified offset
    /// </summary>
    /// <param name="point">NativePointFloat</param>
    /// <param name="offset">NativePointFloat</param>
    /// <returns>NativePointFloat</returns>
    [Pure]
    public static NativePointFloat Offset(this NativePointFloat point, NativePointFloat offset)
    {
        return new NativePointFloat(point.X + offset.X, point.Y + offset.Y);
    }

    /// <summary>
    /// Create a NativePoint, using rounded values, from the specified NativePointFloat
    /// </summary>
    /// <param name="point">NativePointFloat</param>
    /// <returns>NativePoint</returns>
    [Pure]
    public static NativePoint Round(this NativePointFloat point)
    {
        return new NativePoint((int)Math.Round(point.X), (int)Math.Round(point.Y));
    }
}