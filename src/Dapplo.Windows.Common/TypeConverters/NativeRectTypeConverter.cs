//  Dapplo - building blocks for desktop applications
//  Copyright (C) 2017-2019  Dapplo
// 
//  For more information see: http://dapplo.net/
//  Dapplo repositories are hosted on GitHub: https://github.com/dapplo
// 
//  This file is part of Dapplo.Windows
// 
//  Dapplo.Windows is free software: you can redistribute it and/or modify
//  it under the terms of the GNU Lesser General Public License as published by
//  the Free Software Foundation, either version 3 of the License, or
//  (at your option) any later version.
// 
//  Dapplo.Windows is distributed in the hope that it will be useful,
//  but WITHOUT ANY WARRANTY; without even the implied warranty of
//  MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
//  GNU Lesser General Public License for more details.
// 
//  You should have a copy of the GNU Lesser General Public License
//  along with Dapplo.Windows. If not, see <http://www.gnu.org/licenses/lgpl.txt>.

using System;
using System.ComponentModel;
using System.Globalization;
using Dapplo.Windows.Common.Structs;

namespace Dapplo.Windows.Common.TypeConverters
{
    /// <summary>
    /// This implements a TypeConverter for the NativeRect structur
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
                return $"{nativeRect.Left},{nativeRect.Top},{nativeRect.Width},{nativeRect.Height}";
            }
            return base.ConvertTo(context, culture, value, destinationType);
        }
    }
}
