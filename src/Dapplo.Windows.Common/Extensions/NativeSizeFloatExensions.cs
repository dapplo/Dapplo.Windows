// Copyright (c) Dapplo and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
using System;
using System.Diagnostics.Contracts;
using Dapplo.Windows.Common.Structs;

namespace Dapplo.Windows.Common.Extensions
{
    /// <summary>
    ///     Helper method for the NativeSizeFloatExensions struct
    /// </summary>
    public static class NativeSizeFloatExensions
    {
        /// <summary>
        /// Create a new NativeSizeFloat, from the supplied one, using the specified width
        /// </summary>
        /// <param name="size">NativeSizeFloat</param>
        /// <param name="width">float</param>
        /// <returns>NativeSizeFloat</returns>
        [Pure]
        public static NativeSizeFloat ChangeWidth(this NativeSizeFloat size, float width)
        {
            return new NativeSizeFloat(width, size.Height);
        }

        /// <summary>
        /// Create a new NativeSizeFloat, from the supplied one, using the specified height
        /// </summary>
        /// <param name="size">NativeSizeFloat</param>
        /// <param name="height">float</param>
        /// <returns>NativeSizeFloat</returns>
        [Pure]
        public static NativeSizeFloat ChangeHeight(this NativeSizeFloat size, float height)
        {
            return new NativeSizeFloat(size.Width, height);
        }

        /// <summary>
        /// Create a NativeSize, using rounded values, from the specified NativeSizeFloat
        /// </summary>
        /// <param name="size">NativeSizeFloat</param>
        /// <returns>NativeSize</returns>
        [Pure]
        public static NativeSize Round(this NativeSizeFloat size)
        {
            return new NativeSize((int)Math.Round(size.Width), (int)Math.Round(size.Height));
        }
    }
}