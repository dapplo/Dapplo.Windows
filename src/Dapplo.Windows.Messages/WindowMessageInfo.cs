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

namespace Dapplo.Windows.Messages
{
    /// <summary>
    /// Container for the windows messages
    /// </summary>
    public class WindowMessageInfo
    {
        /// <summary>
        /// IntPtr with the Handle of the window
        /// </summary>
        public IntPtr Handle { get; private set; }

        /// <summary>
        /// WindowsMessages which is the actual message
        /// </summary>
        public WindowsMessages Message { get; private set; }

        /// <summary>
        /// IntPtr with the word-param
        /// </summary>
        public IntPtr WordParam { get; private set; }

        /// <summary>
        /// IntPtr with the long-param
        /// </summary>
        public IntPtr LongParam { get; private set; }

        /// <summary>
        /// Factory method for the Window Message Info
        /// </summary>
        /// <param name="hWnd">IntPtr with the Handle of the window</param>
        /// <param name="msg">WindowsMessages which is the actual message</param>
        /// <param name="wParam">IntPtr with the word-param</param>
        /// <param name="lParam">IntPtr with the long-param</param>
        /// <returns>WindowMessageInfo</returns>
        public static WindowMessageInfo Create(IntPtr hWnd, int msg, IntPtr wParam, IntPtr lParam)
        {
            return new WindowMessageInfo
            {
                Handle = hWnd,
                Message = (WindowsMessages)msg,
                WordParam = wParam,
                LongParam = lParam
            };
        }
    }
}
