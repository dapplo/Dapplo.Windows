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

namespace Dapplo.Windows.DesktopWindowsManager.Enums
{
    /// <summary>
    ///     A flag to indicate which properties are set by the DwmUpdateThumbnailProperties method
    /// </summary>
    [Flags]
    public enum DwmThumbnailPropertyFlags
    {
        /// <summary>
        ///     A value for the rcDestination member has been specified.
        /// </summary>
        Destination = 0x00000001,

        /// <summary>
        ///     A value for the rcSource member has been specified.
        /// </summary>
        Source = 0x00000002,

        /// <summary>
        ///     A value for the opacity member has been specified.
        /// </summary>
        Opacity = 0x00000004,

        /// <summary>
        ///     A value for the fVisible member has been specfied.
        /// </summary>
        Visible = 0x00000008,

        /// <summary>
        ///     A value for the fSourceClientAreaOnly member has been specified.
        /// </summary>
        SourceClientAreaOnly = 0x00000010
    }
}