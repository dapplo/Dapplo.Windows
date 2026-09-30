// Copyright (c) Dapplo and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
using System;
using System.Diagnostics.CodeAnalysis;

namespace Dapplo.Windows.User32.Enums;

/// <summary>
///     Used to open a desktop
/// </summary>
[Flags]
[SuppressMessage("ReSharper", "InconsistentNaming")]
public enum DesktopAccessRight : uint
{
#pragma warning disable 1591
    DESKTOP_READOBJECTS = 0x00000001,
    DESKTOP_CREATEWINDOW = 0x00000002,
    DESKTOP_CREATEMENU = 0x00000004,
    DESKTOP_HOOKCONTROL = 0x00000008,
    DESKTOP_JOURNALRECORD = 0x00000010,
    DESKTOP_JOURNALPLAYBACK = 0x00000020,
    DESKTOP_ENUMERATE = 0x00000040,
    DESKTOP_WRITEOBJECTS = 0x00000080,
    DESKTOP_SWITCHDESKTOP = 0x00000100,

    /// <summary>
    ///     All the desktop specific rights (0x1FF), this is not GENERIC_ALL
    /// </summary>
    DESKTOP_ALL_SPECIFIC = DESKTOP_READOBJECTS | DESKTOP_CREATEWINDOW | DESKTOP_CREATEMENU |
                           DESKTOP_HOOKCONTROL | DESKTOP_JOURNALRECORD | DESKTOP_JOURNALPLAYBACK |
                           DESKTOP_ENUMERATE | DESKTOP_WRITEOBJECTS | DESKTOP_SWITCHDESKTOP,

    DELETE = 0x00010000,
    READ_CONTROL = 0x00020000,
    WRITE_DAC = 0x00040000,
    WRITE_OWNER = 0x00080000,

    GENERIC_ALL = 0x10000000,
    GENERIC_EXECUTE = 0x20000000,
    GENERIC_WRITE = 0x40000000,
    GENERIC_READ = 0x80000000

#pragma warning restore 1591
}