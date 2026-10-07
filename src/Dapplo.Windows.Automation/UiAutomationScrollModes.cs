// Copyright (c) Dapplo and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
namespace Dapplo.Windows.Automation;

/// <summary>
///     How a <see cref="UiAutomationScroller"/> scrolls
/// </summary>
public enum UiAutomationScrollModes
{
    /// <summary>
    ///     Use the UI Automation ScrollPattern: SetScrollPercent, or Scroll with small / large increments when setting the percentage
    ///     is not supported. No input is generated, the cursor doesn't move and the window doesn't need to be in front.
    /// </summary>
    ScrollPattern,

    /// <summary>
    ///     Move the mouse wheel over the element, one notch at a time, until the position (read via the ScrollPattern) moved far enough.
    ///     For controls which report the ScrollPattern but don't move when it is used. The cursor moves to the wheel location.
    /// </summary>
    MouseWheel
}
