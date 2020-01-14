// Copyright (c) Dapplo and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
using System;
using System.Runtime.InteropServices;
using Dapplo.Windows.Messages.Structs;

namespace Dapplo.Windows.Messages
{
    /// <summary>
    /// This is a simple message loop for console applications
    /// </summary>
    public static class MessageLoop
    {
        /// <summary>
        /// This defines a delegate for handling windows messages
        /// </summary>
        /// <param name="message">Msg</param>
        /// <returns>true to continue</returns>
        public delegate bool MessageProc(ref Msg message);

        [DllImport("user32.dll")]
        private static extern IntPtr DispatchMessage([In] ref Msg lpMsg);
        [DllImport("user32.dll")]
        private static extern bool TranslateMessage([In] ref Msg lpMsg);

        /// <summary>
        /// 
        /// </summary>
        /// <param name="lpMsg">A pointer to an MSG structure that receives message information from the thread's message queue.</param>
        /// <param name="hWnd">A handle to the window whose messages are to be retrieved. The window must belong to the current thread.
        /// 
        /// If hWnd is NULL, GetMessage retrieves messages for any window that belongs to the current thread, and any messages on the current thread's message queue whose hWnd value is NULL (see the MSG structure). Therefore if hWnd is NULL, both window messages and thread messages are processed.
        /// 
        /// If hWnd is -1, GetMessage retrieves only messages on the current thread's message queue whose hWnd value is NULL, that is, thread messages as posted by PostMessage (when the hWnd parameter is NULL) or PostThreadMessage.</param>
        /// <param name="wMsgFilterMin">The integer value of the lowest message value to be retrieved. Use WM_KEYFIRST (0x0100) to specify the first keyboard message or WM_MOUSEFIRST (0x0200) to specify the first mouse message.
        /// 
        /// Use WM_INPUT here and in wMsgFilterMax to specify only the WM_INPUT messages.
        /// 
        /// If wMsgFilterMin and wMsgFilterMax are both zero, GetMessage returns all available messages (that is, no range filtering is performed).</param>
        /// <param name="wMsgFilterMax">The integer value of the highest message value to be retrieved. Use WM_KEYLAST to specify the last keyboard message or WM_MOUSELAST to specify the last mouse message.
        /// 
        /// Use WM_INPUT here and in wMsgFilterMin to specify only the WM_INPUT messages.
        /// 
        /// If wMsgFilterMin and wMsgFilterMax are both zero, GetMessage returns all available messages (that is, no range filtering is performed).</param>
        /// <returns></returns>
        [DllImport("user32.dll", SetLastError = true)]
        private static extern sbyte GetMessage(out Msg lpMsg, IntPtr hWnd, uint wMsgFilterMin, uint wMsgFilterMax);

        /// <summary>
        /// This processes messages, so a console application can use message based functionality, like low level keyboard events.
        /// The loop is very basic,
        /// </summary>
        /// <param name="handler">An optional function to process the message, return false to stop handling</param>
        /// <param name="hWnd">IntPtr with the window handle to get the messages for</param>
        /// <param name="wMsgFilterMin">The integer value of the lowest message value to be retrieved. Use WM_KEYFIRST (0x0100) to specify the first keyboard message or WM_MOUSEFIRST (0x0200) to specify the first mouse message.</param>
        /// <param name="wMsgFilterMax">The integer value of the highest message value to be retrieved. Use WM_KEYLAST to specify the last keyboard message or WM_MOUSELAST to specify the last mouse message.</param>
        public static void ProcessMessages(MessageProc handler = null, IntPtr hWnd = default, uint? wMsgFilterMin = null, uint? wMsgFilterMax = null)
        {
            do
            {
                var hasMessage = GetMessage(out var msg, hWnd, wMsgFilterMin ?? 0, wMsgFilterMax ?? 0);
                if (hasMessage == -1)
                {
                    // Error!
                    break;
                }
                if (hasMessage == 0)
                {
                    // Error!
                    break;
                }

                // Typical logic for processing the messages
                TranslateMessage(ref msg);
                DispatchMessage(ref msg);

                if (handler != null && !handler(ref msg))
                {
                    break;
                }
            } while (true);
        }
    }
}
