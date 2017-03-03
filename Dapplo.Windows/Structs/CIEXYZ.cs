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

using System.Runtime.InteropServices;

#endregion

namespace Dapplo.Windows.Structs
{
	/// <summary>
	/// CIE XYZ 1931 color space
	/// </summary>
	[StructLayout(LayoutKind.Sequential)]
	public struct CieXyz
	{
		/// <summary>
		/// is a mix of cone response curves chosen to be orthogonal to luminance and non-negative
		/// FXPT2DOT30 is a fixed-point values with a 2-bit integer part and a 30-bit fractional part.
		/// </summary>
		public uint ciexyzX;
		/// <summary>
		/// Luminance 
		/// FXPT2DOT30 is a fixed-point values with a 2-bit integer part and a 30-bit fractional part.
		/// </summary>
		public uint ciexyzY;
		/// <summary>
		/// is somewhat equal to blue
		/// FXPT2DOT30 is a fixed-point values with a 2-bit integer part and a 30-bit fractional part.
		/// </summary>
		public uint ciexyzZ;

		/// <summary>
		/// Factory for a CieXyz
		/// </summary>
		/// <param name="fxPt2Dot30">uint</param>
		public static CieXyz Create(uint fxPt2Dot30)
		{
			return new CieXyz
			{
				ciexyzX = fxPt2Dot30,
				ciexyzY = fxPt2Dot30,
				ciexyzZ = fxPt2Dot30,
			};
		}
	}
}