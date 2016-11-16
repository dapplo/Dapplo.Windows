//  Dapplo - building blocks for desktop applications
//  Copyright (C) 2016 Dapplo
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
using System.Windows;

#endregion

namespace Dapplo.Windows.Desktop
{
	public class WindowInfo
	{
		public Rect Bounds { get; set; }

		public IList<WindowInfo> Children { get; set; }

		public string Classname { get; set; }

		public IntPtr Handle { get; set; }

		public bool HasClassname { get; set; }

		public bool HasParent { get; set; }

		public IntPtr Parent { get; set; }

		public string Text { get; set; }

		public static WindowInfo CreateFor(IntPtr handle)
		{
			return new WindowInfo
			{
				Handle = handle
			};
		}
	}
}