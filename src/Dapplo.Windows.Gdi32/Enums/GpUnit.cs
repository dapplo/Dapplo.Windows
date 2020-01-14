// Copyright (c) Dapplo and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
namespace Dapplo.Windows.Gdi32.Enums
{
    /// <summary>
    ///     GDI Plus unit description.
    /// </summary>
    public enum GpUnit
    {
        /// <summary>
        ///     World coordinate (non-physical unit).
        /// </summary>
        UnitWorld,

        /// <summary>
        ///     Variable - for PageTransform only.
        /// </summary>
        UnitDisplay,

        /// <summary>
        ///     Each unit is one device pixel.
        /// </summary>
        UnitPixel,

        /// <summary>
        ///     Each unit is a printer's point, or 1/72 inch.
        /// </summary>
        UnitPoint,

        /// <summary>
        ///     Each unit is 1 inch.
        /// </summary>
        UnitInch,

        /// <summary>
        ///     Each unit is 1/300 inch.
        /// </summary>
        UnitDocument,

        /// <summary>
        ///     Each unit is 1 millimeter.
        /// </summary>
        UnitMillimeter
    }
}