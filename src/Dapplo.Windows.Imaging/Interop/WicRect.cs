// Copyright (c) Dapplo and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Runtime.InteropServices;

namespace Dapplo.Windows.Imaging.Interop;

/// <summary>
/// WICRect from wincodec.idl
/// </summary>
[StructLayout(LayoutKind.Sequential)]
internal struct WicRect
{
    public int X;
    public int Y;
    public int Width;
    public int Height;

    public WicRect(int x, int y, int width, int height)
    {
        X = x;
        Y = y;
        Width = width;
        Height = height;
    }
}
