// Copyright (c) Dapplo and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
using System;
using System.ComponentModel;
using System.Runtime.InteropServices;

namespace Dapplo.Windows.Clipboard.Internals;

internal static class NativeMethods
{
    /// <summary>
    ///     See
    ///     <a href="https://msdn.microsoft.com/en-us/library/windows/desktop/ms649038(v=vs.85).aspx">EnumClipboardFormats function</a>
    ///     Enumerates the data formats currently available on the clipboard.
    ///     Clipboard data formats are stored in an ordered list. To perform an enumeration of clipboard data formats, you make
    ///     a series of calls to the EnumClipboardFormats function. For each call, the format parameter specifies an available
    ///     clipboard format, and the function returns the next available clipboard format.
    /// </summary>
    /// <param name="format">
    ///     To start an enumeration of clipboard formats, set format to zero. When format is zero, the
    ///     function retrieves the first available clipboard format. For subsequent calls during an enumeration, set format to
    ///     the result of the previous EnumClipboardFormats call.
    /// </param>
    /// <returns>If the function succeeds, the return value is the clipboard format that follows the specified format, namely the next available clipboard format.
    ///     If the function fails, the return value is zero. To get extended error information, call GetLastError. If the clipboard is not open, the function fails.
    ///     If there are no more clipboard formats to enumerate, the return value is zero. In this case, the GetLastError function returns the value ERROR_SUCCESS.
    ///     This lets you distinguish between function failure and the end of enumeration.
    /// </returns>
    [DllImport("user32", SetLastError = true)]
    internal static extern uint EnumClipboardFormats(uint format);

    /// <summary>
    /// Determines whether the clipboard contains data in the specified format.
    /// </summary>
    /// <param name="format">uint for the format</param>
    /// <returns>bool</returns>
    [DllImport("user32", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    internal static extern bool IsClipboardFormatAvailable(uint format);

    /// <summary>
    /// Empties the clipboard and frees handles to data in the clipboard. The function then assigns ownership of the clipboard to the window that currently has the clipboard open.
    /// </summary>
    /// <returns>bool</returns>
    [DllImport("user32", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    internal static extern bool EmptyClipboard();

    /// <summary>
    /// Retrieves data from the clipboard in a specified format. The clipboard must have been opened previously.
    /// </summary>
    /// <param name="format">uint with the clipboard format.</param>
    /// <returns>IntPtr with a handle to the memory</returns>
    [DllImport("user32", SetLastError = true)]
    internal static extern IntPtr GetClipboardData(uint format);

    /// <summary>
    /// Places data on the clipboard in a specified clipboard format.
    /// The window must be the current clipboard owner, and the application must have called the OpenClipboard function.
    /// (When responding to the WM_RENDERFORMAT and WM_RENDERALLFORMATS messages, the clipboard owner must not call OpenClipboard before calling SetClipboardData.)
    /// </summary>
    /// <param name="format">uint</param>
    /// <param name="memory">IntPtr to the memory area</param>
    /// <returns>IntPtr with handle or IntPtr.Zero when an error occurred</returns>
    [DllImport("user32", SetLastError = true)]
    internal static extern IntPtr SetClipboardData(uint format, IntPtr memory);

    /// <summary>
    /// Frees the specified global memory object and invalidates its handle.
    /// </summary>
    /// <param name="hMem">IntPtr with the handle to the global memory object</param>
    /// <returns>IntPtr.Zero if the function succeeds, otherwise the handle</returns>
    [DllImport("kernel32", SetLastError = true)]
    internal static extern IntPtr GlobalFree(IntPtr hMem);

    /// <summary>
    /// Retrieves the currently supported clipboard formats, this does not need the clipboard to be opened.
    /// See <a href="https://learn.microsoft.com/en-us/windows/win32/api/winuser/nf-winuser-getupdatedclipboardformats">GetUpdatedClipboardFormats function</a>
    /// </summary>
    /// <param name="lpuiFormats">array which receives the formats</param>
    /// <param name="cFormats">number of entries in lpuiFormats</param>
    /// <param name="pcFormatsOut">the number of formats</param>
    /// <returns>true if successful</returns>
    [DllImport("user32", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool GetUpdatedClipboardFormats([Out] uint[] lpuiFormats, uint cFormats, out uint pcFormatsOut);

    /// <summary>
    /// Retrieve the currently supported clipboard formats, without opening the clipboard.
    /// </summary>
    /// <returns>array with the format IDs</returns>
    /// <exception cref="Win32Exception">When the formats could not be retrieved</exception>
    internal static uint[] GetUpdatedClipboardFormats()
    {
        const int errorInsufficientBuffer = 122;
        var buffer = new uint[32];
        // The number of formats can change between calls, so retry a few times
        for (var attempt = 0; attempt < 5; attempt++)
        {
            if (GetUpdatedClipboardFormats(buffer, (uint)buffer.Length, out var count))
            {
                if (count >= buffer.Length)
                {
                    return buffer;
                }
                var result = new uint[count];
                Array.Copy(buffer, result, (int)count);
                return result;
            }
            var error = Marshal.GetLastWin32Error();
            if (error != errorInsufficientBuffer)
            {
                throw new Win32Exception(error);
            }
            buffer = new uint[Math.Max((int)count, buffer.Length * 2)];
        }
        throw new Win32Exception(errorInsufficientBuffer);
    }

    /// <summary>
    ///     See
    ///     <a href="https://msdn.microsoft.com/en-us/library/windows/desktop/ms649040(v=vs.85).aspx">GetClipboardFormatName function</a>
    ///     Retrieves from the clipboard the name of the specified registered format.
    ///     The function copies the name to the specified buffer.
    /// </summary>
    /// <param name="format">uint with the id of the format</param>
    /// <param name="lpszFormatName">Name of the format</param>
    /// <param name="cchMaxCount">Maximum size of the output</param>
    /// <returns></returns>
    [DllImport("user32", SetLastError = true, CharSet = CharSet.Unicode)]
    internal static extern unsafe int GetClipboardFormatName(uint format, [Out] char* lpszFormatName, int cchMaxCount);

    /// <summary>
    /// Registers a new clipboard format. This format can then be used as a valid clipboard format.
    ///
    /// If a registered format with the specified name already exists, a new format is not registered and the return value identifies the existing format. This enables more than one application to copy and paste data using the same registered clipboard format. Note that the format name comparison is case-insensitive.
    /// Registered clipboard formats are identified by values in the range 0xC000 through 0xFFFF.
    /// When registered clipboard formats are placed on or retrieved from the clipboard, they must be in the form of an HGLOBAL value.
    /// </summary>
    /// <param name="lpszFormat">The name of the new format.</param>
    /// <returns>
    /// If the function succeeds, the return value identifies the registered clipboard format.
    /// If the function fails, the return value is zero. To get extended error information, call GetLastError.
    /// </returns>
    [DllImport("user32", SetLastError = true, CharSet = CharSet.Unicode)]
    internal static extern uint RegisterClipboardFormat(string lpszFormat);

    /// <summary>
    /// Returns the hWnd of the owner of the clipboard content
    /// </summary>
    /// <returns>IntPtr with a hWnd</returns>
    [DllImport("user32", SetLastError = true)]
    internal static extern IntPtr GetClipboardOwner();

    /// <summary>
    /// See <a href="https://learn.microsoft.com/windows/win32/api/winuser/nf-winuser-getopenclipboardwindow">GetOpenClipboardWindow</a>,
    /// the window which currently has the clipboard open. This is IntPtr.Zero when the clipboard isn't open, or was opened without a window.
    /// </summary>
    [DllImport("user32")]
    internal static extern IntPtr GetOpenClipboardWindow();

    /// <summary>
    /// See <a href="https://learn.microsoft.com/windows/win32/api/winuser/nf-winuser-getwindowthreadprocessid">GetWindowThreadProcessId</a>
    /// </summary>
    [DllImport("user32", SetLastError = true)]
    internal static extern uint GetWindowThreadProcessId(IntPtr hWnd, out uint processId);

    /// <summary>
    /// See <a href="https://learn.microsoft.com/windows/win32/api/winuser/nf-winuser-internalgetwindowtext">InternalGetWindowText</a>,
    /// unlike GetWindowText this never sends WM_GETTEXT, so it can't hang on a window of a blocked thread.
    /// </summary>
    [DllImport("user32", CharSet = CharSet.Unicode)]
    private static extern unsafe int InternalGetWindowText(IntPtr hWnd, char* text, int maxCount);

    /// <summary>
    /// The title of a window, without sending it a message
    /// </summary>
    /// <param name="hWnd">IntPtr with the window</param>
    /// <returns>string, empty when the window has no title or doesn't exist</returns>
    internal static string GetWindowTitle(IntPtr hWnd)
    {
        const int maxLength = 512;
        unsafe
        {
            var buffer = stackalloc char[maxLength];
            var length = InternalGetWindowText(hWnd, buffer, maxLength);
            return length > 0 ? new string(buffer, 0, Math.Min(length, maxLength - 1)) : string.Empty;
        }
    }

    /// <summary>
    /// See <a href="https://learn.microsoft.com/windows/win32/api/stringapiset/nf-stringapiset-multibytetowidechar">MultiByteToWideChar</a>
    /// </summary>
    [DllImport("kernel32", SetLastError = true)]
    internal static extern unsafe int MultiByteToWideChar(uint codePage, uint flags, byte* multiByte, int multiByteLength, char* wideChar, int wideCharLength);

    /// <summary>
    /// See <a href="https://learn.microsoft.com/windows/win32/api/winnls/nf-winnls-getlocaleinfow">GetLocaleInfoW</a>, used with
    /// LOCALE_RETURN_NUMBER: the value is a DWORD, the size is 2 characters
    /// </summary>
    [DllImport("kernel32", SetLastError = true, EntryPoint = "GetLocaleInfoW")]
    internal static extern int GetLocaleInfoNumber(uint locale, uint localeType, out uint value, int valueSizeInChars);

    /// <summary>
    /// Retrieves the sequence number of the clipboard
    /// </summary>
    /// <returns>sequence number or 0 if this cannot be retrieved</returns>
    [DllImport("user32", SetLastError = true)]
    internal static extern uint GetClipboardSequenceNumber();

    /// <summary>
    /// Retrieves the names of dropped files that result from a successful drag-and-drop operation.
    /// </summary>
    /// <param name="hDrop">Identifier of the structure that contains the file names of the dropped files.</param>
    /// <param name="iFile">Index of the file to query. If the value of this parameter is 0xFFFFFFFF, DragQueryFile returns a count of the files dropped. If the value of this parameter is between zero and the total number of files dropped, DragQueryFile copies the file name with the corresponding value to the buffer pointed to by the lpszFile parameter.</param>
    /// <param name="lpszFile">The address of a buffer that receives the file name of a dropped file when the function returns. This file name is a null-terminated string. If this parameter is NULL, DragQueryFile returns the required size, in characters, of this buffer.</param>
    /// <param name="cch">The size, in characters, of the lpszFile buffer.</param>
    /// <returns>
    /// A nonzero value indicates a successful call.
    /// When the function copies a file name to the buffer, the return value is a count of the characters copied, not including the terminating null character.
    /// If the index value is 0xFFFFFFFF, the return value is a count of the dropped files. Note that the index variable itself returns unchanged, and therefore remains 0xFFFFFFFF.
    /// If the index value is between zero and the total number of dropped files, and the lpszFile buffer address is NULL, the return value is the required size, in characters, of the buffer, not including the terminating null character.
    /// </returns>
    [DllImport("shell32", CharSet = CharSet.Unicode)]
    internal static extern unsafe int DragQueryFile(IntPtr hDrop, uint iFile, [Out] char* lpszFile, int cch);

    /// <summary>
    ///     Add a window as a clipboard format listener
    ///     See
    ///     <a href="https://msdn.microsoft.com/en-us/library/windows/desktop/ms649033(v=vs.85).aspx">
    ///         AddClipboardFormatListener
    ///         function
    ///     </a>
    /// </summary>
    /// <param name="hWnd">IntPtr for the window to handle the messages</param>
    /// <returns>true if it worked, false if not; call GetLastError to see what was the problem</returns>
    [DllImport("user32", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    internal static extern bool AddClipboardFormatListener(IntPtr hWnd);

    /// <summary>
    ///     Remove a window as a clipboard format listener
    ///     See
    ///     <a href="https://msdn.microsoft.com/en-us/library/windows/desktop/ms649050(v=vs.85).aspx">
    ///         RemoveClipboardFormatListener
    ///         function
    ///     </a>
    /// </summary>
    /// <param name="hWnd">IntPtr for the window to handle the messages</param>
    /// <returns>true if it worked, false if not; call GetLastError to see what was the problem</returns>
    [DllImport("user32", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    internal static extern bool RemoveClipboardFormatListener(IntPtr hWnd);
}