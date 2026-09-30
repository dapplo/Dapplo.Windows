// Copyright (c) Dapplo and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Diagnostics.Contracts;

namespace Dapplo.Windows.Wpf;

/// <summary>
///     Conversions between System.Drawing.Color and System.Windows.Media.Color, e.g. for <c>DwmApi.ColorizationDrawingColor.ToMediaColor()</c>
/// </summary>
public static class ColorExtensions
{
    /// <summary>
    ///     Convert a System.Drawing.Color to a System.Windows.Media.Color
    /// </summary>
    /// <param name="color">System.Drawing.Color</param>
    /// <returns>System.Windows.Media.Color</returns>
    [Pure]
    public static System.Windows.Media.Color ToMediaColor(this System.Drawing.Color color) => System.Windows.Media.Color.FromArgb(color.A, color.R, color.G, color.B);

    /// <summary>
    ///     Convert a System.Windows.Media.Color to a System.Drawing.Color
    /// </summary>
    /// <param name="color">System.Windows.Media.Color</param>
    /// <returns>System.Drawing.Color</returns>
    [Pure]
    public static System.Drawing.Color ToDrawingColor(this System.Windows.Media.Color color) => System.Drawing.Color.FromArgb(color.A, color.R, color.G, color.B);
}
