// Copyright (c) Dapplo and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
using System;
using System.ComponentModel;
using System.Globalization;
using Dapplo.Windows.Common.Structs;

namespace Dapplo.Windows.Common.TypeConverters;

/// <summary>
/// This implements a TypeConverter for the NativeRectFloat structure, the format is "Left,Top,Width,Height" using the invariant culture
/// </summary>
public class NativeRectFloatTypeConverter : TypeConverter
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
        if (value is string nativeRectFStringValue)
        {
            string[] xywh = nativeRectFStringValue.Split(',');
            if (xywh.Length == 4 &&
                float.TryParse(xywh[0], NumberStyles.Float, CultureInfo.InvariantCulture, out var x) &&
                float.TryParse(xywh[1], NumberStyles.Float, CultureInfo.InvariantCulture, out var y) &&
                float.TryParse(xywh[2], NumberStyles.Float, CultureInfo.InvariantCulture, out var w) &&
                float.TryParse(xywh[3], NumberStyles.Float, CultureInfo.InvariantCulture, out var h))
            {
                return new NativeRectFloat(x, y, w, h);
            }
        }
        return base.ConvertFrom(context, culture, value);
    }

    /// <inheritdoc />
    public override object ConvertTo(ITypeDescriptorContext context, CultureInfo culture, object value, Type destinationType)
    {
        if (destinationType == typeof(string) && value is NativeRectFloat nativeRectF)
        {
            // "R" makes sure the value round-trips on .NET Framework too
            return string.Join(",", nativeRectF.Left.ToString("R", CultureInfo.InvariantCulture), nativeRectF.Top.ToString("R", CultureInfo.InvariantCulture), nativeRectF.Width.ToString("R", CultureInfo.InvariantCulture), nativeRectF.Height.ToString("R", CultureInfo.InvariantCulture));
        }
        return base.ConvertTo(context, culture, value, destinationType);
    }
}