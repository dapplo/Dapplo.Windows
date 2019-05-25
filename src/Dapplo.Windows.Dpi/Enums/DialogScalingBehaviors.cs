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
    ///    Describes per-monitor DPI scaling behavior overrides for child windows within dialogs. The values in this enumeration are bitfields and can be combined.
    ///
    /// This enum is used with SetDialogControlDpiChangeBehavior in order to override the default per-monitor DPI scaling behavior for a child window within a dialog.
    /// 
    /// These settings only apply to individual controls within dialogs. The dialog-wide per-monitor DPI scaling behavior of a dialog is controlled by DIALOG_DPI_CHANGE_BEHAVIORS.
    /// </summary>
    [Flags]
    public enum DialogScalingBehaviors
    {
        /// <summary>
        ///    The default behavior of the dialog manager. The dialog managed will update the font, size, and position of the child window on DPI changes.
        /// </summary>
        Default = 0,

        /// <summary>
        ///     Prevents the dialog manager from sending an updated font to the child window via WM_SETFONT in response to a DPI change.
        /// </summary>
        DisableFontUpdate = 1,

        /// <summary>
        ///     Prevents the dialog manager from resizing and repositioning the child window in response to a DPI change.
        /// </summary>
        DisableRelayout = 2
    }
}