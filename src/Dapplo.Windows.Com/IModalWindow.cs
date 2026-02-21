// Copyright (c) Dapplo and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Runtime.InteropServices;
using Dapplo.Windows.Common.Enums;

namespace Dapplo.Windows.Com;

/// <summary>
/// Exposes a method that represents a modal window. This interface is used in the Windows XP Packaging API.
/// See <a href="https://docs.microsoft.com/en-us/windows/win32/api/shobjidl_core/nn-shobjidl_core-imodalwindow">IModalWindow interface</a>
/// </summary>
[ComImport]
[InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
[Guid("B4DB1657-70D7-485E-8E3E-6FCB5A5C1802")]
public interface IModalWindow
{
    /// <summary>
    /// Launches the modal window.
    /// </summary>
    /// <param name="parent">The handle of the owner window. This value can be NULL.</param>
    /// <returns>If the method succeeds, it returns S_OK. If the user cancels the dialog box, it returns HRESULT_FROM_WIN32(ERROR_CANCELLED).</returns>
    [PreserveSig]
    HResult Show([In] IntPtr parent);
}
