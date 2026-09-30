// Copyright (c) Dapplo and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
using System;
using System.ComponentModel;
using System.Globalization;
using Dapplo.Windows.Common.Structs;

namespace Dapplo.Windows.Common.TypeConverters;

/// <summary>
/// This implements a TypeConverter for the NativeSize structure, the format is "Width,Height" using the invariant culture
/// </summary>
public class NativeSizeTypeConverter : TypeConverter
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
                int.TryParse(wh[0], NumberStyles.Integer, CultureInfo.InvariantCulture, out var w) &&
                int.TryParse(wh[1], NumberStyles.Integer, CultureInfo.InvariantCulture, out var h))
            {
                return new NativeSize(w, h);
            }
        }
        return base.ConvertFrom(context, culture, value);
    }

    /// <inheritdoc />
    public override object ConvertTo(ITypeDescriptorContext context, CultureInfo culture, object value, Type destinationType)
    {
        if (destinationType == typeof(string) && value is NativeSize nativeSize)
        {
            // Always use the invariant culture, ConvertFrom parses with it (e.g. sv-SE would write U+2212 as negative sign)
            return string.Concat(nativeSize.Width.ToString(CultureInfo.InvariantCulture), ",", nativeSize.Height.ToString(CultureInfo.InvariantCulture));
        }
        return base.ConvertTo(context, culture, value, destinationType);
    }
}