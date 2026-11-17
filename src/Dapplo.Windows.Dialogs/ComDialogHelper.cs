// Copyright (c) Dapplo and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading;
using Dapplo.Windows.Dialogs.Interop;

namespace Dapplo.Windows.Dialogs;

/// <summary>
/// Shared internal helper that handles all COM interactions for the file and folder dialog builders.
/// </summary>
internal static class ComDialogHelper
{
    internal static readonly Guid ClsidFileOpenDialog = new Guid("DC1C5A9C-E88A-4DDE-A5A1-60F82A20AEF7");
    internal static readonly Guid ClsidFileSaveDialog = new Guid("C0B4E2F3-BA21-4773-8DBA-335EC946EB8B");
    private static readonly Guid IidIShellItem = new Guid("43826D1E-E718-42EE-BC55-A1E261C37BFE");

    // HRESULT returned when the user dismisses the dialog via Cancel or Escape.
    // Equals HRESULT_FROM_WIN32(ERROR_CANCELLED) = 0x800704C7.
    internal const int HResultCancelled = unchecked((int)0x800704C7);

    /// <summary>
    /// Throws an <see cref="InvalidOperationException"/> when the current thread is not an STA thread.
    /// The Common Item Dialog is an apartment-threaded COM object with UI: on an MTA thread (thread pool, Task.Run, a console Main without [STAThread])
    /// COM would create it on a hidden STA host thread, where it can't have the caller's window as owner and can hang or fail.
    /// </summary>
    /// <param name="dialogName">Name of the dialog for the message</param>
    /// <exception cref="InvalidOperationException">When the current thread is not an STA thread</exception>
    internal static void EnsureStaThread(string dialogName)
    {
        var apartmentState = Thread.CurrentThread.GetApartmentState();
        if (apartmentState == ApartmentState.STA)
        {
            return;
        }
        throw new InvalidOperationException(
            $"{dialogName}.ShowDialog must be called from an STA thread, but the current thread (managed id {Environment.CurrentManagedThreadId}) is {apartmentState}. " +
            "Call it from the UI thread, mark Main with [STAThread], or start a dedicated Thread with SetApartmentState(ApartmentState.STA). " +
            "Thread pool threads (Task.Run, await continuations without a UI SynchronizationContext) are always MTA.");
    }

    /// <summary>
    /// Creates a COM dialog coclass instance and casts it to <typeparamref name="T"/>.
    /// </summary>
    /// <param name="clsid">The CLSID of the dialog</param>
    /// <param name="dialogName">Name of the dialog (builder) for the error message</param>
    /// <exception cref="InvalidOperationException">Called from a thread which is not an STA thread.</exception>
    /// <exception cref="PlatformNotSupportedException">Called on a non-Windows platform.</exception>
    /// <exception cref="COMException">The COM object could not be instantiated.</exception>
    internal static T CreateDialog<T>(Guid clsid, string dialogName) where T : class
    {
        // Check before any COM object is created
        EnsureStaThread(dialogName);
        var type = Type.GetTypeFromCLSID(clsid)
            ?? throw new PlatformNotSupportedException(
                "The Windows Common Item Dialog is only available on Windows Vista or later.");

        return (T)(Activator.CreateInstance(type)
            ?? throw new COMException($"Activator.CreateInstance returned null for CLSID {clsid}."));
    }

    /// <summary>
    /// Converts filter tuples to <see cref="FilterSpec"/> structs and applies them to the dialog,
    /// preselecting the filter with the 0-based <paramref name="selectedFilterIndex"/> (the first one when it is out of range).
    /// Does nothing when <paramref name="filters"/> is empty.
    /// </summary>
    internal static void ApplyFilters(
        Action<uint, FilterSpec[]> setFileTypes,
        Action<uint> setFileTypeIndex,
        List<(string Name, string Pattern)> filters,
        int selectedFilterIndex = 0)
    {
        if (filters == null || filters.Count == 0) return;

        var specs = new FilterSpec[filters.Count];
        for (int i = 0; i < filters.Count; i++)
            specs[i] = new FilterSpec(filters[i].Name, filters[i].Pattern);

        setFileTypes((uint)specs.Length, specs);
        setFileTypeIndex(ToComFileTypeIndex(selectedFilterIndex, specs.Length));
    }

    /// <summary>
    /// Converts the 0-based filter index of the builder API to the 1-based index of <c>IFileDialog::SetFileTypeIndex</c>.
    /// An index outside the filters selects the first filter (1).
    /// </summary>
    /// <param name="filterIndex">0-based index of the filter</param>
    /// <param name="filterCount">Number of filters</param>
    internal static uint ToComFileTypeIndex(int filterIndex, int filterCount) =>
        filterIndex >= 0 && filterIndex < filterCount ? (uint)filterIndex + 1 : 1u;

    /// <summary>
    /// Converts the 1-based index of <c>IFileDialog::GetFileTypeIndex</c> to the 0-based filter index of <see cref="FileDialogResult.SelectedFilterIndex"/>.
    /// Returns -1 when there are no filters or the index isn't one of them.
    /// </summary>
    /// <param name="comFileTypeIndex">1-based index from GetFileTypeIndex</param>
    /// <param name="filterCount">Number of filters</param>
    internal static int FromComFileTypeIndex(uint comFileTypeIndex, int filterCount) =>
        filterCount > 0 && comFileTypeIndex >= 1 && comFileTypeIndex <= (uint)filterCount ? (int)comFileTypeIndex - 1 : -1;

    /// <summary>
    /// Reads the file type the user chose, after <c>Show</c> returned S_OK and before the dialog is released.
    /// </summary>
    /// <param name="getFileTypeIndex">IFileDialog.GetFileTypeIndex of the dialog</param>
    /// <param name="filterCount">Number of filters which were set</param>
    /// <returns>0-based index of the filter, -1 when no filters were set or the dialog doesn't report one</returns>
    internal static int ReadSelectedFilterIndex(GetFileTypeIndexDelegate getFileTypeIndex, int filterCount)
    {
        if (filterCount == 0) return -1;
        try
        {
            getFileTypeIndex(out var comFileTypeIndex);
            return FromComFileTypeIndex(comFileTypeIndex, filterCount);
        }
        catch (COMException)
        {
            // Non-critical: the path is there, only the file type is unknown
            return -1;
        }
    }

    /// <summary>Signature of IFileDialog.GetFileTypeIndex.</summary>
    internal delegate void GetFileTypeIndexDelegate(out uint fileTypeIndex);

    /// <summary>
    /// Parses a Windows Forms / WPF style filter text, e.g. <c>"Images|*.png;*.jpg|All files|*.*"</c>, into name and pattern pairs.
    /// </summary>
    /// <param name="filter">Filter text: pairs of display name and pattern, all separated by <c>|</c>. <see langword="null"/> or empty gives no filters.</param>
    /// <exception cref="ArgumentException">The text doesn't consist of name and pattern pairs (an odd number of parts), like Windows Forms and WPF.</exception>
    internal static List<(string Name, string Pattern)> ParseFilter(string filter)
    {
        var result = new List<(string Name, string Pattern)>();
        if (string.IsNullOrEmpty(filter)) return result;

        var parts = filter.Split('|');
        if (parts.Length % 2 != 0)
        {
            throw new ArgumentException($"The filter \"{filter}\" must consist of pairs of a name and a pattern, separated by '|', e.g. \"Images|*.png;*.jpg|All files|*.*\".", nameof(filter));
        }
        for (int i = 0; i < parts.Length; i += 2)
        {
            result.Add((parts[i], parts[i + 1]));
        }
        return result;
    }

    /// <summary>
    /// The owner for <c>Show</c>: <paramref name="ownerHandle"/>, or when it is <see cref="IntPtr.Zero"/> the active window of the calling thread
    /// (like the Windows Forms and WPF dialogs), so the dialog stays in front of the window it was opened from.
    /// Stays <see cref="IntPtr.Zero"/> (a top-level dialog) when the thread has no active window.
    /// </summary>
    internal static IntPtr ResolveOwner(IntPtr ownerHandle) =>
        ownerHandle != IntPtr.Zero ? ownerHandle : GetActiveWindow();

    [DllImport("user32.dll", ExactSpelling = true)]
    private static extern IntPtr GetActiveWindow();

    /// <summary>
    /// Sets the initial folder on the dialog using <see cref="SHCreateItemFromParsingName"/>.
    /// Failures are silently ignored so a bad path never prevents the dialog from opening.
    /// </summary>
    internal static void ApplyInitialDirectory(Action<IShellItem> setFolder, string path)
    {
        if (string.IsNullOrEmpty(path) || !Directory.Exists(path)) return;

        try
        {
            var shellItem = ShellItemFromPath(path);
            try
            {
                setFolder(shellItem);
            }
            finally
            {
                Marshal.ReleaseComObject(shellItem);
            }
        }
        catch (COMException)
        {
            // Non-critical: the dialog will open at the default location instead.
        }
    }

    /// <summary>
    /// Adds custom places to the dialog's navigation sidebar.
    /// Failures for individual paths are silently ignored.
    /// </summary>
    internal static void ApplyPlaces(
        Action<IShellItem, FileDialogAddPlaceFlags> addPlace,
        List<(string Path, bool AtTop)> places)
    {
        if (places == null || places.Count == 0) return;

        foreach (var (path, atTop) in places)
        {
            if (string.IsNullOrEmpty(path) || !Directory.Exists(path)) continue;
            try
            {
                var shellItem = ShellItemFromPath(path);
                try
                {
                    addPlace(shellItem, atTop ? FileDialogAddPlaceFlags.Top : FileDialogAddPlaceFlags.Bottom);
                }
                finally
                {
                    Marshal.ReleaseComObject(shellItem);
                }
            }
            catch (COMException)
            {
                // Non-critical: skip bad paths.
            }
        }
    }

    /// <summary>
    /// Combines the dialog's current options (from <c>GetOptions</c>) with <paramref name="wanted"/>, keeping the dialog's defaults
    /// (e.g. PathMustExist, FileMustExist, NoReadOnlyReturn). Always adds <see cref="FileOpenOptions.ForceFileSystem"/>,
    /// so only file-system items can be picked, and <see cref="FileOpenOptions.NoChangeDir"/>,
    /// so the dialog never changes the process's current directory.
    /// </summary>
    internal static FileOpenOptions CombineOptions(FileOpenOptions current, FileOpenOptions wanted) =>
        current | wanted | FileOpenOptions.ForceFileSystem | FileOpenOptions.NoChangeDir;

    /// <summary>Returns the file-system path string for the given <see cref="IShellItem"/>.</summary>
    internal static string GetFileSysPath(IShellItem item)
    {
        item.GetDisplayName(ShellItemDisplayName.FileSysPath, out var path);
        return path;
    }

    /// <summary>
    /// Returns the file-system path string for the given <see cref="IShellItem"/> and releases the item,
    /// also when <c>GetDisplayName</c> throws.
    /// </summary>
    internal static string GetFileSysPathAndRelease(IShellItem item)
    {
        try
        {
            return GetFileSysPath(item);
        }
        finally
        {
            Marshal.ReleaseComObject(item);
        }
    }

    /// <summary>
    /// Collects file-system paths from all items in an <see cref="IShellItemArray"/>,
    /// releasing each item and the array itself before returning.
    /// </summary>
    internal static IReadOnlyList<string> CollectPaths(IShellItemArray items)
    {
        try
        {
            items.GetCount(out var count);
            var result = new List<string>((int)count);
            for (uint i = 0; i < count; i++)
            {
                items.GetItemAt(i, out var item);
                result.Add(GetFileSysPathAndRelease(item));
            }
            return result;
        }
        finally
        {
            Marshal.ReleaseComObject(items);
        }
    }

    /// <summary>Creates an <see cref="IShellItem"/> from a file-system path.</summary>
    internal static IShellItem ShellItemFromPath(string path)
    {
        var iid = IidIShellItem;
        SHCreateItemFromParsingName(path, IntPtr.Zero, ref iid, out var item);
        return item;
    }

    // PreserveSig = false → the runtime converts a failure HRESULT directly into a COMException.
    [DllImport("shell32.dll", CharSet = CharSet.Unicode, ExactSpelling = true, PreserveSig = false)]
    private static extern void SHCreateItemFromParsingName(
        [MarshalAs(UnmanagedType.LPWStr)] string pszPath,
        IntPtr pbc,
        ref Guid riid,
        [MarshalAs(UnmanagedType.Interface)] out IShellItem ppv);
}
