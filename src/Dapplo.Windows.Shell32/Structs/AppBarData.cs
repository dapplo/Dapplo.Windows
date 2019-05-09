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

using Dapplo.Windows.Common.Structs;
using System;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;
using Dapplo.Windows.Shell32.Enums;

namespace Dapplo.Windows.Shell32.Structs
{
    /// <summary>
    /// Contains information about a system appbar message.
    /// This is used by the <a href="https://msdn.microsoft.com/en-us/library/windows/desktop/bb762108.aspx">SHAppBarMessage function</a>
    /// </summary>
    [StructLayout(LayoutKind.Sequential)]
    [SuppressMessage("ReSharper", "ConvertToAutoProperty", Justification = "Interop structure")]
    [SuppressMessage("ReSharper", "UnusedMember.Global", Justification = "Interop structure")]
    [SuppressMessage("Sonar Code Smell", "S2292:Trivial properties should be auto-implemented", Justification = "Interop!")]
    [SuppressMessage("Sonar Code Smell", "S1450:Private fields only used as local variables in methods should become local variables", Justification = "Interop!")]
    public struct AppBarData
    {
        /// <summary>
        /// Size of the structure, used internally
        /// </summary>
        private int _cbSize;

        private IntPtr _hWnd;
        private uint _uCallbackMessage;
        private AppBarEdges _uEdge;

        private NativeRect _rc;
        private int _lParam;

        /// <summary>
        /// Sets the handle to the appbar window.  Not all messages use this member.
        /// See the individual message page to see if you need to provide an hWindow value.
        /// </summary>
        public void SetWindowHandle(IntPtr hWnd)
        {
            _hWnd = hWnd;
        }

        /// <summary>
        /// An application-defined message identifier. The application uses the specified identifier for notification messages that it sends to the appbar identified by the hWnd member.
        /// This member is used when sending the ABM_NEW message.
        /// </summary>
        public uint CallbackMessageIdentifier
        {
            get { return _uCallbackMessage; }
            set { _uCallbackMessage = value; }
        }

        /// <summary>
        /// This member is used when sending one of these messages:
        /// ABM_GETAUTOHIDEBAR, ABM_SETAUTOHIDEBAR, ABM_GETAUTOHIDEBAREX, ABM_SETAUTOHIDEBAREX, ABM_QUERYPOS, ABM_SETPOS
        /// </summary>
        public AppBarEdges AppBarEdge
        {
            get { return _uEdge; }
            set { _uEdge = value; }
        }

        /// <summary>
        /// A RECT structure whose use varies depending on the message:
        /// ABM_GETTASKBARPOS, ABM_QUERYPOS, ABM_SETPOS: The bounding rectangle, in screen coordinates, of an appbar or the Windows taskbar.
        /// ABM_GETAUTOHIDEBAREX, ABM_SETAUTOHIDEBAREX: The monitor on which the operation is being performed. This information can be retrieved through the GetMonitorInfo function.
        /// </summary>
        public NativeRect Bounds
        {
            get { return _rc; }
            set { _rc = value; }
        }

        /// <summary>
        /// Used for the ABM_SETAUTOHIDEBAR, ABM_SETAUTOHIDEBAREX message
        /// </summary>
        public bool AutoHide
        {
            get { return _lParam != 0; }
            set { _lParam = value? 1 : 0; }
        }

        /// <summary>
        /// Used for the ABM_SETAUTOHIDEBAR, ABM_SETAUTOHIDEBAREX message
        /// </summary>
        public AppBarStates State
        {
            get { return (AppBarStates) _lParam; }
            set { _lParam = (int)value; }
        }

        /// <summary>
        ///     Gets the default (empty) value.
        /// </summary>
        public static AppBarData Create()
        {
            return new AppBarData
            {
                _cbSize = Marshal.SizeOf(typeof(AppBarData))
            };
        }
    }
}
