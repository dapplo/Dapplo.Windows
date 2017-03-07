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

#region using

using System;
using System.Collections.Generic;
using System.Linq;

#endregion

namespace Dapplo.Windows.Clipboard
{
	/// <summary>
	///     This event contains the information of clipboard changes
	/// </summary>
	public class ClipboardUpdateEventArgs : EventArgs
	{
		private readonly IEnumerable<string> _clipboardFormats;
		private IList<string> _clipboardFormatList;

		/// <summary>
		/// Constructor for the EventArgs
		/// </summary>
		/// <param name="clipboardFormats">IEnumerable of string</param>
		internal ClipboardUpdateEventArgs(IEnumerable<string> clipboardFormats)
		{
			_clipboardFormats = clipboardFormats;
		}

		/// <summary>
		///     The available formats on the clipboard
		/// </summary>
		public IEnumerable<string> Formats
		{
			get
			{
				if (_clipboardFormatList == null)
				{
					_clipboardFormatList = _clipboardFormats.ToList();
				}
				return _clipboardFormatList;
			}
		}
	}
}