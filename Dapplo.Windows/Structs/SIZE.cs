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
using System.Runtime.InteropServices;
using System.Windows;

#endregion

namespace Dapplo.Windows.Structs
{
	[StructLayout(LayoutKind.Sequential)]
	[Serializable]
	public struct SIZE
	{
		public int width;
		public int height;

		public SIZE(Size size)
			: this((int) size.Width, (int) size.Height)
		{
		}

		public SIZE(System.Drawing.Size size) : this(size.Width, size.Height)
		{
		}

		public SIZE(int width, int height)
		{
			this.width = width;
			this.height = height;
		}

		public Size ToSize()
		{
			return new Size(width, height);
		}

		public System.Drawing.Size ToSystemDrawingSize()
		{
			return new System.Drawing.Size(width, height);
		}

		public bool IsEmpty()
		{
			return width*height == 0;
		}
	}
}