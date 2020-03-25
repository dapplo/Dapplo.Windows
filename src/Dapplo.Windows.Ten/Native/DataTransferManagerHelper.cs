// Copyright (c) Dapplo and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.ComponentModel;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.ApplicationModel.DataTransfer;
using Dapplo.Windows.Common.Extensions;

namespace Dapplo.Windows.Ten.Native
{
	/// <summary>
	/// Wraps the interop for calling the ShareUI
	/// </summary>
	public class DataTransferManagerHelper
	{
		private const string DataTransferManagerId = "a5caee9b-8708-49d1-8d36-67d25a8da00c";
		private readonly IDataTransferManagerInterOp _dataTransferManagerInterOp;
		private readonly IntPtr _windowHandle;

        /// <summary>
        /// The DataTransferManager
        /// </summary>
		public DataTransferManager DataTransferManager
		{
			get;
			private set;
		}
        /// <summary>
        /// Constructor which takes a handle to initialize
        /// </summary>
        /// <param name="handle"></param>
		public DataTransferManagerHelper(IntPtr handle)
		{
			//TODO: Add a check for failure here. This will fail for versions of Windows below Windows 10
			IActivationFactory activationFactory = WindowsRuntimeMarshal.GetActivationFactory(typeof(DataTransferManager));

			// ReSharper disable once SuspiciousTypeConversion.Global
			_dataTransferManagerInterOp = (IDataTransferManagerInterOp)activationFactory;

			_windowHandle = handle;
			var riid = new Guid(DataTransferManagerId);
		    var hResult = _dataTransferManagerInterOp.GetForWindow(_windowHandle, riid, out var dataTransferManager);
			if (hResult.Failed())
			{
				throw new Win32Exception();
			}
			DataTransferManager = dataTransferManager;
		}

		/// <summary>
		/// Show the share UI
		/// </summary>
		public void ShowShareUi()
		{
			var hResult = _dataTransferManagerInterOp.ShowShareUIForWindow(_windowHandle);
		    if (hResult.Failed())
		    {
                throw new Win32Exception();
			}
		}
	}

}
