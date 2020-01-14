// Copyright (c) Dapplo and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
using System;
using System.Drawing;
using System.Security;

namespace Dapplo.Windows.Gdi32.SafeHandles
{
    /// <summary>
    ///     A hbitmap SafeHandle implementation, use this for disposable usage of HBitmap
    /// </summary>
    public class SafeHBitmapHandle : SafeObjectHandle
    {
        /// <summary>
        ///     Default constructor is needed to support marshalling!!
        /// </summary>
        [SecurityCritical]
        public SafeHBitmapHandle() : base(true)
        {
        }

        /// <summary>
        ///     Create a SafeHBitmapHandle from an existing handle
        /// </summary>
        /// <param name="preexistingHandle">IntPtr to HBitmap</param>
        [SecurityCritical]
        public SafeHBitmapHandle(IntPtr preexistingHandle) : base(true)
        {
            SetHandle(preexistingHandle);
        }

        /// <summary>
        ///     Create a SafeHBitmapHandle from a Bitmap
        /// </summary>
        /// <param name="bitmap">Bitmap to call GetHbitmap on</param>
        [SecurityCritical]
        public SafeHBitmapHandle(Bitmap bitmap) : base(true)
        {
            SetHandle(bitmap.GetHbitmap());
        }
    }
}