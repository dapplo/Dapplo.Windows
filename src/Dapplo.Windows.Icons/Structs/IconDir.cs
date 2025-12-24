// Copyright (c) Dapplo and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Runtime.InteropServices;

namespace Dapplo.Windows.Icons.Structs;

/// <summary>
/// Represents the ICONDIR structure in an ICO file.
/// See <a href="https://devblogs.microsoft.com/oldnewthing/20101018-00/?p=12513">The format of icon resources</a>
/// and <a href="https://en.wikipedia.org/wiki/ICO_(file_format)">ICO file format</a>
/// </summary>
[StructLayout(LayoutKind.Sequential, Pack = 1)]
public struct IconDir
{
    /// <summary>
    /// Reserved, must be 0
    /// </summary>
    public ushort Reserved;

    /// <summary>
    /// Resource type: 1 for icons, 2 for cursors
    /// </summary>
    public ushort Type;

    /// <summary>
    /// Number of images (icon directory entries) in this file
    /// </summary>
    public ushort Count;

    /// <summary>
    /// Size of the ICONDIR structure in bytes
    /// </summary>
    public const int Size = 6;

    /// <summary>
    /// Creates a new ICONDIR structure for an icon file
    /// </summary>
    /// <param name="count">Number of icon entries</param>
    /// <returns>IconDir structure</returns>
    public static IconDir CreateIcon(ushort count)
    {
        return new IconDir
        {
            Reserved = 0,
            Type = 1, // Icon
            Count = count
        };
    }

    /// <summary>
    /// Creates a new ICONDIR structure for a cursor file
    /// </summary>
    /// <param name="count">Number of cursor entries</param>
    /// <returns>IconDir structure</returns>
    public static IconDir CreateCursor(ushort count)
    {
        return new IconDir
        {
            Reserved = 0,
            Type = 2, // Cursor
            Count = count
        };
    }
}
