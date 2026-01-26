// Copyright (c) Dapplo and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Dapplo.Windows.User32;
using Dapplo.Windows.User32.Structs;
using System;
using System.Runtime.InteropServices;

namespace Dapplo.Windows.Icons;

/// <summary>
/// Win32 native methods for Icon
/// </summary>
public static class NativeCursorMethods
{
    /// <summary>
    ///     See
    ///     <a href="https://msdn.microsoft.com/en-us/library/windows/desktop/ms648389(v=vs.85).aspx">GetCursorInfo function</a>
    ///     Retrieves information about the global cursor.
    /// </summary>
    /// <param name="cursorInfo">CursorInfo structure to fill</param>
    /// <returns>bool</returns>
    [DllImport(User32Api.User32, SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static extern bool GetCursorInfo(ref CursorInfo cursorInfo);

    [DllImport(User32Api.User32, SetLastError = true)]
    internal static extern bool DestroyCursor(IntPtr hCursor);


    [DllImport(User32Api.User32, SetLastError = true)]
    internal static extern IntPtr LoadImage(IntPtr hInst, IntPtr name, int type, int cx, int cy, int fuLoad);

    [DllImport(User32Api.User32, SetLastError = true)]
    internal static extern IntPtr LoadImage(IntPtr hInst, string name, int type, int cx, int cy, int fuLoad);
}