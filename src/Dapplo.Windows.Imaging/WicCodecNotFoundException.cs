// Copyright (c) Dapplo and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Runtime.InteropServices;

namespace Dapplo.Windows.Imaging;

/// <summary>
/// WIC has no codec for the container format or the stream (WINCODEC_ERR_COMPONENTNOTFOUND, 0x88982F50),
/// or the codec is registered but can't be initialized (WINCODEC_ERR_COMPONENTINITIALIZEFAILURE, 0x88982F8B, e.g. the HEIF encoder without the HEVC video extension).
/// This is a normal case, e.g. when a Store codec like JPEG XL or HEIF isn't installed, so callers can fall back. <see cref="System.Runtime.InteropServices.ExternalException.ErrorCode"/> tells which.
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
