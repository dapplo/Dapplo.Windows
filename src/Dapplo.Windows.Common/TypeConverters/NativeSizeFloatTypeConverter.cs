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
    /// This implements a TypeConverter for the NativeSizeFloat structur
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
                string[] hw = sizeStringValue.Split(',');
                if (hw.Length == 2 &&
                    float.TryParse(hw[0], NumberStyles.Number, CultureInfo.InvariantCulture, out var h) &&
                    float.TryParse(hw[1], NumberStyles.Number, CultureInfo.InvariantCulture, out var w))
                {
                    return new NativeSizeFloat(h, w);
                }
            }
            return base.ConvertFrom(context, culture, value);
        }

        /// <inheritdoc />
        public override object ConvertTo(ITypeDescriptorContext context, CultureInfo culture, object value, Type destinationType)
        {
            if (destinationType == typeof(string) && value is NativeSizeFloat nativeSizeFloat)
            {
                return $"{nativeSizeFloat.Width.ToString(CultureInfo.InvariantCulture)},{nativeSizeFloat.Height.ToString(CultureInfo.InvariantCulture)}";
            }
            return base.ConvertTo(context, culture, value, destinationType);
        }
    }
}
