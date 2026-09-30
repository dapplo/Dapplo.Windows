// Copyright (c) Dapplo and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
using System.Runtime.InteropServices;

namespace Dapplo.Windows.Citrix.Structs;

/// <summary>
///     This structure is returned when WFQuerySessionInformation is called with WFInfoClasses.ClientLatency
/// </summary>
[StructLayout(LayoutKind.Sequential)]
public struct ClientLatency
{
    private readonly uint _average;
    private readonly uint _last;
    private readonly uint _deviation;

    /// <summary>
    ///     Return the client's average latency
    /// </summary>
    public uint Average => _average;
    /// <summary>
    ///     Return the client's last latency
    /// </summary>
    public uint Last => _last;

    /// <summary>
    ///     Return the client's latency deviation
    /// </summary>
    public uint Deviation => _deviation;
}