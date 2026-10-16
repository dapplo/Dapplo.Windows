// Copyright (c) Dapplo and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;

namespace Dapplo.Windows.Shell32.Enums;

/// <summary>
///     What SHGetStockIconInfo returns (SHGSI_* flags).
///     See <a href="https://learn.microsoft.com/windows/win32/api/shellapi/nf-shellapi-shgetstockiconinfo">SHGetStockIconInfo function</a>.
/// </summary>
[Flags]
public enum StockIconFlags : uint
{
    /// <summary>The path and the icon index in that file are returned in StockIconInfo.Path and StockIconInfo.IconIndex (SHGSI_ICONLOCATION), this is always done</summary>
    IconLocation = 0,

    /// <summary>The large icon (default) or the small icon is returned in StockIconInfo.IconHandle (SHGSI_ICON), the caller destroys it with DestroyIcon</summary>
    Icon = 0x000000100,

    /// <summary>The index of the icon in the system image list is returned in StockIconInfo.SystemImageIndex (SHGSI_SYSICONINDEX)</summary>
    SystemIconIndex = 0x000004000,

    /// <summary>Modifies <see cref="Icon"/>: the link overlay is added to the icon (SHGSI_LINKOVERLAY)</summary>
    LinkOverlay = 0x000008000,

    /// <summary>Modifies <see cref="Icon"/>: the icon is blended with the system highlight color (SHGSI_SELECTED)</summary>
    Selected = 0x000010000,

    /// <summary>Modifies <see cref="Icon"/>: the large icon, the size of SM_CXICON (SHGSI_LARGEICON), the default</summary>
    LargeIcon = 0x000000000,

    /// <summary>Modifies <see cref="Icon"/>: the small icon, the size of SM_CXSMICON (SHGSI_SMALLICON)</summary>
    SmallIcon = 0x000000001,

    /// <summary>Modifies <see cref="LargeIcon"/> or <see cref="SmallIcon"/>: the icon in the size the shell uses (SHGSI_SHELLICONSIZE)</summary>
    ShellIconSize = 0x000000004
}
