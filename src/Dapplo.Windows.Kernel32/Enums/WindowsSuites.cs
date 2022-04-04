﻿// Copyright (c) Dapplo and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
using System;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;

namespace Dapplo.Windows.Kernel32.Enums;

/// <summary>
///     A bit mask that identifies the product suites available on the system. This member can be a combination of the
///     following values.
///     See
///     <a href="https://msdn.microsoft.com/en-us/library/windows/desktop/ms724833(v=vs.85).aspx">OSVERSIONINFOEX structure</a>
/// </summary>
[Flags]
[SuppressMessage("ReSharper", "InconsistentNaming")]
public enum WindowsSuites : ushort
{
#pragma warning disable 1591
    [Description("Microsoft Small Business Server was once installed on the system.")] SmallBusiness = 0x00000001,
    [Description("Enterprise Edition, or Advanced Server is installed.")] Enterprise = 0x00000002,
    [Description("Microsoft BackOffice components are installed.")] BackOffice = 0x00000004,
    [Description("")] CommunicationServer = 0x00000008,
    [Description("Terminal Services is installed.")] TerminalServer = 0x00000010,
    [Description("Microsoft Small Business Server is installed with the restrictive client license in force.")] SmallBusinessRestricted = 0x00000020,
    [Description("Windows XP Embedded is installed.")] EmbeddedNT = 0x00000040,
    [Description("Windows Server 2008 Datacenter, Windows Server 2003, Datacenter Edition, or Windows 2000 Datacenter Server is installed.")] DataCenter = 0x00000080,
    [Description("Remote Desktop is supported, but only one interactive session is supported.")] SingleUserTS = 0x00000100,
    [Description(" Home Edition is installed")] Personal = 0x00000200,
    [Description("Web Edition is installed.")] Blade = 0x00000400,
    [Description("Embedded 'restricted'.")] EmbeddedRestricted = 0x00000800,
    [Description("Security appliance")] SecurityAppliance = 0x00001000,
    [Description("Storage Server is installed")] StorageServer = 0x00002000,
    [Description("Windows Server 2003, Compute Cluster Edition is installed.")] ComputeServer = 0x00004000,
    [Description("Windows Home Server is installed.")] WHServer = 0x00008000
#pragma warning restore 1591
}