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
using System.Runtime.InteropServices;

#endregion

namespace Dapplo.Windows.App
{
    // This is used for Windows 8 to see if the App Launcher is active
    // See https://msdn.microsoft.com/en-us/library/windows/desktop/jj554119.aspx
    [ComImport]
    [Guid("2246EA2D-CAEA-4444-A3C4-6DE827E44313")]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    internal interface IAppVisibility
    {
        MonitorAppVisibility GetAppVisibilityOnMonitor(IntPtr hMonitor);
        bool IsLauncherVisible { get; }
    }
}