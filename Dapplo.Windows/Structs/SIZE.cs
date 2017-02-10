#region Dapplo 2016 - GNU Lesser General Public License

// Dapplo - building blocks for .NET applications
// Copyright (C) 2017 Dapplo
// 
// For more information see: http://dapplo.net/
// Dapplo repositories are hosted on GitHub: https://github.com/dapplo
// 
// This file is part of Dapplo.Windows
// 
// Dapplo.Windows is free software: you can redistribute it and/or modify
// it under the terms of the GNU Lesser General Public License as published by
// the Free Software Foundation, either version 3 of the License, or
// (at your option) any later version.
// 
// Dapplo.Windows is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU Lesser General Public License for more details.
// 
// You should have a copy of the GNU Lesser General Public License
// along with Dapplo.Windows. If not, see <http://www.gnu.org/licenses/lgpl.txt>.

#endregion

#region Usings

using System;
using System.Runtime.InteropServices;
using System.Windows;

#endregion

namespace Dapplo.Windows.Structs
{
	/// <summary>
	/// This structure should be used everywhere where native methods need a size struct.
	/// </summary>
	[StructLayout(LayoutKind.Sequential)]
	[Serializable]
	public struct SIZE
	{
		private int _width;
		private int _height;

		/// <summary>
		/// The Width of the size struct
		/// </summary>
		public int Width
		{
			get
			{
				return _width;
			}
			set
			{
				_width = value;
			}
		}

		/// <summary>
		/// The Width of the size struct
		/// </summary>
		public int Height
		{
			get
			{
				return _height;
			}
			set
			{
				_height = value;
			}
		}

		/// <summary>
		/// Constructor from S.W.Size
		/// </summary>
		/// <param name="size"></param>
		public SIZE(Size size)
			: this((int) size.Width, (int) size.Height)
		{
		}

		/// <summary>
		/// Constructor from S.D.Size
		/// </summary>
		/// <param name="size"></param>
		public SIZE(System.Drawing.Size size) : this(size.Width, size.Height)
		{
		}

		/// <summary>
		/// Size contructor
		/// </summary>
		/// <param name="width">int</param>
		/// <param name="height">int</param>
		public SIZE(int width, int height)
		{
			_width = width;
			_height = height;
		}

		/// <summary>
		/// Create System.Drawing.Size or System.Windows.Size depending on the generic type
		/// </summary>
		/// <typeparam name="TSize"></typeparam>
		/// <returns>S.D.Size or S.W.Size</returns>
		public TSize ToSize<TSize>()
		{
			if (typeof(TSize) == typeof(Size))
			{
				return (TSize)(object)new Size(_width, _height);
			}
			return (TSize)(object)new System.Drawing.Size(_width, _height);
		}

		/// <summary>
		/// Checks if the width * height are 0
		/// </summary>
		/// <returns>true if the size is empty</returns>
		public bool IsEmpty()
		{
			return _width * _height == 0;
		}
	}
}