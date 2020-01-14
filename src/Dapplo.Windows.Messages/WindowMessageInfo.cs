// Copyright (c) Dapplo and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
using System;
using Dapplo.Windows.Messages.Enumerations;

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
