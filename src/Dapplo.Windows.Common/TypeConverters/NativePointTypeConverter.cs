// Copyright (c) Dapplo and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
using System;
using System.ComponentModel;
using System.Diagnostics.Contracts;
using System.Globalization;
using Dapplo.Windows.Common.Structs;

namespace Dapplo.Windows.Common.TypeConverters
{
    /// <summary>
    /// This implements a TypeConverter for the NativePoint structur
    /// </summary>
    public class NativePointTypeConverter : TypeConverter
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
                    int.TryParse(xy[0], NumberStyles.Integer, CultureInfo.InvariantCulture, out var x) &&
                    int.TryParse(xy[1], NumberStyles.Integer, CultureInfo.InvariantCulture, out var y))
                {
                    return new NativePoint(x, y);
                }
            }
            return base.ConvertFrom(context, culture, value);
        }

        /// <inheritdoc />
        [Pure]
        public override object ConvertTo(ITypeDescriptorContext context, CultureInfo culture, object value, Type destinationType)
        {
            if (destinationType == typeof(string) && value is NativePoint nativePoint)
            {
                return $"{nativePoint.X},{nativePoint.Y}";
            }
            return base.ConvertTo(context, culture, value, destinationType);
        }
    }
}
