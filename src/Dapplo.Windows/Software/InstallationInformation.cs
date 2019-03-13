using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Dapplo.Log;
using Microsoft.Win32;

namespace Dapplo.Windows.Software
{
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
                            var value = Convert.ChangeType(propertyValue, propertyInfo.PropertyType);
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
        /// Retrieves all the installed software
        /// </summary>
        /// <returns>IEnumerable with SoftwareDetails</returns>
        public static IEnumerable<SoftwareDetails> InstalledSoftware()
        {
            string uninstallKey = @"SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall";
            using (var registryKey = Registry.LocalMachine.OpenSubKey(uninstallKey))
            {
                if (registryKey == null)
                {
                    yield break;
                }
                foreach (var subKeyName in registryKey.GetSubKeyNames())
                {
                    using (var subKey = registryKey.OpenSubKey(subKeyName))
                    {
                        yield return MapFromRegistryKey(subKeyName, subKey);
                    }
                }
            }
        }
    }
}
