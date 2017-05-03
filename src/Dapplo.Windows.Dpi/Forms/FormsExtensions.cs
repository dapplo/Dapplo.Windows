//  Dapplo - building blocks for desktop applications
//  Copyright (C) 2016-2017 Dapplo
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

#region using

using System.Windows.Forms;

#endregion

namespace Dapplo.Windows.Dpi.Forms
{
    /// <summary>
    ///     Extensions for Windows Form
    /// </summary>
    public static class FormsExtensions
    {
        /// <summary>
        ///     Handle DPI changes for the specified Control (Form, ContextMenu etc)
        ///     Using this doesn't enable dpi scaling in the non client area, for this you will need to call:
        ///     DpiHandler.TryEnableNonClientDpiScaling(this.Handle) from the WndProc in the WM_NCCREATE message
        /// </summary>
        /// <param name="control">Control</param>
        /// <returns>DpiHandler</returns>
        public static DpiHandler AttachFormDpiHandler(this Control control)
        {
            var dpiHandler = new DpiHandler();
            var listener = new WinProcListener(control);
            listener.AddHook(dpiHandler.HandleMessages);
            dpiHandler.MessageHandler = listener;
            return dpiHandler;
        }
    }
}