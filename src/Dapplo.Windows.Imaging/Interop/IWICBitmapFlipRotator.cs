// Copyright (c) Dapplo and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Runtime.InteropServices;
using Dapplo.Windows.Common.Enums;

namespace Dapplo.Windows.Imaging.Interop;

/// <summary>
/// Flips and rotates a source, inherits IWICBitmapSource: those methods are repeated first, in the order of wincodec.idl.
/// See <a href="https://learn.microsoft.com/en-us/windows/win32/api/wincodec/nn-wincodec-iwicbitmapfliprotator">IWICBitmapFlipRotator interface</a>
/// </summary>
[ComImport]
[InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
[Guid("5009834F-2D6A-41ce-9E1B-17C5AFF7A782")]
internal interface IWICBitmapFlipRotator
{
    // IWICBitmapSource
    [PreserveSig] HResult GetSize(out uint width, out uint height);
    [PreserveSig] HResult GetPixelFormat(out Guid pixelFormat);
    [PreserveSig] HResult GetResolution(out double dpiX, out double dpiY);
    [PreserveSig] HResult CopyPalette(nint palette);
    [PreserveSig] HResult CopyPixels(nint rectangle, uint stride, uint bufferSize, nint buffer);

    // IWICBitmapFlipRotator
    [PreserveSig] HResult Initialize(IWICBitmapSource pISource, WicBitmapTransformOptions options);
}

/// <summary>
/// WICBitmapTransformOptions from wincodec.idl
/// </summary>
[Flags]
internal enum WicBitmapTransformOptions : uint
{
    Rotate0 = 0x0,
    Rotate90 = 0x1,
    Rotate180 = 0x2,
    Rotate270 = 0x3,
    FlipHorizontal = 0x8,
    FlipVertical = 0x10
}
