// Copyright (c) Dapplo and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Windows.Forms;
using Dapplo.Windows.Messages;
using Dapplo.Windows.Dpi;
using Dapplo.Windows.Forms.Messages;

namespace Dapplo.Windows.Forms.Dpi
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
        ///     The DpiHandler moves / resizes the form to the suggested rectangle of WM_DPICHANGED, WinForms still processes the message too (and can scale the form, when its Per-Monitor V2 support is enabled).
        ///     The DpiHandler follows handle recreation, and is disposed (completing OnDpiChanged) when the form is disposed.
        /// </summary>
        /// <param name="form">Form</param>
        /// <returns>DpiHandler</returns>
        public static DpiHandler AttachDpiHandler(this Form form)
        {
            if (form == null)
            {
                throw new ArgumentNullException(nameof(form));
            }
            // Create a DpiHandler which runs "outside" of the form (not via WinProc)
            var dpiHandler = new DpiHandler(true);
            dpiHandler.MessageHandler = form.WinProcFormsMessages().Subscribe(message => dpiHandler.HandleWindowMessages(message), dpiHandler.Dispose);
            if (form.IsHandleCreated)
            {
                // The WM_CREATE was already processed
                dpiHandler.RefreshDpi(form.Handle);
            }
            return dpiHandler;
        }

        /// <summary>
        ///     Handle DPI changes for the specified ContextMenuStrip, the DpiHandler is disposed (completing OnDpiChanged) when the ContextMenuStrip is disposed.
        /// </summary>
        /// <param name="contextMenuStrip">ContextMenuStrip</param>
        /// <returns>DpiHandler</returns>
        public static DpiHandler AttachDpiHandler(this ContextMenuStrip contextMenuStrip)
        {
            if (contextMenuStrip == null)
            {
                throw new ArgumentNullException(nameof(contextMenuStrip));
            }
            // Create a DpiHandler which runs "outside" of the contextMenu (not via WinProc)
            var dpiHandler = new DpiHandler(true);
            dpiHandler.MessageHandler = contextMenuStrip.WinProcFormsMessages().Subscribe(message => dpiHandler.HandleContextMenuMessages(message), dpiHandler.Dispose);
            return dpiHandler;
        }
    }
}