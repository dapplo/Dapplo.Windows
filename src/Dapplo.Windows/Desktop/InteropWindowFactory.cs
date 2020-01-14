// Copyright (c) Dapplo and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
using System;

namespace Dapplo.Windows.Desktop
{
    /// <summary>
    ///     Factory for InteropWindows
    /// </summary>
    public static class InteropWindowFactory
    {
        /// <summary>
        ///     Factory method to create a InteropWindow for the supplied handle
        /// </summary>
        /// <param name="handle">int</param>
        /// <returns>InteropWindow</returns>
        public static InteropWindow CreateFor(int handle)
        {
            return CreateFor(new IntPtr(handle));
        }

        /// <summary>
        ///     Factory method to create a InteropWindow for the supplied handle
        /// </summary>
        /// <param name="handle">IntPtr</param>
        /// <returns>InteropWindow</returns>
        public static InteropWindow CreateFor(IntPtr handle)
        {
            return new InteropWindow(handle);
        }
    }
}