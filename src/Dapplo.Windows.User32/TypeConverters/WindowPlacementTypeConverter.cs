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
using Dapplo.Windows.Common.TypeConverters;
using Dapplo.Windows.User32.Enums;
using Dapplo.Windows.User32.Structs;

namespace Dapplo.Windows.User32.TypeConverters
{
    /// <summary>
    /// This implements a TypeConverter for the WindowPlacement structur
    /// </summary>
    public class WindowPlacementTypeConverter : TypeConverter
    {
        private readonly NativePointTypeConverter _nativePointTypeConverter = TypeDescriptor.GetConverter(typeof(NativePoint)) as NativePointTypeConverter;
        private readonly NativeRectTypeConverter _nativeRectTypeConverter = TypeDescriptor.GetConverter(typeof(NativeRect)) as NativeRectTypeConverter;


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
            if (value is string windowPlacementString)
            {
                string[] cmdMinMaxNormal = windowPlacementString.Split('|');
                if (cmdMinMaxNormal.Length == 4 && Enum.TryParse(cmdMinMaxNormal[0], true, out ShowWindowCommands showCommand))
                {
                    var windowPlacement = WindowPlacement.Create();
                    windowPlacement.ShowCmd = showCommand;
                    windowPlacement.MinPosition = (NativePoint?)_nativePointTypeConverter.ConvertFromInvariantString(cmdMinMaxNormal[1]) ?? NativePoint.Empty;
                    windowPlacement.MaxPosition = (NativePoint?)_nativePointTypeConverter.ConvertFromInvariantString(cmdMinMaxNormal[2]) ?? NativePoint.Empty;
                    windowPlacement.NormalPosition = (NativeRect?)_nativeRectTypeConverter.ConvertFromInvariantString(cmdMinMaxNormal[3]) ?? NativeRect.Empty;

                    return windowPlacement;
                }
            }
            return base.ConvertFrom(context, culture, value);
        }

        /// <inheritdoc />
        public override object ConvertTo(ITypeDescriptorContext context, CultureInfo culture, object value, Type destinationType)
        {
            if (destinationType == typeof(string) && value is WindowPlacement windowPlacement)
            {
                return $"{windowPlacement.ShowCmd}|{_nativePointTypeConverter.ConvertToInvariantString(windowPlacement.MinPosition)}|{_nativePointTypeConverter.ConvertToInvariantString(windowPlacement.MaxPosition)}|{_nativeRectTypeConverter.ConvertToInvariantString(windowPlacement.NormalPosition)}";
            }
            return base.ConvertTo(context, culture, value, destinationType);
        }
    }
}
