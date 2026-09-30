// Copyright (c) Dapplo and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
using System;
using System.ComponentModel;
using System.Globalization;
using Dapplo.Windows.Common.Structs;

namespace Dapplo.Windows.Common.TypeConverters;

/// <summary>
/// This implements a TypeConverter for the NativeSizeFloat structure, the format is "Width,Height" using the invariant culture
/// </summary>
public class NativeSizeFloatTypeConverter : TypeConverter
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
        if (value is string sizeStringValue)
        {
            string[] wh = sizeStringValue.Split(',');
            if (wh.Length == 2 &&
                float.TryParse(wh[0], NumberStyles.Float, CultureInfo.InvariantCulture, out var w) &&
                float.TryParse(wh[1], NumberStyles.Float, CultureInfo.InvariantCulture, out var h))
            {
                return new NativeSizeFloat(w, h);
            }
        }
        return base.ConvertFrom(context, culture, value);
    }

    /// <inheritdoc />
    public override object ConvertTo(ITypeDescriptorContext context, CultureInfo culture, object value, Type destinationType)
    {
        if (destinationType == typeof(string) && value is NativeSizeFloat nativeSizeFloat)
        {
            // "R" makes sure the value round-trips on .NET Framework too
            return string.Concat(nativeSizeFloat.Width.ToString("R", CultureInfo.InvariantCulture), ",", nativeSizeFloat.Height.ToString("R", CultureInfo.InvariantCulture));
        }
        return base.ConvertTo(context, culture, value, destinationType);
    }
}