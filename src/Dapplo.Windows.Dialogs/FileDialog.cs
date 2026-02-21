// Copyright (c) Dapplo and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using Dapplo.Windows.Com;
using Dapplo.Windows.Com.Enums;
using Dapplo.Windows.Com.Structs;
using Dapplo.Windows.Common.Extensions;

namespace Dapplo.Windows.Dialogs;

/// <summary>
/// Provides simple, high-level wrappers around the Windows Common Item Dialog (Vista+)
/// for picking files and folders via COM/P-Invoke without WinForms or WPF dependencies.
/// </summary>
/// <remarks>
/// <para>
/// All methods return <see langword="null"/> when the user cancels the dialog — no exception is thrown.
/// Unexpected COM errors or platform incompatibilities are surfaced as exceptions.
/// </para>
/// <para><b>Use cases and examples:</b></para>
///
/// <para><b>1. Save a file (e.g. a screenshot) to a location chosen by the user:</b></para>
/// <code>
/// string path = FileDialog.PickFileToSave(
///     title: "Save Screenshot",
///     suggestedFileName: "screenshot.png",
///     initialDirectory: Environment.GetFolderPath(Environment.SpecialFolder.MyPictures),
///     filters: new[] { ("PNG Image", "*.png"), ("JPEG Image", "*.jpg") },
///     defaultExtension: "png");
///
/// if (path != null)
///     File.Copy(tempFile, path, overwrite: true);
/// </code>
///
/// <para><b>2. Open an existing file, filtered to the types the application understands:</b></para>
/// <code>
/// string path = FileDialog.PickFileToOpen(
///     title: "Open Document",
///     initialDirectory: Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
///     filters: new[] { ("Text files", "*.txt"), ("All files", "*.*") });
///
/// if (path != null)
///     ProcessDocument(File.ReadAllText(path));
/// </code>
///
/// <para><b>3. Let the user pick multiple files at once:</b></para>
/// <code>
/// IReadOnlyList&lt;string&gt; files = FileDialog.PickFilesToOpen(
///     filters: new[] { ("Images", "*.png;*.jpg;*.bmp") });
///
/// if (files != null)
///     foreach (string file in files)
///         ImportImage(file);
/// </code>
///
/// <para><b>4. Let the user configure a default output folder:</b></para>
/// <code>
/// string folder = FileDialog.PickFolder(
///     title: "Select Output Folder",
///     initialDirectory: Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments));
///
/// if (folder != null)
///     settings.OutputDirectory = folder;
/// </code>
///
/// <para><b>Error handling pattern:</b></para>
/// <code>
/// try
/// {
///     string path = FileDialog.PickFileToOpen(
///         filters: new[] { ("Images", "*.png;*.jpg") });
///
///     if (path == null)
///         return; // user cancelled — not an error
///
///     ProcessImage(path);
/// }
/// catch (PlatformNotSupportedException)
/// {
///     // Running on a non-Windows OS; fall back to manual path input.
/// }
/// catch (COMException ex)
/// {
///     // Unexpected dialog failure.
///     logger.LogError(ex, "File dialog failed with HRESULT {Hr}", ex.ErrorCode);
/// }
/// </code>
/// </remarks>
public static class FileDialog
{
    // CLSIDs for the Windows Common Item Dialog COM coclasses.
    // These are stable, documented Windows identifiers kept private so callers never need them.
    private static readonly Guid ClsidFileOpenDialog = new Guid("DC1C5A9C-E88A-4DDE-A5A1-60F82A20AEF7");
    private static readonly Guid ClsidFileSaveDialog = new Guid("C0B4E2F3-BA21-4773-8DBA-335EC946EB8B");

    // IID for IShellItem, used when calling SHCreateItemFromParsingName.
    private static readonly Guid IidIShellItem = new Guid("43826D1E-E718-42EE-BC55-A1E261C37BFE");

    // HRESULT returned when the user dismisses the dialog via Cancel or Escape.
    // Equals HRESULT_FROM_WIN32(ERROR_CANCELLED) = 0x800704C7.
    private const int HResultCancelled = unchecked((int)0x800704C7);

    /// <summary>
    /// Presents a modern open-file dialog to the user and returns the selected file path.
    /// </summary>
    /// <param name="ownerHandle">
    /// Handle of the owner window. Pass <see cref="IntPtr.Zero"/> (default) for a top-level dialog.
    /// </param>
    /// <param name="title">
    /// Text shown in the dialog title bar.
    /// <see langword="null"/> lets Windows use its built-in default ("Open").
    /// </param>
    /// <param name="initialDirectory">
    /// The folder displayed when the dialog first opens.
    /// <see langword="null"/> or a non-existent path causes Windows to remember and reuse the last
    /// folder visited by the application (automatic per-application MRU).
    /// </param>
    /// <param name="filters">
    /// File-type filter entries shown in the type drop-down.
    /// Each entry is a <c>(friendly name, pattern)</c> tuple, e.g.
    /// <c>new[] { ("Images", "*.png;*.jpg"), ("All files", "*.*") }</c>.
    /// <see langword="null"/> displays all files.
    /// </param>
    /// <param name="defaultExtension">
    /// Extension appended when the user omits one; do not include the leading dot (e.g. <c>"txt"</c>).
    /// </param>
    /// <returns>
    /// The full path of the chosen file, or <see langword="null"/> when the user cancelled.
    /// </returns>
    /// <exception cref="COMException">
    /// The dialog could not be created or returned an unexpected HRESULT.
    /// </exception>
    /// <exception cref="PlatformNotSupportedException">
    /// Called on a non-Windows platform where the COM dialog is unavailable.
    /// </exception>
    public static string PickFileToOpen(
        IntPtr ownerHandle = default,
        string title = null,
        string initialDirectory = null,
        (string Name, string Pattern)[] filters = null,
        string defaultExtension = null)
    {
        var dialog = CreateDialog<IFileOpenDialog>(ClsidFileOpenDialog);
        try
        {
            if (title != null) dialog.SetTitle(title);
            dialog.SetOptions(FileOpenOptions.FileMustExist);
            ApplyFilters(dialog.SetFileTypes, dialog.SetFileTypeIndex, filters);
            if (defaultExtension != null) dialog.SetDefaultExtension(defaultExtension);
            ApplyInitialDirectory(dialog.SetFolder, initialDirectory);

            var hr = dialog.Show(ownerHandle);
            if ((int)hr == HResultCancelled) return null;
            hr.ThrowOnFailure();

            dialog.GetResult(out var item);
            return GetFileSysPath(item);
        }
        finally
        {
            Marshal.ReleaseComObject(dialog);
        }
    }

    /// <summary>
    /// Presents a modern open-file dialog that allows the user to select multiple files at once.
    /// </summary>
    /// <param name="ownerHandle">
    /// Handle of the owner window. Pass <see cref="IntPtr.Zero"/> (default) for a top-level dialog.
    /// </param>
    /// <param name="title">
    /// Text shown in the dialog title bar.
    /// <see langword="null"/> uses Windows built-in default ("Open").
    /// </param>
    /// <param name="initialDirectory">
    /// The folder displayed when the dialog first opens, or <see langword="null"/> to reuse
    /// the last folder visited by the application.
    /// </param>
    /// <param name="filters">
    /// File-type filters shown in the type drop-down, e.g.
    /// <c>new[] { ("Images", "*.png;*.jpg"), ("All files", "*.*") }</c>.
    /// <see langword="null"/> displays all files.
    /// </param>
    /// <returns>
    /// A read-only list of full file paths chosen by the user,
    /// or <see langword="null"/> when the user cancelled.
    /// </returns>
    /// <exception cref="COMException">The dialog encountered an unexpected error.</exception>
    /// <exception cref="PlatformNotSupportedException">Called on a non-Windows platform.</exception>
    public static IReadOnlyList<string> PickFilesToOpen(
        IntPtr ownerHandle = default,
        string title = null,
        string initialDirectory = null,
        (string Name, string Pattern)[] filters = null)
    {
        var dialog = CreateDialog<IFileOpenDialog>(ClsidFileOpenDialog);
        try
        {
            if (title != null) dialog.SetTitle(title);
            dialog.SetOptions(FileOpenOptions.FileMustExist | FileOpenOptions.AllowMultiSelect);
            ApplyFilters(dialog.SetFileTypes, dialog.SetFileTypeIndex, filters);
            ApplyInitialDirectory(dialog.SetFolder, initialDirectory);

            var hr = dialog.Show(ownerHandle);
            if ((int)hr == HResultCancelled) return null;
            hr.ThrowOnFailure();

            dialog.GetResults(out var items);
            return CollectPaths(items);
        }
        finally
        {
            Marshal.ReleaseComObject(dialog);
        }
    }

    /// <summary>
    /// Presents a modern save-file dialog to the user and returns the chosen save path.
    /// </summary>
    /// <param name="ownerHandle">
    /// Handle of the owner window. Pass <see cref="IntPtr.Zero"/> (default) for a top-level dialog.
    /// </param>
    /// <param name="title">
    /// Text shown in the dialog title bar.
    /// <see langword="null"/> uses Windows built-in default ("Save As").
    /// </param>
    /// <param name="initialDirectory">
    /// The folder displayed when the dialog first opens, or <see langword="null"/> to reuse
    /// the last folder visited by the application.
    /// </param>
    /// <param name="suggestedFileName">
    /// Pre-filled text in the file-name edit box, e.g. <c>"report.pdf"</c> or <c>"screenshot"</c>.
    /// The user can freely change it.
    /// </param>
    /// <param name="filters">
    /// File-type filters shown in the type drop-down, e.g.
    /// <c>new[] { ("PDF files", "*.pdf"), ("All files", "*.*") }</c>.
    /// <see langword="null"/> displays all files.
    /// </param>
    /// <param name="defaultExtension">
    /// Extension appended when the user omits one; do not include the leading dot (e.g. <c>"pdf"</c>).
    /// </param>
    /// <returns>
    /// The full save path chosen by the user, or <see langword="null"/> when the user cancelled.
    /// </returns>
    /// <exception cref="COMException">The dialog encountered an unexpected error.</exception>
    /// <exception cref="PlatformNotSupportedException">Called on a non-Windows platform.</exception>
    public static string PickFileToSave(
        IntPtr ownerHandle = default,
        string title = null,
        string initialDirectory = null,
        string suggestedFileName = null,
        (string Name, string Pattern)[] filters = null,
        string defaultExtension = null)
    {
        var dialog = CreateDialog<IFileSaveDialog>(ClsidFileSaveDialog);
        try
        {
            if (title != null) dialog.SetTitle(title);
            // OverwritePrompt is the default for save dialogs; keep it explicit.
            dialog.SetOptions(FileOpenOptions.OverwritePrompt);
            ApplyFilters(dialog.SetFileTypes, dialog.SetFileTypeIndex, filters);
            if (suggestedFileName != null) dialog.SetFileName(suggestedFileName);
            if (defaultExtension != null) dialog.SetDefaultExtension(defaultExtension);
            ApplyInitialDirectory(dialog.SetFolder, initialDirectory);

            var hr = dialog.Show(ownerHandle);
            if ((int)hr == HResultCancelled) return null;
            hr.ThrowOnFailure();

            dialog.GetResult(out var item);
            return GetFileSysPath(item);
        }
        finally
        {
            Marshal.ReleaseComObject(dialog);
        }
    }

    /// <summary>
    /// Presents a modern folder-picker dialog to the user and returns the selected folder path.
    /// </summary>
    /// <param name="ownerHandle">
    /// Handle of the owner window. Pass <see cref="IntPtr.Zero"/> (default) for a top-level dialog.
    /// </param>
    /// <param name="title">
    /// Text shown in the dialog title bar.
    /// <see langword="null"/> uses the Windows built-in default.
    /// </param>
    /// <param name="initialDirectory">
    /// The folder displayed when the dialog first opens, or <see langword="null"/> to reuse
    /// the last folder visited by the application.
    /// </param>
    /// <returns>
    /// The full path of the selected folder, or <see langword="null"/> when the user cancelled.
    /// </returns>
    /// <exception cref="COMException">The dialog encountered an unexpected error.</exception>
    /// <exception cref="PlatformNotSupportedException">Called on a non-Windows platform.</exception>
    public static string PickFolder(
        IntPtr ownerHandle = default,
        string title = null,
        string initialDirectory = null)
    {
        var dialog = CreateDialog<IFileOpenDialog>(ClsidFileOpenDialog);
        try
        {
            if (title != null) dialog.SetTitle(title);
            // PickFolders suppresses the file-name edit box and makes the dialog navigate folders only.
            dialog.SetOptions(FileOpenOptions.PickFolders);
            ApplyInitialDirectory(dialog.SetFolder, initialDirectory);

            var hr = dialog.Show(ownerHandle);
            if ((int)hr == HResultCancelled) return null;
            hr.ThrowOnFailure();

            dialog.GetResult(out var item);
            return GetFileSysPath(item);
        }
        finally
        {
            Marshal.ReleaseComObject(dialog);
        }
    }

    // ─── Private helpers ──────────────────────────────────────────────────────────

    /// <summary>
    /// Creates and returns a COM dialog instance of the requested type.
    /// </summary>
    /// <exception cref="PlatformNotSupportedException">The COM class is not available (non-Windows).</exception>
    /// <exception cref="COMException">The COM object could not be instantiated.</exception>
    private static T CreateDialog<T>(Guid clsid) where T : class
    {
        var type = Type.GetTypeFromCLSID(clsid)
            ?? throw new PlatformNotSupportedException(
                "The Windows Common Item Dialog is only available on Windows.");

        return (T)(Activator.CreateInstance(type)
            ?? throw new COMException($"Activator.CreateInstance returned null for CLSID {clsid}."));
    }

    /// <summary>
    /// Converts a <c>(Name, Pattern)</c> filter array to <see cref="FilterSpec"/> and applies
    /// it to the dialog via the supplied delegate pair. Does nothing when <paramref name="filters"/>
    /// is <see langword="null"/> or empty.
    /// </summary>
    private static void ApplyFilters(
        Action<uint, FilterSpec[]> setFileTypes,
        Action<uint> setFileTypeIndex,
        (string Name, string Pattern)[] filters)
    {
        if (filters == null || filters.Length == 0) return;

        var specs = new FilterSpec[filters.Length];
        for (int i = 0; i < filters.Length; i++)
            specs[i] = new FilterSpec(filters[i].Name, filters[i].Pattern);

        setFileTypes((uint)specs.Length, specs);
        setFileTypeIndex(1); // select the first filter by default (1-based index)
    }

    /// <summary>
    /// Sets the initial folder on the dialog using <see cref="SHCreateItemFromParsingName"/>.
    /// Failures are silently ignored so a bad path never prevents the dialog from opening.
    /// </summary>
    private static void ApplyInitialDirectory(Action<IShellItem> setFolder, string path)
    {
        if (string.IsNullOrEmpty(path) || !Directory.Exists(path)) return;

        try
        {
            var shellItem = ShellItemFromPath(path);
            setFolder(shellItem);
            Marshal.ReleaseComObject(shellItem);
        }
        catch (COMException)
        {
            // Non-critical: opening the dialog at the default location is acceptable.
        }
    }

    /// <summary>
    /// Returns the file-system path string for the given <see cref="IShellItem"/>.
    /// </summary>
    private static string GetFileSysPath(IShellItem item)
    {
        item.GetDisplayName(ShellItemDisplayName.FileSysPath, out var path);
        return path;
    }

    /// <summary>
    /// Iterates over all items in an <see cref="IShellItemArray"/> and returns their
    /// file-system paths as a list. Releases each item and the array itself when done.
    /// </summary>
    private static IReadOnlyList<string> CollectPaths(IShellItemArray items)
    {
        items.GetCount(out var count);
        var result = new List<string>((int)count);
        for (uint i = 0; i < count; i++)
        {
            items.GetItemAt(i, out var item);
            result.Add(GetFileSysPath(item));
            Marshal.ReleaseComObject(item);
        }
        Marshal.ReleaseComObject(items);
        return result;
    }

    /// <summary>
    /// Creates an <see cref="IShellItem"/> from a file-system path by calling
    /// <c>SHCreateItemFromParsingName</c> (shell32.dll).
    /// </summary>
    private static IShellItem ShellItemFromPath(string path)
    {
        var iid = IidIShellItem;
        SHCreateItemFromParsingName(path, IntPtr.Zero, ref iid, out var item);
        return item;
    }

    // PreserveSig = false → runtime converts a failure HRESULT directly into a COMException.
    [DllImport("shell32.dll", CharSet = CharSet.Unicode, ExactSpelling = true, PreserveSig = false)]
    private static extern void SHCreateItemFromParsingName(
        [MarshalAs(UnmanagedType.LPWStr)] string pszPath,
        IntPtr pbc,
        ref Guid riid,
        [MarshalAs(UnmanagedType.Interface)] out IShellItem ppv);
}
