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

using System.Runtime.InteropServices;
using Dapplo.Windows.Structs;

namespace Dapplo.Windows.Citrix
{
	/// <summary>
	/// This structure is returned when WFQuerySessionInformation is called with WFInfoClasses.ClientDisplay
	/// </summary>
	[StructLayout(LayoutKind.Sequential)]
	public struct ClientDisplay
	{
		private readonly int _horizontalResolution;
		private readonly int _verticalResolution;
		private readonly int _colorDepth;

		/// <summary>
		/// Return the client's display size
		/// </summary>
		public SIZE ClientSize => new SIZE(_horizontalResolution, _verticalResolution);

		/// <summary>
		/// Returns the number of colors the client can display
		/// </summary>
		public int ColorDepth
		{
			get
			{
				switch (_colorDepth)
				{
					case 1:
						return 16;
					case 2:
						return 256;
					case 4:
						return 2^16;
					case 8:
						return 2^24;
				}
				return -1;
			}
		}
	}
}
