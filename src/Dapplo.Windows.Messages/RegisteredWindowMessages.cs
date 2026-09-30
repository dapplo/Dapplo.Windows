// Copyright (c) Dapplo and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
using System.Runtime.InteropServices;
using Dapplo.Windows.Messages.Enums;

namespace Dapplo.Windows.Messages;

/// <summary>
/// Registers application defined window messages (RegisterWindowMessage), which are unique on the desktop,
/// and resolves the name of a message id.
/// See <a href="https://learn.microsoft.com/windows/win32/api/winuser/nf-winuser-registerwindowmessagew">RegisterWindowMessageW</a>.
/// </summary>
public static class RegisteredWindowMessages
{
    /// <summary>
    /// Get the name of a window message: for a message registered with <see cref="Register"/> (0xC000 through 0xFFFF) this is the registered name,
    /// for the other messages the name of the <see cref="WindowsMessages"/> value (or the number, when it has no name).
    /// </summary>
    /// <param name="messageId">uint with the message id, e.g. as returned by <see cref="Register"/></param>
    /// <returns>string with the name, null when the id is in the registered range but no message with this id was registered</returns>
    public static string GetName(uint messageId)
    {
        // Not a message which we can resolve
        if (messageId < (uint) WindowsMessages.WM_APPLICATION_STRING)
        {
            return ((WindowsMessages)messageId).ToString();
        }
        // We "abuse" the GetClipboardFormatName to get this information, looks weird but it works
        unsafe
        {
            const int capacity = 256;
            var clipboardFormatName = stackalloc char[capacity];

            int numberOfChars = GetClipboardFormatName(messageId, clipboardFormatName, capacity);
            if (numberOfChars <= 0)
            {
                return null;
            }
            return new string(clipboardFormatName, 0, numberOfChars);

        }
    }

    /// <summary>
    /// Register a window message, which is unique on the desktop: every process which registers the same name gets the same id.
    /// Use this e.g. to communicate between instances of an application with PostMessage / SendMessage.
    /// </summary>
    /// <param name="name">string with the unique name of the message, e.g. "MyApp.ShowMainWindow"</param>
    /// <returns>uint with the message id, in the range 0xC000 through 0xFFFF, or 0 when it failed (use Marshal.GetLastWin32Error for the reason)</returns>
    public static uint Register(string name)
    {
        return RegisterWindowMessageW(name);
    }

    /// <summary>
    ///     See
    ///     <a href="https://msdn.microsoft.com/en-us/library/windows/desktop/ms649040(v=vs.85).aspx">GetClipboardFormatName function</a>
    ///     Retrieves from the clipboard the name of the specified registered format.
    ///     The function copies the name to the specified buffer.
    /// </summary>
    /// <param name="format">int with the id of the format</param>
    /// <param name="lpszFormatName">Name of the format</param>
    /// <param name="cchMaxCount">Maximum size of the output</param>
    /// <returns>characters</returns>
    [DllImport("user32", SetLastError = true, CharSet = CharSet.Unicode)]
    private static extern unsafe int GetClipboardFormatName(uint format, [Out] char* lpszFormatName, int cchMaxCount);

    /// <summary>
    /// Defines a new window message that is guaranteed to be unique throughout the system. The message value can be used when sending or posting messages.
    /// </summary>
    /// <param name="lpString">string with the message</param>
    /// <returns>
    /// If the message is successfully registered, the return value is a message identifier in the range 0xC000 through 0xFFFF.
    /// If the function fails, the return value is zero. To get extended error information, call GetLastError.
    /// </returns>
    [DllImport("user32", SetLastError = true, CharSet = CharSet.Unicode)]
    private static extern uint RegisterWindowMessageW(string lpString);
}