// Copyright (c) Dapplo and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Runtime.InteropServices;

namespace Dapplo.Windows.Dialogs.Interop;

/// <summary>
/// Exposes methods that allow access to the items in a collection of Shell items.
/// See <a href="https://docs.microsoft.com/en-us/windows/win32/api/shobjidl_core/nn-shobjidl_core-ishellitemarray">IShellItemArray interface</a>
/// </summary>
[ComImport]
[InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
[Guid("B63EA76D-1F85-456F-A19C-48159EFA858B")]
internal interface IShellItemArray
{
    void BindToHandler([In] nint pbc, [In] ref System.Guid bhid, [In] ref System.Guid riid, out nint ppvOut);
    void GetPropertyStore(int flags, [In] ref System.Guid riid, out nint ppv);
    void GetPropertyDescriptionList([In] nint keyType, [In] ref System.Guid riid, out nint ppv);
    void GetAttributes(uint attribFlags, uint sfgaoMask, out uint psfgaoAttribs);
    void GetCount(out uint pdwNumItems);
    void GetItemAt(uint dwIndex, out IShellItem ppsi);
    void EnumItems(out nint ppenumShellItems);
}
