// Copyright (c) Dapplo and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Dapplo.Windows.Imaging.Interop;

/// <summary>
/// WICDecodeOptions from wincodec.idl
/// </summary>
internal enum WicDecodeOptions : uint
{
    MetadataCacheOnDemand = 0x00000000,
    MetadataCacheOnLoad = 0x00000001
}

/// <summary>
/// WICBitmapEncoderCacheOption from wincodec.idl
/// </summary>
internal enum WicBitmapEncoderCacheOption : uint
{
    InMemory = 0x00000000,
    TempFile = 0x00000001,
    NoCache = 0x00000002
}

/// <summary>
/// WICBitmapDitherType from wincodec.idl (only the values which are used)
/// </summary>
internal enum WicBitmapDitherType : uint
{
    None = 0x00000000
}

/// <summary>
/// WICBitmapPaletteType from wincodec.idl (only the values which are used)
/// </summary>
internal enum WicBitmapPaletteType : uint
{
    Custom = 0x00000000
}
