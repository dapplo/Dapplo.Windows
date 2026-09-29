// Copyright (c) Dapplo and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
#if !NETSTANDARD2_0
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
        /// <summary>
        /// Creates the handle with an unaware DPI awareness context, the thread DPI awareness context is restored directly afterwards.
        /// </summary>
        protected override void CreateHandle()
        {
            using (NativeDpiMethods.ScopedThreadDpiAwarenessContext(DpiAwarenessContext.Unaware))
            {
                base.CreateHandle();
            }
        }
    }
}
#endif