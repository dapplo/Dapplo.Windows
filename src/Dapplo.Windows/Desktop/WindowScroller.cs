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
using Dapplo.Log;
using Dapplo.Windows.Common;
using Dapplo.Windows.Common.Structs;
using Dapplo.Windows.Input.Enums;
using Dapplo.Windows.Input.Keyboard;
using Dapplo.Windows.Input.Mouse;
using Dapplo.Windows.Messages;
using Dapplo.Windows.User32;
using Dapplo.Windows.User32.Enums;
using Dapplo.Windows.User32.Structs;
using Microsoft.Win32;

#endregion

namespace Dapplo.Windows.Desktop
{
    /// <summary>
    ///     The is a container class to help to scroll a window
    /// </summary>
    public class WindowScroller
    {
        private static readonly LogSource Log = new LogSource();

        /// <summary>
        ///     This is used to be able to reset the location, and also detect if we are at the end.
        ///     Some windows might add content when the user is (almost) at the end.
        /// </summary>
        public ScrollInfo InitialScrollInfo { get; internal set; }

        /// <summary>
        ///     Returns true if the scroller is at the end
        /// </summary>
        /// <returns>bool</returns>
        public bool IsAtEnd
        {
            get
            {
                if (!GetPosition(out var scrollInfo))
                {
                    return false;
                }
                if (KeepInitialBounds)
                {
                    return InitialScrollInfo.Maximum <= Math.Max(scrollInfo.Position, scrollInfo.TrackingPosition) + scrollInfo.PageSize - 1;
                }
                return scrollInfo.Maximum <= Math.Max(scrollInfo.Position, scrollInfo.TrackingPosition) + scrollInfo.PageSize - 1;
            }
        }

        /// <summary>
        ///     Returns true if the scroller is at the start
        /// </summary>
        /// <returns>bool</returns>
        public bool IsAtStart
        {
            get
            {
                if (!GetPosition(out var scrollInfo))
                {
                    return false;
                }
                if (KeepInitialBounds)
                {
                    return InitialScrollInfo.Minimum >= Math.Max(scrollInfo.Position, scrollInfo.TrackingPosition);
                }
                return scrollInfo.Minimum >= Math.Max(scrollInfo.Position, scrollInfo.TrackingPosition);
            }
        }

        /// <summary>
        ///     Some windows might add content when the user is (almost) at the end.
        ///     If this is true, the scrolling doesn't go beyond the intial bounds.
        ///     If this is false, the initial value is only used for reset.
        /// </summary>
        public bool KeepInitialBounds { get; set; } = true;

        /// <summary>
        ///     Get the information on the used scrollbar, if any.
        ///     This can be used to detect the location of the scrollbar
        /// </summary>
        public ScrollBarInfo? ScrollBar { get; internal set; }

        /// <summary>
        ///     What scrollbar to use
        /// </summary>
        public ScrollBarTypes ScrollBarType { get; internal set; } = ScrollBarTypes.Vertical;

        /// <summary>
        ///     Area of the scrollbar, this can be the WindowToScroll
        /// </summary>
        public IInteropWindow ScrollBarWindow { get; set; }

        /// <summary>
        ///     Area which is scrolling, can be the WindowToScroll
        /// </summary>
        public IInteropWindow ScrollingWindow { get; set; }

        /// <summary>
        ///     Specify which scroll mode needs to be used
        /// </summary>
        public ScrollModes ScrollMode { get; set; } = ScrollModes.WindowsMessage;

        /// <summary>
        ///     Get the scroll-lines from the registry
        /// </summary>
        public static int ScrollWheelLinesFromRegistry
        {
            get
            {
                using (var key = Registry.CurrentUser.OpenSubKey(@"Control Panel\Desktop", false))
                {
                    if (!(key?.GetValue("WheelScrollLines") is string wheelScrollLines))
                    {
                        return 3;
                    }

                    if (int.TryParse(wheelScrollLines, out var scrollLines))
                    {
                        return scrollLines;
                    }
                }
                return 3;
            }
        }

        /// <summary>
        ///     Does the scrollbar need to represent the changes?
        /// </summary>
        public bool ShowChanges { get; set; } = true;

        /// <summary>
        ///     Amount of delta the scrollwheel scrolls
        /// </summary>
        public int WheelDelta { get; set; }

        /// <summary>
        ///     Apply position from the scrollInfo
        /// </summary>
        /// <param name="scrollInfo">SCROLLINFO ref</param>
        /// <returns>bool</returns>
        private bool ApplyPosition(ref ScrollInfo scrollInfo)
        {
            if (ShowChanges)
            {
                User32Api.SetScrollInfo(ScrollBarWindow.Handle, ScrollBarType, ref scrollInfo, true);
            }
            switch (ScrollBarType)
            {
                case ScrollBarTypes.Horizontal:
                    User32Api.SendMessage(ScrollingWindow.Handle, WindowsMessages.WM_HSCROLL, 4 + 0x10000 * scrollInfo.Position, 0);
                    break;
                case ScrollBarTypes.Vertical:
                case ScrollBarTypes.Control:
                    User32Api.SendMessage(ScrollingWindow.Handle, WindowsMessages.WM_VSCROLL, (int) ((uint) ScrollBarCommands.SB_THUMBPOSITION + (scrollInfo.Position << 16)), 0);
                    break;
            }
            return true;
        }


        /// <summary>
        ///     Move to the end
        /// </summary>
        /// <returns>bool if this worked</returns>
        public bool End()
        {
            var result = false;
            var hasScrollInfo = TryRetrievePosition(out var scrollInfoBefore);
            switch (ScrollMode)
            {
                case ScrollModes.KeyboardPageUpDown:
                    KeyboardInputGenerator.KeyDown(VirtualKeyCode.Control);
                    KeyboardInputGenerator.KeyPresses(VirtualKeyCode.End);
                    KeyboardInputGenerator.KeyUp(VirtualKeyCode.Control);
                    result = true;
                    break;
                case ScrollModes.WindowsMessage:
                    result = SendScrollMessage(ScrollBarCommands.SB_BOTTOM);
                    break;
                case ScrollModes.AbsoluteWindowMessage:
                    result = hasScrollInfo;
                    if (hasScrollInfo)
                    {
                        // Calculate end position, clone the scrollInfoBefore
                        var scrollInfoForEnd = scrollInfoBefore;
                        scrollInfoForEnd.Position = scrollInfoBefore.Maximum;
                        result = ApplyPosition(ref scrollInfoForEnd);
                    }
                    break;
                case ScrollModes.MouseWheel:
                    result = true;
                    while (!IsAtEnd)
                    {
                        if (!Next())
                        {
                            break;
                        }
                    }
                    break;
            }
            return result;
        }

        /// <summary>
        ///     Get current position
        /// </summary>
        /// <returns>SCROLLINFO</returns>
        public bool GetPosition(out ScrollInfo scrollInfo)
        {
            scrollInfo = ScrollInfo.Create(ScrollInfoMask.All);

            return User32Api.GetScrollInfo(ScrollBarWindow.Handle, ScrollBarType, ref scrollInfo);
        }

        /// <summary>
        ///     Method to set the ScrollbarInfo, if we can get it
        /// </summary>
        /// <param name="forceUpdate">set to true to force an update, default is false</param>
        /// <returns>ScrollBarInfo?</returns>
        public ScrollBarInfo? GetScrollbarInfo(bool forceUpdate = false)
        {
            // Prevent updates, if there is already a value
            if (ScrollBar.HasValue && !forceUpdate)
            {
                return ScrollBar;
            }
            var objectId = ObjectIdentifiers.Client;
            switch (ScrollBarType)
            {
                case ScrollBarTypes.Control:
                    objectId = ObjectIdentifiers.Client;
                    break;
                case ScrollBarTypes.Vertical:
                    objectId = ObjectIdentifiers.VerticalScrollbar;
                    break;
                case ScrollBarTypes.Horizontal:
                    objectId = ObjectIdentifiers.HorizontalScrollbar;
                    break;
            }
            var scrollbarInfo = ScrollBarInfo.Create();
            var hasScrollbarInfo = User32Api.GetScrollBarInfo(ScrollBarWindow.Handle, objectId, ref scrollbarInfo);
            if (!hasScrollbarInfo)
            {
                var error = Win32.GetLastErrorCode();
                if (Log.IsVerboseEnabled())
                {
                    Log.Verbose().WriteLine("Error retrieving Scrollbar info : {0}", Win32.GetMessage(error));
                }
                return null;
            }
            ScrollBar = scrollbarInfo;
            return scrollbarInfo;
        }

        /// <summary>
        ///     Returns true if the window needs focus to scroll
        /// </summary>
        /// <returns>true if focus is needed</returns>
        public bool NeedsFocus()
        {
            return ScrollMode == ScrollModes.KeyboardPageUpDown;
        }

        /// <summary>
        ///     Go to the next "page"
        /// </summary>
        /// <returns>bool if this worked</returns>
        public bool Next()
        {
            var result = false;
            var hasScrollInfo = TryRetrievePosition(out var scrollInfoBefore);

            switch (ScrollMode)
            {
                case ScrollModes.KeyboardPageUpDown:
                    result = KeyboardInputGenerator.KeyPresses(VirtualKeyCode.Next) == 2;
                    break;
                case ScrollModes.WindowsMessage:
                    result = SendScrollMessage(ScrollBarCommands.SB_PAGEDOWN);
                    break;
                case ScrollModes.AbsoluteWindowMessage:
                    if (!hasScrollInfo)
                    {
                        return false;
                    }
                    // Calculate next position, clone the scrollInfoBefore
                    var scrollInfoForPrevious = scrollInfoBefore;
                    scrollInfoForPrevious.Position = Math.Min(scrollInfoBefore.Maximum, scrollInfoBefore.Position + (int) scrollInfoBefore.PageSize);
                    result = ApplyPosition(ref scrollInfoForPrevious);
                    break;
                case ScrollModes.MouseWheel:
                    var bounds = ScrollingWindow.GetInfo().Bounds;
                    var middlePoint = new NativePoint(bounds.X + bounds.Width / 2, bounds.Y + bounds.Height / 2);
                    result = MouseInputGenerator.MoveMouseWheel(-WheelDelta, middlePoint) == 1;
                    break;
            }
            return result;
        }

        /// <summary>
        ///     Go to the previous "page"
        /// </summary>
        /// <returns>bool if this worked</returns>
        public bool Previous()
        {
            var result = false;
            var hasScrollInfo = TryRetrievePosition(out var scrollInfoBefore);

            switch (ScrollMode)
            {
                case ScrollModes.KeyboardPageUpDown:
                    result = KeyboardInputGenerator.KeyPresses(VirtualKeyCode.Prior) == 2;
                    break;
                case ScrollModes.WindowsMessage:
                    result = SendScrollMessage(ScrollBarCommands.SB_PAGEUP);
                    break;
                case ScrollModes.AbsoluteWindowMessage:
                    if (!hasScrollInfo)
                    {
                        return false;
                    }
                    // Calculate previous position, clone the scrollInfoBefore
                    var scrollInfoForPrevious = scrollInfoBefore;
                    scrollInfoForPrevious.Position = Math.Max(scrollInfoBefore.Minimum, scrollInfoBefore.Position - (int) scrollInfoBefore.PageSize);
                    result = ApplyPosition(ref scrollInfoForPrevious);
                    break;
                case ScrollModes.MouseWheel:
                    var bounds = ScrollingWindow.GetInfo().Bounds;
                    var middlePoint = new NativePoint(bounds.X + bounds.Width / 2, bounds.Y + bounds.Height / 2);
                    result = MouseInputGenerator.MoveMouseWheel(WheelDelta, middlePoint) == 1;
                    break;
            }
            return result;
        }

        /// <summary>
        ///     Set the position back to the original, only works for windows which support ScrollModes.WindowsMessage
        /// </summary>
        /// <returns>true if this worked</returns>
        public bool Reset()
        {
            var initialScrollInfo = InitialScrollInfo;
            return ApplyPosition(ref initialScrollInfo);
        }

        /// <summary>
        ///     Helper method to send the right message
        /// </summary>
        /// <param name="scrollBarCommand">ScrollBarCommands enum to specify where to scroll</param>
        /// <returns>true if this was possible</returns>
        private bool SendScrollMessage(ScrollBarCommands scrollBarCommand)
        {
            switch (ScrollBarType)
            {
                case ScrollBarTypes.Horizontal:
                    User32Api.SendMessage(ScrollingWindow.Handle, WindowsMessages.WM_HSCROLL, scrollBarCommand, 0);
                    return true;
                case ScrollBarTypes.Vertical:
                    User32Api.SendMessage(ScrollingWindow.Handle, WindowsMessages.WM_VSCROLL, scrollBarCommand, 0);
                    return true;
                default:
                    return false;
            }
        }

        /// <summary>
        ///     Move to the start
        /// </summary>
        /// <returns>bool if this worked</returns>
        public bool Start()
        {
            var result = false;
            var hasScrollInfo = TryRetrievePosition(out var scrollInfoBefore);
            switch (ScrollMode)
            {
                case ScrollModes.KeyboardPageUpDown:
                    KeyboardInputGenerator.KeyDown(VirtualKeyCode.Control);
                    KeyboardInputGenerator.KeyPresses(VirtualKeyCode.Home);
                    KeyboardInputGenerator.KeyUp(VirtualKeyCode.Control);
                    result = true;
                    break;
                case ScrollModes.WindowsMessage:
                    result = SendScrollMessage(ScrollBarCommands.SB_TOP);
                    break;
                case ScrollModes.AbsoluteWindowMessage:
                    result = hasScrollInfo;
                    if (hasScrollInfo)
                    {
                        // Calculate start position, clone the scrollInfoBefore
                        var scrollInfoForStart = scrollInfoBefore;
                        scrollInfoForStart.Position = scrollInfoBefore.Minimum;
                        result = ApplyPosition(ref scrollInfoForStart);
                    }
                    break;
                case ScrollModes.MouseWheel:
                    result = true;
                    while (!IsAtStart)
                    {
                        if (!Previous())
                        {
                            break;
                        }
                    }
                    break;
            }
            return result;
        }

        /// <summary>
        ///     Retrieve position from the scrollInfo
        /// </summary>
        /// <param name="scrollInfo">ScrollInfo out</param>
        /// <returns>bool</returns>
        private bool TryRetrievePosition(out ScrollInfo scrollInfo)
        {
            var hasScrollInfo = GetPosition(out scrollInfo);
            if (!Log.IsVerboseEnabled())
            {
                return hasScrollInfo;
            }

            if (Log.IsVerboseEnabled())
            {
                if (hasScrollInfo)
                {
                    Log.Verbose().WriteLine("Retrieved ScrollInfo: {0}", scrollInfo);
                }
                else
                {
                    Log.Verbose().WriteLine("Couldn't get scrollinfo.");
                }
            }

            return hasScrollInfo;
        }
    }
}