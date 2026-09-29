// Copyright (c) Dapplo and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
using System;
using System.Threading;
using Dapplo.Log;
using Dapplo.Windows.Common;
using Dapplo.Windows.Common.Structs;
using Dapplo.Windows.Input.Enums;
using Dapplo.Windows.Input.Keyboard;
using Dapplo.Windows.Input.Mouse;
using Dapplo.Windows.Messages;
using Dapplo.Windows.Messages.Enumerations;
using Dapplo.Windows.User32;
using Dapplo.Windows.User32.Enums;
using Dapplo.Windows.User32.Structs;

namespace Dapplo.Windows.Desktop;

/// <summary>
///     The is a container class to help to scroll a window
/// </summary>
public class WindowScroller
{
    private static readonly LogSource Log = new LogSource();

    /// <summary>
    ///     The delta of one mouse wheel notch, see WHEEL_DELTA
    /// </summary>
    public const int WheelDeltaPerNotch = 120;

    /// <summary>
    ///     SPI_GETWHEELSCROLLLINES returns this (WHEEL_PAGESCROLL) when one wheel notch scrolls a page
    /// </summary>
    private const uint WheelPageScroll = uint.MaxValue;

    /// <summary>
    ///     How often, and with which interval in milliseconds, the position is checked after a mouse wheel movement was injected
    /// </summary>
    private const int PositionChangeChecks = 10;
    private const int PositionChangeCheckInterval = 10;

    /// <summary>
    ///     This is used to be able to reset the location, and also detect if we are at the end.
    ///     Some windows might add content when the user is (almost) at the end.
    /// </summary>
    public ScrollInfo InitialScrollInfo { get; internal set; }

    /// <summary>
    ///     Returns true if the scroller is at the end
    /// </summary>
    /// <returns>bool</returns>
    public bool IsAtEnd => GetPosition(out var scrollInfo) && IsAtEndPosition(scrollInfo);

    /// <summary>
    ///     Check if the specified ScrollInfo is at the end
    /// </summary>
    /// <param name="scrollInfo">ScrollInfo</param>
    /// <returns>bool</returns>
    private bool IsAtEndPosition(ScrollInfo scrollInfo)
    {
        var maximum = KeepInitialBounds ? InitialScrollInfo.Maximum : scrollInfo.Maximum;
        return maximum <= Math.Max(scrollInfo.Position, scrollInfo.TrackingPosition) + scrollInfo.PageSize - 1;
    }

    /// <summary>
    ///     Returns true if the scroller is at the start
    /// </summary>
    /// <returns>bool</returns>
    public bool IsAtStart => GetPosition(out var scrollInfo) && IsAtStartPosition(scrollInfo);

    /// <summary>
    ///     Check if the specified ScrollInfo is at the start
    /// </summary>
    /// <param name="scrollInfo">ScrollInfo</param>
    /// <returns>bool</returns>
    private bool IsAtStartPosition(ScrollInfo scrollInfo)
    {
        var minimum = KeepInitialBounds ? InitialScrollInfo.Minimum : scrollInfo.Minimum;
        return minimum >= Math.Max(scrollInfo.Position, scrollInfo.TrackingPosition);
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
    ///     Get the number of lines one mouse wheel notch scrolls, via SystemParametersInfo(SPI_GETWHEELSCROLLLINES).
    ///     0 means the wheel doesn't scroll, uint.MaxValue (WHEEL_PAGESCROLL) means one notch scrolls a page.
    /// </summary>
    public static uint ScrollWheelLines => User32Api.GetWheelScrollLines();

    /// <summary>
    ///     Calculate the mouse wheel delta which is needed to scroll approximately one page.
    ///     The result is always a positive value, of at least <see cref="WheelDeltaPerNotch"/>.
    /// </summary>
    /// <param name="pageSize">uint with the page size, from the ScrollInfo</param>
    /// <param name="wheelScrollLines">uint with the lines per wheel notch, see <see cref="ScrollWheelLines"/></param>
    /// <returns>int with the wheel delta</returns>
    public static int CalculateWheelDelta(uint pageSize, uint wheelScrollLines)
    {
        // 0: the wheel doesn't scroll, WHEEL_PAGESCROLL: one notch is already a page
        if (wheelScrollLines == 0 || wheelScrollLines == WheelPageScroll || pageSize == 0)
        {
            return WheelDeltaPerNotch;
        }
        // The page size is in scroll units, which for most windows are lines, so this is an approximation
        var delta = (long)WheelDeltaPerNotch * pageSize / wheelScrollLines;
        return (int)Math.Min(Math.Max(delta, WheelDeltaPerNotch), int.MaxValue);
    }

    /// <summary>
    ///     The maximum number of mouse wheel movements End() and Start() do in ScrollModes.MouseWheel
    /// </summary>
    public int MaxMouseWheelSteps { get; set; } = 500;

    /// <summary>
    ///     Does the scrollbar need to represent the changes?
    /// </summary>
    public bool ShowChanges { get; set; } = true;

    /// <summary>
    ///     Amount of delta the scrollwheel scrolls, a value of 0 or less is replaced by <see cref="WheelDeltaPerNotch"/>
    /// </summary>
    public int WheelDelta { get; set; }

    /// <summary>
    ///     The wheel delta which is really used, this prevents injecting 0 or reversed wheel movements
    /// </summary>
    private int EffectiveWheelDelta => WheelDelta > 0 ? WheelDelta : WheelDeltaPerNotch;

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
        var wParam = CreateScrollWParam(ScrollBarCommands.SB_THUMBPOSITION, scrollInfo.Position);
        switch (ScrollBarType)
        {
            case ScrollBarTypes.Horizontal:
                User32Api.SendMessage(ScrollingWindow.Handle, WindowsMessages.WM_HSCROLL, wParam, IntPtr.Zero);
                break;
            case ScrollBarTypes.Vertical:
            case ScrollBarTypes.Control:
                User32Api.SendMessage(ScrollMessageTarget, WindowsMessages.WM_VSCROLL, wParam, ScrollMessageLParam);
                break;
        }
        return true;
    }

    /// <summary>
    ///     The lParam for WM_HSCROLL / WM_VSCROLL: the handle of the scroll bar control for ScrollBarTypes.Control, otherwise IntPtr.Zero (standard scroll bar)
    /// </summary>
    private IntPtr ScrollMessageLParam => ScrollBarType == ScrollBarTypes.Control && ScrollBarWindow is not null ? ScrollBarWindow.Handle : IntPtr.Zero;

    /// <summary>
    ///     The window which receives WM_VSCROLL, this is the ScrollingWindow.
    ///     A scroll bar control notifies its parent, so if the ScrollingWindow is the scroll bar control itself the message goes to the parent of the control.
    /// </summary>
    private IntPtr ScrollMessageTarget
    {
        get
        {
            var scrollingWindowHandle = ScrollingWindow.Handle;
            if (ScrollBarType != ScrollBarTypes.Control || ScrollBarWindow is null || ScrollBarWindow.Handle != scrollingWindowHandle)
            {
                return scrollingWindowHandle;
            }
            var parentHandle = User32Api.GetParent(scrollingWindowHandle);
            return parentHandle != IntPtr.Zero ? parentHandle : scrollingWindowHandle;
        }
    }

    /// <summary>
    ///     Create the wParam for a WM_HSCROLL / WM_VSCROLL message, the low-order word is the scroll bar command and the high-order word the (16-bit) position
    /// </summary>
    /// <param name="scrollBarCommand">ScrollBarCommands</param>
    /// <param name="position">int with the position, only used for SB_THUMBPOSITION and SB_THUMBTRACK</param>
    /// <returns>IntPtr with the wParam</returns>
    public static IntPtr CreateScrollWParam(ScrollBarCommands scrollBarCommand, int position)
    {
        // Build the 32-bit value with uint arithmetic, and use the int constructor so this never overflows on 32-bit
        var wParam = unchecked(((uint)position << 16) | ((uint)scrollBarCommand & 0xFFFF));
        return new IntPtr(unchecked((int)wParam));
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
                result = MouseWheelUntil(true);
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
                result = MouseInputGenerator.MoveMouseWheel(-EffectiveWheelDelta, middlePoint) == 1;
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
                result = MouseInputGenerator.MoveMouseWheel(EffectiveWheelDelta, middlePoint) == 1;
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
                User32Api.SendMessage(ScrollingWindow.Handle, WindowsMessages.WM_HSCROLL, scrollBarCommand, IntPtr.Zero);
                return true;
            case ScrollBarTypes.Vertical:
            case ScrollBarTypes.Control:
                User32Api.SendMessage(ScrollMessageTarget, WindowsMessages.WM_VSCROLL, scrollBarCommand, ScrollMessageLParam);
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
                result = MouseWheelUntil(false);
                break;
        }
        return result;
    }

    /// <summary>
    ///     Move the mouse wheel until the end or start is reached.
    ///     This stops when the scroll information is not available, the position doesn't change anymore or after <see cref="MaxMouseWheelSteps"/> steps,
    ///     so windows which don't reflect wheel scrolling in their scrollbar cannot cause an endless loop.
    /// </summary>
    /// <param name="toEnd">true to scroll to the end, false to scroll to the start</param>
    /// <returns>true if the end or start was reached</returns>
    private bool MouseWheelUntil(bool toEnd)
    {
        for (var step = 0; step < MaxMouseWheelSteps; step++)
        {
            if (!TryRetrievePosition(out var scrollInfoBefore))
            {
                return false;
            }
            if (toEnd ? IsAtEndPosition(scrollInfoBefore) : IsAtStartPosition(scrollInfoBefore))
            {
                return true;
            }
            if (!(toEnd ? Next() : Previous()))
            {
                return false;
            }
            if (!WaitForPositionChange(scrollInfoBefore))
            {
                Log.Verbose().WriteLine("Scroll position didn't change after a mouse wheel movement, stopping.");
                return false;
            }
        }
        Log.Warn().WriteLine("Stopped mouse wheel scrolling after {0} steps.", MaxMouseWheelSteps);
        return false;
    }

    /// <summary>
    ///     Injected input is processed asynchronously, so poll the position for a short time to detect a change
    /// </summary>
    /// <param name="scrollInfoBefore">ScrollInfo from before the scrolling</param>
    /// <returns>true if the position changed</returns>
    private bool WaitForPositionChange(ScrollInfo scrollInfoBefore)
    {
        for (var check = 0; check < PositionChangeChecks; check++)
        {
            if (!GetPosition(out var scrollInfoAfter))
            {
                return false;
            }
            if (scrollInfoAfter.Position != scrollInfoBefore.Position || scrollInfoAfter.TrackingPosition != scrollInfoBefore.TrackingPosition)
            {
                return true;
            }
            Thread.Sleep(PositionChangeCheckInterval);
        }
        return false;
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