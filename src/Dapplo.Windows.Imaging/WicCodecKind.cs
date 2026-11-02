// Copyright (c) Dapplo and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Dapplo.Windows.Imaging;

/// <summary>
/// The kind of a WIC codec
/// </summary>
public enum WicCodecKind
{
    /// <summary>
    /// The codec reads (decodes) images, WICDecoder
    /// </summary>
    Decoder,

    /// <summary>
    /// The codec writes (encodes) images, WICEncoder
    /// </summary>
    Encoder
}
