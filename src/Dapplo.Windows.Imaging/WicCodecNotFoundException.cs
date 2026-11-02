// Copyright (c) Dapplo and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Runtime.InteropServices;

namespace Dapplo.Windows.Imaging;

/// <summary>
/// WIC has no codec for the container format or the stream (WINCODEC_ERR_COMPONENTNOTFOUND).
/// This is a normal case, e.g. when a Store codec like JPEG XL or HEIF isn't installed, so callers can fall back.
/// </summary>
public sealed class WicCodecNotFoundException : COMException
{
    /// <summary>
    /// Create the exception
    /// </summary>
    /// <param name="message">string</param>
    /// <param name="errorCode">the HRESULT</param>
    public WicCodecNotFoundException(string message, int errorCode) : base(message, errorCode)
    {
    }
}
