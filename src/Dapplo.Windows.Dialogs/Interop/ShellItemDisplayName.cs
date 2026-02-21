// Copyright (c) Dapplo and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Dapplo.Windows.Dialogs.Interop;

/// <summary>
/// Requests the form of an item's display name to retrieve through IShellItem::GetDisplayName.
/// See <a href="https://docs.microsoft.com/en-us/windows/win32/api/shobjidl_core/ne-shobjidl_core-sigdn">SIGDN enumeration</a>
/// </summary>
internal enum ShellItemDisplayName : uint
{
    /// <summary>Returns the display name relative to the parent folder.</summary>
    NormalDisplay = 0x00000000,
    /// <summary>Returns the parsing name relative to the parent folder.</summary>
    ParentRelativeParsing = 0x80018001,
    /// <summary>Returns the parsing name relative to the desktop.</summary>
    DesktopAbsoluteParsing = 0x80028000,
    /// <summary>Returns the editing name relative to the parent folder.</summary>
    ParentRelativeEditing = 0x80031001,
    /// <summary>Returns the editing name relative to the desktop.</summary>
    DesktopAbsoluteEditing = 0x8004c000,
    /// <summary>Returns the item's file system path, if it has one.</summary>
    FileSysPath = 0x80058000,
    /// <summary>Returns the item's URL, if it has one.</summary>
    Url = 0x80068000,
    /// <summary>Returns the path relative to the parent folder in a friendly format as displayed in an address bar.</summary>
    ParentRelativeForAddressBar = 0x8007c001,
    /// <summary>Returns the path relative to the parent folder.</summary>
    ParentRelative = 0x80080001,
}
