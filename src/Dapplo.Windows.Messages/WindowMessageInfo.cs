using System;

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
        /// <param name="hwnd">IntPtr with the Handle of the window</param>
        /// <param name="msg">WindowsMessages which is the actual message</param>
        /// <param name="wParam">IntPtr with the word-param</param>
        /// <param name="lParam">IntPtr with the long-param</param>
        /// <param name="handled"></param>
        /// <returns>WindowMessageInfo</returns>
        public static WindowMessageInfo Create(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
        {
            return new WindowMessageInfo
            {
                Handle = hwnd,
                Message = (WindowsMessages)msg,
                WordParam = wParam,
                LongParam = lParam
            };
        }
    }
}
