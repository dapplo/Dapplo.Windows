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

namespace Dapplo.Windows.Dpi.Forms
{
	/// <summary>
	/// This is a DPI-Aware Form, making DPI support very easy: just extend your Form from this
	/// </summary>
	public class DpiAwareForm : Form
	{
		/// <summary>
		/// The DpiHandler used for this form
		/// </summary>
		protected DpiHandler DpiHandler { get; } = new DpiHandler();

		/// <summary>
		/// Override the WndProc to make sure the DpiHandler is informed of the WM_NCCREATE message
		/// </summary>
		/// <param name="m">Message</param>
		protected override void WndProc(ref Message m)
		{
			bool handled = false;
			DpiHandler.HandleMessages(m.HWnd, m.Msg, m.WParam, m.LParam, ref handled);
			if (!handled)
			{
				base.WndProc(ref m);
			}
		}
	}
}
