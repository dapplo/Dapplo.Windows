// Copyright (c) Dapplo and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;

namespace Dapplo.Windows.Gdi32.Structs
{
    /// <summary>
    ///     Color representation using CIEXYZ color components
    /// </summary>
    [StructLayout(LayoutKind.Sequential)]
    [SuppressMessage("ReSharper", "ConvertToAutoPropertyWhenPossible")]
    [SuppressMessage("Sonar Code Smell", "S2292:Trivial properties should be auto-implemented", Justification = "Interop!")]
    public struct CieXyzTripple
    {
        private CieXyz _cieXyzRed;
        private CieXyz _cieXyzGreen;
        private CieXyz _cieXyzBlue;

        /// <summary>
        ///     A CIE XYZ 1931 color space for the red component
        /// </summary>
        public CieXyz Red {
            get { return _cieXyzRed; }
            set { _cieXyzRed = value; }
        }

        /// <summary>
        ///     A CIE XYZ 1931 color space for the green component
        /// </summary>
        public CieXyz Green
        {
            get { return _cieXyzGreen; }
            set { _cieXyzGreen = value; }
        }

        /// <summary>
        ///     A CIE XYZ 1931 color space for the blue component
        /// </summary>
        public CieXyz Blue
        {
            get { return _cieXyzBlue; }
            set { _cieXyzBlue = value; }
        }

        /// <summary>
        ///     Factory method
        /// </summary>
        /// <param name="red">CieXyz</param>
        /// <param name="green">CieXyz</param>
        /// <param name="blue">CieXyz</param>
        /// <returns>CieXyzTripple</returns>
        public static CieXyzTripple Create(CieXyz red, CieXyz green, CieXyz blue)
        {
            return new CieXyzTripple
            {
                _cieXyzRed = red,
                _cieXyzGreen = green,
                _cieXyzBlue = blue
            };
        }
    }
}