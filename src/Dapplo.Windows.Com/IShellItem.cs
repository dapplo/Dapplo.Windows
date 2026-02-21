// Copyright (c) Dapplo and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Runtime.InteropServices;
using Dapplo.Windows.Com.Enums;

namespace Dapplo.Windows.Com;

/// <summary>
/// Exposes methods that retrieve information about a Shell item. IShellItem and IShellItem2 are the preferred representations of items in any new code.
/// See <a href="https://docs.microsoft.com/en-us/windows/win32/api/shobjidl_core/nn-shobjidl_core-ishellitem">IShellItem interface</a>
/// </summary>
[ComImport]
[InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
[Guid("43826D1E-E718-42EE-BC55-A1E261C37BFE")]
public interface IShellItem
{
    /// <summary>
    /// Binds to a handler for an item as specified by the handler ID value (BHID).
    /// </summary>
    /// <param name="pbc">A pointer to an IBindCtx interface on a bind context object. Used to pass optional parameters to the handler. The contents of the bind context are handler-specific. For example, when binding to BHID_Stream, the STGM flags in the bind context indicate the mode of access desired (read or read/write).</param>
    /// <param name="bhid">Reference to a GUID that specifies which handler will be created.</param>
    /// <param name="riid">IID of the object type to retrieve.</param>
    /// <param name="ppv">When this method returns, contains a pointer of type riid that is returned by the handler specified by rbhid.</param>
    void BindToHandler([In] nint pbc, [In] ref System.Guid bhid, [In] ref System.Guid riid, out nint ppv);

    /// <summary>
    /// Gets the parent of an IShellItem object.
    /// </summary>
    /// <param name="ppsi">The address of a pointer to the IShellItem interface of the parent of this item.</param>
    void GetParent(out IShellItem ppsi);

    /// <summary>
    /// Gets the display name of the IShellItem object.
    /// </summary>
    /// <param name="sigdnName">One of the SIGDN values that indicates how the name should look.</param>
    /// <param name="ppszName">A value that, when this function returns successfully, receives the address of a pointer to the retrieved display name.</param>
    void GetDisplayName(ShellItemDisplayName sigdnName, [MarshalAs(UnmanagedType.LPWStr)] out string ppszName);

    /// <summary>
    /// Gets a requested set of attributes of the IShellItem object.
    /// </summary>
    /// <param name="sfgaoMask">Specifies the attributes to retrieve. They are represented as bit flags.</param>
    /// <param name="psfgaoAttribs">A pointer to a value that, when this method returns successfully, contains the requested attributes.</param>
    void GetAttributes(uint sfgaoMask, out uint psfgaoAttribs);

    /// <summary>
    /// Compares two IShellItem objects.
    /// </summary>
    /// <param name="psi">A pointer to an IShellItem object to compare with the existing IShellItem object.</param>
    /// <param name="hint">One of the SICHINTF values that determines how to perform the comparison.</param>
    /// <param name="piOrder">This parameter receives the result of the comparison. If the two items are the same this parameter equals zero; if they are different the parameter is nonzero.</param>
    void Compare([In] IShellItem psi, uint hint, out int piOrder);
}
