// Copyright (c) Dapplo and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
#if !NETSTANDARD2_0
using System;
using System.Diagnostics.CodeAnalysis;
using System.Windows.Forms;
using Dapplo.Windows.Dpi.Enums;

namespace Dapplo.Windows.Dpi.Forms
{
    /// <summary>
    /// This is a DPI-Unaware Form, making the form use the Windows build-in scaling, even if the application is DPI Aware.
    /// </summary>
    [SuppressMessage("Sonar Code Smell", "S110:Inheritance tree of classes should not be too deep", Justification = "This is what extending Form does...")]
    public class DpiUnawareForm : Form
    {
        private IDisposable _dpiAwarenessContextScope;

        /// <inheritdoc />
        protected override void CreateHandle()
        {
            _dpiAwarenessContextScope = NativeDpiMethods.ScopedThreadDpiAwarenessContext(DpiAwarenessContext.Unaware);
            base.CreateHandle();
        }

        /// <inheritdoc />
        protected override void OnHandleCreated(EventArgs e)
        {
            _dpiAwarenessContextScope.Dispose();
            base.OnHandleCreated(e);
        }
    }
}
#endif