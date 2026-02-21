// Copyright (c) Dapplo and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Dapplo.Windows.Com.Enums;

/// <summary>
/// Requests the form of an item's display name to retrieve through IShellItem::GetDisplayName and SHGetNameFromIDList.
/// See <a href="https://docs.microsoft.com/en-us/windows/win32/api/shobjidl_core/ne-shobjidl_core-sigdn">SIGDN enumeration</a>
/// </summary>
public enum ShellItemDisplayName : uint
{
    /// <summary>
    /// Returns the display name relative to the parent folder. In UI this name is generally ideal for display to the user.
    /// </summary>
    NormalDisplay = 0x00000000,

    /// <summary>
    /// Returns the parsing name relative to the parent folder. This name is not suitable for use in UI.
    /// </summary>
    ParentRelativeParsing = 0x80018001,

    /// <summary>
    /// Returns the parsing name relative to the desktop. This name is not suitable for use in UI.
    /// </summary>
    DesktopAbsoluteParsing = 0x80028000,

    /// <summary>
    /// Returns the editing name relative to the parent folder. In UI this name is suitable for display to the user.
    /// </summary>
    ParentRelativeEditing = 0x80031001,

    /// <summary>
    /// Returns the editing name relative to the desktop. In UI this name is suitable for display to the user.
    /// </summary>
    DesktopAbsoluteEditing = 0x8004c000,

    /// <summary>
    /// Returns the item's file system path, if it has one. Only items that report SFGAO_FILESYSTEM have a file system path. When an item does not have a file system path, a call to IShellItem::GetDisplayName on that item will fail.
    /// </summary>
    FileSysPath = 0x80058000,

    /// <summary>
    /// Returns the item's URL, if it has one. Some items do not have URLs, and in those cases, a call to IShellItem::GetDisplayName will fail.
    /// </summary>
    Url = 0x80068000,

    /// <summary>
    /// Returns the path relative to the parent folder in a friendly format as displayed in an address bar. This name is suitable for display to the user.
    /// </summary>
    ParentRelativeForAddressBar = 0x8007c001,

    /// <summary>
    /// Returns the path relative to the parent folder.
    /// </summary>
    ParentRelative = 0x80080001,

    /// <summary>
    /// Introduced in Windows 8.
    /// </summary>
    ParentRelativeForUI = 0x80094001
}
