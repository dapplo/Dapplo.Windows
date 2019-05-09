//  Dapplo - building blocks for desktop applications
//  Copyright (C) 2017-2019  Dapplo
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

using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;

#endregion

namespace Dapplo.Windows.Gdi32.Structs
{
    /// <summary>
    ///     Contains members that specify the nature of a Gaussian blur.
    /// </summary>
    /// <remarks>Cannot be pinned with GCHandle due to bool value.</remarks>
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    [SuppressMessage("Sonar Code Smell", "S1450:Trivial properties should be auto-implementedPrivate fields only used as local variables in methods should become local variables", Justification = "Interop!")]
    public struct BlurParams
    {
        private float _radius;
        private bool _expandEdges;

        /// <summary>
        /// </summary>
        /// <param name="radius">
        ///     Real number that specifies the blur radius (the radius of the Gaussian convolution kernel) in
        ///     pixels. The radius must be in the range 0 through 255. As the radius increases, the resulting
        ///     bitmap becomes more blurry.
        /// </param>
        /// <param name="expandEdges">
        ///     Boolean value that specifies whether the bitmap expands by an amount equal to the blur radius.
        ///     If TRUE, the bitmap expands by an amount equal to the radius so that it can have soft edges.
        ///     If FALSE, the bitmap remains the same size and the soft edges are clipped.
        /// </param>
        /// <returns></returns>
        public static BlurParams Create(float radius, bool expandEdges)
        {
            return new BlurParams
            {
                _radius = radius,
                _expandEdges = expandEdges
            };
        }
    }
}