//  Dapplo - building blocks for desktop applications
//  Copyright (C) 2016-2017 Dapplo
// 
//  For more information see: http://dapplo.net/
//  Dapplo repositories are hosted on GitHub: https://github.com/dapplo
// 
//  This file is part of Dapplo.Windows
// 
//  Dapplo.Windows is free software: you can redistribute it and/or modify
//  it under the terms of the GNU Lesser General Public License as published by
//  the Free Software Foundation, either version 3 of the License, or
//  (at your option) any later version.
// 
//  Dapplo.Windows is distributed in the hope that it will be useful,
//  but WITHOUT ANY WARRANTY; without even the implied warranty of
//  MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
//  GNU Lesser General Public License for more details.
// 
//  You should have a copy of the GNU Lesser General Public License
//  along with Dapplo.Windows. If not, see <http://www.gnu.org/licenses/lgpl.txt>.

using System.Windows.Forms;
using Dapplo.Windows.Desktop;

namespace Dapplo.Windows.Forms
{
	/// <summary>
	/// Extensions for Windows Form
	/// </summary>
	public static class FormsExtensions
	{
		/// <summary>
		/// Handle DPI changes for the specified Form
		/// </summary>
		/// <param name="form">Form</param>
		/// <returns>DpiHandler</returns>
		public static DpiHandler HandleDpiChanges(this Form form)
		{
			var dpiHandler = new DpiHandler();
			var listener = new WinProcListener(form);
			listener.AddHook(dpiHandler.HandleMessages);
			dpiHandler.MessageHandler = listener;
			return dpiHandler;
		}
	}
}
