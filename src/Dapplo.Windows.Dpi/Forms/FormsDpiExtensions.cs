// Copyright (c) Dapplo and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
#if !NETSTANDARD2_0

using System;
using System.Windows.Forms;
using Dapplo.Windows.Messages;

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