// Copyright (c) Dapplo and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
using System;
using System.Security;

namespace Dapplo.Windows.Gdi32.SafeHandles;

/// <summary>
///     A DIB Section SafeHandle implementation
/// </summary>
public class SafeDibSectionHandle : SafeObjectHandle
{
    /// <summary>
    ///     Default constructor is needed to support marshalling!!
    /// </summary>
    [SecurityCritical]
    public SafeDibSectionHandle() : base(true)
    {
    }

    /// <summary>
    ///     Create a SafeDibSectionHandle for an existing DIB Section
    /// </summary>
    /// <param name="preexistingHandle"></param>
    [SecurityCritical]
    public SafeDibSectionHandle(IntPtr preexistingHandle) : base(true)
    {
        SetHandle(preexistingHandle);
    }
}