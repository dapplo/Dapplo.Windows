// Copyright (c) Dapplo and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
using Microsoft.Win32.SafeHandles;

namespace Dapplo.Windows.Gdi32.SafeHandles
{
    /// <summary>
    ///     Base class for all Safe "DC" Handles
    /// </summary>
    public abstract class SafeDcHandle : SafeHandleZeroOrMinusOneIsInvalid
    {
        /// <summary>
        ///     Constructor which passes the SafeHandleZeroOrMinusOneIsInvalid to the base
        /// </summary>
        /// <param name="ownsHandle">
        ///     true to reliably release the handle during the finalization phase; false to prevent reliable
        ///     release (not recommended).
        /// </param>
        protected SafeDcHandle(bool ownsHandle) : base(ownsHandle)
        {
        }
    }
}