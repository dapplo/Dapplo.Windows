// Copyright (c) Dapplo and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
using System;
using System.Collections.Generic;
using System.IO;
using Dapplo.Windows.Enums;
using Dapplo.Windows.Kernel32;
using Dapplo.Windows.User32;
using Dapplo.Windows.User32.Enums;

namespace Dapplo.Windows.Desktop;

/// <summary>
///     Finds out if a window on the local desktop is the picture of a session on another machine, drawn by a remote display client: Citrix
///     Workspace app, Remote Desktop (mstsc, msrdc / Windows App), VMware / Omnissa Horizon, Hyper-V vmconnect or a VNC viewer.
///     Such a window is a bitmap for Windows: UI Automation has no text or elements for its content, so e.g. reading the text of the window
///     has to fall back to OCR.
/// </summary>
/// <remarks>
///     The check uses the name of the executable of the process which owns the top-level window, and the class names of the window and
///     its top-level window, so it is cheap and needs nothing of the remote display client. This is the opposite question of
///     Dapplo.Windows.Citrix: WinFrame tells if the current process runs inside a Citrix session.
/// </remarks>
public static class RemoteDisplayWindow
{
    // The executables of remote display clients, every window of these processes shows a remote session (apart from small dialogs of the client)
    private static readonly Dictionary<string, RemoteDisplayKinds> ProcessKinds = new(StringComparer.OrdinalIgnoreCase)
    {
        // Citrix Workspace app: the ICA engine draws seamless applications and the session, CDViewer is the Desktop Viewer
        ["wfica32"] = RemoteDisplayKinds.Citrix,
        ["wfica64"] = RemoteDisplayKinds.Citrix,
        ["wfica"] = RemoteDisplayKinds.Citrix,
        ["CDViewer"] = RemoteDisplayKinds.Citrix,
        // Remote Desktop client (Azure Virtual Desktop) and Windows App, the sessions are drawn by msrdc
        ["msrdc"] = RemoteDisplayKinds.RemoteDesktop,
        // VMware / Omnissa Horizon client, the session window belongs to the remote MKS process (vmware-view is the launcher with local UI)
        ["vmware-remotemks"] = RemoteDisplayKinds.Horizon,
        // Hyper-V Virtual Machine Connection
        ["vmconnect"] = RemoteDisplayKinds.HyperV,
        // VNC viewers
        ["vncviewer"] = RemoteDisplayKinds.Vnc,
        ["tvnviewer"] = RemoteDisplayKinds.Vnc
    };

    // Window classes of remote display clients, for processes whose other windows are local (mstsc shows its connection dialog locally)
    private static readonly Dictionary<string, RemoteDisplayKinds> ClassKinds = new(StringComparer.OrdinalIgnoreCase)
    {
        // Citrix seamless application windows
        ["Transparent Windows Client"] = RemoteDisplayKinds.Citrix,
        // Remote Desktop: the session window, its output window and RemoteApp windows
        ["TscShellContainerClass"] = RemoteDisplayKinds.RemoteDesktop,
        ["IHWindowClass"] = RemoteDisplayKinds.RemoteDesktop,
        ["OPContainerClass"] = RemoteDisplayKinds.RemoteDesktop,
        ["RAIL_WINDOW"] = RemoteDisplayKinds.RemoteDesktop
    };

    /// <summary>
    ///     Is the window the picture of a session on another machine, see <see cref="GetKind(IntPtr)"/>
    /// </summary>
    /// <param name="windowHandle">IntPtr with the handle of a top-level or child window</param>
    /// <returns>true when the window belongs to a known remote display client</returns>
    public static bool IsRemoteDisplay(IntPtr windowHandle) => GetKind(windowHandle) != RemoteDisplayKinds.None;

    /// <summary>
    ///     Is the window the picture of a session on another machine, see <see cref="GetKind(IntPtr)"/>
    /// </summary>
    /// <param name="window">IInteropWindow</param>
    /// <returns>true when the window belongs to a known remote display client</returns>
    public static bool IsRemoteDisplay(IInteropWindow window) => GetKind(window) != RemoteDisplayKinds.None;

    /// <summary>
    ///     Which remote display client draws the window, if any
    /// </summary>
    /// <param name="window">IInteropWindow</param>
    /// <returns>RemoteDisplayKinds, None for a null window</returns>
    public static RemoteDisplayKinds GetKind(IInteropWindow window) => window is null ? RemoteDisplayKinds.None : GetKind(window.Handle);

    /// <summary>
    ///     Which remote display client draws the window, if any. The window and its top-level window (GA_ROOT) are checked, so a child window
    ///     (e.g. the output window of a Remote Desktop session) gives the same answer as its top-level window.
    /// </summary>
    /// <param name="windowHandle">IntPtr with the handle of a top-level or child window</param>
    /// <returns>RemoteDisplayKinds, None when the window doesn't exist or belongs to no known remote display client</returns>
    public static RemoteDisplayKinds GetKind(IntPtr windowHandle)
    {
        if (windowHandle == IntPtr.Zero || !User32Api.IsWindow(windowHandle))
        {
            return RemoteDisplayKinds.None;
        }
        var root = User32Api.GetAncestor(windowHandle, GetAncestorFlags.GA_ROOT);
        if (root == IntPtr.Zero)
        {
            root = windowHandle;
        }
        User32Api.GetWindowThreadProcessId(root, out var processId);
        var processName = processId == 0 ? null : Path.GetFileNameWithoutExtension(Kernel32Api.GetProcessPath(processId) ?? string.Empty);

        var kind = Classify(processName, User32Api.GetClassname(root));
        if (kind == RemoteDisplayKinds.None && root != windowHandle)
        {
            kind = Classify(null, User32Api.GetClassname(windowHandle));
        }
        return kind;
    }

    /// <summary>
    ///     The rules of <see cref="GetKind(IntPtr)"/>, without a window: which remote display client a window with this process and class is
    /// </summary>
    /// <param name="processName">string with the name of the executable, with or without ".exe", null or empty when unknown</param>
    /// <param name="className">string with the class name of the window, null or empty when unknown</param>
    /// <returns>RemoteDisplayKinds</returns>
    public static RemoteDisplayKinds Classify(string processName, string className)
    {
        if (!string.IsNullOrEmpty(processName))
        {
            if (processName.EndsWith(".exe", StringComparison.OrdinalIgnoreCase))
            {
                processName = processName.Substring(0, processName.Length - 4);
            }
            if (ProcessKinds.TryGetValue(processName, out var processKind))
            {
                return processKind;
            }
        }
        if (!string.IsNullOrEmpty(className) && ClassKinds.TryGetValue(className, out var classKind))
        {
            return classKind;
        }
        return RemoteDisplayKinds.None;
    }
}
