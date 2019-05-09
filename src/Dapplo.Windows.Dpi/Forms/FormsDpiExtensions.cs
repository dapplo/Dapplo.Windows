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

#if !NETSTANDARD2_0
#region using

using System;
using System.Windows.Forms;
using Dapplo.Windows.Messages;

#endregion

namespace Dapplo.Windows.Dpi.Forms
{
    /// <summary>
    ///     Extensions for Windows Form
    /// </summary>
    public static class FormsDpiExtensions
    {
        /// <summary>
        ///     Handle DPI changes for the specified Form
        ///     Using this DOES NOT enable dpi scaling in the non client area, for this you will need to call:
        ///     DpiHandler.TryEnableNonClientDpiScaling(this.Handle) from the WndProc in the WM_NCCREATE message.
        ///     It's better to extend DpiAwareForm, which does this for you. 
        /// </summary>
        /// <param name="form">Control</param>
        /// <returns>DpiHandler</returns>
        public static DpiHandler AttachDpiHandler(this Form form)
        {
            // Create a DpiHandler which runs "outside" of the form (not via WinProc)
            var dpiHandler = new DpiHandler(true);
            dpiHandler.MessageHandler = form.WinProcFormsMessages().Subscribe(message => dpiHandler.HandleWindowMessages(message));
            return dpiHandler;
        }

        /// <summary>
        ///     Handle DPI changes for the specified ContextMenuStrip
        /// </summary>
        /// <param name="contextMenuStrip">ContextMenuStrip</param>
        /// <returns>DpiHandler</returns>
        public static DpiHandler AttachDpiHandler(this ContextMenuStrip contextMenuStrip)
        {
            // Create a DpiHandler which runs "outside" of the contextMenu (not via WinProc)
            var dpiHandler = new DpiHandler(true);
            dpiHandler.MessageHandler = contextMenuStrip.WinProcFormsMessages().Subscribe(message => dpiHandler.HandleContextMenuMessages(message));
            return dpiHandler;
        }
    }
}
#endif