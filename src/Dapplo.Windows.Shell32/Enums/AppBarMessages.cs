// Copyright (c) Dapplo and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
namespace Dapplo.Windows.Shell32.Enums
{
    /// <summary>
    /// Sends an appbar message to the system.
    ///     See <a href="https://msdn.microsoft.com/en-us/library/windows/desktop/bb762108.aspx">SHAppBarMessage function</a>
    /// </summary>
    public enum AppBarMessages
    {
        /// <summary>
        /// ABM_NEW - Registers a new appbar and specifies the message identifier that the system should use to send notification messages to the appbar.
        /// </summary>
        New = 0,
        /// <summary>
        /// ABM_REMOVE - Unregisters an appbar, removing the bar from the system's internal list.
        /// </summary>
        Remove = 1,
        /// <summary>
        /// ABM_QUERYPOS - Requests a size and screen position for an appbar.
        /// </summary>
        QueryPosition = 2,
        /// <summary>
        /// ABM_SETPOS - Sets the size and screen position of an appbar.
        /// </summary>
        SetPosition = 3,
        /// <summary>
        /// ABM_GETSTATE - Retrieves the autohide and always-on-top states of the Windows taskbar.
        /// </summary>
        GetState = 4,
        /// <summary>
        /// ABM_GETTASKBARPOS - Retrieves the bounding rectangle of the Windows taskbar. Note that this applies only to the
        /// system taskbar. Other objects, particularly toolbars supplied with third-party software, also can be
        /// present. As a result, some of the screen area not covered by the Windows taskbar might not be visible
        /// to the user. To retrieve the area of the screen not covered by both the taskbar and other app bars—the
        /// working area available to your application—, use the GetMonitorInfo function.
        /// </summary>
        GetTaskbarPosition = 5,
        /// <summary>
        /// ABM_ACTIVATE - Notifies the system to activate or deactivate an appbar. The lParam member of the APPBARDATA pointed to by pData is set to TRUE to activate or FALSE to deactivate.
        /// </summary>
        Activate = 6,
        /// <summary>
        /// ABM_GETAUTOHIDEBAR - Retrieves the handle to the autohide appbar associated with a particular edge of the screen.
        /// </summary>
        GetAutoHideAppBar = 7,
        /// <summary>
        /// ABM_SETAUTOHIDEBAR - Registers or unregisters an autohide appbar for an edge of the screen.
        /// </summary>
        SetAutohideAppBar = 8,
        /// <summary>
        /// ABM_WINDOWPOSCHANGED - Notifies the system when an appbar's position has changed.
        /// </summary>
        WindowPositionChanged = 9,
        /// <summary>
        /// ABM_SETSTATE - Windows XP and later: Sets the state of the appbar's autohide and always-on-top attributes.
        /// </summary>
        SetState = 10,
        /// <summary>
        /// ABM_GETAUTOHIDEBAREX - Windows XP and later: Retrieves the handle to the autohide appbar associated with a particular edge of a particular monitor.
        /// </summary>
        GetAutoHideAppBarExtended = 11,
        /// <summary>
        /// ABM_SETAUTOHIDEBAREX - Windows XP and later: Registers or unregisters an autohide appbar for an edge of a particular monitor.
        /// </summary>
        SetAutoHideAppBarExtended = 12
    }
}
