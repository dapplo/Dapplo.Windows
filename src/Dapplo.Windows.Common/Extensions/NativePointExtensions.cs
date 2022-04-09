// Copyright (c) Dapplo and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Diagnostics.Contracts;
using Dapplo.Windows.Common.Structs;

namespace Dapplo.Windows.Common.Extensions;

/// <summary>
///     Helper method for the NativePoint struct
/// </summary>
public static class NativePointExtensions
{
    /// <summary>
    /// Create a new NativePoint, from the supplied one, using the specified X coordinate
    /// </summary>
    /// <param name="point">NativePoint</param>
    /// <param name="x">int</param>
    /// <returns>NativePoint</returns>
    [Pure]
    public static NativePoint ChangeX(this NativePoint point, int x)
    {
        return new NativePoint(x, point.Y);
    }

    /// <summary>
    /// Create a new NativePoint, from the supplied one, using the specified Y coordinate
    /// </summary>
    /// <param name="point">NativePoint</param>
    /// <param name="y">int</param>
    /// <returns>NativePoint</returns>
    [Pure]
    public static NativePoint ChangeY(this NativePoint point, int y)
    {
        return new NativePoint(point.X, y);
    }

    /// <summary>
    /// Create a new NativePoint, from the supplied one, using the specified offset
    /// </summary>
    /// <param name="point">NativePoint</param>
    /// <param name="offset">NativePoint</param>
    /// <returns>NativePoint</returns>
    [Pure]
    public static NativePoint Offset(this NativePoint point, NativePoint offset)
    {
        return new NativePoint(point.X + offset.X, point.Y + offset.Y);
    }

    /// <summary>
    /// Create a new NativePoint, from the supplied one, using the specified offset
    /// </summary>
    /// <param name="point">NativePoint</param>
    /// <param name="offsetX">int</param>
    /// <param name="offsetY">int</param>
    /// <returns>NativePoint</returns>
    [Pure]
    public static NativePoint Offset(this NativePoint point, int? offsetX = null, int? offsetY = null)
    {
        return new NativePoint(point.X + offsetX ?? 0, point.Y + offsetY ?? 0);
    }
}