// Copyright (c) Dapplo and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
using System;
using System.Runtime.InteropServices;

namespace Dapplo.Windows.Input.Structs;

/// <summary>
///     A struct used by SendInput to store information for synthesizing input events such as keystrokes, mouse movement,
///     and mouse clicks.
///     See <a href="https://msdn.microsoft.com/en-us/library/windows/desktop/ms646272(v=vs.85).aspx">LASTINPUTINFO structure</a>
/// </summary>
[StructLayout(LayoutKind.Sequential)]
public struct LastInputInfo
{
    private uint _cbSize;
    private readonly uint _dwTime;

    /// <summary>
    /// The tick count for the last registered input 
    /// </summary>
    public uint TickCountLastInput => _dwTime;

    /// <summary>
    /// The timespan for how long ago the last input was
    /// </summary>
    /// <remarks>
    /// The tick difference is calculated with modular (unchecked) uint arithmetic, so this stays correct when the tick count wraps (every 49.7 days).
    /// </remarks>
    public TimeSpan LastInputTimeSpan => TimeSpan.FromMilliseconds(unchecked((uint)Environment.TickCount - _dwTime));

    /// <summary>
    /// Returns the DateTimeOffset for the tick count of the last input
    /// </summary>
    public DateTimeOffset LastInputDateTime => DateTimeOffset.Now.Subtract(LastInputTimeSpan);

    /// <summary>
    ///     A factory method to simplify creating the LastInputInfo struct
    /// </summary>
    /// <returns>LastInputInfo</returns>
    public static LastInputInfo Create()
    {
        return new LastInputInfo
        {
            _cbSize = (uint)Marshal.SizeOf(typeof(LastInputInfo))
        };
    }
}