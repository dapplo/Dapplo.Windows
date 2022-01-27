// Copyright (c) Dapplo and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
using System.Diagnostics.Contracts;
using Dapplo.Windows.Common.Structs;

namespace Dapplo.Windows.Common.Extensions;

/// <summary>
///     Helper method for the NativeSizeExensions struct
/// </summary>
public static class NativeSizeExensions
{
    /// <summary>
    /// Create a new NativeSize, from the supplied one, using the specified width
    /// </summary>
    /// <param name="size">NativeSize</param>
    /// <param name="width">int</param>
    /// <returns>NativeSize</returns>
    [Pure]
    public static NativeSize ChangeWidth(this NativeSize size, int width)
    {
        return new NativeSize(width, size.Height);
    }

    /// <summary>
    /// Create a new NativeSize, from the supplied one, using the specified height
    /// </summary>
    /// <param name="size">NativeSize</param>
    /// <param name="height">int</param>
    /// <returns>NativeSize</returns>
    [Pure]
    public static NativeSize ChangeHeight(this NativeSize size, int height)
    {
        return new NativeSize(size.Width, height);
    }
}