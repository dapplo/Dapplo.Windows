// Copyright (c) Dapplo and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
using System;
using System.Collections.Generic;
using System.Linq;
using Dapplo.Windows.Desktop;
using Dapplo.Windows.Enums;

namespace Dapplo.Windows.Automation;

/// <summary>
///     The visible text of a window at one moment, from <see cref="UiAutomationText.ReadVisibleTextAsync(System.IntPtr, Dapplo.Windows.Common.Structs.NativeRect, System.TimeSpan?, System.TimeSpan?, System.Threading.CancellationToken)"/>. Immutable.
/// </summary>
public sealed class UiAutomationTextSnapshot
{
    private static readonly IReadOnlyList<UiAutomationTextLine> NoLines = Array.Empty<UiAutomationTextLine>();

    /// <summary>
    ///     Create a snapshot, e.g. for tests
    /// </summary>
    /// <param name="lines">the lines from top to bottom, null for none</param>
    /// <param name="isComplete">false when not all text could be read</param>
    public UiAutomationTextSnapshot(IReadOnlyList<UiAutomationTextLine> lines, bool isComplete) : this(lines, isComplete, RemoteDisplayKinds.None)
    {
    }

    /// <summary>
    ///     Create a snapshot, e.g. for tests
    /// </summary>
    /// <param name="lines">the lines from top to bottom, null for none</param>
    /// <param name="isComplete">false when not all text could be read</param>
    /// <param name="remoteDisplay">RemoteDisplayKinds of the window, None when it shows its own content</param>
    public UiAutomationTextSnapshot(IReadOnlyList<UiAutomationTextLine> lines, bool isComplete, RemoteDisplayKinds remoteDisplay)
    {
        Lines = lines ?? NoLines;
        IsComplete = isComplete;
        RemoteDisplay = remoteDisplay;
    }

    /// <summary>
    ///     The lines from top to bottom (lines with the same top from left to right), never null
    /// </summary>
    public IReadOnlyList<UiAutomationTextLine> Lines { get; }

    /// <summary>
    ///     False when the snapshot may miss text: UI Automation isn't available or the window is gone, the tree of the window still looked
    ///     incomplete when the content wait was over (Chromium builds it on the first request), a request timed out, or a limit was reached
    /// </summary>
    public bool IsComplete { get; }

    /// <summary>
    ///     The remote display client which draws the window (<see cref="RemoteDisplayWindow.GetKind(System.IntPtr)"/>), None when the window
    ///     shows its own content. A remote display window (Citrix, Remote Desktop, Horizon, ...) is a picture for Windows, UI Automation has no
    ///     text for its content: the snapshot is then never complete and the text has to come from OCR.
    /// </summary>
    public RemoteDisplayKinds RemoteDisplay { get; }

    /// <summary>
    ///     All words of all lines, top to bottom
    /// </summary>
    public IEnumerable<UiAutomationTextWord> Words => Lines.SelectMany(line => line.Words);

    /// <summary>
    ///     The text: the lines separated by a line break
    /// </summary>
    public string Text => string.Join(Environment.NewLine, Lines.Select(line => line.Text));

    /// <inheritdoc />
    /// <remarks>Without the text, so it can be logged</remarks>
    public override string ToString() => $"UiAutomationTextSnapshot {{Lines: {Lines.Count}; IsComplete: {IsComplete}; RemoteDisplay: {RemoteDisplay}}}";
}
