// Copyright (c) Dapplo and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Win32.SafeHandles;
using System;
using System.Security;

namespace Dapplo.Windows.Gdi32.SafeHandles;

/// <summary>
///     Class SafeNonDisposableObjectHandle is used a result of SelectObject, and not disposed
/// </summary>
public class SafeNonDisposableObjectHandle : SafeObjectHandle
{
    /// <summary>
    ///     Default constructor is needed to support marshalling!!
    /// </summary>
    [SecurityCritical]
    public SafeNonDisposableObjectHandle() : base(false)
    {
    }

    /// <summary>
    ///     Create a SafeNonDisposableObjectHandle from an existing handle
    /// </summary>
    /// <param name="preexistingHandle">IntPtr to Object</param>
    [SecurityCritical]
    public SafeNonDisposableObjectHandle(IntPtr preexistingHandle) : base(false)
    {
        SetHandle(preexistingHandle);
    }
}