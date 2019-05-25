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

namespace Dapplo.Windows.Dpi.Enums
{
    /// <summary>
    ///     Identifies the DPI hosting behavior for a window.
    ///     This behavior allows windows created in the thread to host child windows with a different DPI_AWARENESS_CONTEXT
    /// </summary>
    public enum DpiHostingBehavior
    {
        /// <summary>
        ///     Invalid DPI hosting behavior. This usually occurs if the previous SetThreadDpiHostingBehavior call used an invalid parameter.
        /// </summary>
        Invalid = -1,

        /// <summary>
        ///     Default DPI hosting behavior. The associated window behaves as normal, and cannot create or re-parent child windows with a different DPI_AWARENESS_CONTEXT.
        /// </summary>
        Default = 0,

        /// <summary>
        ///     Mixed DPI hosting behavior. This enables the creation and re-parenting of child windows with different DPI_AWARENESS_CONTEXT. These child windows will be independently scaled by the OS.
        /// </summary>
        Mixed = 1

    }
}