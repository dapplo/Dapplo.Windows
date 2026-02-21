// Copyright (c) Dapplo and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
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
    /// Creates a COM dialog coclass instance and casts it to <typeparamref name="T"/>.
    /// </summary>
    /// <exception cref="PlatformNotSupportedException">Called on a non-Windows platform.</exception>
    /// <exception cref="COMException">The COM object could not be instantiated.</exception>
    internal static T CreateDialog<T>(Guid clsid) where T : class
    {
        var type = Type.GetTypeFromCLSID(clsid)
            ?? throw new PlatformNotSupportedException(
                "The Windows Common Item Dialog is only available on Windows Vista or later.");

        return (T)(Activator.CreateInstance(type)
            ?? throw new COMException($"Activator.CreateInstance returned null for CLSID {clsid}."));
    }

    /// <summary>
    /// Converts filter tuples to <see cref="FilterSpec"/> structs and applies them to the dialog.
    /// Does nothing when <paramref name="filters"/> is empty.
    /// </summary>
    internal static void ApplyFilters(
        Action<uint, FilterSpec[]> setFileTypes,
        Action<uint> setFileTypeIndex,
        List<(string Name, string Pattern)> filters)
    {
        if (filters == null || filters.Count == 0) return;

        var specs = new FilterSpec[filters.Count];
        for (int i = 0; i < filters.Count; i++)
            specs[i] = new FilterSpec(filters[i].Name, filters[i].Pattern);

        setFileTypes((uint)specs.Length, specs);
        setFileTypeIndex(1); // 1-based; select the first filter by default
    }

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
            setFolder(shellItem);
            Marshal.ReleaseComObject(shellItem);
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
                addPlace(shellItem, atTop ? FileDialogAddPlaceFlags.Top : FileDialogAddPlaceFlags.Bottom);
                Marshal.ReleaseComObject(shellItem);
            }
            catch (COMException)
            {
                // Non-critical: skip bad paths.
            }
        }
    }

    /// <summary>Returns the file-system path string for the given <see cref="IShellItem"/>.</summary>
    internal static string GetFileSysPath(IShellItem item)
    {
        item.GetDisplayName(ShellItemDisplayName.FileSysPath, out var path);
        return path;
    }

    /// <summary>
    /// Collects file-system paths from all items in an <see cref="IShellItemArray"/>,
    /// releasing each item and the array itself before returning.
    /// </summary>
    internal static IReadOnlyList<string> CollectPaths(IShellItemArray items)
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
