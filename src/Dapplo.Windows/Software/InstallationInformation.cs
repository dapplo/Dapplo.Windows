// Copyright (c) Dapplo and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;
using Dapplo.Log;
using Microsoft.Win32;

namespace Dapplo.Windows.Software;

/// <summary>
/// a helper class to evaluate the installed software
/// </summary>
public static class InstallationInformation
{
    private static readonly LogSource Log = new LogSource();
    private static readonly PropertyInfo[] SoftwareDetailsPropertyInfos = typeof(SoftwareDetails).GetProperties(BindingFlags.Instance | BindingFlags.Public);

    /// <summary>
    /// Helper method to convert from a RegistryKey object to a SoftwareDetails class
    /// </summary>
    /// <param name="subkeyName">string</param>
    /// <param name="subKey">RegistryKey</param>
    /// <returns>SoftwareDetails</returns>
    private static SoftwareDetails MapFromRegistryKey(string subkeyName, RegistryKey subKey)
    {
        if (subKey == null)
        {
            return null;
        }

        var valueNames = subKey.GetValueNames().ToList();
        // Map values to Software Class, take the ID from the skName?
        var softwareDetails = new SoftwareDetails();

        if (Guid.TryParse(subkeyName, out Guid id))
        {
            softwareDetails.Id = id;
        }
        softwareDetails.DisplayName = subkeyName;

        foreach (var propertyInfo in SoftwareDetailsPropertyInfos)
        {
            if (!valueNames.Contains(propertyInfo.Name))
            {
                continue;
            }

            var propertyValue = subKey.GetValue(propertyInfo.Name);
            if (propertyValue == null)
            {
                continue;
            }
            try
            {
                switch (subKey.GetValueKind(propertyInfo.Name))
                {
                    case RegistryValueKind.DWord:
                        var intValue = Convert.ToInt32(propertyValue);
                        if (propertyInfo.PropertyType == typeof(bool))
                        {
                            propertyInfo.SetValue(softwareDetails, intValue == 1);
                        }
                        else
                        {
                            propertyInfo.SetValue(softwareDetails, intValue);
                        }
                        break;
                    case RegistryValueKind.QWord:
                        var longValue = Convert.ToInt64(propertyValue);
                        if (propertyInfo.PropertyType == typeof(bool))
                        {
                            propertyInfo.SetValue(softwareDetails, longValue == 1);
                        }
                        else
                        {
                            propertyInfo.SetValue(softwareDetails, longValue);
                        }
                        break;
                    default:
                        string stringValue = propertyValue as string;
                        if (string.IsNullOrEmpty(stringValue))
                        {
                            continue;
                        }
                        var value = Convert.ChangeType(propertyValue, propertyInfo.PropertyType, CultureInfo.InvariantCulture);
                        propertyInfo.SetValue(softwareDetails, value);
                        break;
                }
            }
            catch (Exception ex)
            {
                Log.Warn().WriteLine(ex, "Couldn't parse value {0} to {1} for product {2}", propertyValue, propertyInfo.Name, softwareDetails.DisplayName);
            }
        }
        return softwareDetails;
    }

    /// <summary>
    /// Retrieves all the installed software: the 64-bit and 32-bit machine wide installations and the installations for the current user.
    /// </summary>
    /// <returns>IEnumerable with SoftwareDetails, never with null entries</returns>
    public static IEnumerable<SoftwareDetails> InstalledSoftware()
    {
        const string uninstallKey = @"SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall";
        // On a 32-bit OS there is only one view, requesting the 64-bit view would return the same keys twice
        var machineViews = Environment.Is64BitOperatingSystem ? new[] { RegistryView.Registry64, RegistryView.Registry32 } : new[] { RegistryView.Default };
        foreach (var machineView in machineViews)
        {
            foreach (var softwareDetails in InstalledSoftware(RegistryHive.LocalMachine, machineView, uninstallKey))
            {
                yield return softwareDetails;
            }
        }
        // The per-user installations, HKCU is shared between the views
        foreach (var softwareDetails in InstalledSoftware(RegistryHive.CurrentUser, RegistryView.Default, uninstallKey))
        {
            yield return softwareDetails;
        }
    }

    /// <summary>
    /// Retrieves the installed software from the specified hive and view
    /// </summary>
    /// <param name="hive">RegistryHive</param>
    /// <param name="view">RegistryView</param>
    /// <param name="uninstallKey">string with the path of the uninstall key</param>
    /// <returns>IEnumerable with SoftwareDetails</returns>
    private static IEnumerable<SoftwareDetails> InstalledSoftware(RegistryHive hive, RegistryView view, string uninstallKey)
    {
        using var baseKey = RegistryKey.OpenBaseKey(hive, view);
        using var registryKey = baseKey.OpenSubKey(uninstallKey);
        if (registryKey == null)
        {
            yield break;
        }
        foreach (var subKeyName in registryKey.GetSubKeyNames())
        {
            using var subKey = registryKey.OpenSubKey(subKeyName);
            var softwareDetails = MapFromRegistryKey(subKeyName, subKey);
            if (softwareDetails != null)
            {
                yield return softwareDetails;
            }
        }
    }
}