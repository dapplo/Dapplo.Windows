// Copyright (c) Dapplo and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Generic;

namespace Dapplo.Windows.Dialogs;

/// <summary>
/// Represents the outcome of showing a file or folder dialog.
/// </summary>
/// <remarks>
/// <para>
/// A result is either a <em>cancellation</em> (<see cref="WasCancelled"/> is <see langword="true"/>)
/// or a <em>successful selection</em> (<see cref="WasCancelled"/> is <see langword="false"/>).
/// </para>
/// <para>
/// If the dialog fails unexpectedly (e.g. COM error, platform not supported), the builder's
/// <c>ShowDialog</c> method throws — a <see cref="FileDialogResult"/> is never returned for failures.
/// This makes it easy to distinguish the three cases:
/// </para>
/// <code>
/// FileDialogResult result;
/// try
/// {
///     result = new FileOpenDialogBuilder().AddFilter("All files", "*.*").ShowDialog();
/// }
/// catch (System.Runtime.InteropServices.COMException ex)
/// {
///     // Unexpected COM failure — log or rethrow.
///     logger.LogError(ex, "Dialog failed: {Hr}", ex.ErrorCode);
///     return;
/// }
/// catch (System.PlatformNotSupportedException)
/// {
///     // Non-Windows OS — fall back to a text input.
///     return;
/// }
///
/// if (result.WasCancelled)
///     return; // User pressed Cancel or Escape — not an error.
///
/// string path = result.SelectedPath; // guaranteed non-null here
/// </code>
/// </remarks>
public sealed class FileDialogResult
{
    private FileDialogResult(bool wasCancelled, string selectedPath, IReadOnlyList<string> selectedPaths)
    {
        WasCancelled = wasCancelled;
        SelectedPath = selectedPath;
        SelectedPaths = selectedPaths;
    }

    /// <summary>
    /// Gets a value indicating whether the user dismissed the dialog without making a selection
    /// (by pressing Cancel, Escape, or the × button).
    /// </summary>
    /// <remarks>
    /// When <see langword="true"/>, both <see cref="SelectedPath"/> and <see cref="SelectedPaths"/>
    /// will be <see langword="null"/>. No exception is thrown for cancellations.
    /// </remarks>
    public bool WasCancelled { get; }

    /// <summary>
    /// Gets the selected file or folder path, or <see langword="null"/> if the dialog was cancelled.
    /// For multi-select results, returns the first selected path.
    /// </summary>
    public string SelectedPath { get; }

    /// <summary>
    /// Gets all selected paths when the dialog was configured for multiple selection
    /// (via <see cref="FileOpenDialogBuilder.AllowMultipleSelection"/>),
    /// or <see langword="null"/> if the dialog was cancelled or configured for single selection.
    /// </summary>
    public IReadOnlyList<string> SelectedPaths { get; }

    internal static FileDialogResult Cancelled() =>
        new FileDialogResult(wasCancelled: true, selectedPath: null, selectedPaths: null);

    internal static FileDialogResult FromPath(string path) =>
        new FileDialogResult(wasCancelled: false, selectedPath: path, selectedPaths: null);

    internal static FileDialogResult FromPaths(IReadOnlyList<string> paths) =>
        new FileDialogResult(wasCancelled: false,
            selectedPath: paths.Count > 0 ? paths[0] : null,
            selectedPaths: paths);
}
