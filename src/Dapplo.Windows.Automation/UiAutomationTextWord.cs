// Copyright (c) Dapplo and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
using Dapplo.Windows.Common.Structs;

namespace Dapplo.Windows.Automation;

/// <summary>
///     A word of the visible text of a window, see <see cref="UiAutomationText"/>. Immutable.
/// </summary>
public sealed class UiAutomationTextWord
{
    /// <summary>
    ///     Create a word, e.g. for tests
    /// </summary>
    /// <param name="bounds">NativeRect in screen pixels</param>
    /// <param name="text">string with the text, null is stored as empty</param>
    /// <param name="linkTarget">string with the target of the link the word belongs to, null (or empty) when it isn't part of a link</param>
    public UiAutomationTextWord(NativeRect bounds, string text, string linkTarget = null)
    {
        Bounds = bounds;
        Text = text ?? string.Empty;
        LinkTarget = string.IsNullOrEmpty(linkTarget) ? null : linkTarget;
    }

    /// <summary>
    ///     The bounds in screen pixels (physical pixels for a per-monitor DPI aware process), clipped to the element, the window and the area
    ///     which was read. A word which wraps over two lines has the union of its parts.
    /// </summary>
    public NativeRect Bounds { get; }

    /// <summary>
    ///     The text, without leading or trailing white space. For an element without TextPattern this is the whole name of the element
    ///     (several words), with the element's bounds.
    /// </summary>
    public string Text { get; }

    /// <summary>
    ///     The target of the link (the value of the hyperlink's ValuePattern, e.g. the URL in a browser), null when the word isn't part
    ///     of a link or the link has no value
    /// </summary>
    public string LinkTarget { get; }

    /// <summary>
    ///     True when the word is part of a link which has a target
    /// </summary>
    public bool IsLink => LinkTarget != null;

    /// <inheritdoc />
    /// <remarks>Without the text or the link target, so it can be logged</remarks>
    public override string ToString() => $"UiAutomationTextWord {{Bounds: {Bounds}; Length: {Text.Length}; IsLink: {IsLink}}}";
}
