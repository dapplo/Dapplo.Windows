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

using System.Runtime.InteropServices;

namespace Dapplo.Windows.Structs
{
	/// <summary>
	/// Contains members that specify the nature of a Gaussian blur.
	/// </summary>
	/// <remarks>Cannot be pinned with GCHandle due to bool value.</remarks>
	[StructLayout(LayoutKind.Sequential, Pack = 1)]
	public struct BlurParams
	{
		/// <summary>
		/// Real number that specifies the blur radius (the radius of the Gaussian convolution kernel) in 
		/// pixels. The radius must be in the range 0 through 255. As the radius increases, the resulting 
		/// bitmap becomes more blurry.
		/// </summary>
		public float Radius;

		/// <summary>
		/// Boolean value that specifies whether the bitmap expands by an amount equal to the blur radius. 
		/// If TRUE, the bitmap expands by an amount equal to the radius so that it can have soft edges. 
		/// If FALSE, the bitmap remains the same size and the soft edges are clipped.
		/// </summary>
		public bool ExpandEdges;
	}
}
