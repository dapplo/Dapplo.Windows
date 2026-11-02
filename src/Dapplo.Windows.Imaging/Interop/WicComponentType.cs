// Copyright (c) Dapplo and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;

namespace Dapplo.Windows.Imaging.Interop;

/// <summary>
/// WICComponentType from wincodec.idl
/// </summary>
[Flags]
internal enum WicComponentType : uint
{
    Decoder = 0x00000001,
    Encoder = 0x00000002,
    PixelFormatConverter = 0x00000004,
    MetadataReader = 0x00000008,
    MetadataWriter = 0x00000010,
    PixelFormat = 0x00000020,
    AllComponents = 0x0000003F
}
