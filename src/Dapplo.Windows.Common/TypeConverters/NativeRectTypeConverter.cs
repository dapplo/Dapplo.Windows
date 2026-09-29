// Copyright (c) Dapplo and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
using System;
using System.ComponentModel;
using System.Globalization;
using Dapplo.Windows.Common.Structs;

namespace Dapplo.Windows.Common.TypeConverters;

/// <summary>
/// This implements a TypeConverter for the NativeRect structure, the format is "Left,Top,Width,Height" using the invariant culture
/// </summary>
public class NativeRectTypeConverter : TypeConverter
{
    /// <inheritdoc />
    public override bool CanConvertFrom(ITypeDescriptorContext context, Type sourceType)
    {
        return sourceType == typeof(string) || base.CanConvertFrom(context, sourceType);
    }

    /// <inheritdoc />
    public override bool CanConvertTo(ITypeDescriptorContext context, Type destinationType)
    {
        return destinationType == typeof(string) || base.CanConvertTo(context, destinationType);
    }

    /// <inheritdoc />
    public override object ConvertFrom(ITypeDescriptorContext context, CultureInfo culture, object value)
    {
        if (value is string nativeRectStringValue)
        {
            string[] xywh = nativeRectStringValue.Split(',');
            if (xywh.Length == 4 &&
                int.TryParse(xywh[0], NumberStyles.Integer, CultureInfo.InvariantCulture, out var x) &&
                int.TryParse(xywh[1], NumberStyles.Integer, CultureInfo.InvariantCulture, out var y) &&
                int.TryParse(xywh[2], NumberStyles.Integer, CultureInfo.InvariantCulture, out var w) &&
                int.TryParse(xywh[3], NumberStyles.Integer, CultureInfo.InvariantCulture, out var h))
            {
                return new NativeRect(x, y, w, h);
            }
        }
        return base.ConvertFrom(context, culture, value);
    }

    /// <inheritdoc />
    public override object ConvertTo(ITypeDescriptorContext context, CultureInfo culture, object value, Type destinationType)
    {
        if (destinationType == typeof(string) && value is NativeRect nativeRect)
        {
            // Always use the invariant culture, ConvertFrom parses with it (e.g. sv-SE would write U+2212 as negative sign)
            return string.Join(",", nativeRect.Left.ToString(CultureInfo.InvariantCulture), nativeRect.Top.ToString(CultureInfo.InvariantCulture), nativeRect.Width.ToString(CultureInfo.InvariantCulture), nativeRect.Height.ToString(CultureInfo.InvariantCulture));
        }
        return base.ConvertTo(context, culture, value, destinationType);
    }
}