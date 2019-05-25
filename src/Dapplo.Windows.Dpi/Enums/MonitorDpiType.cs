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

#region using

using System;

#endregion

namespace Dapplo.Windows.Dpi.Enums
{
    /// <summary>
    ///     See
    ///     <a href="https://msdn.microsoft.com/en-us/library/windows/desktop/dn280511(v=vs.85).aspx">
    ///         MONITOR_DPI_TYPE
    ///         enumeration
    ///     </a>
    /// </summary>
    [Flags]
    public enum MonitorDpiType
    {
        /// <summary>
        ///     The effective DPI.
        ///     This value should be used when determining the correct scale factor for scaling UI elements.
        ///     This incorporates the scale factor set by the user for this specific display.
        /// </summary>
        EffectiveDpi = 0,

        /// <summary>
        ///     The angular DPI.
        ///     This DPI ensures rendering at a compliant angular resolution on the screen.
        ///     This does not include the scale factor set by the user for this specific display
        /// </summary>
        AngularDpi = 1,

        /// <summary>
        ///     The raw DPI.
        ///     This value is the linear DPI of the screen as measured on the screen itself.
        ///     Use this value when you want to read the pixel density and not the recommended scaling setting.
        ///     This does not include the scale factor set by the user for this specific display and is not guaranteed to be a
        ///     supported DPI value.
        /// </summary>
        RawDpi = 2
    }
}