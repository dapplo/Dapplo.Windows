
namespace Dapplo.Windows.User32.Enums;

using System;

/// <summary>
/// Specifies window class style flags used when registering a window class in the Windows API.
/// </summary>
/// <remarks>These flags control various behaviors and characteristics of windows created with the associated
/// class, such as redraw behavior, device context allocation, and special effects. Multiple values can be combined
/// using a bitwise OR operation. These styles correspond to the CS_* constants used in native Win32
/// programming.</remarks>
[Flags]
public enum WindowsClassStyles : uint
{
    /// <summary>
    /// Redraws the entire window if a movement or size adjustment changes the height of the client area.
    /// </summary>
    VREDRAW = 0x0001,

    /// <summary>
    /// Redraws the entire window if a movement or size adjustment changes the width of the client area.
    /// </summary>
    HREDRAW = 0x0002,

    /// <summary>
    /// Sends a double-click message to the window procedure when the user double-clicks the mouse 
    /// while the cursor is within a window belonging to the class.
    /// </summary>
    DBLCLKS = 0x0008,

    /// <summary>
    /// Allocates a unique device context for each window in the class.
    /// </summary>
    OWNDC = 0x0020,

    /// <summary>
    /// Allocates one device context to be shared by all windows in the class.
    /// </summary>
    CLASSDC = 0x0040,

    /// <summary>
    /// Sets the clipping rectangle of the child window to that of the parent window so that the child can draw on the parent.
    /// </summary>
    PARENTDC = 0x0080,

    /// <summary>
    /// Disables Close on the window menu.
    /// </summary>
    NOCLOSE = 0x0200,

    /// <summary>
    /// Saves, as a bitmap, the portion of the screen image obscured by a window of this class.
    /// </summary>
    SAVEBITS = 0x0800,

    /// <summary>
    /// Aligns the window's client area on a byte boundary (in the x direction).
    /// </summary>
    BYTEALIGNCLIENT = 0x1000,

    /// <summary>
    /// Aligns the window on a byte boundary (in the x direction).
    /// </summary>
    BYTEALIGNWINDOW = 0x2000,

    /// <summary>
    /// Indicates that the window class is an application global class.
    /// </summary>
    GLOBALCLASS = 0x4000,

    /// <summary>
    /// Enables the drop shadow effect on a window.
    /// </summary>
    DROPSHADOW = 0x00020000,

    /// <summary>
    /// (Internal Use) IME window class.
    /// </summary>
    IME = 0x00010000
}