// Copyright (c) Dapplo and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Runtime.InteropServices;

namespace Dapplo.Windows.Dialogs.Interop;

/// <summary>
/// Exposes methods that retrieve information about a Shell item.
/// See <a href="https://docs.microsoft.com/en-us/windows/win32/api/shobjidl_core/nn-shobjidl_core-ishellitem">IShellItem interface</a>
/// </summary>
[ComImport]
[InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
[Guid("43826D1E-E718-42EE-BC55-A1E261C37BFE")]
internal interface IShellItem
{
    void BindToHandler([In] nint pbc, [In] ref System.Guid bhid, [In] ref System.Guid riid, out nint ppv);
    void GetParent(out IShellItem ppsi);
    void GetDisplayName(ShellItemDisplayName sigdnName, [MarshalAs(UnmanagedType.LPWStr)] out string ppszName);
    void GetAttributes(uint sfgaoMask, out uint psfgaoAttribs);
    void Compare([In] IShellItem psi, uint hint, out int piOrder);
}
