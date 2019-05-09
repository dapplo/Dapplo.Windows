//  Dapplo - building blocks for desktop applications
//  Copyright (C) 2017-2019  Dapplo
// 
//  For more information see: http://dapplo.net/
//  Dapplo repositories are hosted on GitHub: https://github.com/dapplo
// 
//  This file is part of Dapplo.Windows
// 
//  Dapplo.Windows is free software: you can redistribute it and/or modify
//  it under the terms of the GNU Lesser General Public License as published by
//  the Free Software Foundation, either version 3 of the License, or
//  (at your option) any later version.
// 
//  Dapplo.Windows is distributed in the hope that it will be useful,
//  but WITHOUT ANY WARRANTY; without even the implied warranty of
//  MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
//  GNU Lesser General Public License for more details.
// 
//  You should have a copy of the GNU Lesser General Public License
//  along with Dapplo.Windows. If not, see <http://www.gnu.org/licenses/lgpl.txt>.

using System;

namespace Dapplo.Windows.Dpi.Enums
{
    /// <summary>
    ///    In Per Monitor v2 contexts, dialogs will automatically respond to DPI changes by resizing themselves and re-computing the positions of their child windows (here referred to as re-layouting). This enum works in conjunction with SetDialogDpiChangeBehavior in order to override the default DPI scaling behavior for dialogs.
    ///    This does not affect DPI scaling behavior for the child windows of dialogs(beyond re-layouting), which is controlled by DIALOG_CONTROL_DPI_CHANGE_BEHAVIORS.
       /// </summary>
    [Flags]
    public enum DialogDpiChangeBehaviors
    {
        /// <summary>
        ///    The default behavior of the dialog manager. In response to a DPI change, the dialog manager will re-layout each control, update the font on each control, resize the dialog, and update the dialog's own font.
        /// </summary>
        Default = 0,

        /// <summary>
        ///     Prevents the dialog manager from responding to WM_GETDPISCALEDSIZE and WM_DPICHANGED, disabling all default DPI scaling behavior.
        /// </summary>
        DisableAll = 1,

        /// <summary>
        ///     Prevents the dialog manager from resizing the dialog in response to a DPI change.
        /// </summary>
        DisableResize = 2,

        /// <summary>
        ///     Prevents the dialog manager from re-layouting all of the dialogue's immediate children HWNDs in response to a DPI change.
        /// </summary>
        DisableControlRelayout = 3
    }
}