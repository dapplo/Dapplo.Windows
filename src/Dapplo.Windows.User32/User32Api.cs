// Copyright (c) Dapplo and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;
using Dapplo.Log;
using Dapplo.Windows.Common;
using Dapplo.Windows.Common.Structs;
using Dapplo.Windows.Messages.Enums;
using Dapplo.Windows.User32.Enums;
using Dapplo.Windows.User32.SafeHandles;
using Dapplo.Windows.User32.Structs;

namespace Dapplo.Windows.User32;

/// <summary>
///     Native wrappers for the User32 DLL
/// </summary>
public static class User32Api
{
    private static readonly LogSource Log = new LogSource();

    private static bool _canCallGetPhysicalCursorPos = true;

    /// <summary>
    ///     Retrieves the cursor location safely, accounting for DPI settings in Vista/Windows 7.
    ///     This uses GetPhysicalCursorPos, and falls back to GetCursorPos if that fails.
    /// </summary>
    /// <returns>
    ///     NativePoint with cursor location, relative to the origin of the monitor setup
    ///     (i.e. negative coordinates are possible in multi screen setups)
    /// </returns>
    public static NativePoint GetCursorLocation()
    {
        if (_canCallGetPhysicalCursorPos)
        {
            try
            {
                if (GetPhysicalCursorPos(out var cursorLocation))
                {
                    return cursorLocation;
                }
                var error = Win32.GetLastErrorCode();
                Log.Error().WriteLine("Error retrieving PhysicalCursorPos : {0}", Win32.GetMessage(error));
            }
            catch (Exception ex)
            {
                Log.Error().WriteLine(ex, "Exception retrieving PhysicalCursorPos, no longer calling this. Cause :");
                _canCallGetPhysicalCursorPos = false;
            }
        }
        if (GetCursorPos(out var logicalCursorLocation))
        {
            return logicalCursorLocation;
        }
        throw new Win32Exception();
    }

    /// <summary>
    /// Get the display info for the specified monitor handle
    /// </summary>
    /// <param name="monitorHandle">IntPtr</param>
    /// <param name="index">int</param>
    /// <returns>DisplayInfo</returns>
    public static DisplayInfo GetDisplayInfo(IntPtr monitorHandle, int index)
    {
        var monitorInfoEx = MonitorInfoEx.Create();
        var success = GetMonitorInfo(monitorHandle, ref monitorInfoEx);
        if (!success)
        {
            return null;
        }

        return new DisplayInfo
        {
            MonitorHandle = monitorHandle,
            Index = index,
            ScreenWidth = monitorInfoEx.Monitor.Width,
            ScreenHeight = monitorInfoEx.Monitor.Height,
            Bounds = monitorInfoEx.Monitor,
            WorkingArea = monitorInfoEx.WorkArea,
            IsPrimary = (monitorInfoEx.Flags & MonitorInfoFlags.Primary) == MonitorInfoFlags.Primary,
            DeviceName = monitorInfoEx.DeviceName
        };
    }

    /// <summary>
    /// Retrieve all available display infos
    /// </summary>
    /// <returns>IReadOnlyList of DisplayInfo</returns>
    public static IReadOnlyList<DisplayInfo> EnumDisplays()
    {
        var result = new List<DisplayInfo>();
        int index = 1;

        bool EnumDisplayMonitorsCallback(IntPtr monitorHandle, IntPtr hdcMonitor, ref NativeRect lprcMonitor, IntPtr data)
        {
            var displayInfo = GetDisplayInfo(monitorHandle, index++);
            if (displayInfo != null)
            {
                result.Add(displayInfo);
            }
            return true;
        }

        EnumDisplayMonitors(IntPtr.Zero, IntPtr.Zero, EnumDisplayMonitorsCallback, IntPtr.Zero);

        return result;
    }

    /// <summary>
    /// This returns the list of window handles for the specified thread
    /// </summary>
    /// <param name="threadId">int</param>
    /// <returns>List of IntPtr</returns>
    public static List<IntPtr> EnumThreadWindows(int threadId)
    {
        var handles = new List<IntPtr>();
        User32Api.EnumThreadWindows(threadId, (hWnd, lParam) =>
        {
            handles.Add(hWnd);
            return true;
        }, IntPtr.Zero);
        return handles;
    }

    /// <summary>
    ///     Helper method to create a Win32 exception with the windows message in it
    /// </summary>
    /// <param name="method">string with current method</param>
    /// <returns>Exception</returns>
    public static Exception CreateWin32Exception(string method)
    {
        var exceptionToThrow = new Win32Exception();
        exceptionToThrow.Data.Add("Method", method);
        return exceptionToThrow;
    }

    /// <summary>
    ///     Wrapper for the GetClassLong which decides if the system is 64-bit or not and calls the right one.
    /// </summary>
    /// <param name="hWnd">IntPtr</param>
    /// <param name="index">ClassLongIndex</param>
    /// <returns>IntPtr</returns>
    public static IntPtr GetClassLongWrapper(IntPtr hWnd, ClassLongIndex index)
    {
        if (IntPtr.Size > 4)
        {
            return GetClassLongPtr(hWnd, index);
        }
        return GetClassLong(hWnd, index);
    }

    /// <summary>
    ///     Retrieve the windows classname
    /// </summary>
    /// <param name="hWnd">IntPtr for the window</param>
    /// <returns>string</returns>
    public static string GetClassname(IntPtr hWnd)
    {
        unsafe
        {
            const int capacity = 260;
            char* classname = stackalloc char[capacity];
            var characters = User32Api.GetClassName(hWnd, classname, capacity);
            return characters == 0 ? string.Empty : new string(classname, 0, characters);
        }
    }

    /// <summary>
    ///     Return the count of GDI objects.
    /// </summary>
    /// <returns>Return the count of GDI objects.</returns>
    public static uint GetGuiResourcesGdiCount()
    {
        using var currentProcess = Process.GetCurrentProcess();
        return GetGuiResources(currentProcess.Handle, 0);
    }

    /// <summary>
    ///     Return the count of USER objects.
    /// </summary>
    /// <returns>Return the count of USER objects.</returns>
    public static uint GetGuiResourcesUserCount()
    {
        using var currentProcess = Process.GetCurrentProcess();
        return GetGuiResources(currentProcess.Handle, 1);
    }

    /// <summary>
    ///     Retrieve the windows caption (title), with GetWindowText, the length of the caption is not limited.
    ///     Note: for a window of the current process GetWindowText sends WM_GETTEXT. For a window of the calling thread this is a direct call,
    ///     but for a window which belongs to another thread of the current process this can deadlock when that thread is waiting for the caller.
    ///     Use <see cref="GetInternalText"/> for such windows.
    /// </summary>
    /// <param name="hWnd">IntPtr for the window</param>
    /// <returns>string with the caption, empty if there is none</returns>
    public static string GetText(IntPtr hWnd)
    {
        var length = GetWindowTextLength(hWnd);
        if (length <= 0)
        {
            return string.Empty;
        }
        // Room for the terminating 0 and one extra character, so a completely filled buffer signals that the caption grew in the meantime
        return ReadCaption(hWnd, (int)Math.Min(length + 2L, MaxWindowTextLength), false);
    }

    /// <summary>
    ///     Retrieve the caption (title) which Windows stored for the window, with InternalGetWindowText.
    ///     This never sends a message to the window, so it cannot hang or deadlock, even for windows of other threads of the current process.
    ///     A text which the window procedure only supplies via WM_GETTEXT is not returned.
    /// </summary>
    /// <param name="hWnd">IntPtr for the window</param>
    /// <returns>string with the caption, empty if there is none</returns>
    public static string GetInternalText(IntPtr hWnd)
    {
        return ReadCaption(hWnd, 256, true);
    }

    /// <summary>
    ///     Read the caption into a buffer of the specified capacity, the buffer is enlarged when the caption doesn't fit
    /// </summary>
    /// <param name="hWnd">IntPtr for the window</param>
    /// <param name="capacity">int with the initial capacity, including the terminating 0</param>
    /// <param name="useInternalGetWindowText">true to use InternalGetWindowText, false for GetWindowText</param>
    /// <returns>string</returns>
    private static unsafe string ReadCaption(IntPtr hWnd, int capacity, bool useInternalGetWindowText)
    {
        // Most captions are short: the first attempt uses a buffer on the stack, only a longer caption needs one on the heap
        if (capacity <= StackAllocCaptionThreshold)
        {
            var stackBuffer = stackalloc char[capacity];
            if (TryReadCaption(hWnd, stackBuffer, capacity, useInternalGetWindowText, out var caption))
            {
                return caption;
            }
            capacity = (int)Math.Min(capacity * 2L, MaxWindowTextLength);
        }

        while (true)
        {
            var heapBuffer = new char[capacity];
            fixed (char* buffer = heapBuffer)
            {
                if (TryReadCaption(hWnd, buffer, capacity, useInternalGetWindowText, out var caption))
                {
                    return caption;
                }
            }
            capacity = (int)Math.Min(capacity * 2L, MaxWindowTextLength);
        }
    }

    /// <summary>
    ///     Read the caption into the buffer
    /// </summary>
    /// <param name="hWnd">IntPtr for the window</param>
    /// <param name="buffer">char pointer to the buffer</param>
    /// <param name="capacity">int with the capacity of the buffer, including the terminating 0</param>
    /// <param name="useInternalGetWindowText">true to use InternalGetWindowText, false for GetWindowText</param>
    /// <param name="caption">string with the caption, when the result is true</param>
    /// <returns>false when the buffer was completely filled (the caption might be truncated) and a bigger buffer is possible</returns>
    private static unsafe bool TryReadCaption(IntPtr hWnd, char* buffer, int capacity, bool useInternalGetWindowText, out string caption)
    {
        var copied = useInternalGetWindowText ? InternalGetWindowText(hWnd, buffer, capacity) : GetWindowText(hWnd, buffer, capacity);
        if (copied <= 0)
        {
            caption = string.Empty;
            return true;
        }
        // A completely filled buffer means the caption might be truncated, retry with a bigger buffer
        if (copied < capacity - 1 || capacity >= MaxWindowTextLength)
        {
            caption = new string(buffer, 0, Math.Min(copied, capacity - 1));
            return true;
        }
        caption = null;
        return false;
    }

    /// <summary>
    ///     Captions up to this number of characters (including the terminating 0) are first read into a buffer on the stack
    /// </summary>
    private const int StackAllocCaptionThreshold = 256;

    /// <summary>
    ///     The maximum number of characters which <see cref="GetTextFromWindow"/>, <see cref="GetText"/> and <see cref="GetInternalText"/> retrieve, longer texts are truncated.
    /// </summary>
    public const int MaxWindowTextLength = 1024 * 1024;

    /// <summary>
    ///     Buffers up to this number of characters (including the terminating 0) are allocated on the stack, larger ones on the heap.
    /// </summary>
    private const int StackAllocTextThreshold = 512;

    /// <summary>
    ///     Timeout, in milliseconds, for messages which are sent to (possibly foreign) windows to retrieve information.
    /// </summary>
    private const uint InformationMessageTimeout = 500;

    /// <summary>
    ///     Get the text of a control, this is not the caption.
    ///     The messages are sent with SendMessageTimeout and SMTO_ABORTIFHUNG, so a hung window cannot block the caller.
    ///     Texts longer than <see cref="MaxWindowTextLength"/> characters are truncated.
    /// </summary>
    /// <param name="hWnd">IntPtr</param>
    /// <returns>string with the text, or null if there is no text or the window didn't respond in time</returns>
    public static string GetTextFromWindow(IntPtr hWnd)
    {
        // Get the size of the string required to hold the window's text.
        if (!TrySendMessage(hWnd, WindowsMessages.WM_GETTEXTLENGTH, IntPtr.Zero, IntPtr.Zero, out var lengthResult, InformationMessageTimeout))
        {
            return null;
        }

        var length = lengthResult.ToInt64();
        // If the return is 0, there is no text.
        if (length <= 0)
        {
            return null;
        }

        var bufferSize = (int)Math.Min(length, MaxWindowTextLength) + 1;
        unsafe
        {
            if (bufferSize <= StackAllocTextThreshold)
            {
                var stackBuffer = stackalloc char[bufferSize];
                return ReadWindowText(hWnd, stackBuffer, bufferSize);
            }

            var heapBuffer = new char[bufferSize];
            fixed (char* text = heapBuffer)
            {
                return ReadWindowText(hWnd, text, bufferSize);
            }
        }
    }

    /// <summary>
    ///     Send WM_GETTEXT, with a timeout, to the specified window and create a string from the result
    /// </summary>
    /// <param name="hWnd">IntPtr with the window handle</param>
    /// <param name="buffer">char pointer to the buffer to fill</param>
    /// <param name="bufferSize">int with the size of the buffer, in characters, including the terminating 0</param>
    /// <returns>string or null if there is no text or the window didn't respond in time</returns>
    private static unsafe string ReadWindowText(IntPtr hWnd, char* buffer, int bufferSize)
    {
        if (!TrySendMessage(hWnd, WindowsMessages.WM_GETTEXT, new IntPtr(bufferSize), new IntPtr(buffer), out var copiedResult, InformationMessageTimeout))
        {
            return null;
        }

        // The text might have changed since WM_GETTEXTLENGTH, so use the number of characters which were really copied
        var copied = (int)Math.Min(Math.Max(copiedResult.ToInt64(), 0), bufferSize - 1);
        return copied == 0 ? null : new string(buffer, 0, copied);
    }

    /// <summary>
    ///     Get the titlebar info ex for the specified window.
    ///     The message is sent with SendMessageTimeout and SMTO_ABORTIFHUNG, so a hung window cannot block the caller.
    /// </summary>
    /// <param name="hWnd">IntPtr with the window handle</param>
    /// <returns>TitleBarInfoEx, this is empty (only the size is set) if the window didn't respond in time</returns>
    public static TitleBarInfoEx GetTitleBarInfoEx(IntPtr hWnd)
    {
        var result = TitleBarInfoEx.Create();
        if (!SendMessageTimeout(hWnd, WindowsMessages.WM_GETTITLEBARINFOEX, IntPtr.Zero, ref result, SendMessageTimeoutFlags.AbortIfHung | SendMessageTimeoutFlags.ErrorOnExit, InformationMessageTimeout, out _))
        {
            return TitleBarInfoEx.Create();
        }
        return result;
    }

    /// <summary>
    ///     Get the number of lines to scroll when the vertical mouse wheel is moved, see SPI_GETWHEELSCROLLLINES.
    /// </summary>
    /// <returns>uint with the number of lines, 0 means no scrolling, uint.MaxValue (WHEEL_PAGESCROLL) means scroll a page. If the value cannot be retrieved, the Windows default of 3 is returned.</returns>
    public static uint GetWheelScrollLines()
    {
        return SystemParametersInfo(SystemParametersInfoActions.SPI_GETWHEELSCROLLLINES, 0, out uint wheelScrollLines, SystemParametersInfoBehaviors.None) ? wheelScrollLines : 3;
    }

    /// <summary>
    ///     Wrapper for the GetWindowLong which decides if the system is 64-bit or not and calls the right one.
    /// </summary>
    /// <param name="hWnd">IntPtr</param>
    /// <param name="index">WindowLongIndex</param>
    /// <returns></returns>
    public static IntPtr GetWindowLongWrapper(IntPtr hWnd, WindowLongIndex index)
    {
        return IntPtr.Size == 8 ?
            GetWindowLongPtr(hWnd, index) :
            GetWindowLong(hWnd, index);
    }

    /// <summary>
    ///     Wrapper for the SetWindowLong which decides if the system is 64-bit or not and calls the right one.
    /// </summary>
    /// <param name="hWnd">IntPtr</param>
    /// <param name="index">WindowLongIndex</param>
    /// <param name="replacementValue">IntPtr</param>
    /// <returns>
    ///     IntPtr with the previous value.
    ///     As 0 is also a valid previous value, a failure is only signalled by 0 together with a non-zero <see cref="Marshal.GetLastWin32Error"/>,
    ///     the last error is cleared before the call so it's reliable.
    /// </returns>
    public static IntPtr SetWindowLongWrapper(IntPtr hWnd, WindowLongIndex index, IntPtr replacementValue)
    {
        // SetWindowLong(Ptr) doesn't reset the last error on success, clear it so a 0 result can be told apart from a failure
        ClearLastError(0);
        if (IntPtr.Size == 8)
        {
            return SetWindowLongPtr(hWnd, index, replacementValue);
        }
        return new IntPtr(SetWindowLong(hWnd, index, replacementValue.ToInt32()));
    }

    /// <summary>
    ///     Set the Extended WindowStyle
    /// </summary>
    /// <param name="hWnd">IntPtr</param>
    /// <param name="extendedWindowStyleFlags">ExtendedWindowStyleFlags</param>
    /// <returns>IntPtr with the previous value, see <see cref="SetWindowLongWrapper"/> on how to detect a failure</returns>
    public static IntPtr SetExtendedWindowStyle(IntPtr hWnd, ExtendedWindowStyleFlags extendedWindowStyleFlags)
    {
        // The flags are a 32-bit value, the int constructor prevents an OverflowException on 32-bit for values with the high bit set
        return User32Api.SetWindowLongWrapper(hWnd, WindowLongIndex.GWL_EXSTYLE, new IntPtr(unchecked((int)extendedWindowStyleFlags)));
    }

    /// <summary>
    ///     Set the WindowStyleFlags
    /// </summary>
    /// <param name="hWnd">IntPtr</param>
    /// <param name="windowStyleFlags">WindowStyleFlags</param>
    /// <returns>IntPtr with the previous value, see <see cref="SetWindowLongWrapper"/> on how to detect a failure</returns>
    public static IntPtr SetWindowStyle(IntPtr hWnd, WindowStyleFlags windowStyleFlags)
    {
        // The flags are a 32-bit value, the int constructor prevents an OverflowException on 32-bit for WS_POPUP (0x80000000)
        return User32Api.SetWindowLongWrapper(hWnd, WindowLongIndex.GWL_STYLE, new IntPtr(unchecked((int)windowStyleFlags)));
    }

    /// <summary>
    /// Try to send a WindowsMessage, this will return if the target didn't respond in the specified timeout (300ms by default)
    /// </summary>
    /// <param name="hWnd">IntPtr window handle</param>
    /// <param name="message">WindowsMessages</param>
    /// <param name="wParam">IntPtr</param>
    /// <param name="lParam">IntPtr</param>
    /// <param name="result">out IntPtr</param>
    /// <param name="timeout">uint with optional number of milliseconds, default is 300</param>
    /// <returns>bool true if the SendMessage worked</returns>
    public static bool TrySendMessage(IntPtr hWnd, WindowsMessages message, IntPtr wParam, IntPtr lParam, out IntPtr result, uint timeout = 300)
    {
        var isSuccess = SendMessageTimeout(hWnd, message, wParam, lParam, SendMessageTimeoutFlags.AbortIfHung |SendMessageTimeoutFlags.ErrorOnExit, timeout, out result);
        if (!isSuccess)
        {
            result = IntPtr.Zero;
        }
        return isSuccess;
    }

    /// <summary>
    /// Try to send a WindowsMessage, this will return if the target didn't respond in the specified timeout (300ms by default)
    /// </summary>
    /// <param name="hWnd">IntPtr window handle</param>
    /// <param name="message">uint</param>
    /// <param name="wParam">IntPtr</param>
    /// <param name="lParam">IntPtr</param>
    /// <param name="result">out IntPtr</param>
    /// <param name="timeout">uint with optional number of milliseconds, default is 300</param>
    /// <returns>bool true if the SendMessage worked</returns>
    public static bool TrySendMessage(IntPtr hWnd, uint message, IntPtr wParam, IntPtr lParam, out IntPtr result, uint timeout = 300)
    {
        var isSuccess = SendMessageTimeout(hWnd, message, wParam, lParam, SendMessageTimeoutFlags.AbortIfHung | SendMessageTimeoutFlags.ErrorOnExit, timeout, out result);
        if (!isSuccess)
        {
            result = IntPtr.Zero;
        }
        return isSuccess;
    }

    /// <summary>
    /// Try to send a WindowsMessage, this will return if the target didn't respond in the specified timeout (300ms by default)
    /// </summary>
    /// <param name="hWnd">IntPtr window handle</param>
    /// <param name="message">uint</param>
    /// <param name="wParam">IntPtr</param>
    /// <param name="lParam">IntPtr</param>
    /// <param name="result">out UIntPtr</param>
    /// <param name="timeout">uint with optional number of milliseconds, default is 300</param>
    /// <returns>bool true if the SendMessage worked</returns>
    public static bool TrySendMessage(IntPtr hWnd, uint message, IntPtr wParam, IntPtr lParam, out UIntPtr result, uint timeout = 300)
    {
        var isSuccess = User32Api.SendMessageTimeout(hWnd, message, wParam, lParam, SendMessageTimeoutFlags.AbortIfHung | SendMessageTimeoutFlags.ErrorOnExit, timeout, out result);
        if (!isSuccess)
        {
            result = UIntPtr.Zero;
        }
        return isSuccess;
    }

    /// <summary>
    /// The DLL Name for the User32 library
    /// </summary>
    public const string User32 = "user32";

    /// <summary>
    ///     kernel32 SetLastError, used to clear the last error before calling functions which don't reset it on success
    /// </summary>
    /// <param name="dwErrCode">uint with the error code</param>
    [DllImport("kernel32", EntryPoint = "SetLastError", ExactSpelling = true)]
    private static extern void ClearLastError(uint dwErrCode);

    /// <summary>
    ///     Delegate description for the windows enumeration
    /// </summary>
    /// <param name="hWnd">IntPtr</param>
    /// <param name="lParam">IntPtr</param>
    public delegate bool EnumWindowsProc(IntPtr hWnd, IntPtr lParam);
    internal delegate bool MonitorEnumDelegate(IntPtr hMonitor, IntPtr hdcMonitor, ref NativeRect lprcMonitor, IntPtr dwData);

    /// <summary>
    /// Determines the visibility state of the specified window.
    /// </summary>
    /// <param name="hWnd">A handle to the window to be tested.</param>
    /// <returns>
    /// If the specified window, its parent window, its parent's parent window, and so forth, have the WS_VISIBLE style, the return value is nonzero. Otherwise, the return value is zero.
    /// Because the return value specifies whether the window has the WS_VISIBLE style, it may be nonzero even if the window is totally obscured by other windows.
    ///
    /// Remarks:
    /// The visibility state of a window is indicated by the WS_VISIBLE style bit. When WS_VISIBLE is set, the window is displayed and subsequent drawing into it is displayed as long as the window has the WS_VISIBLE style.
    /// Any drawing to a window with the WS_VISIBLE style will not be displayed if the window is obscured by other windows or is clipped by its parent window.
    /// </returns>
    [DllImport(User32)]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static extern bool IsWindowVisible(IntPtr hWnd);

    /// <summary>
    /// Determines whether the specified window handle identifies an existing window.
    /// </summary>
    /// <param name="hWnd">A handle to the window to be tested.</param>
    /// <returns>
    /// If the window handle identifies an existing window, the return value is true.
    /// If the window handle does not identify an existing window, the return value is false.
    /// </returns>
    [DllImport(User32)]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static extern bool IsWindow(IntPtr hWnd);

    /// <summary>
    /// See <a href="https://msdn.microsoft.com/en-us/library/windows/desktop/ms633522(v=vs.85).aspx">GetWindowThreadProcessId function</a>
    /// Retrieves the identifier of the thread that created the specified window and, optionally, the identifier of the process that created the window.
    /// </summary>
    /// <param name="hWnd">A handle to the window.</param>
    /// <param name="processId">A pointer to a variable that receives the process identifier. If this parameter is not NULL, GetWindowThreadProcessId copies the identifier of the process to the variable; otherwise, it does not.</param>
    /// <returns>The return value is the identifier of the thread that created the window.</returns>
    [DllImport(User32, SetLastError = true)]
    public static extern int GetWindowThreadProcessId(IntPtr hWnd, out int processId);

    /// <summary>
    /// See <a href="https://msdn.microsoft.com/en-us/library/windows/desktop/ms633522(v=vs.85).aspx">GetWindowThreadProcessId function</a>
    /// Retrieves the identifier of the thread that created the specified window and, optionally, the identifier of the process that created the window.
    /// </summary>
    /// <param name="hWnd">A handle to the window.</param>
    /// <param name="processId">A pointer to a variable that receives the process identifier. If this parameter is not NULL, GetWindowThreadProcessId copies the identifier of the process to the variable; otherwise, it does not.</param>
    /// <returns>The return value is the identifier of the thread that created the window.</returns>
    [DllImport(User32, SetLastError = true)]
    public static extern int GetWindowThreadProcessId(IntPtr hWnd, IntPtr processId);

    /// <summary>
    /// See <a href="https://docs.microsoft.com/en-us/windows/win32/api/winuser/nf-winuser-attachthreadinput">AttachThreadInput function</a>
    /// </summary>
    /// <param name="idAttach">int with the identifier of the thread to be attached to another thread</param>
    /// <param name="idAttachTo">int with the identifier of the thread to which idAttach will be attached</param>
    /// <param name="fAttach">true to attach, false to detach</param>
    /// <returns>bool true if the function succeeds</returns>
    [DllImport(User32, SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static extern bool AttachThreadInput(int idAttach, int idAttachTo, [MarshalAs(UnmanagedType.Bool)] bool fAttach);

    /// <summary>
    ///     Retrieves a handle to the specified window's parent or owner.
    ///     To retrieve a handle to a specified ancestor, use the GetAncestor function.
    /// </summary>
    /// <param name="hWnd">A handle to the window whose parent window handle is to be retrieved.</param>
    /// <returns>
    ///     IntPtr handle to the parent window or IntPtr.Zero if none
    ///     If the window is a child window, the return value is a handle to the parent window. If the window is a top-level
    ///     window with the WS_POPUP style, the return value is a handle to the <b>owner</b> window.
    ///     Use <see cref="GetAncestor"/> with <see cref="GetAncestorFlags.GA_PARENT"/> for the real parent, and <see cref="GetWindow"/> with <see cref="GetWindowCommands.GW_OWNER"/> for the owner.
    /// </returns>
    [DllImport(User32, SetLastError = true)]
    public static extern IntPtr GetParent(IntPtr hWnd);

    /// <summary>
    ///     Retrieves the handle to the ancestor of the specified window, see
    ///     <a href="https://learn.microsoft.com/en-us/windows/win32/api/winuser/nf-winuser-getancestor">GetAncestor function</a>.
    ///     Unlike <see cref="GetParent"/>, GA_PARENT never returns the owner. For a top-level window GA_PARENT returns the desktop window.
    /// </summary>
    /// <param name="hWnd">IntPtr with the window handle, if this is the desktop window the function returns IntPtr.Zero</param>
    /// <param name="getAncestorFlags">GetAncestorFlags</param>
    /// <returns>IntPtr with the handle of the ancestor</returns>
    [DllImport(User32)]
    public static extern IntPtr GetAncestor(IntPtr hWnd, GetAncestorFlags getAncestorFlags);

    /// <summary>
    ///     See
    ///     <a href="https://msdn.microsoft.com/en-us/library/windows/desktop/ms633541(v=vs.85).aspx">SetParent function</a>
    ///     Changes the parent window of the specified child window.
    /// </summary>
    /// <param name="hWndChild">IntPtr</param>
    /// <param name="hWndNewParent">IntPtr</param>
    /// <returns>
    ///     If the function succeeds, the return value is a handle to the previous parent window.
    ///     If the function fails, the return value is NULL. To get extended error information, call GetLastError.
    /// </returns>
    [DllImport(User32, SetLastError = true)]
    public static extern IntPtr SetParent(IntPtr hWndChild, IntPtr hWndNewParent);

    /// <summary>
    ///     Retrieves a handle to a window that has the specified relationship (Z-Order or owner) to the specified window.
    ///     See
    ///     <a href="https://msdn.microsoft.com/en-us/library/windows/desktop/ms633515(v=vs.85).aspx">GetWindow function</a>
    /// </summary>
    /// <param name="hWnd">
    ///     IntPtr A handle to a window. The window handle retrieved is relative to this window, based on the
    ///     value of the uCmd parameter.
    /// </param>
    /// <param name="getWindowCommand">
    ///     GetWindowCommands The relationship between the specified window and the window whose
    ///     handle is to be retrieved. See GetWindowCommands
    /// </param>
    /// <returns></returns>
    [DllImport(User32, SetLastError = true)]
    public static extern IntPtr GetWindow(IntPtr hWnd, GetWindowCommands getWindowCommand);

    /// <summary>
    ///     See
    ///     <a href="https://msdn.microsoft.com/en-us/library/windows/desktop/ms633548(v=vs.85).aspx">ShowWindow function</a>
    /// </summary>
    /// <param name="hWnd">A handle to the window.</param>
    /// <param name="nCmdShow">
    ///     ShowWindowCommands
    ///     Controls how the window is to be shown.
    ///     This parameter is ignored the first time an application calls ShowWindow, if the program that launched the
    ///     application provides a STARTUPINFO structure.
    ///     Otherwise, the first time ShowWindow is called, the value should be the value obtained by the WinMain function in
    ///     its nCmdShow parameter.
    ///     In subsequent calls, this parameter can be one of the following values.
    /// </param>
    /// <returns>bool</returns>
    [DllImport(User32, SetLastError = true)]
    public static extern bool ShowWindow(IntPtr hWnd, ShowWindowCommands nCmdShow);

    /// <summary>
    ///     Get the caption of the window
    /// See <a href="https://msdn.microsoft.com/en-us/library/windows/desktop/ms633546.aspx">SetWindowText function</a>
    /// </summary>
    /// <param name="hWnd">IntPtr with the window handle</param>
    /// <param name="caption">string with the new caption</param>
    /// <returns>int with the size of the caption</returns>
    [DllImport(User32, CharSet = CharSet.Unicode, SetLastError = true)]
    public static extern int SetWindowText(IntPtr hWnd, string caption);

    /// <summary>
    ///     Get the caption of the window
    /// </summary>
    /// <param name="hWnd">IntPtr with the window handle</param>
    /// <param name="lpString">char * to place the </param>
    /// <param name="capacity">size of the buffer</param>
    /// <returns>int with the size of the caption</returns>
    [DllImport(User32, CharSet = CharSet.Unicode, SetLastError = true)]
    internal static extern unsafe int GetWindowText(IntPtr hWnd, char* lpString, int capacity);

    /// <summary>
    ///     Get the caption of the window as stored by Windows, without sending WM_GETTEXT, see
    ///     <a href="https://learn.microsoft.com/en-us/windows/win32/api/winuser/nf-winuser-internalgetwindowtext">InternalGetWindowText function</a>
    /// </summary>
    /// <param name="hWnd">IntPtr with the window handle</param>
    /// <param name="lpString">char * to place the caption into</param>
    /// <param name="capacity">size of the buffer, including the terminating 0</param>
    /// <returns>int with the number of copied characters, not including the terminating 0</returns>
    [DllImport(User32, CharSet = CharSet.Unicode, SetLastError = true, ExactSpelling = true)]
    internal static extern unsafe int InternalGetWindowText(IntPtr hWnd, char* lpString, int capacity);

    /// <summary>
    /// See <a href="https://msdn.microsoft.com/en-us/library/windows/desktop/ms633521.aspx">GetWindowTextLength  function</a>
    /// Retrieves the length, in characters, of the specified window's title bar text (if the window has a title bar). If the specified window is a control, the function retrieves the length of the text within the control. However, GetWindowTextLength cannot retrieve the length of the text of an edit control in another application.
    /// </summary>
    /// <param name="hWnd">A handle to the window or control.</param>
    /// <returns>
    /// If the function succeeds, the return value is the length, in characters, of the text. Under certain conditions, this value may actually be greater than the length of the text. For more information, see the following Remarks section.
    /// If the window has no text, the return value is zero. To get extended error information, call GetLastError.
    /// </returns>
    [DllImport(User32, CharSet = CharSet.Unicode, SetLastError = true)]
    public static extern int GetWindowTextLength(IntPtr hWnd);

    /// <summary>
    /// Retrieves the current color of the specified display element.
    /// Display elements are the parts of a window and the display that appear on the system display screen.
    /// </summary>
    /// <param name="nIndex">SysColorIndexes with the display element whose color is to be retrieved.</param>
    /// <returns>
    /// The function returns the red, green, blue (RGB) color value of the given element.
    /// If the nIndex parameter is out of range, the return value is zero.
    /// Because zero is also a valid RGB value, you cannot use GetSysColor to determine whether a system color is supported by the current platform.Instead, use the GetSysColorBrush function, which returns NULL if the color is not supported.
    /// </returns>
    [DllImport(User32)]
    public static extern uint GetSysColor(SysColorIndexes nIndex);

    /// <summary>
    ///     Bring the specified window to the front
    /// </summary>
    /// <param name="hWnd">IntPtr specifying the hWnd</param>
    /// <returns>true if the call was successfull</returns>
    [DllImport(User32, SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static extern bool BringWindowToTop(IntPtr hWnd);

    /// <summary>
    /// Retrieves the hWnd for the window which is currently the foreground window
    /// </summary>
    /// <returns>IntPtr</returns>
    [DllImport(User32)]
    public static extern IntPtr GetForegroundWindow();

    /// <summary>
    ///     Get the hWnd of the Desktop window
    /// </summary>
    /// <returns>IntPtr</returns>
    [DllImport(User32)]
    public static extern IntPtr GetDesktopWindow();

    /// <summary>
    ///     Set the current foreground window
    /// </summary>
    /// <param name="hWnd">IntPtr with the handle to the window</param>
    /// <returns>bool</returns>
    [DllImport(User32, SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static extern bool SetForegroundWindow(IntPtr hWnd);

    /// <summary>
    ///     Sets the keyboard focus to the specified window. The window must be attached to the calling thread's message queue.
    /// </summary>
    /// <param name="hWnd">
    ///     A handle to the window that will receive the keyboard input. If this parameter is NULL, keystrokes
    ///     are ignored.
    /// </param>
    /// <returns>
    ///     IntPtr
    ///     If the function succeeds, the return value is the handle to the window that previously had the keyboard focus.
    ///     If the hWnd parameter is invalid or the window is not attached to the calling thread's message queue, the return
    ///     value is NULL.
    ///     To get extended error information, call GetLastError.
    /// </returns>
    [DllImport(User32, SetLastError = true)]
    public static extern IntPtr SetFocus(IntPtr hWnd);

    /// <summary>
    ///     Get the WindowPlacement for the specified window
    /// </summary>
    /// <param name="hWnd">IntPtr</param>
    /// <param name="windowPlacement">WindowPlacement</param>
    /// <returns>true if success</returns>
    [DllImport(User32, SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static extern bool GetWindowPlacement(IntPtr hWnd, ref WindowPlacement windowPlacement);

    /// <summary>
    ///     Set the WindowPlacement for the specified window
    /// </summary>
    /// <param name="hWnd">IntPtr</param>
    /// <param name="windowPlacement">WindowPlacement</param>
    /// <returns>true if the call was sucessfull</returns>
    [DllImport(User32, SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static extern bool SetWindowPlacement(IntPtr hWnd, [In] ref WindowPlacement windowPlacement);

    /// <summary>
    ///     Return true if the specified window is minimized
    /// </summary>
    /// <param name="hWnd">IntPtr for the hWnd</param>
    /// <returns>true if minimized</returns>
    [DllImport(User32)]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static extern bool IsIconic(IntPtr hWnd);

    /// <summary>
    ///     Return true if the specified window is maximized
    /// </summary>
    /// <param name="hWnd">IntPtr for the hWnd</param>
    /// <returns>true if maximized</returns>
    [DllImport(User32)]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static extern bool IsZoomed(IntPtr hWnd);

    /// <summary>
    ///     Get the classname of the specified window
    /// </summary>
    /// <param name="hWnd">IntPtr with the hWnd</param>
    /// <param name="className">char * to place the classname into</param>
    /// <param name="nMaxCount">max size for the string builder length</param>
    /// <returns>nr of characters returned</returns>
    [DllImport(User32, CharSet = CharSet.Unicode, SetLastError = true)]
    internal static extern unsafe int GetClassName(IntPtr hWnd, char* className, int nMaxCount);

    [DllImport(User32, SetLastError = true, CharSet = CharSet.Unicode, EntryPoint = "GetClassLongW", ExactSpelling = true)]
    internal static extern IntPtr GetClassLong(IntPtr hWnd, ClassLongIndex index);

    [DllImport(User32, SetLastError = true, CharSet = CharSet.Unicode, EntryPoint = "GetClassLongPtrW", ExactSpelling = true)]
    internal static extern IntPtr GetClassLongPtr(IntPtr hWnd, ClassLongIndex index);

    /// <summary>
    ///     See
    ///     <a href="https://msdn.microsoft.com/en-us/library/windows/desktop/dd162869(v=vs.85).aspx">PrintWindow function</a>
    /// </summary>
    /// <param name="hWnd">IntPtr</param>
    /// <param name="hDc">IntPtr</param>
    /// <param name="printWindowFlags">PrintWindowFlags</param>
    /// <returns>bool</returns>
    [DllImport(User32, SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static extern bool PrintWindow(IntPtr hWnd, IntPtr hDc, PrintWindowFlags printWindowFlags);

    /// <summary>
    ///     Used for the WM_VSCROLL and WM_HSCROLL windows messages
    /// </summary>
    /// <param name="hWnd">IntPtr</param>
    /// <param name="windowsMessage">WindowsMessages</param>
    /// <param name="sysCommand">SysCommands</param>
    /// <param name="lParam">IntPtr</param>
    /// <returns>IntPtr</returns>
    public static IntPtr SendMessage(IntPtr hWnd, WindowsMessages windowsMessage, SysCommands sysCommand, IntPtr lParam)
    {
        return SendMessage(hWnd, windowsMessage, new IntPtr((int)sysCommand), lParam);
    }

    /// <summary>
    ///     Used for the WM_VSCROLL and WM_HSCROLL windows messages
    /// </summary>
    /// <param name="hWnd">IntPtr</param>
    /// <param name="windowsMessage">WindowsMessages</param>
    /// <param name="scrollBarCommand">ScrollBarCommands</param>
    /// <param name="lParam">IntPtr, IntPtr.Zero for a standard scroll bar or the handle of the scroll bar control</param>
    /// <returns>IntPtr, 0 if the message was processed</returns>
    public static IntPtr SendMessage(IntPtr hWnd, WindowsMessages windowsMessage, ScrollBarCommands scrollBarCommand, IntPtr lParam)
    {
        return SendMessage(hWnd, windowsMessage, new IntPtr(unchecked((int)scrollBarCommand)), lParam);
    }

    /// <summary>
    ///  Used for calls where the arguments are IntPtr
    /// </summary>
    /// <param name="hWnd">IntPtr</param>
    /// <param name="windowsMessage">WindowsMessages</param>
    /// <param name="wParam">IntPtr</param>
    /// <param name="lParam">IntPtr</param>
    /// <returns>IntPtr</returns>
    [DllImport(User32, SetLastError = true, CharSet = CharSet.Unicode, EntryPoint = "SendMessageW", ExactSpelling = true)]
    public static extern IntPtr SendMessage(IntPtr hWnd, WindowsMessages windowsMessage, IntPtr wParam, IntPtr lParam);

    /// <summary>
    ///     SendMessageTimeout for getting TitleBarInfoEx
    /// </summary>
    /// <param name="hWnd">IntPtr</param>
    /// <param name="msg">WindowsMessages</param>
    /// <param name="wParam">IntPtr</param>
    /// <param name="lParam">TitleBarInfoEx</param>
    /// <param name="fuFlags">SendMessageTimeoutFlags</param>
    /// <param name="uTimeout">uint with the timeout in milliseconds</param>
    /// <param name="lpdwResult">IntPtr with the result of the message processing</param>
    /// <returns>bool false if timeout true if the sendmessage returned</returns>
    [DllImport(User32, SetLastError = true, CharSet = CharSet.Unicode, EntryPoint = "SendMessageTimeoutW", ExactSpelling = true)]
    private static extern bool SendMessageTimeout(IntPtr hWnd, WindowsMessages msg, IntPtr wParam, ref TitleBarInfoEx lParam, SendMessageTimeoutFlags fuFlags, uint uTimeout, out IntPtr lpdwResult);

    /// <summary>
    ///     Used for WM_SETTEXT or another message where a string needs to be send
    /// </summary>
    /// <param name="hWnd">IntPtr</param>
    /// <param name="windowsMessage">WindowsMessages</param>
    /// <param name="wParam">IntPtr</param>
    /// <param name="lParam">string</param>
    /// <returns>IntPtr, The return value specifies the result of the message processing; it depends on the message sent.</returns>
    [DllImport(User32, SetLastError = true, CharSet = CharSet.Unicode, EntryPoint = "SendMessageW", ExactSpelling = true)]
    public static extern IntPtr SendMessage(IntPtr hWnd, WindowsMessages windowsMessage, IntPtr wParam, [MarshalAs(UnmanagedType.LPWStr)] string lParam);

    /// <summary>
    ///     Places (posts) a message in the message queue of the thread that created the window and returns without waiting for it to be processed.
    ///     See <a href="https://learn.microsoft.com/windows/win32/api/winuser/nf-winuser-postmessagew">PostMessageW</a>.
    ///     Don't post messages with pointers to memory (e.g. WM_SETTEXT), the memory might be gone before the message is processed; use SendMessage for those.
    /// </summary>
    /// <param name="hWnd">
    ///     IntPtr, the window whose window procedure receives the message. <see cref="WindowHandles.HWND_BROADCAST"/> posts it to all top-level windows,
    ///     IntPtr.Zero posts a thread message to the calling thread (like <see cref="PostThreadMessage(int, uint, IntPtr, IntPtr)"/> with the current thread).
    /// </param>
    /// <param name="windowsMessage">WindowsMessages</param>
    /// <param name="wParam">IntPtr, pointer-sized message-specific information</param>
    /// <param name="lParam">IntPtr, pointer-sized message-specific information</param>
    /// <returns>true if the message was posted, false if not: use <see cref="Marshal.GetLastWin32Error"/> for the reason, e.g. ERROR_NOT_ENOUGH_QUOTA when the queue is full or ERROR_ACCESS_DENIED when UIPI blocks it</returns>
    [DllImport(User32, SetLastError = true, CharSet = CharSet.Unicode, EntryPoint = "PostMessageW", ExactSpelling = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static extern bool PostMessage(IntPtr hWnd, WindowsMessages windowsMessage, IntPtr wParam, IntPtr lParam);

    /// <summary>
    ///     Places (posts) a message, e.g. one registered with RegisterWindowMessage, in the message queue of the thread that created the window and returns without waiting for it to be processed.
    ///     See <a href="https://learn.microsoft.com/windows/win32/api/winuser/nf-winuser-postmessagew">PostMessageW</a>.
    ///     Don't post messages with pointers to memory, the memory might be gone before the message is processed; use SendMessage for those.
    /// </summary>
    /// <param name="hWnd">
    ///     IntPtr, the window whose window procedure receives the message. <see cref="WindowHandles.HWND_BROADCAST"/> posts it to all top-level windows,
    ///     IntPtr.Zero posts a thread message to the calling thread.
    /// </param>
    /// <param name="message">uint with the message id</param>
    /// <param name="wParam">IntPtr, pointer-sized message-specific information</param>
    /// <param name="lParam">IntPtr, pointer-sized message-specific information</param>
    /// <returns>true if the message was posted, false if not: use <see cref="Marshal.GetLastWin32Error"/> for the reason</returns>
    [DllImport(User32, SetLastError = true, CharSet = CharSet.Unicode, EntryPoint = "PostMessageW", ExactSpelling = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static extern bool PostMessage(IntPtr hWnd, uint message, IntPtr wParam, IntPtr lParam);

    /// <summary>
    ///     Posts a message to the message queue of the specified thread, it returns without waiting for the thread to process the message.
    ///     The message has no window (hWnd is IntPtr.Zero), so it is not dispatched to a window procedure: the message loop of that thread must handle it, e.g. WM_QUIT to end it.
    ///     See <a href="https://learn.microsoft.com/windows/win32/api/winuser/nf-winuser-postthreadmessagew">PostThreadMessageW</a>.
    /// </summary>
    /// <param name="threadId">int with the id of the thread, as returned by <see cref="GetWindowThreadProcessId(IntPtr, IntPtr)"/> or GetCurrentThreadId (not the managed thread id)</param>
    /// <param name="windowsMessage">WindowsMessages</param>
    /// <param name="wParam">IntPtr, pointer-sized message-specific information</param>
    /// <param name="lParam">IntPtr, pointer-sized message-specific information</param>
    /// <returns>true if the message was posted, false if not: use <see cref="Marshal.GetLastWin32Error"/> for the reason, e.g. ERROR_INVALID_THREAD_ID when the thread has no message queue (yet)</returns>
    [DllImport(User32, SetLastError = true, CharSet = CharSet.Unicode, EntryPoint = "PostThreadMessageW", ExactSpelling = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static extern bool PostThreadMessage(int threadId, WindowsMessages windowsMessage, IntPtr wParam, IntPtr lParam);

    /// <summary>
    ///     Posts a message, e.g. one registered with RegisterWindowMessage, to the message queue of the specified thread, it returns without waiting for the thread to process the message.
    ///     See <a href="https://learn.microsoft.com/windows/win32/api/winuser/nf-winuser-postthreadmessagew">PostThreadMessageW</a>.
    /// </summary>
    /// <param name="threadId">int with the id of the thread, as returned by <see cref="GetWindowThreadProcessId(IntPtr, IntPtr)"/> or GetCurrentThreadId (not the managed thread id)</param>
    /// <param name="message">uint with the message id</param>
    /// <param name="wParam">IntPtr, pointer-sized message-specific information</param>
    /// <param name="lParam">IntPtr, pointer-sized message-specific information</param>
    /// <returns>true if the message was posted, false if not: use <see cref="Marshal.GetLastWin32Error"/> for the reason</returns>
    [DllImport(User32, SetLastError = true, CharSet = CharSet.Unicode, EntryPoint = "PostThreadMessageW", ExactSpelling = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static extern bool PostThreadMessage(int threadId, uint message, IntPtr wParam, IntPtr lParam);

    /// <summary>
    ///     GetWindowLongW, only exported by the 32-bit user32, use <see cref="GetWindowLongWrapper"/>
    /// </summary>
    [DllImport(User32, SetLastError = true, CharSet = CharSet.Unicode, EntryPoint = "GetWindowLongW", ExactSpelling = true)]
    internal static extern IntPtr GetWindowLong(IntPtr hWnd, WindowLongIndex index);

    /// <summary>
    ///     GetWindowLongPtrW, only exported by the 64-bit user32, use <see cref="GetWindowLongWrapper"/>
    /// </summary>
    [DllImport(User32, SetLastError = true, CharSet = CharSet.Unicode, EntryPoint = "GetWindowLongPtrW", ExactSpelling = true)]
    internal static extern IntPtr GetWindowLongPtr(IntPtr hWnd, WindowLongIndex nIndex);

    /// <summary>
    /// Changes an attribute of the specified window. The function also sets a value at the specified offset in the extra window memory.
    /// </summary>
    /// <param name="hWnd">IntPtr with the hWnd</param>
    /// <param name="index">WindowLongIndex</param>
    /// <param name="replacementValue">int</param>
    /// <returns>int with 0 if failed, other value was the previous value</returns>
    [DllImport(User32, SetLastError = true, CharSet = CharSet.Unicode, EntryPoint = "SetWindowLongW", ExactSpelling = true)]
    internal static extern int SetWindowLong(IntPtr hWnd, WindowLongIndex index, int replacementValue);

    /// <summary>
    /// Changes an attribute of the specified window. The function also sets a value at the specified offset in the extra window memory.
    /// </summary>
    /// <param name="hWnd">IntPtr with the hWnd</param>
    /// <param name="index">WindowLongIndex</param>
    /// <param name="replacementValue">IntPtr</param>
    /// <returns>IntPtr with 0 if failed, other value was the previous value</returns>
    [DllImport(User32, SetLastError = true, CharSet = CharSet.Unicode, EntryPoint = "SetWindowLongPtrW", ExactSpelling = true)]
    internal static extern IntPtr SetWindowLongPtr(IntPtr hWnd, WindowLongIndex index, IntPtr replacementValue);

    /// <summary>
    ///     See
    ///     <a href="https://msdn.microsoft.com/en-us/library/windows/desktop/dd145064(v=vs.85).aspx">
    ///         MonitorFromWindow
    ///         function
    ///     </a>
    ///     The MonitorFromWindow function retrieves a handle to the display monitor that has the largest area of intersection
    ///     with the bounding rectangle of a specified window.
    /// </summary>
    /// <param name="hWnd"></param>
    /// <param name="monitorFrom">MonitorFromFlags</param>
    /// <returns>IntPtr for the monitor</returns>
    [DllImport(User32, SetLastError = true)]
    public static extern IntPtr MonitorFromWindow(IntPtr hWnd, MonitorFrom monitorFrom);

    /// <summary>
    ///     The MonitorFromRect function retrieves a handle to the display monitor that has the largest area of intersection
    ///     with a specified rectangle.
    ///     see
    ///     <a href="https://msdn.microsoft.com/en-us/library/windows/desktop/dd145063(v=vs.85).aspx">MonitorFromRect function</a>
    /// </summary>
    /// <param name="rect">A RECT structure that specifies the rectangle of interest in virtual-screen coordinates.</param>
    /// <param name="monitorFrom">MonitorFromRectFlags</param>
    /// <returns>HMONITOR handle</returns>
    [DllImport(User32, SetLastError = true)]
    public static extern IntPtr MonitorFromRect([In] ref NativeRect rect, MonitorFrom monitorFrom);

    /// <summary>
    ///     See <a href="https://msdn.microsoft.com/en-us/library/windows/desktop/ms633516(v=vs.85).aspx">GetWindowInfo</a>
    ///     Retrieves information about the specified window.
    /// </summary>
    /// <param name="hWnd">IntPtr</param>
    /// <param name="windowInfo">WindowInfo (use WindowInfo.Create)</param>
    /// <returns>bool if false than get the last error</returns>
    [DllImport(User32, SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static extern bool GetWindowInfo(IntPtr hWnd, ref WindowInfo windowInfo);

    /// <summary>
    ///     See <a href="https://msdn.microsoft.com/en-us/library/windows/desktop/ms633497(v=vs.85).aspx">here</a>
    /// </summary>
    /// <param name="enumFunc">EnumWindowsProc</param>
    /// <param name="param">An application-defined value to be passed to the callback function.</param>
    /// <returns>true if success</returns>
    [DllImport(User32, SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static extern bool EnumWindows(EnumWindowsProc enumFunc, IntPtr param);

    /// <summary>
    ///     See
    ///     <a href="https://msdn.microsoft.com/en-us/library/windows/desktop/ms633495(v=vs.85).aspx">
    ///         EnumThreadWindows
    ///         function
    ///     </a>
    ///     Enumerates all non child windows associated with a thread by passing the handle to each window, in turn, to an
    ///     application-defined callback function.
    ///     EnumThreadWindows continues until the last window is enumerated or the callback function returns FALSE.
    ///     To enumerate child windows of a particular window, use the EnumChildWindows function.
    /// </summary>
    /// <param name="threadId">The identifier of the thread whose windows are to be enumerated.</param>
    /// <param name="enumFunc">EnumWindowsProc</param>
    /// <param name="param">An application-defined value to be passed to the callback function.</param>
    /// <returns></returns>
    [DllImport(User32, SetLastError = true)]
    public static extern bool EnumThreadWindows(int threadId, EnumWindowsProc enumFunc, IntPtr param);

    /// <summary>
    ///     See <a href="https://msdn.microsoft.com/en-us/library/windows/desktop/ms633497(v=vs.85).aspx">here</a>
    /// </summary>
    /// <param name="hWndParent">IntPtr with hWnd of parent window, if this is IntPtr.Zero this function behaves as EnumWindows</param>
    /// <param name="enumFunc">EnumWindowsProc</param>
    /// <param name="param">An application-defined value to be passed to the callback function.</param>
    /// <returns>true if success</returns>
    [DllImport(User32, SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static extern bool EnumChildWindows(IntPtr hWndParent, EnumWindowsProc enumFunc, IntPtr param);

    /// <summary>
    ///     See <a href="https://msdn.microsoft.com/en-us/library/windows/desktop/bb787583(v=vs.85).aspx">GetScrollInfo</a> for
    ///     more information.
    /// </summary>
    /// <param name="hWnd">IntPtr with the window handle</param>
    /// <param name="scrollBar">ScrollBarTypes</param>
    /// <param name="scrollInfo">ScrollInfo ref</param>
    /// <returns>bool if it worked</returns>
    [DllImport(User32, SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static extern bool GetScrollInfo(IntPtr hWnd, ScrollBarTypes scrollBar, ref ScrollInfo scrollInfo);

    /// <summary>
    ///     See <a href="https://msdn.microsoft.com/en-us/library/windows/desktop/bb787595(v=vs.85).aspx">SetScrollInfo</a> for
    ///     more information.
    /// </summary>
    /// <param name="hWnd">IntPtr with the window handle</param>
    /// <param name="scrollBar">ScrollBarTypes</param>
    /// <param name="scrollInfo">ScrollInfo ref</param>
    /// <param name="redraw">bool to specify if a redraw should be made</param>
    /// <returns>int with the current position of the scroll box</returns>
    [DllImport(User32, SetLastError = true)]
    public static extern int SetScrollInfo(IntPtr hWnd, ScrollBarTypes scrollBar, ref ScrollInfo scrollInfo, bool redraw);

    /// <summary>
    ///     See
    ///     <a href="https://msdn.microsoft.com/en-us/library/windows/desktop/bb787601(v=vs.85).aspx">ShowScrollBar function</a>
    ///     for more information.
    /// </summary>
    /// <param name="hWnd">IntPtr</param>
    /// <param name="scrollBar">ScrollBarTypes</param>
    /// <param name="show">true to show, false to hide</param>
    /// <returns>
    ///     If the function succeeds, the return value is nonzero.
    ///     If the function fails, the return value is zero. To get extended error information, call GetLastError.
    /// </returns>
    [DllImport(User32, SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static extern bool ShowScrollBar(IntPtr hWnd, ScrollBarTypes scrollBar, [MarshalAs(UnmanagedType.Bool)] bool show);

    /// <summary>
    ///     See
    ///     <a href="https://msdn.microsoft.com/en-us/library/windows/desktop/bb787581(v=vs.85).aspx">GetScrollBarInfo function</a>
    ///     for more information.
    /// </summary>
    /// <param name="hWnd">
    ///     Handle to a window associated with the scroll bar whose information is to be retrieved. If the
    ///     idObject parameter is OBJID_CLIENT, hWnd is a handle to a scroll bar control. Otherwise, hWnd is a handle to a
    ///     window created with WS_VSCROLL and/or WS_HSCROLL style.
    /// </param>
    /// <param name="idObject">
    ///     Specifies the scroll bar object. Can be ObjectIdentifiers.Client,
    ///     ObjectIdentifiers.HorizontalScrollbar, ObjectIdentifiers.VerticalScrollbar
    /// </param>
    /// <param name="scrollBarInfo">ScrollBarInfo ref</param>
    /// <returns>bool</returns>
    [DllImport(User32, SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static extern bool GetScrollBarInfo(IntPtr hWnd, ObjectIdentifiers idObject, ref ScrollBarInfo scrollBarInfo);

    /// <summary>
    /// See <a href="https://docs.microsoft.com/en-us/windows/win32/api/winuser/nf-winuser-setwindowdisplayaffinity">>SetWindowDisplayAffinity function</a>
    /// </summary>
    /// <param name="hWnd">IntPtr</param>
    /// <param name="windowDisplayAffinity">WindowDisplayAffinity</param>
    /// <returns>bool true if it worked</returns>
    [DllImport(User32, SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static extern bool SetWindowDisplayAffinity(IntPtr hWnd, WindowDisplayAffinity windowDisplayAffinity);

    /// <summary>
    /// See <a href="https://docs.microsoft.com/en-us/windows/win32/api/winuser/nf-winuser-getwindowdisplayaffinity">>SetWindowDisplayAffinity function</a>
    /// </summary>
    /// <param name="hWnd">IntPtr</param>
    /// <param name="windowDisplayAffinity">WindowDisplayAffinity out</param>
    /// <returns>bool true if it worked</returns>
    [DllImport(User32, SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static extern bool GetWindowDisplayAffinity(IntPtr hWnd, out WindowDisplayAffinity windowDisplayAffinity);

    /// <summary>
    ///     See
    ///     <a href="https://msdn.microsoft.com/en-us/library/windows/desktop/dd144950(v=vs.85).aspx">GetWindowRgn function</a>
    /// </summary>
    /// <param name="hWnd">IntPtr</param>
    /// <param name="hRgn">SafeHandle</param>
    /// <returns>RegionResults</returns>
    [DllImport(User32, SetLastError = true)]
    public static extern RegionResults GetWindowRgn(IntPtr hWnd, SafeHandle hRgn);

    /// <summary>
    /// Changes the size, position, and Z order of a child, pop-up, or top-level window. These windows are ordered according to their appearance on the screen. The topmost window receives the highest rank and is the first window in the Z order.
    /// See <a href="https://msdn.microsoft.com/en-us/library/windows/desktop/ms633545(v=vs.85).aspx">SetWindowPos</a>
    /// </summary>
    /// <param name="hWnd">IntPtr, a handle to the window.</param>
    /// <param name="hWndInsertAfter">IntPtr, a handle to the window to precede the positioned window in the Z order. This parameter must be a window handle or one of
    /// <see cref="WindowHandles.HWND_TOP"/>, <see cref="WindowHandles.HWND_BOTTOM"/>, <see cref="WindowHandles.HWND_TOPMOST"/> or <see cref="WindowHandles.HWND_NOTOPMOST"/>.
    /// It is ignored when uFlags contains SWP_NOZORDER.</param>
    /// <param name="x">int</param>
    /// <param name="y">int</param>
    /// <param name="cx">int</param>
    /// <param name="cy">int</param>
    /// <param name="uFlags">WindowPos</param>
    /// <returns>bool</returns>
    [DllImport(User32, SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static extern bool SetWindowPos(IntPtr hWnd, IntPtr hWndInsertAfter, int x, int y, int cx, int cy, WindowPos uFlags);

    /// <summary>
    /// Examines the Z order of the child windows associated with the specified parent window and retrieves a handle to the child window at the top of the Z order.
    /// See <a href="https://msdn.microsoft.com/en-us/library/windows/desktop/ms633514(v=vs.85).aspx">GetTopWindow</a>
    /// </summary>
    /// <param name="hWnd">A handle to the parent window whose child windows are to be examined. If this parameter is NULL, the function returns a handle to the window at the top of the Z order.</param>
    /// <returns>If the function succeeds, the return value is a handle to the child window at the top of the Z order. If the specified window has no child windows, the return value is NULL. To get extended error information, use the GetLastError function.</returns>
    [DllImport(User32, SetLastError = true)]
    public static extern IntPtr GetTopWindow(IntPtr hWnd);

    /// <summary>
    /// The GetWindowDC function retrieves the device context (DC) for the entire window, including title bar, menus, and scroll bars. A window device context permits painting anywhere in a window, because the origin of the device context is the upper-left corner of the window instead of the client area.
    /// GetWindowDC assigns default attributes to the window device context each time it retrieves the device context.Previous attributes are lost.
    /// See <a href="https://docs.microsoft.com/en-us/windows/desktop/api/winuser/nf-winuser-getwindowdc">GetWindowDC function</a>
    /// </summary>
    /// <param name="hWnd">A handle to the window whose DC is to be retrieved. If this value is NULL, GetWindowDC retrieves the DC for the entire screen.</param>
    /// <returns>If the function succeeds, the return value is a handle to a device context for the specified window.</returns>
    [DllImport(User32, SetLastError = true)]
    public static extern IntPtr GetWindowDC(IntPtr hWnd);

    /// <summary>
    /// The GetDC function retrieves a handle to a device context (DC) for the client area of a specified window or for the entire screen. You can use the returned handle in subsequent GDI functions to draw in the DC. The device context is an opaque data structure, whose values are used internally by GDI.
    /// See <a href="https://msdn.microsoft.com/en-us/library/windows/desktop/dd144871(v=vs.85).aspx">GetDC function</a>
    /// </summary>
    /// <param name="hWnd">A handle to the window whose DC is to be retrieved. If this value is NULL, GetDC retrieves the DC for the entire screen.</param>
    /// <returns>If the function succeeds, the return value is a handle to the DC for the specified window's client area.</returns>
    [DllImport(User32, SetLastError = true)]
    public static extern IntPtr GetDC(IntPtr hWnd);

    /// <summary>
    /// The ReleaseDC function releases a device context (DC), freeing it for use by other applications. The effect of the ReleaseDC function depends on the type of DC. It frees only common and window DCs. It has no effect on class or private DCs.
    /// </summary>
    /// <param name="hWnd">IntPtr A handle to the window whose DC is to be released.</param>
    /// <param name="hDc">IntPtr A handle to the DC to be released.</param>
    /// <returns>The return value indicates whether the DC was released.</returns>
    [DllImport(User32, SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static extern bool ReleaseDC(IntPtr hWnd, IntPtr hDc);

    /// <summary>
    /// Retrieves a handle to the top-level window whose class name and window name match the specified strings. This function does not search child windows. This function does not perform a case-sensitive search.
    /// To search child windows, beginning with a specified child window, use the FindWindowEx function.
    /// </summary>
    /// <param name="lpClassName">string</param>
    /// <param name="lpWindowName">string</param>
    /// <returns>If the function succeeds, the return value is a handle to the window that has the specified class name and window name.</returns>
    [DllImport(User32, SetLastError = true, CharSet = CharSet.Unicode)]
    public static extern IntPtr FindWindow(string lpClassName, string lpWindowName);

    /// <summary>
    /// Retrieves a handle to a window whose class name and window name match the specified strings. The function searches child windows, beginning with the one following the specified child window. This function does not perform a case-sensitive search.
    /// </summary>
    /// <param name="hWndParent">
    /// IntPtr, A handle to the parent window whose child windows are to be searched.
    /// If hWndParent is NULL, the function uses the desktop window as the parent window. The function searches among windows that are child windows of the desktop.
    /// If hWndParent is <see cref="WindowHandles.HWND_MESSAGE"/>, the function searches all message-only windows.
    /// </param>
    /// <param name="hWndChildAfter">
    /// IntPtr, a handle to a child window. The search begins with the next child window in the Z order. The child window must be a direct child window of hWndParent, not just a descendant window.
    /// If hWndChildAfter is NULL, the search begins with the first child window of hWndParent.
    /// Note that if both hWndParent and hWndChildAfter are NULL, the function searches all top-level and message-only windows.
    /// </param>
    /// <param name="lpszClass">
    /// The class name or a class atom created by a previous call to the RegisterClass or RegisterClassEx function. The atom must be placed in the low-order word of lpszClass; the high-order word must be zero.
    /// If lpszClass is a string, it specifies the window class name. The class name can be any name registered with RegisterClass or RegisterClassEx, or any of the predefined control-class names, or it can be MAKEINTATOM(0x8000). In this latter case, 0x8000 is the atom for a menu class. For more information, see the Remarks section of this topic.
    /// </param>
    /// <param name="lpszWindow">The window name (the window's title). If this parameter is NULL, all window names match.</param>
    /// <remarks>If the lpszWindow parameter is not NULL, FindWindowEx calls the GetWindowText function to retrieve the window name for comparison. For a description of a potential problem that can arise, see the Remarks section of GetWindowText.
    /// An application can call this function in the following way.
    /// FindWindowEx( NULL, NULL, MAKEINTATOM(0x8000), NULL );
    /// Note that 0x8000 is the atom for a menu class. When an application calls this function, the function checks whether a context menu is being displayed that the application created.</remarks>
    /// <returns>
    /// If the function succeeds, the return value is a handle to the window that has the specified class and window names.
    /// If the function fails, the return value is NULL. To get extended error information, call GetLastError.
    /// </returns>
    [DllImport(User32, SetLastError = true, CharSet = CharSet.Unicode)]
    public static extern IntPtr FindWindowEx(IntPtr hWndParent, IntPtr hWndChildAfter, string lpszClass, string lpszWindow);

    /// uiFlags: 0 - Count of GDI objects
    /// uiFlags: 1 - Count of USER objects
    /// - Win32 GDI objects (pens, brushes, fonts, palettes, regions, device contexts, bitmap headers)
    /// - Win32 USER objects:
    /// - 	WIN32 resources (accelerator tables, bitmap resources, dialog box templates, font resources, menu resources, raw data resources, string table entries, message table entries, cursors/icons)
    /// - Other USER objects (windows, menus)
    [DllImport(User32, SetLastError = true)]
    public static extern uint GetGuiResources(IntPtr hProcess, uint uiFlags);

    /// <summary>
    /// Sends the specified message to one or more windows, depending on the specified fuFlags this can handle issues with hanging message loops.
    /// See <a href="https://msdn.microsoft.com/en-us/library/windows/desktop/ms644952(v=vs.85).aspx">SendMessageTimeout function</a>
    /// </summary>
    /// <param name="hWnd">IntPtr</param>
    /// <param name="msg">WindowsMessages</param>
    /// <param name="wParam">IntPtr</param>
    /// <param name="lParam">IntPtr</param>
    /// <param name="fuFlags">SendMessageTimeoutFlags</param>
    /// <param name="uTimeout">uint The duration of the time-out period, in milliseconds. If the message is a broadcast message, each window can use the full time-out period. For example, if you specify a five second time-out period and there are three top-level windows that fail to process the message, you could have up to a 15 second delay.</param>
    /// <param name="lpdwResult">UIntPtr The result of the message processing. The value of this parameter depends on the message that is specified.</param>
    /// <returns>bool false if timeout true if the sendmessage returned</returns>
    [DllImport(User32, SetLastError = true, CharSet = CharSet.Unicode)]
    public static extern bool SendMessageTimeout(IntPtr hWnd, WindowsMessages msg, IntPtr wParam, IntPtr lParam, SendMessageTimeoutFlags fuFlags, uint uTimeout, out IntPtr lpdwResult);

    /// <summary>
    /// Sends the specified message to one or more windows, depending on the specified fuFlags this can handle issues with hanging message loops.
    /// See <a href="https://msdn.microsoft.com/en-us/library/windows/desktop/ms644952(v=vs.85).aspx">SendMessageTimeout function</a>
    /// </summary>
    /// <param name="hWnd">IntPtr</param>
    /// <param name="msg">uint</param>
    /// <param name="wParam">IntPtr</param>
    /// <param name="lParam">IntPtr</param>
    /// <param name="fuFlags">SendMessageTimeoutFlags</param>
    /// <param name="uTimeout">uint The duration of the time-out period, in milliseconds. If the message is a broadcast message, each window can use the full time-out period. For example, if you specify a five second time-out period and there are three top-level windows that fail to process the message, you could have up to a 15 second delay.</param>
    /// <param name="lpdwResult">IntPtr The result of the message processing. The value of this parameter depends on the message that is specified.</param>
    /// <returns>bool false if timeout true if the sendmessage returned</returns>
    [DllImport(User32, SetLastError = true, CharSet = CharSet.Unicode)]
    public static extern bool SendMessageTimeout(IntPtr hWnd, uint msg, IntPtr wParam, IntPtr lParam, SendMessageTimeoutFlags fuFlags, uint uTimeout, out IntPtr lpdwResult);

    /// <summary>
    /// Sends the specified message to one or more windows, depending on the specified fuFlags this can handle issues with hanging message loops.
    /// See <a href="https://msdn.microsoft.com/en-us/library/windows/desktop/ms644952(v=vs.85).aspx">SendMessageTimeout function</a>
    /// </summary>
    /// <param name="hWnd">IntPtr</param>
    /// <param name="msg">uint</param>
    /// <param name="wParam">IntPtr</param>
    /// <param name="lParam">IntPtr</param>
    /// <param name="fuFlags">SendMessageTimeoutFlags</param>
    /// <param name="uTimeout">uint The duration of the time-out period, in milliseconds. If the message is a broadcast message, each window can use the full time-out period. For example, if you specify a five second time-out period and there are three top-level windows that fail to process the message, you could have up to a 15 second delay.</param>
    /// <param name="lpdwResult">UIntPtr The result of the message processing. The value of this parameter depends on the message that is specified.</param>
    /// <returns>bool false if timeout true if the sendmessage returned</returns>
    [DllImport(User32, SetLastError = true, CharSet = CharSet.Unicode)]
    public static extern bool SendMessageTimeout(IntPtr hWnd, uint msg, IntPtr wParam, IntPtr lParam, SendMessageTimeoutFlags fuFlags, uint uTimeout, out UIntPtr lpdwResult);

    [DllImport(User32, SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    internal static extern bool GetPhysicalCursorPos(out NativePoint cursorLocation);

    [DllImport(User32, SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool GetCursorPos(out NativePoint cursorLocation);

    /// <summary>
    /// The MapWindowPoints function converts (maps) a point from a coordinate space relative to one window to a coordinate space relative to another window.
    /// </summary>
    /// <param name="hWndFrom">IntPtr window handle for the from window, IntPtr.Zero for screen coordinates</param>
    /// <param name="hWndTo">IntPtr window handle for the to window, IntPtr.Zero for screen coordinates</param>
    /// <param name="point">NativePoint to convert, in device units</param>
    /// <returns>See <see cref="MapWindowPoints(IntPtr, IntPtr, NativePoint[])"/></returns>
    public static int MapWindowPoints(IntPtr hWndFrom, IntPtr hWndTo, ref NativePoint point)
    {
        return MapWindowPointsInternal(hWndFrom, hWndTo, ref point, 1);
    }

    /// <summary>
    /// The MapWindowPoints function converts (maps) a rectangle from a coordinate space relative to one window to a coordinate space relative to another window.
    /// If exactly one of the windows is mirrored, the left and right coordinates are swapped so the result is a normalized rectangle.
    /// </summary>
    /// <param name="hWndFrom">IntPtr window handle for the from window, IntPtr.Zero for screen coordinates</param>
    /// <param name="hWndTo">IntPtr window handle for the to window, IntPtr.Zero for screen coordinates</param>
    /// <param name="rect">NativeRect to convert, in device units</param>
    /// <returns>See <see cref="MapWindowPoints(IntPtr, IntPtr, NativePoint[])"/></returns>
    public static int MapWindowPoints(IntPtr hWndFrom, IntPtr hWndTo, ref NativeRect rect)
    {
        return MapWindowPointsInternal(hWndFrom, hWndTo, ref rect, 2);
    }

    /// <summary>
    /// The MapWindowPoints function converts (maps) a set of points from a coordinate space relative to one window to a coordinate space relative to another window.
    /// </summary>
    /// <param name="hWndFrom">IntPtr window handle for the from window, IntPtr.Zero for screen coordinates</param>
    /// <param name="hWndTo">IntPtr window handle for the to window, IntPtr.Zero for screen coordinates</param>
    /// <param name="points">NativePoint array with the points to convert, in device units, these are converted in place</param>
    /// <returns>If the function succeeds, the low-order word of the return value is the number of pixels added to the horizontal coordinate of each source point in order to compute the horizontal coordinate of each destination point. (In addition to that, if precisely one of hWndFrom and hWndTo is mirrored, then each resulting horizontal coordinate is multiplied by -1.) The high-order word is the number of pixels added to the vertical coordinate of each source point in order to compute the vertical coordinate of each destination point.
    /// If the function fails, the return value is zero. Call SetLastError prior to calling this method to differentiate an error return value from a legitimate "0" return value.</returns>
    public static int MapWindowPoints(IntPtr hWndFrom, IntPtr hWndTo, NativePoint[] points)
    {
        if (points == null)
        {
            throw new ArgumentNullException(nameof(points));
        }
        return points.Length == 0 ? 0 : MapWindowPointsInternal(hWndFrom, hWndTo, points, points.Length);
    }

    [DllImport(User32, SetLastError = true, EntryPoint = "MapWindowPoints", ExactSpelling = true)]
    private static extern int MapWindowPointsInternal(IntPtr hWndFrom, IntPtr hWndTo, ref NativePoint lpPoints, int cPoints);

    [DllImport(User32, SetLastError = true, EntryPoint = "MapWindowPoints", ExactSpelling = true)]
    private static extern int MapWindowPointsInternal(IntPtr hWndFrom, IntPtr hWndTo, ref NativeRect lpRect, int cPoints);

    [DllImport(User32, SetLastError = true, EntryPoint = "MapWindowPoints", ExactSpelling = true)]
    private static extern int MapWindowPointsInternal(IntPtr hWndFrom, IntPtr hWndTo, [In, Out] NativePoint[] lpPoints, int cPoints);

    /// <summary>
    ///     See
    ///     <a href="https://msdn.microsoft.com/en-us/library/windows/desktop/ms724385(v=vs.85).aspx">GetSystemMetrics function</a>
    /// </summary>
    /// <param name="index">SystemMetric</param>
    /// <returns>int</returns>
    [DllImport(User32)]
    public static extern int GetSystemMetrics(SystemMetric index);

    /// <summary>
    /// Sets the mouse capture to the specified window belonging to the current thread.SetCapture captures mouse input either when the mouse is over the capturing window, or when the mouse button was pressed while the mouse was over the capturing window and the button is still down. Only one window at a time can capture the mouse.
    /// If the mouse cursor is over a window created by another thread, the system will direct mouse input to the specified window only if a mouse button is down.
    /// See <a href="https://msdn.microsoft.com/en-us/library/windows/desktop/ms646262(v=vs.85).aspx">SetCapture function</a>
    /// </summary>
    /// <param name="hWnd">A handle to the window in the current thread that is to capture the mouse.</param>
    /// <remarks>
    /// Only the foreground window can capture the mouse. When a background window attempts to do so, the window receives messages only for mouse events that occur when the cursor hot spot is within the visible portion of the window. Also, even if the foreground window has captured the mouse, the user can still click another window, bringing it to the foreground.
    /// When the window no longer requires all mouse input, the thread that created the window should call the ReleaseCapture function to release the mouse.
    /// This function cannot be used to capture mouse input meant for another process.
    /// When the mouse is captured, menu hotkeys and other keyboard accelerators do not work.
    /// </remarks>
    /// <returns>The return value is a handle to the window that had previously captured the mouse. If there is no such window, the return value is NULL.</returns>
    [DllImport(User32, SetLastError = true)]
    public static extern IntPtr SetCapture(IntPtr hWnd);

    /// <summary>
    /// Releases the mouse capture from a window in the current thread and restores normal mouse input processing. A window that has captured the mouse receives all mouse input, regardless of the position of the cursor, except when a mouse button is clicked while the cursor hot spot is in the window of another thread.
    /// See <a href="https://msdn.microsoft.com/en-us/library/windows/desktop/ms646261(v=vs.85).aspx">ReleaseCapture function</a>
    /// </summary>
    /// <returns>bool if it worked</returns>
    [DllImport(User32, SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static extern bool ReleaseCapture();

    [DllImport(User32, SetLastError = true)]
    internal static extern SafeDesktopHandle OpenInputDesktop(uint dwFlags, [MarshalAs(UnmanagedType.Bool)] bool fInherit, DesktopAccessRight dwDesiredAccess);

    [DllImport(User32, SetLastError = true)]
    internal static extern IntPtr GetThreadDesktop(int dwThreadId);

    [DllImport(User32, SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    internal static extern bool SetThreadDesktop(SafeDesktopHandle hDesktop);

    [DllImport(User32, SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    internal static extern bool SetThreadDesktop(IntPtr hDesktop);

    [DllImport(User32, SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    internal static extern bool CloseDesktop(IntPtr hDesktop);

    [DllImport(User32, SetLastError = true, CharSet = CharSet.Unicode)]
    internal static extern bool EnumDisplayMonitors(IntPtr hdc, IntPtr lprcClip, MonitorEnumDelegate lpfnEnum, IntPtr dwData);

    [DllImport(User32, SetLastError = true, CharSet = CharSet.Unicode)]
    [return: MarshalAs(UnmanagedType.Bool)]
    internal static extern bool GetMonitorInfo(IntPtr hMonitor, ref MonitorInfoEx lpmi);

    /// <summary>
    ///     See
    ///     <a href="https://msdn.microsoft.com/en-us/library/windows/desktop/ms724947(v=vs.85).aspx">
    ///         SystemParametersInfo
    ///         function
    ///     </a>
    ///     For setting a string parameter
    /// </summary>
    /// <param name="uiAction">SystemParametersInfoActions</param>
    /// <param name="uiParam">
    ///     A parameter whose usage and format depends on the system parameter being queried or set. For more
    ///     information about system-wide parameters, see the uiAction parameter. If not otherwise indicated, you must specify
    ///     zero for this parameter.
    /// </param>
    /// <param name="pvParam">string</param>
    /// <param name="fWinIni">SystemParametersInfoBehaviors</param>
    /// <returns>bool</returns>
    [DllImport(User32, CharSet = CharSet.Unicode, SetLastError = true, EntryPoint = "SystemParametersInfoW", ExactSpelling = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static extern bool SystemParametersInfo(SystemParametersInfoActions uiAction, uint uiParam, string pvParam, SystemParametersInfoBehaviors fWinIni);

    /// <summary>
    ///     See
    ///     <a href="https://msdn.microsoft.com/en-us/library/windows/desktop/ms724947(v=vs.85).aspx">
    ///         SystemParametersInfo
    ///         function
    ///     </a>
    ///     For reading a string parameter
    /// </summary>
    /// <param name="uiAction">SystemParametersInfoActions</param>
    /// <param name="uiParam">
    ///     A parameter whose usage and format depends on the system parameter being queried or set. For more
    ///     information about system-wide parameters, see the uiAction parameter. If not otherwise indicated, you must specify
    ///     zero for this parameter.
    /// </param>
    /// <param name="pvParam">string</param>
    /// <param name="fWinIni">SystemParametersInfoBehaviors</param>
    /// <returns>bool</returns>
    [DllImport(User32, CharSet = CharSet.Unicode, SetLastError = true, EntryPoint = "SystemParametersInfoW", ExactSpelling = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static extern bool SystemParametersInfo(SystemParametersInfoActions uiAction, uint uiParam, StringBuilder pvParam, SystemParametersInfoBehaviors fWinIni);

    /// <summary>
    ///     See
    ///     <a href="https://msdn.microsoft.com/en-us/library/windows/desktop/ms724947(v=vs.85).aspx">
    ///         SystemParametersInfo
    ///         function
    ///     </a>
    ///     For setting AnimationInfo
    /// </summary>
    /// <param name="uiAction">SystemParametersInfoActions</param>
    /// <param name="uiParam">
    ///     A parameter whose usage and format depends on the system parameter being queried or set. For more
    ///     information about system-wide parameters, see the uiAction parameter. If not otherwise indicated, you must specify
    ///     zero for this parameter.
    /// </param>
    /// <param name="animationInfo">AnimationInfo</param>
    /// <param name="fWinIni">SystemParametersInfoBehaviors</param>
    /// <returns>bool</returns>
    [DllImport(User32, CharSet = CharSet.Unicode, SetLastError = true, EntryPoint = "SystemParametersInfoW", ExactSpelling = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static extern bool SystemParametersInfo(SystemParametersInfoActions uiAction, uint uiParam, ref AnimationInfo animationInfo, SystemParametersInfoBehaviors fWinIni);

    /// <summary>
    ///     SystemParametersInfo for reading a UINT parameter, e.g. SPI_GETWHEELSCROLLLINES
    /// </summary>
    /// <param name="uiAction">SystemParametersInfoActions</param>
    /// <param name="uiParam">uint, must be 0 for most actions</param>
    /// <param name="pvParam">out uint with the value</param>
    /// <param name="fWinIni">SystemParametersInfoBehaviors</param>
    /// <returns>bool</returns>
    [DllImport(User32, CharSet = CharSet.Unicode, SetLastError = true, EntryPoint = "SystemParametersInfoW", ExactSpelling = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool SystemParametersInfo(SystemParametersInfoActions uiAction, uint uiParam, out uint pvParam, SystemParametersInfoBehaviors fWinIni);

    /// <summary>
    /// Locks the workstation's display. Locking a workstation protects it from unauthorized use.
    /// </summary>
    /// <returns>Because the function executes asynchronously, true indicates that the operation has been initiated. If false, call GetLastError</returns>
    [DllImport(User32, SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static extern bool LockWorkStation();

    /// <summary>
    ///     See
    ///     <a href="https://msdn.microsoft.com/en-us/library/windows/desktop/ms648389(v=vs.85).aspx">GetCursorInfo function</a>
    ///     Retrieves information about the global cursor.
    /// </summary>
    /// <param name="cursorInfo">CursorInfo structure to fill</param>
    /// <returns>bool</returns>
    [DllImport(User32, SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static extern bool GetCursorInfo(ref CursorInfo cursorInfo);

    /// <summary>
    /// Destroys the specified cursor and releases any resources associated with it.
    /// </summary>
    /// <param name="hCursor">A handle to the cursor to be destroyed. The handle must have been created by the application.</param>
    /// <returns>true if the cursor was successfully destroyed; otherwise, false.</returns>
    [DllImport(User32, SetLastError = true)]
    internal static extern bool DestroyCursor(IntPtr hCursor);

    /// <summary>
    /// Fills a rectangle by using the specified brush and logical device context.
    /// </summary>
    /// <param name="hDC">A handle to the device context in which the rectangle is to be filled.</param>
    /// <param name="lprc">A reference to a NativeRect structure that specifies the logical coordinates of the rectangle to be filled.</param>
    /// <param name="hbr">A handle to the brush used to fill the rectangle.</param>
    /// <returns>true if the function succeeds</returns>
    [DllImport(User32, SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static extern bool FillRect(IntPtr hDC, [In] ref NativeRect lprc, IntPtr hbr);
}