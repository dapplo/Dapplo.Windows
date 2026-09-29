// Copyright (c) Dapplo and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
using System;
using System.ComponentModel;
using System.Diagnostics.Contracts;
using System.Globalization;
using Dapplo.Windows.Common.Structs;

namespace Dapplo.Windows.Common.TypeConverters;

/// <summary>
/// This implements a TypeConverter for the NativePointFloat structure, the format is "X,Y" using the invariant culture
/// </summary>
public class NativePointFloatTypeConverter : TypeConverter
{
    /// <inheritdoc />
    [Pure]
    public override bool CanConvertFrom(ITypeDescriptorContext context, Type sourceType)
    {
        return sourceType == typeof(string) || base.CanConvertFrom(context, sourceType);
    }

    /// <inheritdoc />
    [Pure]
    public override bool CanConvertTo(ITypeDescriptorContext context, Type destinationType)
    {
        return destinationType == typeof(string) || base.CanConvertTo(context, destinationType);
    }

    /// <inheritdoc />
    [Pure]
    public override object ConvertFrom(ITypeDescriptorContext context, CultureInfo culture, object value)
    {
        if (value is string pointStringValue)
        {
            string[] xy = pointStringValue.Split(',');
            if (xy.Length == 2 &&
                float.TryParse(xy[0], NumberStyles.Float, CultureInfo.InvariantCulture, out var x) &&
                float.TryParse(xy[1], NumberStyles.Float, CultureInfo.InvariantCulture, out var y))
            {
                return new NativePointFloat(x, y);
            }
        }
        return base.ConvertFrom(context, culture, value);
    }

    /// <inheritdoc />
    [Pure]
    public override object ConvertTo(ITypeDescriptorContext context, CultureInfo culture, object value, Type destinationType)
    {
        if (destinationType == typeof(string) && value is NativePointFloat nativePointFloat)
        {
            // "R" makes sure the value round-trips on .NET Framework too
            return string.Concat(nativePointFloat.X.ToString("R", CultureInfo.InvariantCulture), ",", nativePointFloat.Y.ToString("R", CultureInfo.InvariantCulture));
        }
        return base.ConvertTo(context, culture, value, destinationType);
    }
}
