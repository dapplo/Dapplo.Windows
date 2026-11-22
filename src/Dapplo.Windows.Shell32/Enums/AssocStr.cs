// Copyright (c) Dapplo and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Dapplo.Windows.Shell32.Enums;

/// <summary>
///     Which string AssocQueryString returns (ASSOCSTR_* values).
///     See <a href="https://learn.microsoft.com/windows/win32/api/shlwapi/ne-shlwapi-assocstr">ASSOCSTR enumeration</a>.
/// </summary>
public enum AssocStr
{
    /// <summary>The command line of the verb, e.g. "C:\Program Files\App\app.exe" "%1" (ASSOCSTR_COMMAND)</summary>
    Command = 1,

    /// <summary>The executable of the verb (ASSOCSTR_EXECUTABLE)</summary>
    Executable = 2,

    /// <summary>The friendly name of the document type (ASSOCSTR_FRIENDLYDOCNAME)</summary>
    FriendlyDocName = 3,

    /// <summary>The friendly name of the executable (ASSOCSTR_FRIENDLYAPPNAME)</summary>
    FriendlyAppName = 4,

    /// <summary>Ignore the information of the verb, use the information of the application (ASSOCSTR_NOOPEN)</summary>
    NoOpen = 5,

    /// <summary>The value of the shellnew key (ASSOCSTR_SHELLNEWVALUE)</summary>
    ShellNewValue = 6,

    /// <summary>The DDE command template (ASSOCSTR_DDECOMMAND)</summary>
    DdeCommand = 7,

    /// <summary>The DDE command to start the application (ASSOCSTR_DDEIFEXEC)</summary>
    DdeIfExec = 8,

    /// <summary>The DDE application name (ASSOCSTR_DDEAPPLICATION)</summary>
    DdeApplication = 9,

    /// <summary>The DDE topic name (ASSOCSTR_DDETOPIC)</summary>
    DdeTopic = 10,

    /// <summary>The InfoTip registry value, the properties shown in a tool tip (ASSOCSTR_INFOTIP)</summary>
    InfoTip = 11,

    /// <summary>The QuickTip registry value (ASSOCSTR_QUICKTIP)</summary>
    QuickTip = 12,

    /// <summary>The TileInfo registry value (ASSOCSTR_TILEINFO)</summary>
    TileInfo = 13,

    /// <summary>The perceived type of the content, e.g. "image" (ASSOCSTR_CONTENTTYPE)</summary>
    ContentType = 14,

    /// <summary>The default icon, path and index (ASSOCSTR_DEFAULTICON)</summary>
    DefaultIcon = 15,

    /// <summary>The CLSID of the shell extension handler of the extra string (ASSOCSTR_SHELLEXTENSION)</summary>
    ShellExtension = 16,

    /// <summary>The CLSID of the drop target (ASSOCSTR_DROPTARGET)</summary>
    DropTarget = 17,

    /// <summary>The CLSID of the delegate execute (ASSOCSTR_DELEGATEEXECUTE)</summary>
    DelegateExecute = 18,

    /// <summary>The supported URI protocols (ASSOCSTR_SUPPORTED_URI_PROTOCOLS)</summary>
    SupportedUriProtocols = 19,

    /// <summary>The ProgID of the association (ASSOCSTR_PROGID)</summary>
    ProgId = 20,

    /// <summary>The AppUserModelID of the application (ASSOCSTR_APPID)</summary>
    AppId = 21,

    /// <summary>The publisher of the application (ASSOCSTR_APPPUBLISHER)</summary>
    AppPublisher = 22,

    /// <summary>The icon reference of the application (ASSOCSTR_APPICONREFERENCE)</summary>
    AppIconReference = 23
}
