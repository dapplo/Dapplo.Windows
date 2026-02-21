// Copyright (c) Dapplo and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using Dapplo.Windows.Dialogs.Interop;

namespace Dapplo.Windows.Dialogs;

/// <summary>
/// Fluent builder for showing a modern Windows save-file dialog.
/// </summary>
/// <example>
/// <code>
/// var result = new FileSaveDialogBuilder()
///     .WithTitle("Save Screenshot")
///     .WithSuggestedFileName("screenshot.png")
///     .WithDefaultExtension("png")
///     .WithInitialDirectory(Environment.GetFolderPath(Environment.SpecialFolder.MyPictures))
///     .AddFilter("PNG Image", "*.png")
///     .AddFilter("JPEG Image", "*.jpg")
///     .ShowDialog();
///
/// if (!result.WasCancelled)
///     File.Copy(tempFile, result.SelectedPath, overwrite: true);
/// </code>
/// </example>
public sealed class FileSaveDialogBuilder
{
    private string _title;
    private string _initialDirectory;
    private string _suggestedFileName;
    private string _defaultExtension;
    private readonly List<(string Name, string Pattern)> _filters = new();
    private readonly List<(string Path, bool AtTop)> _places = new();

    /// <summary>Sets the text shown in the dialog title bar.</summary>
    /// <param name="title">Title bar text. <see langword="null"/> uses the Windows default ("Save As").</param>
    public FileSaveDialogBuilder WithTitle(string title)
    {
        _title = title;
        return this;
    }

    /// <summary>
    /// Sets the folder displayed when the dialog first opens.
    /// Ignored when <paramref name="path"/> is <see langword="null"/> or the directory does not exist;
    /// Windows then reuses the last folder visited by the application.
    /// </summary>
    public FileSaveDialogBuilder WithInitialDirectory(string path)
    {
        _initialDirectory = path;
        return this;
    }

    /// <summary>
    /// Pre-fills the file-name edit box with the given name (e.g. <c>"screenshot.png"</c>).
    /// The user can freely change it.
    /// </summary>
    public FileSaveDialogBuilder WithSuggestedFileName(string fileName)
    {
        _suggestedFileName = fileName;
        return this;
    }

    /// <summary>Sets the extension appended when the user omits one (without the leading dot, e.g. <c>"png"</c>).</summary>
    public FileSaveDialogBuilder WithDefaultExtension(string extension)
    {
        _defaultExtension = extension;
        return this;
    }

    /// <summary>
    /// Adds a file-type filter entry to the type drop-down.
    /// </summary>
    /// <param name="name">Friendly name, e.g. <c>"PNG Image"</c>.</param>
    /// <param name="pattern">File pattern, e.g. <c>"*.png"</c>.</param>
    public FileSaveDialogBuilder AddFilter(string name, string pattern)
    {
        _filters.Add((name, pattern));
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
    public FileSaveDialogBuilder AddPlace(string path, bool atTop = false)
    {
        _places.Add((path, atTop));
        return this;
    }

    /// <summary>
    /// Shows the dialog and returns the result.
    /// </summary>
    /// <param name="ownerHandle">
    /// Handle of the owner window. Pass <see cref="IntPtr.Zero"/> (default) for a top-level dialog.
    /// </param>
    /// <returns>
    /// A <see cref="FileDialogResult"/> describing the outcome.
    /// Check <see cref="FileDialogResult.WasCancelled"/> to distinguish a cancellation from a selection.
    /// </returns>
    /// <exception cref="COMException">An unexpected COM error occurred.</exception>
    /// <exception cref="PlatformNotSupportedException">Called on a non-Windows platform.</exception>
    public FileDialogResult ShowDialog(IntPtr ownerHandle = default)
    {
        var dialog = ComDialogHelper.CreateDialog<IFileSaveDialog>(ComDialogHelper.ClsidFileSaveDialog);
        try
        {
            if (_title != null) dialog.SetTitle(_title);
            // OverwritePrompt is the default for save dialogs; set it explicitly.
            dialog.SetOptions(FileOpenOptions.OverwritePrompt);
            ComDialogHelper.ApplyFilters(dialog.SetFileTypes, dialog.SetFileTypeIndex, _filters);
            if (_suggestedFileName != null) dialog.SetFileName(_suggestedFileName);
            if (_defaultExtension != null) dialog.SetDefaultExtension(_defaultExtension);
            ComDialogHelper.ApplyInitialDirectory(dialog.SetFolder, _initialDirectory);
            ComDialogHelper.ApplyPlaces(dialog.AddPlace, _places);

            var hr = dialog.Show(ownerHandle);
            if (hr == ComDialogHelper.HResultCancelled) return FileDialogResult.Cancelled();
            if (hr != 0) Marshal.ThrowExceptionForHR(hr);

            dialog.GetResult(out var item);
            var path = ComDialogHelper.GetFileSysPath(item);
            Marshal.ReleaseComObject(item);
            return FileDialogResult.FromPath(path);
        }
        finally
        {
            Marshal.ReleaseComObject(dialog);
        }
    }
}
