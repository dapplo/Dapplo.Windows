﻿// Copyright (c) Dapplo and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
using System.Runtime.InteropServices;
using Dapplo.Windows.Messages.Enumerations;

namespace Dapplo.Windows.Messages;

/// <summary>
/// Helper class to work with windows messages
/// </summary>
public static class WindowsMessage
{
    /// <summary>
    /// This returns the name of a windows message, which was registered with RegisterWindowMessage 
    /// </summary>
    /// <param name="messageId">uint with the id which was returned by RegisterWindowMessage</param>
    /// <returns>string</returns>
    public static string GetWindowsMessage(uint messageId)
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
    /// Register a windows message
    /// </summary>
    /// <param name="message">Windows message</param>
    /// <returns>uint with the message ID</returns>
    public static uint RegisterWindowsMessage(string message)
    {
        return RegisterWindowMessageW(message);
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