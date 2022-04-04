// Copyright (c) Dapplo and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Dapplo.Windows.DesktopWindowsManager.Enums;

/// <summary>
///     Flags used by the DwmGetWindowAttribute and DwmSetWindowAttribute functions to specify window attributes for
///     non-client rendering.
///     See http://msdn.microsoft.com/en-us/library/aa969530.aspx
/// </summary>
public enum DwmWindowAttributes
{
    /// <summary>
    ///     Use with DwmGetWindowAttribute. Discovers whether non-client rendering is enabled. The retrieved value is of type
    ///     BOOL. TRUE if non-client rendering is enabled; otherwise, FALSE.
    /// </summary>
    NcRenderingEnabled = 1,

    /// <summary>
    ///     Use with DwmSetWindowAttribute. Sets the non-client rendering policy. The pvAttribute parameter points to a value
    ///     from the DWMNCRENDERINGPOLICY enumeration.
    /// </summary>
    NcrenderingPolicy,

    /// <summary>
    ///     Use with DwmSetWindowAttribute. Enables or forcibly disables DWM transitions. The pvAttribute parameter points to a
    ///     value of TRUE to disable transitions or FALSE to enable transitions.
    /// </summary>
    TransitionsForcedisabled,

    /// <summary>
    ///     Use with DwmSetWindowAttribute. Enables content rendered in the non-client area to be visible on the frame drawn by
    ///     DWM. The pvAttribute parameter points to a value of TRUE to enable content rendered in the non-client area to be
    ///     visible on the frame; otherwise, it points to FALSE.
    /// </summary>
    AllowNcPaint,

    /// <summary>
    ///     Use with DwmGetWindowAttribute. Retrieves the bounds of the caption button area in the window-relative space. The
    ///     retrieved value is of type RECT.
    /// </summary>
    CaptionButtonBounds,

    /// <summary>
    ///     Use with DwmSetWindowAttribute. Specifies whether non-client content is right-to-left (RTL) mirrored. The
    ///     pvAttribute parameter points to a value of TRUE if the non-client content is right-to-left (RTL) mirrored;
    ///     otherwise, it points to FALSE.
    /// </summary>
    NonclientRtlLayout,

    /// <summary>
    ///     Use with DwmSetWindowAttribute. Forces the window to display an iconic thumbnail or peek representation (a static
    ///     bitmap), even if a live or snapshot representation of the window is available. This value normally is set during a
    ///     window's creation and not changed throughout the window's lifetime. Some scenarios, however, might require the
    ///     value to change over time. The pvAttribute parameter points to a value of TRUE to require a iconic thumbnail or
    ///     peek representation; otherwise, it points to FALSE.
    /// </summary>
    ForceIconicRepresentation,

    /// <summary>
    ///     Use with DwmSetWindowAttribute. Sets how Flip3D treats the window. The pvAttribute parameter points to a value from
    ///     the DWMFLIP3DWINDOWPOLICY enumeration.
    /// </summary>
    Flip3dPolicy,

    /// <summary>
    ///     Use with DwmGetWindowAttribute. Retrieves the extended frame bounds rectangle in screen space. The retrieved value
    ///     is of type RECT.
    /// </summary>
    ExtendedFrameBounds,

    /// <summary>
    ///     Use with DwmSetWindowAttribute. The window will provide a bitmap for use by DWM as an iconic thumbnail or peek
    ///     representation (a static bitmap) for the window. DWMWA_HAS_ICONIC_BITMAP can be specified with
    ///     DWMWA_FORCE_ICONIC_REPRESENTATION. DWMWA_HAS_ICONIC_BITMAP normally is set during a window's creation and not
    ///     changed throughout the window's lifetime. Some scenarios, however, might require the value to change over time. The
    ///     pvAttribute parameter points to a value of TRUE to inform DWM that the window will provide an iconic thumbnail or
    ///     peek representation; otherwise, it points to FALSE.
    ///     Windows Vista and earlier:  This value is not supported.
    /// </summary>
    HasIconicBitmap,

    /// <summary>
    ///     Use with DwmSetWindowAttribute. Do not show peek preview for the window. The peek view shows a full-sized preview
    ///     of the window when the mouse hovers over the window's thumbnail in the taskbar. If this attribute is set, hovering
    ///     the mouse pointer over the window's thumbnail dismisses peek (in case another window in the group has a peek
    ///     preview showing). The pvAttribute parameter points to a value of TRUE to prevent peek functionality or FALSE to
    ///     allow it.
    ///     Windows Vista and earlier:  This value is not supported.
    /// </summary>
    DisallowPeek,

    /// <summary>
    ///     Use with DwmSetWindowAttribute. Prevents a window from fading to a glass sheet when peek is invoked. The
    ///     pvAttribute parameter points to a value of TRUE to prevent the window from fading during another window's peek or
    ///     FALSE for normal behavior.
    ///     Windows Vista and earlier:  This value is not supported.
    /// </summary>
    ExcludedFromPeek,

    /// <summary>
    ///     Use with DwmGetWindowAttribute. Cloaks the window such that it is not visible to the user. The window is still
    ///     composed by DWM.
    ///     Using with DirectComposition:  Use the DWMWA_CLOAK flag to cloak the layered child window when animating a
    ///     representation of the window's content via a DirectComposition visual which has been associated with the layered
    ///     child window. For more details on this usage case, see How to How to animate the bitmap of a layered child window.
    ///     Windows 7 and earlier:  This value is not supported.
    /// </summary>
    Cloak,

    /// <summary>
    ///     Use with DwmGetWindowAttribute. If the window is cloaked, provides one of the following values explaining why:
    ///     Name	Value	Meaning
    ///     DWM_CLOAKED_APP	0x0000001	The window was cloaked by its owner application.
    ///     DWM_CLOAKED_SHELL	0x0000002	The window was cloaked by the Shell.
    ///     DWM_CLOAKED_INHERITED	0x0000004	The cloak value was inherited from its owner window.
    ///     Windows 7 and earlier:  This value is not supported.
    /// </summary>
    Cloaked,

    /// <summary>
    ///     Use with DwmSetWindowAttribute. Freeze the window's thumbnail image with its current visuals. Do no further live
    ///     updates on the thumbnail image to match the window's contents.
    ///     Windows 7 and earlier:  This value is not supported.
    /// </summary>
    FreezeRepresentation,

    /// <summary>
    /// Undocumented :(
    /// </summary>
    PassiveUpdateMode,

    /// <summary>
    /// Use with DwmSetWindowAttribute. Enables a non-UWP window to use host backdrop brushes.
    /// If this flag is set, then a Win32 app that calls Windows::UI::Composition APIs can build transparency effects using the host backdrop brush (see Compositor.CreateHostBackdropBrush).
    /// The pvAttribute parameter points to a value of type BOOL. TRUE to enable host backdrop brushes for the window, or FALSE to disable it.
    /// This value is supported starting with Windows 11 Build 22000.
    /// </summary>
    UseHostBackdropBrush,

    /// <summary>
    /// Use with DwmSetWindowAttribute. Allows the window frame for this window to be drawn in dark mode colors when the dark mode system setting is enabled.
    /// For compatibility reasons, all windows default to light mode regardless of the system setting.
    /// The pvAttribute parameter points to a value of type BOOL.
    /// TRUE to honor dark mode for the window, FALSE to always use light mode.
    ///  This value is supported starting with Windows 11 Build 22000.
    /// </summary>
    UseImmersiveDarkMode = 20,

    /// <summary>
    /// Use with DwmSetWindowAttribute. Specifies the rounded corner preference for a window. The pvAttribute parameter points to a value of type DWM_WINDOW_CORNER_PREFERENCE.
    /// This value is supported starting with Windows 11 Build 22000.
    /// </summary>
    WindowCornerPreference = 33,

    /// <summary>
    /// Use with DwmSetWindowAttribute. Specifies the color of the window border. The pvAttribute parameter points to a value of type COLORREF.
    /// The app is responsible for changing the border color according to state changes, such as a change in window activation.
    /// This value is supported starting with Windows 11 Build 22000.
    /// </summary>
    BorderColor,

    /// <summary>
    /// Use with DwmSetWindowAttribute. Specifies the color of the caption. The pvAttribute parameter points to a value of type COLORREF.
    /// This value is supported starting with Windows 11 Build 22000.
    /// </summary>
    CaptionColor,

    /// <summary>
    /// Use with DwmSetWindowAttribute. Specifies the color of the caption text. The pvAttribute parameter points to a value of type COLORREF.
    /// This value is supported starting with Windows 11 Build 22000.
    /// </summary>
    TextColor,

    /// <summary>
    /// Use with DwmGetWindowAttribute. Retrieves the width of the outer border that the DWM would draw around this window.
    /// The value can vary depending on the DPI of the window. The pvAttribute parameter points to a value of type UINT.
    /// This value is supported starting with Windows 11 Build 22000.
    /// </summary>
    VisibleFrameBorderThickness,

    /// <summary>
    ///     The maximum recognized DWMWINDOWATTRIBUTE value, used for validation purposes.
    /// </summary>
    Last
}