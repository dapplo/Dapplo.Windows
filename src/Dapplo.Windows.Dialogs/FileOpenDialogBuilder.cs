// Copyright (c) Dapplo and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using Dapplo.Windows.Dialogs.Interop;

namespace Dapplo.Windows.Dialogs;

/// <summary>
/// Fluent builder for showing a modern Windows open-file dialog.
/// </summary>
/// <example>
/// Single file:
/// <code>
/// var result = new FileOpenDialogBuilder()
///     .WithTitle("Open Document")
///     .WithInitialDirectory(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments))
///     .AddFilter("Text files", "*.txt")
///     .AddFilter("All files", "*.*")
///     .ShowDialog();
///
/// if (!result.WasCancelled)
///     ProcessDocument(result.SelectedPath);
/// </code>
/// Multiple files:
/// <code>
/// var result = new FileOpenDialogBuilder()
///     .AddFilter("Images", "*.png;*.jpg;*.bmp")
///     .AllowMultipleSelection()
///     .ShowDialog();
///
/// if (!result.WasCancelled)
///     foreach (string path in result.SelectedPaths)
///         ImportImage(path);
/// </code>
/// </example>
public sealed class FileOpenDialogBuilder
{
    private string _title;
    private string _initialDirectory;
    private string _defaultExtension;
    private bool _allowMultiSelect;
    private int _selectedFilterIndex;
    private readonly List<(string Name, string Pattern)> _filters = new();
    private readonly List<(string Path, bool AtTop)> _places = new();

    /// <summary>Sets the text shown in the dialog title bar.</summary>
    /// <param name="title">Title bar text. <see langword="null"/> uses the Windows default ("Open").</param>
    public FileOpenDialogBuilder WithTitle(string title)
    {
        _title = title;
        return this;
    }

    /// <summary>
    /// Sets the folder displayed when the dialog first opens.
    /// Ignored when <paramref name="path"/> is <see langword="null"/> or the directory does not exist;
    /// Windows then reuses the last folder visited by the application.
    /// </summary>
    public FileOpenDialogBuilder WithInitialDirectory(string path)
    {
        _initialDirectory = path;
        return this;
    }

    /// <summary>
    /// Adds a file-type filter entry to the type drop-down.
    /// </summary>
    /// <param name="name">Friendly name, e.g. <c>"Image files"</c>.</param>
    /// <param name="pattern">File pattern, e.g. <c>"*.png;*.jpg"</c>.</param>
    public FileOpenDialogBuilder AddFilter(string name, string pattern)
    {
        _filters.Add((name, pattern));
        return this;
    }

    /// <summary>Sets the extension appended when the user omits one (without the leading dot, e.g. <c>"txt"</c>).</summary>
    public FileOpenDialogBuilder WithDefaultExtension(string extension)
    {
        _defaultExtension = extension;
        return this;
    }

    /// <summary>Enables multiple file selection. Use <see cref="FileDialogResult.SelectedPaths"/> for the result.</summary>
    public FileOpenDialogBuilder AllowMultipleSelection()
    {
        _allowMultiSelect = true;
        return this;
    }

    /// <summary>
    /// Adds the file-type filters of a Windows Forms / WPF style filter text, e.g. <c>"Images|*.png;*.jpg|All files|*.*"</c>:
    /// pairs of display name and pattern, all separated by <c>|</c>. Several patterns of one filter are separated by <c>;</c>.
    /// <see langword="null"/> or an empty text adds nothing.
    /// </summary>
    /// <param name="filter">The filter text, as for <c>System.Windows.Forms.FileDialog.Filter</c> or <c>Microsoft.Win32.FileDialog.Filter</c>.</param>
    /// <exception cref="ArgumentException">The text isn't made of name and pattern pairs (an odd number of parts).</exception>
    public FileOpenDialogBuilder AddFilters(string filter)
    {
        _filters.AddRange(ComDialogHelper.ParseFilter(filter));
        return this;
    }

    /// <summary>
    /// Preselects a file type in the type drop-down: the 0-based index of the filters added with <c>AddFilter</c> / <c>AddFilters</c>,
    /// in the order they were added. When the index is out of range when the dialog is shown, the first filter is selected.
    /// Without this the first filter is selected.
    /// </summary>
    /// <param name="index">0-based index of the filter</param>
    public FileOpenDialogBuilder WithSelectedFilter(int index)
    {
        _selectedFilterIndex = index;
        return this;
    }

    /// <summary>
    /// Adds a custom location to the dialog's navigation sidebar.
    /// Ignored when <paramref name="path"/> does not exist.
    /// </summary>
    /// <param name="path">Full path to the directory to add.</param>
    /// <param name="atTop">
    /// <see langword="true"/> to add the place at the top of the sidebar;
    /// <see langword="false"/> (default) to add it at the bottom.
    /// </param>
    public FileOpenDialogBuilder AddPlace(string path, bool atTop = false)
    {
        _places.Add((path, atTop));
        return this;
    }

    /// <summary>
    /// Shows the dialog and returns the result.
    /// </summary>
    /// <param name="ownerHandle">
    /// Handle of the owner window. With <see cref="IntPtr.Zero"/> (default) the active window of the calling thread is the owner,
    /// like for the Windows Forms and WPF dialogs, so the dialog stays in front of the window it was opened from;
    /// only when the thread has no active window the dialog is top-level.
    /// </param>
    /// <returns>
    /// A <see cref="FileDialogResult"/> describing the outcome.
    /// Check <see cref="FileDialogResult.WasCancelled"/> to distinguish a cancellation from a selection.
    /// </returns>
    /// <exception cref="COMException">An unexpected COM error occurred.</exception>
    /// <exception cref="PlatformNotSupportedException">Called on a non-Windows platform.</exception>
    /// <exception cref="InvalidOperationException">Called from a thread which is not an STA thread (e.g. a thread pool thread), checked before any COM object is created.</exception>
    public FileDialogResult ShowDialog(IntPtr ownerHandle = default)
    {
        var dialog = ComDialogHelper.CreateDialog<IFileOpenDialog>(ComDialogHelper.ClsidFileOpenDialog, nameof(FileOpenDialogBuilder));
        try
        {
            if (_title != null) dialog.SetTitle(_title);

            var options = FileOpenOptions.FileMustExist;
            if (_allowMultiSelect) options |= FileOpenOptions.AllowMultiSelect;
            dialog.GetOptions(out var currentOptions);
            dialog.SetOptions(ComDialogHelper.CombineOptions(currentOptions, options));

            ComDialogHelper.ApplyFilters(dialog.SetFileTypes, dialog.SetFileTypeIndex, _filters, _selectedFilterIndex);
            if (_defaultExtension != null) dialog.SetDefaultExtension(_defaultExtension);
            ComDialogHelper.ApplyInitialDirectory(dialog.SetFolder, _initialDirectory);
            ComDialogHelper.ApplyPlaces(dialog.AddPlace, _places);

            var hr = dialog.Show(ComDialogHelper.ResolveOwner(ownerHandle));
            if (hr == ComDialogHelper.HResultCancelled) return FileDialogResult.Cancelled();
            if (hr != 0) Marshal.ThrowExceptionForHR(hr);

            // Read before the dialog is released
            var selectedFilterIndex = ComDialogHelper.ReadSelectedFilterIndex(dialog.GetFileTypeIndex, _filters.Count);

            if (_allowMultiSelect)
            {
                dialog.GetResults(out var items);
                return FileDialogResult.FromPaths(ComDialogHelper.CollectPaths(items), selectedFilterIndex);
            }

            dialog.GetResult(out var item);
            return FileDialogResult.FromPath(ComDialogHelper.GetFileSysPathAndRelease(item), selectedFilterIndex);
        }
        finally
        {
            Marshal.ReleaseComObject(dialog);
        }
    }
}
