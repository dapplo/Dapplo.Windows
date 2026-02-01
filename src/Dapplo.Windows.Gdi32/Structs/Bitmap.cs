// Copyright (c) Dapplo and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Runtime.InteropServices;

namespace Dapplo.Windows.Gdi32.Structs;

[StructLayout(LayoutKind.Sequential)]
public struct Bitmap
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