// Copyright (c) Dapplo and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;

namespace Dapplo.Windows.Shell32.Enums;

/// <summary>
///     Flags for AssocQueryString (ASSOCF_* values).
///     See <a href="https://learn.microsoft.com/windows/win32/shell/assocf_str">ASSOCF</a>.
/// </summary>
[Flags]
public enum AssocF : uint
{
    /// <summary>No flags (ASSOCF_NONE)</summary>
    None = 0x00000000,

    /// <summary>The association string is an executable, e.g. "notepad.exe", the information of its application registration is returned (ASSOCF_INIT_BYEXENAME)</summary>
    InitByExeName = 0x00000002,

    /// <summary>Don't use the * (all files) key as fallback (ASSOCF_INIT_DEFAULTTOSTAR)</summary>
    InitDefaultToStar = 0x00000004,

    /// <summary>Use the Folder key as fallback (ASSOCF_INIT_DEFAULTTOFOLDER)</summary>
    InitDefaultToFolder = 0x00000008,

    /// <summary>Only HKEY_CLASSES_ROOT under the user key is used (ASSOCF_NOUSERSETTINGS)</summary>
    NoUserSettings = 0x00000010,

    /// <summary>The string isn't truncated when the buffer is too small, E_POINTER is returned instead (ASSOCF_NOTRUNCATE)</summary>
    NoTruncate = 0x00000020,

    /// <summary>The command (verb) must exist (ASSOCF_VERIFY)</summary>
    Verify = 0x00000040,

    /// <summary>Ignore the Rundll.exe based applications (ASSOCF_REMAPRUNDLL)</summary>
    RemapRunDll = 0x00000080,

    /// <summary>Don't fix errors in the registry, e.g. a missing friendly name (ASSOCF_NOFIXUPS)</summary>
    NoFixUps = 0x00000100,

    /// <summary>Ignore the BaseClass value (ASSOCF_IGNOREBASECLASS)</summary>
    IgnoreBaseClass = 0x00000200,

    /// <summary>Don't use "Unknown" as fallback (ASSOCF_INIT_IGNOREUNKNOWN)</summary>
    InitIgnoreUnknown = 0x00000400,

    /// <summary>The ProgID is fixed, no user choice (ASSOCF_INIT_FIXED_PROGID)</summary>
    InitFixedProgId = 0x00000800,

    /// <summary>The association string is a protocol, e.g. "https" or "mailto" (ASSOCF_IS_PROTOCOL)</summary>
    IsProtocol = 0x00001000,

    /// <summary>The association string is a file name extension in the form of a ProgID (ASSOCF_INIT_FOR_FILE)</summary>
    InitForFile = 0x00002000
}
