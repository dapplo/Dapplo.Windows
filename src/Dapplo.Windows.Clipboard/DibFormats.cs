// Copyright (c) Dapplo and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
using System;

namespace Dapplo.Windows.Clipboard;

/// <summary>
/// The device independent bitmap formats to write with <see cref="ClipboardDibExtensions.SetAsDib"/>
/// </summary>
[Flags]
public enum DibFormats
{
    /// <summary>
    /// Nothing
    /// </summary>
    None = 0,

    /// <summary>
    /// CF_DIB: BITMAPINFOHEADER, 32 bpp BI_RGB. Understood by nearly every application, but many of them ignore the alpha channel.
    /// </summary>
    Dib = 1,

    /// <summary>
    /// CF_DIBV5: BITMAPV5HEADER, 32 bpp BI_BITFIELDS with an alpha mask, sRGB, straight (not premultiplied) alpha.
    /// </summary>
    DibV5 = 2
}
