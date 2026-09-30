// Copyright (c) Dapplo and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
using System.Diagnostics.CodeAnalysis;
using System.Windows.Forms;
using Dapplo.Windows.Dpi.Enums;
using Dapplo.Windows.Messages;
using Dapplo.Windows.Messages.Enums;
using Dapplo.Windows.Dpi;

namespace Dapplo.Windows.Forms.Dpi
{
    /// <summary>
    /// A Form which is created Per-Monitor (v2) DPI aware, and which publishes the DPI (changes) via <see cref="FormDpiHandler"/>.
    /// </summary>
    /// <remarks>
    /// What this does:
    /// <list type="bullet">
    /// <item>The window handle is created with the Per-Monitor (v2) DPI awareness context (Per-Monitor v1 as fallback), even when the process is not configured for it.</item>
    /// <item>Non client scaling is enabled for Per-Monitor v1 (Windows 10 1607), Per-Monitor v2 does this by itself.</item>
    /// <item>WM_DPICHANGED is always passed to WinForms. When WinForms handles the DPI change (it raises <see cref="Form.DpiChanged"/>), it scales the fonts and controls and applies the suggested window bounds.
    /// This is what .NET (Core) WinForms does, .NET Framework 4.7+ does this when Per-Monitor V2 is configured in the app.config (System.Windows.Forms.ApplicationConfigurationSection DpiAwareness).</item>
    /// <item>When WinForms did not handle the DPI change, e.g. on .NET Framework without the app.config setting, only the window bounds are adjusted to the rectangle Windows suggests.
    /// Scaling the content is then up to you, use <see cref="FormDpiHandler"/>.OnDpiChanged (and e.g. the BitmapScaleHandler).</item>
    /// <item>A cancelled <see cref="Form.DpiChanged"/> (DpiChangedEventArgs.Cancel) is respected, the bounds are not changed.</item>
    /// </list>
    /// The <see cref="FormDpiHandler"/> survives handle recreation, and is disposed together with the form.
    /// </remarks>
    [SuppressMessage("Sonar Code Smell", "S110:Inheritance tree of classes should not be too deep", Justification = "This is what extending Form does...")]
    public class DpiAwareForm : Form
    {
        // Set when WinForms raised the DpiChanged, while processing the current WM_DPICHANGED
        private bool _dpiChangedByWinForms;

        /// <summary>
        /// Default constructor
        /// </summary>
        public DpiAwareForm()
        {
            // WinForms is the only one which knows if it applied the suggested rectangle, so the DpiHandler must not do it
            FormDpiHandler.ApplySuggestedWindowRect = false;
        }

        /// <summary>
        /// The DpiHandler used for this form, OnDpiChanged publishes the DPI of the form (the first time when the handle is created) and completes when the form is disposed.
        /// </summary>
        protected DpiHandler FormDpiHandler { get; } = new DpiHandler();

        /// <summary>
        /// Creates the handle with a Per Monitor (v2) DPI awareness context, the thread DPI awareness context is restored directly afterwards.
        /// </summary>
        protected override void CreateHandle()
        {
            using (NativeDpiMethods.ScopedThreadDpiAwarenessContext(DpiAwarenessContext.PerMonitorAwareV2, DpiAwarenessContext.PerMonitorAware))
            {
                base.CreateHandle();
            }
        }

        /// <summary>
        /// Registers that WinForms processed the DPI change
        /// </summary>
        /// <param name="e">DpiChangedEventArgs</param>
        protected override void OnDpiChanged(DpiChangedEventArgs e)
        {
            _dpiChangedByWinForms = true;
            base.OnDpiChanged(e);
        }

        /// <summary>
        /// Override the WndProc to inform the DpiHandler, WinForms always processes the message too.
        /// </summary>
        /// <param name="m">Message</param>
        protected override void WndProc(ref Message m)
        {
            var messageInfo = new WindowMessage(m.HWnd, (WindowsMessages)m.Msg, m.WParam, m.LParam);
            if (messageInfo.Msg != WindowsMessages.WM_DPICHANGED)
            {
                if (messageInfo.Msg == WindowsMessages.WM_NCCREATE)
                {
                    // Enable the non client scaling before WinForms (and DefWindowProc) processes the WM_NCCREATE
                    FormDpiHandler.HandleWindowMessages(messageInfo);
                    base.WndProc(ref m);
                    return;
                }
                base.WndProc(ref m);
                FormDpiHandler.HandleWindowMessages(messageInfo);
                return;
            }

            _dpiChangedByWinForms = false;
            base.WndProc(ref m);
            if (!_dpiChangedByWinForms)
            {
                // WinForms didn't process the DPI change, at least move / resize the window to what Windows suggests
                DpiHandler.ApplySuggestedRect(m.HWnd, m.LParam);
            }
            _dpiChangedByWinForms = false;
            // Publish the DPI change, after WinForms scaled
            FormDpiHandler.HandleWindowMessages(messageInfo);
        }

        /// <summary>
        /// Disposes the FormDpiHandler, which completes the OnDpiChanged
        /// </summary>
        /// <param name="disposing">bool</param>
        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);
            if (disposing)
            {
                FormDpiHandler.Dispose();
            }
        }
    }
}
