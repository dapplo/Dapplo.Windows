/*
 * dapplo - building blocks for desktop applications
 * Copyright (C) 2015 Robin Krom
 * 
 * For more information see: http://dapplo.net/
 * dapplo repositories are hosted on GitHub: https://github.com/dapplo
 * 
 * This program is free software: you can redistribute it and/or modify
 * it under the terms of the GNU General Public License as published by
 * the Free Software Foundation, either version 1 of the License, or
 * (at your option) any later version.
 * 
 * This program is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 * GNU General Public License for more details.
 * 
 * You should have received a copy of the GNU General Public License
 * along with this program. If not, see <http://www.gnu.org/licenses/>.
 */

using System;
using System.Runtime.InteropServices;
using System.Windows;

namespace Dapplo.Windows.Structs
{
	[StructLayout(LayoutKind.Sequential), Serializable()]
	public struct SIZE
	{
		public int width;
		public int height;

		public SIZE(Size size)
			: this((int)size.Width, (int)size.Height)
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
	}
}
