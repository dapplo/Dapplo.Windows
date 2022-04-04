// Copyright (c) Dapplo and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
using System;
using System.ComponentModel;
using System.Globalization;
using Dapplo.Windows.Common.Structs;
using Dapplo.Windows.Common.TypeConverters;
using Dapplo.Windows.User32.Enums;
using Dapplo.Windows.User32.Structs;

namespace Dapplo.Windows.User32.TypeConverters;

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