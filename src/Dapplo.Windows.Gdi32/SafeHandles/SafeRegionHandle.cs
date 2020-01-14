// Copyright (c) Dapplo and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
using System;
using System.Security;

namespace Dapplo.Windows.Gdi32.SafeHandles
{
    /// <summary>
    ///     A hRegion SafeHandle implementation
    /// </summary>
    public class SafeRegionHandle : SafeObjectHandle
    {
        /// <summary>
        ///     Default constructor is needed to support marshalling!!
        /// </summary>
        [SecurityCritical]
        public SafeRegionHandle() : base(true)
        {
        }

        /// <summary>
        ///     Create a SafeRegionHandle from an existing handle
        /// </summary>
        /// <param name="preexistingHandle">IntPtr to region</param>
        [SecurityCritical]
        public SafeRegionHandle(IntPtr preexistingHandle) : base(true)
        {
            SetHandle(preexistingHandle);
        }

        /// <summary>
        ///     Directly call Gdi32.CreateRectRgn
        /// </summary>
        /// <param name="left"></param>
        /// <param name="top"></param>
        /// <param name="right"></param>
        /// <param name="bottom"></param>
        /// <returns>SafeRegionHandle</returns>
        public static SafeRegionHandle CreateRectRgn(int left, int top, int right, int bottom)
        {
            return Gdi32Api.CreateRectRgn(left, top, right, bottom);
        }
    }
}