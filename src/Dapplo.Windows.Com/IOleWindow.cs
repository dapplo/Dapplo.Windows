using System;
using System.Runtime.InteropServices;

namespace Dapplo.Windows.Com
{
    /// <summary>
    /// The IOleWindow interface provides methods that allow an application to obtain the handle to the various windows that participate in in-place activation, and also to enter and exit context-sensitive help mode.
    /// See <a href="http://msdn.microsoft.com/en-us/library/ms680102%28v=vs.85%29.aspx">IOleWindow interface</a>
    /// </summary>
    [ComImport]
    [Guid("00000114-0000-0000-C000-000000000046")]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    public interface IOleWindow
    {
        /// <summary>
        /// Retrieves a handle to one of the windows participating in in-place activation (frame, document, parent, or in-place object window)
        /// </summary>
        /// <param name="phWnd">A pointer to a variable that receives the window handle.</param>
        void GetWindow(out IntPtr phWnd);
        /// <summary>
        /// Determines whether context-sensitive help mode should be entered during an in-place activation session.
        /// </summary>
        /// <param name="fEnterMode">TRUE if help mode should be entered; FALSE if it should be exited.</param>
        void ContextSensitiveHelp([In] [MarshalAs(UnmanagedType.Bool)] bool fEnterMode);
    }
}