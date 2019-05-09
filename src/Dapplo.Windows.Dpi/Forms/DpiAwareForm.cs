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
using System;
using System.Diagnostics.CodeAnalysis;
using System.Windows.Forms;
using Dapplo.Windows.Dpi.Enums;
using Dapplo.Windows.Messages;

namespace Dapplo.Windows.Dpi.Forms
{
    /// <summary>
    /// This is a DPI-Aware Form, making DPI support very easy: just extend your Form from this
    /// </summary>
    [SuppressMessage("Sonar Code Smell", "S110:Inheritance tree of classes should not be too deep", Justification = "This is what extending Form does...")]
    public class DpiAwareForm : Form
    {
        private IDisposable _dpiAwarenessContextScope;

        /// <summary>
        /// The DpiHandler used for this form
        /// </summary>
        protected DpiHandler FormDpiHandler { get; } = new DpiHandler();

        /// <inheritdoc />
        protected override void CreateHandle()
        {
            _dpiAwarenessContextScope = NativeDpiMethods.ScopedThreadDpiAwarenessContext(DpiAwarenessContext.PerMonitorAwareV2, DpiAwarenessContext.PerMonitorAware);
            base.CreateHandle();
        }

        /// <inheritdoc />
        protected override void OnHandleCreated(EventArgs e)
        {
            _dpiAwarenessContextScope.Dispose();
            base.OnHandleCreated(e);
        }

        /// <summary>
        /// Override the WndProc to make sure the DpiHandler is informed of the WM_NCCREATE message
        /// </summary>
        /// <param name="m">Message</param>
        protected override void WndProc(ref Message m)
        {
            bool handled = FormDpiHandler.HandleWindowMessages(WindowMessageInfo.Create(m.HWnd, m.Msg, m.WParam, m.LParam));
            if (!handled)
            {
                base.WndProc(ref m);
            }
        }
    }
}
#endif