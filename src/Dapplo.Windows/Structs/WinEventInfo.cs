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
using Dapplo.Windows.Enums;
using Dapplo.Windows.User32.Enums;

#endregion

namespace Dapplo.Windows.Structs
{
    /// <summary>
    ///     This class represents the information passed to the WinEventProc as described
    ///     <a href="https://msdn.microsoft.com/en-us/library/windows/desktop/dd373885(v=vs.85).aspx">here</a>
    /// </summary>
    public class WinEventInfo
    {
        /// <summary>
        ///     Handle to an event hook function. This value is returned by SetWinEventHook when the hook function is installed and
        ///     is specific to each instance of the hook function.
        /// </summary>
        public IntPtr EventHook { get; private set; }

        /// <summary>
        ///     Identifies the thread that generated the event, or the thread that owns the current window.
        /// </summary>
        public ulong EventThread { get; private set; }

        /// <summary>
        ///     Specifies the time, in milliseconds, that the event was generated.
        /// </summary>
        public ulong EventTime { get; private set; }

        /// <summary>
        ///     Handle to the window that generates the event, or IntPtr.Zero if no window is associated with the event.
        ///     For example, the mouse pointer is not associated with a window.
        /// </summary>
        public IntPtr Handle { get; private set; }

        /// <summary>
        ///     Identifies whether the event was triggered by an object or a child element of the object.
        ///     If this value is CHILDID_SELF (0), the event was triggered by the object;
        ///     otherwise, this value is the child ID of the element that triggered the event.
        /// </summary>
        public long IdChild { get; private set; }

        /// <summary>
        ///     Identifies whether the event was triggered by an object
        /// </summary>
        public bool IsSelf => IdChild == 0;

        /// <summary>
        ///     Handle to the window that generates the event, or IntPtr.Zero if no window is associated with the event.
        ///     For example, the mouse pointer is not associated with a window.
        /// </summary>
        public ObjectIdentifiers ObjectIdentifier { get; private set; }

        /// <summary>
        ///     Specifies the event that occurred.
        /// </summary>
        public WinEvents WinEvent { get; private set; }

        /// <summary>
        ///     Create a WinEventInfo instance
        /// </summary>
        /// <param name="winEventHook">
        ///     Handle to an event hook function. This value is returned by SetWinEventHook when the hook
        ///     function is installed and is specific to each instance of the hook function.
        /// </param>
        /// <param name="winEvent">Specifies the event that occurred. This value is one of the event constants.</param>
        /// <param name="hWnd">
        ///     Handle to the window that generates the event, or NULL if no window is associated with the event.
        ///     For example, the mouse pointer is not associated with a window.
        /// </param>
        /// <param name="idObject">
        ///     Identifies the object associated with the event. This is one of the object identifiers or a
        ///     custom object ID.
        /// </param>
        /// <param name="idChild">
        ///     Identifies whether the event was triggered by an object or a child element of the object. If this
        ///     value is CHILDID_SELF, the event was triggered by the object; otherwise, this value is the child ID of the element
        ///     that triggered the event.
        /// </param>
        /// <param name="eventThread">Identifies the thread that generated the event, or the thread that owns the current window.</param>
        /// <param name="eventTime">Specifies the time, in milliseconds, that the event was generated.</param>
        /// <returns></returns>
        public static WinEventInfo Create(IntPtr winEventHook, WinEvents winEvent, IntPtr hWnd, ObjectIdentifiers idObject, long idChild, ulong eventThread, ulong eventTime)
        {
            return new WinEventInfo
            {
                EventHook = winEventHook,
                WinEvent = winEvent,
                Handle = hWnd,
                ObjectIdentifier = idObject,
                IdChild = idChild,
                EventThread = eventThread,
                EventTime = eventTime
            };
        }
    }
}