// Copyright (c) Dapplo and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Runtime.InteropServices;

namespace Dapplo.Windows.Gdi32.Structs;

/// <summary>
/// Represents a bitmap image, providing properties to access its width and height in pixels.
/// See <a href="https://learn.microsoft.com/en-us/windows/win32/api/wingdi/ns-wingdi-bitmap">BITMAP structure</a>
/// </summary>
/// <remarks>This structure is used to define the dimensions of a bitmap image. The width and height can be set to
/// modify the bitmap's size.</remarks>
[StructLayout(LayoutKind.Sequential)]
public struct GdiBitmap
{
    private int _bmType;
    private int _bmWidth;
    private int _bmHeight;
    private int _bmWidthBytes;
    private short _bmPlanes;
    private short _bmBitsPixel;
    private IntPtr _bmBits;

    /// <summary>
    ///     The width of the bitmap, in pixels.
    /// </summary>
    public int Width
    {
        get => _bmWidth;
        set => _bmWidth = value;
    }

    /// <summary>
    ///     The height of the bitmap, in pixels.
    /// </summary>
    public int Height
    {
        get => _bmHeight;
        set => _bmHeight = value;
    }

}