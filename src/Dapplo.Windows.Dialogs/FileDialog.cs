// Copyright (c) Dapplo and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;

namespace Dapplo.Windows.Dialogs;

/// <summary>
/// Provides one-line convenience wrappers around the Windows Common Item Dialog for the most common scenarios.
/// Returns <see langword="null"/> on cancellation; throws on unexpected errors.
/// </summary>
/// <remarks>
/// For more control (custom sidebar places, multi-select, suggested filenames, etc.)
/// use the builder classes directly: <see cref="FileOpenDialogBuilder"/>,
/// <see cref="FileSaveDialogBuilder"/>, or <see cref="FolderPickerBuilder"/>.
/// See <c>README.md</c> for full documentation and error-handling guidance.
/// </remarks>
public static class FileDialog
{
    /// <summary>
    /// Presents a modern open-file dialog and returns the selected file path,
    /// or <see langword="null"/> if the user cancelled.
    /// </summary>
    /// <param name="ownerHandle">Handle of the owner window (<see cref="IntPtr.Zero"/> for top-level).</param>
    /// <param name="title">Title bar text; <see langword="null"/> uses the Windows default ("Open").</param>
    /// <param name="initialDirectory">Folder shown on open; <see langword="null"/> reuses the last-visited folder.</param>
    /// <param name="filters">File-type filters, e.g. <c>new[] { ("Images", "*.png;*.jpg"), ("All files", "*.*") }</c>.</param>
    /// <param name="defaultExtension">Extension appended when the user omits one (no leading dot, e.g. <c>"txt"</c>).</param>
    /// <exception cref="System.Runtime.InteropServices.COMException">Unexpected COM failure.</exception>
    /// <exception cref="PlatformNotSupportedException">Called on a non-Windows platform.</exception>
    public static string PickFileToOpen(
        IntPtr ownerHandle = default,
        string title = null,
        string initialDirectory = null,
        (string Name, string Pattern)[] filters = null,
        string defaultExtension = null)
    {
        var builder = new FileOpenDialogBuilder();
        if (title != null) builder.WithTitle(title);
        if (initialDirectory != null) builder.WithInitialDirectory(initialDirectory);
        if (defaultExtension != null) builder.WithDefaultExtension(defaultExtension);
        if (filters != null) foreach (var f in filters) builder.AddFilter(f.Name, f.Pattern);

        var result = builder.ShowDialog(ownerHandle);
        return result.WasCancelled ? null : result.SelectedPath;
    }

    /// <summary>
    /// Presents a modern open-file dialog with multiple selection enabled and returns the selected paths,
    /// or <see langword="null"/> if the user cancelled.
    /// </summary>
    /// <param name="ownerHandle">Handle of the owner window (<see cref="IntPtr.Zero"/> for top-level).</param>
    /// <param name="title">Title bar text; <see langword="null"/> uses the Windows default ("Open").</param>
    /// <param name="initialDirectory">Folder shown on open; <see langword="null"/> reuses the last-visited folder.</param>
    /// <param name="filters">File-type filters, e.g. <c>new[] { ("Images", "*.png;*.jpg") }</c>.</param>
    /// <exception cref="System.Runtime.InteropServices.COMException">Unexpected COM failure.</exception>
    /// <exception cref="PlatformNotSupportedException">Called on a non-Windows platform.</exception>
    public static IReadOnlyList<string> PickFilesToOpen(
        IntPtr ownerHandle = default,
        string title = null,
        string initialDirectory = null,
        (string Name, string Pattern)[] filters = null)
    {
        var builder = new FileOpenDialogBuilder().AllowMultipleSelection();
        if (title != null) builder.WithTitle(title);
        if (initialDirectory != null) builder.WithInitialDirectory(initialDirectory);
        if (filters != null) foreach (var f in filters) builder.AddFilter(f.Name, f.Pattern);

        var result = builder.ShowDialog(ownerHandle);
        return result.WasCancelled ? null : result.SelectedPaths;
    }

    /// <summary>
    /// Presents a modern save-file dialog and returns the chosen path,
    /// or <see langword="null"/> if the user cancelled.
    /// </summary>
    /// <param name="ownerHandle">Handle of the owner window (<see cref="IntPtr.Zero"/> for top-level).</param>
    /// <param name="title">Title bar text; <see langword="null"/> uses the Windows default ("Save As").</param>
    /// <param name="initialDirectory">Folder shown on open; <see langword="null"/> reuses the last-visited folder.</param>
    /// <param name="suggestedFileName">Pre-filled file-name, e.g. <c>"screenshot.png"</c>.</param>
    /// <param name="filters">File-type filters, e.g. <c>new[] { ("PNG Image", "*.png") }</c>.</param>
    /// <param name="defaultExtension">Extension appended when the user omits one (no leading dot, e.g. <c>"png"</c>).</param>
    /// <exception cref="System.Runtime.InteropServices.COMException">Unexpected COM failure.</exception>
    /// <exception cref="PlatformNotSupportedException">Called on a non-Windows platform.</exception>
    public static string PickFileToSave(
        IntPtr ownerHandle = default,
        string title = null,
        string initialDirectory = null,
        string suggestedFileName = null,
        (string Name, string Pattern)[] filters = null,
        string defaultExtension = null)
    {
        var builder = new FileSaveDialogBuilder();
        if (title != null) builder.WithTitle(title);
        if (initialDirectory != null) builder.WithInitialDirectory(initialDirectory);
        if (suggestedFileName != null) builder.WithSuggestedFileName(suggestedFileName);
        if (defaultExtension != null) builder.WithDefaultExtension(defaultExtension);
        if (filters != null) foreach (var f in filters) builder.AddFilter(f.Name, f.Pattern);

        var result = builder.ShowDialog(ownerHandle);
        return result.WasCancelled ? null : result.SelectedPath;
    }

    /// <summary>
    /// Presents a modern folder-picker dialog and returns the selected folder path,
    /// or <see langword="null"/> if the user cancelled.
    /// </summary>
    /// <param name="ownerHandle">Handle of the owner window (<see cref="IntPtr.Zero"/> for top-level).</param>
    /// <param name="title">Title bar text; <see langword="null"/> uses the Windows default.</param>
    /// <param name="initialDirectory">Folder shown on open; <see langword="null"/> reuses the last-visited folder.</param>
    /// <exception cref="System.Runtime.InteropServices.COMException">Unexpected COM failure.</exception>
    /// <exception cref="PlatformNotSupportedException">Called on a non-Windows platform.</exception>
    public static string PickFolder(
        IntPtr ownerHandle = default,
        string title = null,
        string initialDirectory = null)
    {
        var builder = new FolderPickerBuilder();
        if (title != null) builder.WithTitle(title);
        if (initialDirectory != null) builder.WithInitialDirectory(initialDirectory);

        var result = builder.ShowDialog(ownerHandle);
        return result.WasCancelled ? null : result.SelectedPath;
    }
}
