// Copyright (c) Dapplo and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Drawing;
using System.Security;

namespace Dapplo.Windows.Gdi32.SafeHandles
{
    /// <summary>
    /// A DeviceContext SafeHandle implementation
    /// </summary>
    public class SafeDeviceContextHandle : SafeDcHandle
    {
        private readonly Graphics _graphics;

        /// <summary>
        /// Needed for marshalling return values
        /// </summary>
        [SecurityCritical]
        public SafeDeviceContextHandle() : base(true)
        {
        }

        [SecurityCritical]
        public SafeDeviceContextHandle(Graphics graphics, IntPtr preexistingHandle) : base(true)
        {
            _graphics = graphics;
            SetHandle(preexistingHandle);
        }

        protected override bool ReleaseHandle()
        {
            _graphics.ReleaseHdc(handle);
            return true;
        }

        /// <summary>
        /// Create a SafeDeviceContextHandle from a Graphics object
        /// </summary>
        /// <param name="graphics"></param>
        /// <returns></returns>
        public static SafeDeviceContextHandle FromGraphics(Graphics graphics)
        {
            return new SafeDeviceContextHandle(graphics, graphics.GetHdc());
        }
    }
}
