// Copyright (c) Dapplo and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Runtime.InteropServices;

namespace Dapplo.Windows.Com;

/// <summary>
/// Exposes methods that allow access to the items in a collection of Shell items resulting from a Shell item array, a search result, or other enumerations.
/// See <a href="https://docs.microsoft.com/en-us/windows/win32/api/shobjidl_core/nn-shobjidl_core-ishellitemarray">IShellItemArray interface</a>
/// </summary>
[ComImport]
[InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
[Guid("B63EA76D-1F85-456F-A19C-48159EFA858B")]
public interface IShellItemArray
{
    /// <summary>
    /// Binds to an object by means of the specified handler.
    /// </summary>
    /// <param name="pbc">A pointer to an IBindCtx interface on a bind context object.</param>
    /// <param name="bhid">One of the BHID values that specifies which handler to use.</param>
    /// <param name="riid">The IID of the object type to retrieve.</param>
    /// <param name="ppvOut">When this method returns, contains the object specified in riid that is returned by the handler specified by rbhid.</param>
    void BindToHandler([In] nint pbc, [In] ref System.Guid bhid, [In] ref System.Guid riid, out nint ppvOut);

    /// <summary>
    /// Gets the property store of the shell item array.
    /// </summary>
    /// <param name="flags">One of the GETPROPERTYSTOREFLAGS constants.</param>
    /// <param name="riid">The IID of the object type to retrieve.</param>
    /// <param name="ppv">When this method returns, contains the interface requested in riid.</param>
    void GetPropertyStore(int flags, [In] ref System.Guid riid, out nint ppv);

    /// <summary>
    /// Gets a property description list for the items in the shell item array.
    /// </summary>
    /// <param name="keyType">A reference to the PROPERTYKEY that identifies the property description list.</param>
    /// <param name="riid">A reference to the IID of the interface to retrieve.</param>
    /// <param name="ppv">When this method returns, contains the interface requested in riid.</param>
    void GetPropertyDescriptionList([In] nint keyType, [In] ref System.Guid riid, out nint ppv);

    /// <summary>
    /// Gets the attributes of the set of items contained in an IShellItemArray. If the array contains more than one item, the attributes retrieved are those that all items share in common.
    /// </summary>
    /// <param name="attribFlags">If the array contains a single item, this method provides the same results as GetAttributes. However, if the array contains multiple items, the attribute sets of all the items are combined into a single attribute set and returned in this value.</param>
    /// <param name="sfgaoMask">A mask that specifies what attributes are being requested, taken from the SFGAO values.</param>
    /// <param name="psfgaoAttribs">A bitmap that, when this method returns successfully, contains the values of the requested attributes.</param>
    void GetAttributes(uint attribFlags, uint sfgaoMask, out uint psfgaoAttribs);

    /// <summary>
    /// Gets the number of items in the given IShellItem array.
    /// </summary>
    /// <param name="pdwNumItems">When this method returns, contains the number of items in the IShellItemArray.</param>
    void GetCount(out uint pdwNumItems);

    /// <summary>
    /// Gets the item at the given index in the IShellItemArray.
    /// </summary>
    /// <param name="dwIndex">The index of the IShellItem requested in the IShellItemArray.</param>
    /// <param name="ppsi">When this method returns, contains the requested IShellItem pointer.</param>
    void GetItemAt(uint dwIndex, out IShellItem ppsi);

    /// <summary>
    /// Gets an enumerator of the items in the array.
    /// </summary>
    /// <param name="ppenumShellItems">When this method returns, contains an IEnumShellItems pointer that enumerates the shell items in the array.</param>
    void EnumItems(out nint ppenumShellItems);
}
