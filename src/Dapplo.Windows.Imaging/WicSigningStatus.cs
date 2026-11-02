// Copyright (c) Dapplo and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;

namespace Dapplo.Windows.Imaging;

/// <summary>
/// The signing status of a WIC component, WICComponentSigning from wincodec.idl.
/// See <a href="https://learn.microsoft.com/en-us/windows/win32/api/wincodec/ne-wincodec-wiccomponentsigning">WICComponentSigning</a>
/// </summary>
[Flags]
public enum WicSigningStatus : uint
{
    /// <summary>
    /// No flags reported
    /// </summary>
    None = 0,

    /// <summary>
    /// WICComponentSigned: the component is signed
    /// </summary>
    Signed = 0x00000001,

    /// <summary>
    /// WICComponentUnsigned: the component is not signed
    /// </summary>
    Unsigned = 0x00000002,

    /// <summary>
    /// WICComponentSafe: the component is safe (allowed) to use, e.g. for an unsigned component which the user or administrator allowed
    /// </summary>
    Safe = 0x00000004,

    /// <summary>
    /// WICComponentDisabled: the component is disabled
    /// </summary>
    Disabled = 0x80000000
}
