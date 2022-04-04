// Copyright (c) Dapplo and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.ComponentModel;

namespace Dapplo.Windows.Common.Extensions;

/// <summary>
/// Some enum extensions used throughout the code
/// </summary>
public static class EnumExtensions
{
    /// <summary>
    /// Get an attribute of a certain type, placed upon an enum value
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="enumVal"></param>
    /// <returns></returns>
    public static T GetAttributeOfType<T>(this Enum enumVal) where T : Attribute
    {
        var type = enumVal.GetType();
        var memInfo = type.GetMember(enumVal.ToString());
        var attributes = memInfo[0].GetCustomAttributes(typeof(T), false);
        return attributes.Length > 0 ? (T)attributes[0] : null;
    }

    /// <summary>
    /// Get the description of an enum
    /// </summary>
    /// <param name="enumVal">Enum</param>
    /// <returns>string</returns>
    public static string GetEnumDescription(this Enum enumVal)
    {
        var descriptionAttribute = enumVal.GetAttributeOfType<DescriptionAttribute>();
        return descriptionAttribute?.Description;
    }
}