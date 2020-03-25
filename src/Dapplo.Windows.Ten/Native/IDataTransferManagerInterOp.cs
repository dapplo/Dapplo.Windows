// Copyright (c) Dapplo and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Runtime.InteropServices;
using Windows.ApplicationModel.DataTransfer;
using Dapplo.Windows.Common.Enums;

namespace Dapplo.Windows.Ten.Native
{
    /// <summary>
    /// The IDataTransferManagerInterOp is documented here: https://msdn.microsoft.com/en-us/library/windows/desktop/jj542488(v=vs.85).aspx.
    /// This interface allows an app to tie the share context to a specific
    /// window using a window handle. Useful for Win32 apps.
    /// </summary>
    [ComImport, Guid("3A3DCD6C-3EAB-43DC-BCDE-45671CE800C8")]
	[InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
	public interface IDataTransferManagerInterOp
	{
		/// <summary>
		/// Get an instance of Windows.ApplicationModel.DataTransfer.DataTransferManager
		/// for the window identified by a window handle
		/// </summary>
		/// <param name="appWindow">The window handle</param>
		/// <param name="riid">ID of the DataTransferManager interface</param>
		/// <param name="pDataTransferManager">The DataTransferManager instance for this window handle</param>
		/// <returns>HResult</returns>
		[PreserveSig]
		HResult GetForWindow([In] IntPtr appWindow, [In] ref Guid riid, [Out] out DataTransferManager pDataTransferManager);

		/// <summary>
		/// Show the share flyout for the window identified by a window handle
		/// </summary>
		/// <param name="appWindow">The window handle</param>
		/// <returns>HResult</returns>
		[PreserveSig]
		HResult ShowShareUIForWindow(IntPtr appWindow);
	}

}
