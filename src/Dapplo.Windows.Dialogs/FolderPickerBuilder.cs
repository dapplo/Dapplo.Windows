// Copyright (c) Dapplo and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Runtime.InteropServices;
using Dapplo.Windows.Dialogs.Interop;

namespace Dapplo.Windows.Dialogs;

/// <summary>
/// Fluent builder for showing a modern Windows folder-picker dialog.
/// </summary>
/// <example>
/// <code>
/// var result = new FolderPickerBuilder()
///     .WithTitle("Select Output Folder")
///     .WithInitialDirectory(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments))
///     .ShowDialog();
///
/// if (!result.WasCancelled)
///     settings.OutputDirectory = result.SelectedPath;
/// </code>
/// </example>
public sealed class FolderPickerBuilder
{
    private string _title;
    private string _initialDirectory;

    /// <summary>Sets the text shown in the dialog title bar.</summary>
    /// <param name="title">Title bar text. <see langword="null"/> uses the Windows default.</param>
    public FolderPickerBuilder WithTitle(string title)
    {
        _title = title;
        return this;
    }

    /// <summary>
    /// Sets the folder displayed when the dialog first opens.
    /// Ignored when <paramref name="path"/> is <see langword="null"/> or the directory does not exist;
    /// Windows then reuses the last folder visited by the application.
    /// </summary>
    public FolderPickerBuilder WithInitialDirectory(string path)
    {
        _initialDirectory = path;
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
        var dialog = ComDialogHelper.CreateDialog<IFileOpenDialog>(ComDialogHelper.ClsidFileOpenDialog);
        try
        {
            if (_title != null) dialog.SetTitle(_title);
            // PickFolders suppresses the file-name edit box and makes the dialog navigate folders only.
            dialog.SetOptions(FileOpenOptions.PickFolders);
            ComDialogHelper.ApplyInitialDirectory(dialog.SetFolder, _initialDirectory);

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
