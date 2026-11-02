// Copyright (c) Dapplo and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Runtime.InteropServices;
using Dapplo.Windows.Common.Enums;

namespace Dapplo.Windows.Imaging.Interop;

/// <summary>
/// A color palette for indexed pixel formats (e.g. GIF), methods in the order of wincodec.idl.
/// See <a href="https://learn.microsoft.com/en-us/windows/win32/api/wincodec/nn-wincodec-iwicpalette">IWICPalette interface</a>
/// </summary>
[ComImport]
[InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
[Guid("00000040-a8f2-4877-ba0a-fd2b6645fb94")]
internal interface IWICPalette
{
    [PreserveSig] HResult InitializePredefined(WicBitmapPaletteType ePaletteType, int fAddTransparentColor);
    [PreserveSig] HResult InitializeCustom(nint pColors, uint colorCount);
    [PreserveSig] HResult InitializeFromBitmap(IWICBitmapSource pISurface, uint colorCount, int fAddTransparentColor);
    [PreserveSig] HResult InitializeFromPalette(IWICPalette pIPalette);
    [PreserveSig] HResult GetType(out WicBitmapPaletteType pePaletteType);
    [PreserveSig] HResult GetColorCount(out uint pcCount);
    [PreserveSig] HResult GetColors(uint colorCount, nint pColors, out uint pcActualColors);
    [PreserveSig] HResult IsBlackWhite(out int pfIsBlackWhite);
    [PreserveSig] HResult IsGrayscale(out int pfIsGrayscale);
    [PreserveSig] HResult HasAlpha(out int pfHasAlpha);
}
