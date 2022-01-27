// Copyright (c) Dapplo and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
using System;
using System.ComponentModel;
using System.Globalization;
using Dapplo.Windows.Common.Structs;

namespace Dapplo.Windows.Common.TypeConverters;

/// <summary>
/// This implements a TypeConverter for the NativeRect structur
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
                float.TryParse(xywh[0], NumberStyles.Number, CultureInfo.InvariantCulture, out var x) &&
                float.TryParse(xywh[1], NumberStyles.Number, CultureInfo.InvariantCulture, out var y) &&
                float.TryParse(xywh[2], NumberStyles.Number, CultureInfo.InvariantCulture, out var w) &&
                float.TryParse(xywh[3], NumberStyles.Number, CultureInfo.InvariantCulture, out var h))
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
            return $"{nativeRectF.Left.ToString(CultureInfo.InvariantCulture)},{nativeRectF.Top.ToString(CultureInfo.InvariantCulture)},{nativeRectF.Width.ToString(CultureInfo.InvariantCulture)},{nativeRectF.Height.ToString(CultureInfo.InvariantCulture)}";
        }
        return base.ConvertTo(context, culture, value, destinationType);
    }
}