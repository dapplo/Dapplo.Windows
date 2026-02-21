// Copyright (c) Dapplo and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Runtime.InteropServices;
using Dapplo.Windows.Com.Enums;
using Dapplo.Windows.Com.Structs;
using Dapplo.Windows.Common.Enums;

namespace Dapplo.Windows.Com;

/// <summary>
/// Exposes methods that initialize, show, and get results from the common file dialog.
/// See <a href="https://docs.microsoft.com/en-us/windows/win32/api/shobjidl_core/nn-shobjidl_core-ifiledialog">IFileDialog interface</a>
/// </summary>
[ComImport]
[InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
[Guid("42F85136-DB7E-439C-85F1-E4075D135FC8")]
public interface IFileDialog
{
    /// <summary>
    /// Launches the modal window.
    /// </summary>
    /// <param name="parent">The handle of the owner window. This value can be NULL.</param>
    /// <returns>If the method succeeds, it returns S_OK. If the user cancels the dialog box, it returns HRESULT_FROM_WIN32(ERROR_CANCELLED).</returns>
    [PreserveSig]
    HResult Show([In] IntPtr parent);

    /// <summary>
    /// Sets the file types that the dialog can open or save.
    /// </summary>
    /// <param name="cFileTypes">The number of elements in the array specified by rgFilterSpec.</param>
    /// <param name="rgFilterSpec">A pointer to an array of COMDLG_FILTERSPEC structures, each representing a file type.</param>
    void SetFileTypes(uint cFileTypes, [In] [MarshalAs(UnmanagedType.LPArray)] FilterSpec[] rgFilterSpec);

    /// <summary>
    /// Sets the file type that appears as selected in the dialog.
    /// </summary>
    /// <param name="iFileType">The index of the file type in the file type array passed to IFileDialog::SetFileTypes in its cFileTypes parameter. Note that this is a one-based index, not zero-based.</param>
    void SetFileTypeIndex(uint iFileType);

    /// <summary>
    /// Gets the currently selected file type.
    /// </summary>
    /// <param name="piFileType">A UINT value that receives the index of the selected file type in the file type array passed to IFileDialog::SetFileTypes in its cFileTypes parameter.</param>
    void GetFileTypeIndex(out uint piFileType);

    /// <summary>
    /// Assigns an event handler that listens for events coming from the dialog.
    /// </summary>
    /// <param name="pfde">A pointer to an IFileDialogEvents implementation that will receive events from the dialog.</param>
    /// <param name="pdwCookie">A DWORD that receives a value identifying this event handler. When the client is finished with the dialog, the client must call the IFileDialog::Unadvise method with this value.</param>
    void Advise([In] nint pfde, out uint pdwCookie);

    /// <summary>
    /// Removes an event handler that was attached through the IFileDialog::Advise method.
    /// </summary>
    /// <param name="dwCookie">The DWORD value that represents the event handler. This value is obtained through the pdwCookie parameter of the IFileDialog::Advise method.</param>
    void Unadvise(uint dwCookie);

    /// <summary>
    /// Sets flags to control the behavior of the dialog.
    /// </summary>
    /// <param name="fos">One or more of the <see cref="FileOpenOptions"/> values.</param>
    void SetOptions(FileOpenOptions fos);

    /// <summary>
    /// Gets the current flags that are set to control dialog behavior.
    /// </summary>
    /// <param name="pfos">When this method returns successfully, points to a value that is a combination of one or more of the <see cref="FileOpenOptions"/> values.</param>
    void GetOptions(out FileOpenOptions pfos);

    /// <summary>
    /// Sets the folder used as a default if there is not a recently used folder value available.
    /// </summary>
    /// <param name="psi">A pointer to the interface that represents the folder.</param>
    void SetDefaultFolder([In] IShellItem psi);

    /// <summary>
    /// Sets a folder that is always selected when the dialog is opened, regardless of previous user action.
    /// </summary>
    /// <param name="psi">A pointer to the interface that represents the folder.</param>
    void SetFolder([In] IShellItem psi);

    /// <summary>
    /// Gets either the folder currently selected in the dialog, or, if the dialog is not currently displayed, the folder that is to be selected when the dialog is opened.
    /// </summary>
    /// <param name="ppsi">The address of a pointer to the interface that represents the folder.</param>
    void GetFolder(out IShellItem ppsi);

    /// <summary>
    /// Gets the user's current selection in the dialog.
    /// </summary>
    /// <param name="ppsi">The address of a pointer to the interface that represents the item currently selected in the dialog. This item can be a file or folder selected in the navigation pane, or something that the user has entered.</param>
    void GetCurrentSelection(out IShellItem ppsi);

    /// <summary>
    /// Sets the file name that appears in the File name edit box when that dialog box is opened.
    /// </summary>
    /// <param name="pszName">A pointer to the name of the file.</param>
    void SetFileName([In] [MarshalAs(UnmanagedType.LPWStr)] string pszName);

    /// <summary>
    /// Retrieves the text currently entered in the dialog's File name edit box.
    /// </summary>
    /// <param name="pszName">The address of a pointer to a buffer that, when this method returns successfully, receives the text.</param>
    void GetFileName([MarshalAs(UnmanagedType.LPWStr)] out string pszName);

    /// <summary>
    /// Sets the title of the dialog.
    /// </summary>
    /// <param name="pszTitle">A pointer to a buffer that contains the title text.</param>
    void SetTitle([In] [MarshalAs(UnmanagedType.LPWStr)] string pszTitle);

    /// <summary>
    /// Sets the text of the Open or Save button.
    /// </summary>
    /// <param name="pszText">A pointer to a buffer that contains the button text.</param>
    void SetOkButtonLabel([In] [MarshalAs(UnmanagedType.LPWStr)] string pszText);

    /// <summary>
    /// Sets the text of the label next to the file name edit box.
    /// </summary>
    /// <param name="pszLabel">A pointer to a buffer that contains the label text.</param>
    void SetFileNameLabel([In] [MarshalAs(UnmanagedType.LPWStr)] string pszLabel);

    /// <summary>
    /// Returns the choice made in the dialog.
    /// </summary>
    /// <param name="ppsi">The address of a pointer to an IShellItem that represents the user's choice.</param>
    void GetResult(out IShellItem ppsi);

    /// <summary>
    /// Adds a folder to the list of places available for the user to open or save items.
    /// </summary>
    /// <param name="psi">A pointer to an IShellItem that represents the folder to be made available to the user.</param>
    /// <param name="fdap">Specifies where the folder is placed within the list. One of the <see cref="FileDialogAddPlaceFlags"/> values.</param>
    void AddPlace([In] IShellItem psi, FileDialogAddPlaceFlags fdap);

    /// <summary>
    /// Sets the default extension to be added to file names.
    /// </summary>
    /// <param name="pszDefaultExtension">A pointer to a buffer that contains the extension text. This string should not include a leading period. For example, "jpg" is correct, while ".jpg" is not.</param>
    void SetDefaultExtension([In] [MarshalAs(UnmanagedType.LPWStr)] string pszDefaultExtension);

    /// <summary>
    /// Closes the dialog.
    /// </summary>
    /// <param name="hr">The code that will be returned by Show to indicate that the dialog was closed before a selection was made.</param>
    void Close([MarshalAs(UnmanagedType.Error)] int hr);

    /// <summary>
    /// Enables a calling application to associate a GUID with a dialog's persisted state.
    /// </summary>
    /// <param name="guid">The GUID to associate with this dialog state.</param>
    void SetClientGuid([In] ref Guid guid);

    /// <summary>
    /// Instructs the dialog to clear all persisted state information.
    /// </summary>
    void ClearClientData();

    /// <summary>
    /// Sets the filter. Deprecated. SetFilter is no longer available for use as of Windows 7.
    /// </summary>
    /// <param name="pFilter">A pointer to the IShellItemFilter that is to be set.</param>
    void SetFilter([In] nint pFilter);
}
