// Copyright (c) Dapplo and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Runtime.InteropServices;

namespace Dapplo.Windows.Dialogs.Interop;

/// <summary>
/// Extends <see cref="IFileDialog"/> with methods specific to the save dialog.
/// See <a href="https://docs.microsoft.com/en-us/windows/win32/api/shobjidl_core/nn-shobjidl_core-ifilesavedialog">IFileSaveDialog interface</a>
/// </summary>
/// <remarks>Instantiate via CLSID <c>{C0B4E2F3-BA21-4773-8DBA-335EC946EB8B}</c>.</remarks>
[ComImport]
[InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
[Guid("84BCCD23-5FDE-4CDB-AEA4-AF64B83D78AB")]
internal interface IFileSaveDialog
{
    [PreserveSig]
    int Show([In] IntPtr parent);

    void SetFileTypes(uint cFileTypes, [In] [MarshalAs(UnmanagedType.LPArray)] FilterSpec[] rgFilterSpec);
    void SetFileTypeIndex(uint iFileType);
    void GetFileTypeIndex(out uint piFileType);
    void Advise([In] nint pfde, out uint pdwCookie);
    void Unadvise(uint dwCookie);
    void SetOptions(FileOpenOptions fos);
    void GetOptions(out FileOpenOptions pfos);
    void SetDefaultFolder([In] IShellItem psi);
    void SetFolder([In] IShellItem psi);
    void GetFolder(out IShellItem ppsi);
    void GetCurrentSelection(out IShellItem ppsi);
    void SetFileName([In] [MarshalAs(UnmanagedType.LPWStr)] string pszName);
    void GetFileName([MarshalAs(UnmanagedType.LPWStr)] out string pszName);
    void SetTitle([In] [MarshalAs(UnmanagedType.LPWStr)] string pszTitle);
    void SetOkButtonLabel([In] [MarshalAs(UnmanagedType.LPWStr)] string pszText);
    void SetFileNameLabel([In] [MarshalAs(UnmanagedType.LPWStr)] string pszLabel);
    void GetResult(out IShellItem ppsi);
    void AddPlace([In] IShellItem psi, FileDialogAddPlaceFlags fdap);
    void SetDefaultExtension([In] [MarshalAs(UnmanagedType.LPWStr)] string pszDefaultExtension);
    void Close([MarshalAs(UnmanagedType.Error)] int hr);
    void SetClientGuid([In] ref Guid guid);
    void ClearClientData();
    void SetFilter([In] nint pFilter);
    void SetSaveAsItem([In] IShellItem psi);
    void SetProperties([In] nint pStore);
    void SetCollectedProperties([In] nint pList, [In] bool fAppendDefault);
    void GetProperties(out nint ppStore);
    void ApplyProperties([In] IShellItem psi, [In] nint pStore, [In] IntPtr hwnd, [In] nint pSink);
}
