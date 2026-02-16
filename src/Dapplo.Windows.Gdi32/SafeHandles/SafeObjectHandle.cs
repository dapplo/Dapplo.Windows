// Copyright (c) Dapplo and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using Microsoft.Win32.SafeHandles;

namespace Dapplo.Windows.Gdi32.SafeHandles;

/// <summary>
///     Abstract class SafeObjectHandle which contains all handles that are cleaned with DeleteObject
/// </summary>
public abstract class SafeObjectHandle : SafeHandleZeroOrMinusOneIsInvalid
{
    /// <summary>
    ///     Create SafeObjectHandle
    /// </summary>
    /// <param name="ownsHandle">True if the class owns the handle</param>
    protected SafeObjectHandle(bool ownsHandle) : base(ownsHandle)
    {
    }

    /// <summary>
    ///     Calls DeleteObject
    /// </summary>
    /// <returns>true if this worked</returns>
    protected override bool ReleaseHandle()
    {
        if (handle != IntPtr.Zero)
        {
            return Gdi32Api.DeleteObject(handle);
        }

        return false;
    }
}