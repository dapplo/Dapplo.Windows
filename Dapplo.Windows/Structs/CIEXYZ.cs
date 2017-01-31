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

using System.Runtime.InteropServices;

#endregion

namespace Dapplo.Windows.Structs
{
	[StructLayout(LayoutKind.Sequential)]
	public struct CieXyz
	{
		public uint ciexyzX; // FXPT2DOT30 -> fixed-point values with a 2-bit integer part and a 30-bit fractional part.
		public uint ciexyzY; // FXPT2DOT30 -> fixed-point values with a 2-bit integer part and a 30-bit fractional part.
		public uint ciexyzZ; // FXPT2DOT30 -> fixed-point values with a 2-bit integer part and a 30-bit fractional part.

		public CieXyz(uint FXPT2DOT30)
		{
			ciexyzX = FXPT2DOT30;
			ciexyzY = FXPT2DOT30;
			ciexyzZ = FXPT2DOT30;
		}
	}
}