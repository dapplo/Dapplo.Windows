// Copyright (c) Dapplo and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
namespace Dapplo.Windows.User32.Enums;

/// <summary>
///     Scroll-modes for the WindowScroller
/// </summary>
public enum ScrollModes
{
    /// <summary>
    ///     Send message to the window with an absolute position
    /// </summary>
    AbsoluteWindowMessage,

    /// <summary>
    ///     Send message to the window for page up or down
    /// </summary>
    WindowsMessage,

    /// <summary>
    ///     Send a mousewheel event
    /// </summary>
    MouseWheel,

    /// <summary>
    ///     Send page up or down as key press
    /// </summary>
    KeyboardPageUpDown
}