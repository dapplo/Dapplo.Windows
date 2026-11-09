// Copyright (c) Dapplo and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
using System;
using System.Collections.Generic;
using System.Linq;
using Dapplo.Windows.Common.Structs;

namespace Dapplo.Windows.Automation;

/// <summary>
///     A line of the visible text of a window, see <see cref="UiAutomationText"/>. Immutable.
/// </summary>
public sealed class UiAutomationTextLine
{
    private static readonly IReadOnlyList<UiAutomationTextWord> NoWords = Array.Empty<UiAutomationTextWord>();

    /// <summary>
    ///     Create a line, e.g. for tests
    /// </summary>
    /// <param name="bounds">NativeRect in screen pixels</param>
    /// <param name="words">the words from left to right (for right-to-left text in the order of the text), null for none</param>
    public UiAutomationTextLine(NativeRect bounds, IReadOnlyList<UiAutomationTextWord> words)
    {
        Bounds = bounds;
        Words = words ?? NoWords;
    }

    /// <summary>
    ///     The bounds in screen pixels, the union of the words
    /// </summary>
    public NativeRect Bounds { get; }

    /// <summary>
    ///     The words, never null
    /// </summary>
    public IReadOnlyList<UiAutomationTextWord> Words { get; }

    /// <summary>
    ///     The text of the line: the words separated by a space
    /// </summary>
    public string Text => string.Join(" ", Words.Select(word => word.Text));

    /// <inheritdoc />
    /// <remarks>Without the text, so it can be logged</remarks>
    public override string ToString() => $"UiAutomationTextLine {{Bounds: {Bounds}; Words: {Words.Count}}}";
}
