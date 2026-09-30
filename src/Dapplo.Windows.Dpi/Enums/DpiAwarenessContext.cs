// Copyright (c) Dapplo and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
using System;

namespace Dapplo.Windows.Dpi.Enums;

/// <summary>
///     Represents a DPI_AWARENESS_CONTEXT, which is a pointer sized handle and not an enum.
///     The static members (<see cref="Unaware"/>, <see cref="SystemAware"/>, <see cref="PerMonitorAware"/>, <see cref="PerMonitorAwareV2"/> and <see cref="UnawareGdiScaled"/>)
///     are the well-known pseudo handles which can be passed to the Windows API.
///     The values returned by Windows, e.g. from <see cref="NativeDpiMethods.GetThreadDpiAwarenessContext"/>, are real handles which differ from the pseudo handles,
///     so never compare these with == or Equals, but use <see cref="NativeDpiMethods.AreDpiAwarenessContextsEqual"/>.
///     See <a href="https://learn.microsoft.com/en-us/windows/win32/hidpi/dpi-awareness-context">DPI_AWARENESS_CONTEXT handle</a>
/// </summary>
public readonly struct DpiAwarenessContext
{
    /// <summary>
    ///     Create a DpiAwarenessContext from a native handle value
    /// </summary>
    /// <param name="value">IntPtr with the DPI_AWARENESS_CONTEXT handle</param>
    public DpiAwarenessContext(IntPtr value)
    {
        Value = value;
    }

    /// <summary>
    ///     The native DPI_AWARENESS_CONTEXT handle value
    /// </summary>
    public IntPtr Value { get; }

    /// <summary>
    ///     True if this is a NULL handle, which is what Windows returns when a call failed
    /// </summary>
    public bool IsNull => Value == IntPtr.Zero;

    /// <summary>
    ///     The NULL DPI_AWARENESS_CONTEXT, this is not a valid context and is returned by Windows when a call failed
    /// </summary>
    public static DpiAwarenessContext Null => default;

    /// <summary>
    ///     DPI unaware.
    ///     This window does not scale for DPI changes and is always assumed to have a scale factor of 100% (96 DPI).
    ///     It will be automatically scaled by the system on any other DPI setting.
    /// </summary>
    public static DpiAwarenessContext Unaware { get; } = new DpiAwarenessContext(new IntPtr(-1));

    /// <summary>
    ///     System DPI aware.
    ///     This window does not scale for DPI changes.
    ///     It will query for the DPI once and use that value for the lifetime of the process.
    ///     If the DPI changes, the process will not adjust to the new DPI value.
    ///     It will be automatically scaled up or down by the system when the DPI changes from the system value.
    /// </summary>
    public static DpiAwarenessContext SystemAware { get; } = new DpiAwarenessContext(new IntPtr(-2));

    /// <summary>
    ///     Per monitor DPI aware.
    ///     This window checks for the DPI when it is created and adjusts the scale factor whenever the DPI changes.
    ///     These processes are not automatically scaled by the system.
    /// </summary>
    public static DpiAwarenessContext PerMonitorAware { get; } = new DpiAwarenessContext(new IntPtr(-3));

    /// <summary>
    ///     Also known as Per Monitor v2. An advancement over the original per-monitor DPI awareness mode, which enables applications to access new DPI-related scaling behaviors on a per top-level window basis.
    ///     Per Monitor v2 was made available in the Creators Update of Windows 10, and is not available on earlier versions of the operating system.
    ///     The additional behaviors introduced are as follows:
    ///     * Child window DPI change notifications - In Per Monitor v2 contexts, the entire window tree is notified of any DPI changes that occur.
    ///     * Scaling of non-client area - All windows will automatically have their non-client area drawn in a DPI sensitive fashion. Calls to EnableNonClientDpiScaling are unnecessary.
    ///     * Scaling of Win32 menus - All NTUSER menus created in Per Monitor v2 contexts will be scaling in a per-monitor fashion.
    ///     * Dialog Scaling - Win32 dialogs created in Per Monitor v2 contexts will automatically respond to DPI changes.
    ///     * Improved scaling of comctl32 controls - Various comctl32 controls have improved DPI scaling behavior in Per Monitor v2 contexts.
    ///     * Improved theming behavior - UxTheme handles opened in the context of a Per Monitor v2 window will operate in terms of the DPI associated with that window.
    /// </summary>
    public static DpiAwarenessContext PerMonitorAwareV2 { get; } = new DpiAwarenessContext(new IntPtr(-4));

    /// <summary>
    ///     DPI unaware with improved quality of GDI-based content.
    ///     This mode behaves similarly to <see cref="Unaware"/>, but also enables the system to automatically improve the rendering quality of text and other GDI-based primitives when the window is displayed on a high-DPI monitor.
    ///     Available starting with Windows 10 version 1809 (build 17763).
    /// </summary>
    public static DpiAwarenessContext UnawareGdiScaled { get; } = new DpiAwarenessContext(new IntPtr(-5));

    /// <inheritdoc />
    public override string ToString()
    {
        return $"DpiAwarenessContext(0x{Value.ToInt64():X})";
    }
}
