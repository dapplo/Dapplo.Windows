// Copyright (c) Dapplo and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
using Dapplo.Windows.Common.Structs;

namespace Dapplo.Windows.Desktop;

/// <summary>
///     Scrolls a window or an area inside a window step by step, e.g. for a scrolling capture.
///     Implemented by <see cref="WindowScroller"/> (Win32 scroll bars, see <see cref="InteropWindowExtensions.GetWindowScroller"/>)
///     and by the UI Automation based scroller of the Dapplo.Windows.Automation package, so a caller can try one and fall back
///     to the other with the same code.
/// </summary>
public interface IScroller
{
    /// <summary>
    ///     True when the scrolled content is at the start (top, or left for horizontal scrolling)
    /// </summary>
    bool IsAtStart { get; }

    /// <summary>
    ///     True when the scrolled content is at the end (bottom, or right for horizontal scrolling)
    /// </summary>
    bool IsAtEnd { get; }

    /// <summary>
    ///     The part of a page that <see cref="Next"/> and <see cref="Previous"/> scroll, greater than 0 and at most 1.
    ///     The default 1.0 scrolls a full page; use e.g. 0.5 so consecutive frames overlap for stitching.
    /// </summary>
    double StepFraction { get; set; }

    /// <summary>
    ///     The visible, scrolling area in screen coordinates (physical pixels for a per-monitor DPI aware process),
    ///     e.g. to crop the frames of a scrolling capture
    /// </summary>
    NativeRect ViewportBounds { get; }

    /// <summary>
    ///     Scroll to the start
    /// </summary>
    /// <returns>bool if this worked</returns>
    bool Start();

    /// <summary>
    ///     Scroll to the end
    /// </summary>
    /// <returns>bool if this worked</returns>
    bool End();

    /// <summary>
    ///     Scroll forward (down or right) by <see cref="StepFraction"/> of a page
    /// </summary>
    /// <returns>bool if this worked</returns>
    bool Next();

    /// <summary>
    ///     Scroll back (up or left) by <see cref="StepFraction"/> of a page
    /// </summary>
    /// <returns>bool if this worked</returns>
    bool Previous();

    /// <summary>
    ///     Scroll back to the position the scroller was created at
    /// </summary>
    /// <returns>bool if this worked</returns>
    bool Reset();
}
