// Copyright (c) Dapplo and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;

namespace Dapplo.Windows.Com.Enums;

/// <summary>
/// Options for the file open and save dialogs.
/// See <a href="https://docs.microsoft.com/en-us/windows/win32/api/shobjidl_core/ne-shobjidl_core-_fileopendialogoptions">FILEOPENDIALOGOPTIONS enumeration</a>
/// </summary>
[Flags]
public enum FileOpenOptions : uint
{
    /// <summary>
    /// When saving a file, prompt before overwriting an existing file of the same name. This is a default value for the Save dialog.
    /// </summary>
    OverwritePrompt = 0x00000002,

    /// <summary>
    /// In the Save dialog, only allow the user to choose a file that has one of the file name extensions specified through IFileDialog::SetFileTypes.
    /// </summary>
    StrictFileTypes = 0x00000004,

    /// <summary>
    /// Don't change the current working directory.
    /// </summary>
    NoChangeDir = 0x00000008,

    /// <summary>
    /// Present an Open dialog that offers a choice of folders rather than files.
    /// </summary>
    PickFolders = 0x00000020,

    /// <summary>
    /// Ensures that returned items are file system items (SFGAO_FILESYSTEM). Note that this does not apply to items returned by IFileDialog::GetCurrentSelection.
    /// </summary>
    ForceFileSystem = 0x00000040,

    /// <summary>
    /// Enables the user to choose any item in the Shell namespace, not just those with SFGAO_STREAM or SFGAO_FILESYSTEM attributes. This flag cannot be combined with FOS_FORCEFILESYSTEM.
    /// </summary>
    AllNonStorageItems = 0x00000080,

    /// <summary>
    /// Do not check for situations that would prevent an application from opening the selected file, such as sharing violations or access denied errors.
    /// </summary>
    NoValidate = 0x00000100,

    /// <summary>
    /// Enables the user to select multiple items in the open dialog. Note that when this flag is set, the IFileOpenDialog interface must be used to retrieve those items.
    /// </summary>
    AllowMultiSelect = 0x00000200,

    /// <summary>
    /// The item returned must be in an existing folder. This is a default value.
    /// </summary>
    PathMustExist = 0x00000800,

    /// <summary>
    /// The item returned must exist. This is a default value for the Open dialog.
    /// </summary>
    FileMustExist = 0x00001000,

    /// <summary>
    /// Prompt to create a new file if the user specifies a file that does not exist. This flag is only meaningful with the Save dialog.
    /// </summary>
    CreatePrompt = 0x00002000,

    /// <summary>
    /// Always show the readonly checkbox regardless of whether the file being opened is read-only.
    /// </summary>
    ShareAware = 0x00004000,

    /// <summary>
    /// Do not return read-only items. This is a default value for the Save dialog.
    /// </summary>
    NoReadOnlyReturn = 0x00008000,

    /// <summary>
    /// Do not test creation of the returned item from IFileSaveDialog. If this flag is not set, the calling application must handle errors, such as denial-of-access, that arise when the item is created.
    /// </summary>
    NoTestFileCreate = 0x00010000,

    /// <summary>
    /// Hide the list of places from which the user has recently opened or saved items. This value is not supported as of Windows 7.
    /// </summary>
    HideMruPlaces = 0x00020000,

    /// <summary>
    /// Hide items shown by default in the view's navigation pane. This flag is often used in conjunction with the IFileDialog::AddPlace method, to hide standard locations and replace them with custom locations.
    /// </summary>
    HidePinnedPlaces = 0x00040000,

    /// <summary>
    /// Shortcuts should not be treated as their target items. This allows an application to open a .lnk file.
    /// </summary>
    NoDereferenceLinks = 0x00100000,

    /// <summary>
    /// (Introduced in Windows 8.) Stop navigation events from being fired on subfolders.
    /// </summary>
    OkButtonNeedsInteraction = 0x00200000,

    /// <summary>
    /// Do not add the item being opened or saved to the recent documents list (SHAddToRecentDocs).
    /// </summary>
    DontAddToRecent = 0x02000000,

    /// <summary>
    /// Include hidden and system items.
    /// </summary>
    ForceShowHidden = 0x10000000,

    /// <summary>
    /// Indicates to the Save As dialog box that it should open in expanded mode. Expanded mode is the mode that is set and remembered by the user. This flag is only valid for the Save As dialog.
    /// </summary>
    DefaultNoMiniMode = 0x20000000,

    /// <summary>
    /// Indicates to the Open dialog box that the preview pane should always be displayed.
    /// </summary>
    ForcePreviewPaneOn = 0x40000000,

    /// <summary>
    /// (Introduced in Windows 8.) Indicates that the caller is opening a file as a stream (STGM_READ | STGM_SHARE_DENY_NONE), so there is no connection to a place in the Shell namespace.
    /// </summary>
    SupportStreamableItems = 0x80000000
}
