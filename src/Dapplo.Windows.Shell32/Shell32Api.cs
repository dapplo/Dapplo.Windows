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

using System;
using System.Runtime.InteropServices;
using Dapplo.Windows.Shell32.Enums;
using Dapplo.Windows.Shell32.Structs;

namespace Dapplo.Windows.Shell32
{
    /// <summary>
    /// An API for Shell32 functionality
    /// </summary>
    public static class Shell32Api
    {
        /// <summary>
        /// Returns an AppBarData struct which describes the Taskbar bounds etc
        /// </summary>
        /// <returns>AppBarData</returns>
        public static AppBarData TaskbarPosition
        {
            get
            {
                var appBarData = AppBarData.Create();
                SHAppBarMessage(AppBarMessages.GetTaskbarPosition, ref appBarData);
                return appBarData;
            }
        }

        /// <summary>
        /// Sends an appbar message to the system.
        /// See <a href="https://msdn.microsoft.com/en-us/library/windows/desktop/bb762108.aspx">SHAppBarMessage function</a>
        /// </summary>
        /// <param name="dwMessage">AppBarMessages - Appbar message value to send.</param>
        /// <param name="pData">A pointer to an AppBarData structure. The content of the structure on entry and on exit depends on the value set in the dwMessage parameter.
        /// See the individual message pages for specifics.</param>
        /// <returns></returns>
        [DllImport("shell32", SetLastError = true)]
        private static extern IntPtr SHAppBarMessage(AppBarMessages dwMessage, [In] ref AppBarData pData);
    }
}
