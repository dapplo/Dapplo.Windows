﻿//  Dapplo - building blocks for desktop applications
//  Copyright (C) 2017-2018  Dapplo
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
using System.Diagnostics.CodeAnalysis;
using System.Windows.Forms;
using Dapplo.Windows.Common;
using Dapplo.Windows.Dpi.Enums;

namespace Dapplo.Windows.Dpi.Forms
{
    /// <summary>
    /// This is a DPI-Unaware Form, making the form use the Windows build-in scaling, even if the application is DPI Aware.
    /// </summary>
    [SuppressMessage("Sonar Code Smell", "S110:Inheritance tree of classes should not be too deep", Justification = "This is what extending Form does...")]
    public class DpiUnawareForm : Form
    {
        private DpiAwarenessContext? _previousDpiAwarenessContext;

        /// <inheritdoc />
        protected override void CreateHandle()
        {
            // Test if this is supported, before calling
            if (WindowsVersion.IsWindows10BuildOrLater(14393) && NativeDpiMethods.IsValidDpiAwarenessContext(DpiAwarenessContext.Unaware))
            {
                _previousDpiAwarenessContext = NativeDpiMethods.SetThreadDpiAwarenessContext(DpiAwarenessContext.Unaware);
            }
            base.CreateHandle();
        }

        /// <inheritdoc />
        protected override void OnHandleCreated(EventArgs e)
        {
            if (_previousDpiAwarenessContext.HasValue)
            {
                NativeDpiMethods.SetThreadDpiAwarenessContext(_previousDpiAwarenessContext.Value);
            }
            base.OnHandleCreated(e);
        }
    }
}
