// Copyright (c) Dapplo and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Runtime.InteropServices;

namespace Dapplo.Windows.Icons.Structs;

/// <summary>
/// Represents the GRPICONDIR structure used in resource files.
/// This is similar to ICONDIR but used for icon resources in PE files.
/// See <a href="https://devblogs.microsoft.com/oldnewthing/20101018-00/?p=12513">The format of icon resources</a>
/// </summary>
[StructLayout(LayoutKind.Sequential, Pack = 1)]
public struct GrpIconDir
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
    /// Number of images (group icon directory entries) in this resource
    /// </summary>
    public ushort Count;

    /// <summary>
    /// Size of the GRPICONDIR structure in bytes
    /// </summary>
    public const int Size = 6;

    /// <summary>
    /// Creates a new GRPICONDIR structure for an icon resource
    /// </summary>
    /// <param name="count">Number of icon entries</param>
    /// <returns>GrpIconDir structure</returns>
    public static GrpIconDir CreateIcon(ushort count)
    {
        return new GrpIconDir
        {
            Reserved = 0,
            Type = 1, // Icon
            Count = count
        };
    }

    /// <summary>
    /// Creates a new GRPICONDIR structure for a cursor resource
    /// </summary>
    /// <param name="count">Number of cursor entries</param>
    /// <returns>GrpIconDir structure</returns>
    public static GrpIconDir CreateCursor(ushort count)
    {
        return new GrpIconDir
        {
            Reserved = 0,
            Type = 2, // Cursor
            Count = count
        };
    }
}
