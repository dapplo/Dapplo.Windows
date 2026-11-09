// Copyright (c) Dapplo and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
namespace Dapplo.Windows.Enums;

/// <summary>
///     What a window on the local desktop shows, see <see cref="Desktop.RemoteDisplayWindow"/>: its own content, or the picture of a session
///     on another machine which a remote display client draws
/// </summary>
public enum RemoteDisplayKinds
{
    /// <summary>
    ///     Not a known remote display client, the window shows its own content
    /// </summary>
    None = 0,

    /// <summary>
    ///     Citrix Workspace app (ICA / HDX): a seamless application or the Desktop Viewer
    /// </summary>
    Citrix,

    /// <summary>
    ///     Microsoft Remote Desktop (RDP): the Remote Desktop Connection (mstsc), the Remote Desktop client or Windows App (msrdc),
    ///     also RemoteApp windows
    /// </summary>
    RemoteDesktop,

    /// <summary>
    ///     VMware / Omnissa Horizon client (Blast / PCoIP)
    /// </summary>
    Horizon,

    /// <summary>
    ///     Hyper-V Virtual Machine Connection (vmconnect)
    /// </summary>
    HyperV,

    /// <summary>
    ///     A VNC viewer (RealVNC, TightVNC, TigerVNC, UltraVNC)
    /// </summary>
    Vnc
}
