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
using System.IO;
using System.Reflection;
using Microsoft.Win32;

namespace Dapplo.Windows.EmbeddedBrowser
{
    /// <summary>
    /// Helper class for the Internet Explorer version
    /// </summary>
    public static class InternetExplorerVersion
    {
        // Internet explorer Registry key
        private const string IeKey = @"Software\Microsoft\Internet Explorer";

        /// <summary>
        ///     Get the version of the Internet Explorer
        /// </summary>
        /// <returns>int with browser version</returns>
        public static int Version
        {
            get
            {
                var maxVer = 0;
                using (var ieKey = Registry.LocalMachine.OpenSubKey(IeKey, false))
                {
                    if (ieKey == null)
                    {
                        return maxVer;
                    }

                    foreach (var value in new[] { "svcVersion", "svcUpdateVersion", "Version", "W2kVersion" })
                    {
                        var objVal = ieKey.GetValue(value, "0");
                        var strVal = Convert.ToString(objVal);

                        var iPos = strVal.IndexOf('.');
                        if (iPos > 0)
                        {
                            strVal = strVal.Substring(0, iPos);
                        }

                        if (int.TryParse(strVal, out var res))
                        {
                            maxVer = Math.Max(maxVer, res);
                        }
                    }
                }

                return maxVer;
            }
        }

        /// <summary>
        ///     Get the highest possible version for the embedded browser
        /// </summary>
        /// <param name="ignoreDoctype">true to ignore the doctype when loading a page</param>
        /// <returns>IE Feature</returns>
        public static int GetEmbVersion(bool ignoreDoctype = true)
        {
            var ieVersion = Version;

            if (ieVersion > 9)
            {
                return ieVersion * 1000 + (ignoreDoctype ? 1 : 0);
            }

            if (ieVersion > 7)
            {
                return ieVersion * 1111;
            }

            return 7000;
        }

        /// <summary>
        ///     Change browser version to the highest possible
        /// </summary>
        /// <param name="ignoreDoctype">true to ignore the doctype when loading a page</param>
        public static void ChangeEmbeddedVersion(bool ignoreDoctype = true)
        {
            var applicationName = Path.GetFileNameWithoutExtension(Assembly.GetEntryAssembly().Location);
            ChangeEmbeddedVersion(applicationName, ignoreDoctype);
        }

        /// <summary>
        ///     Change the browser version for the specified application
        /// </summary>
        /// <param name="applicationName">Name of the process</param>
        /// <param name="ignoreDoctype">true to ignore the doctype when loading a page</param>
        public static void ChangeEmbeddedVersion(string applicationName, bool ignoreDoctype = true)
        {
            ChangeEmbeddedVersion(applicationName, GetEmbVersion(ignoreDoctype));
        }

        /// <summary>
        ///     Fix the browser version for the specified application
        /// </summary>
        /// <param name="applicationName">Name of the process</param>
        /// <param name="ieVersion">
        ///     Version, see
        ///     <a href="https://msdn.microsoft.com/en-us/library/ee330730(v=vs.85).aspx#browser_emulation">Browser Emulation</a>
        /// </param>
        public static void ChangeEmbeddedVersion(string applicationName, int ieVersion)
        {
            ModifyRegistry("HKEY_CURRENT_USER", applicationName + ".exe", ieVersion);
#if DEBUG
            ModifyRegistry("HKEY_CURRENT_USER", applicationName + ".vshost.exe", ieVersion);
#endif
        }

        /// <summary>
        ///     Make the change to the registry
        /// </summary>
        /// <param name="root">HKEY_CURRENT_USER or something</param>
        /// <param name="applicationName">Name of the executable</param>
        /// <param name="ieFeatureVersion">Version to use</param>
        private static void ModifyRegistry(string root, string applicationName, int ieFeatureVersion)
        {
            var regKey = $@"{root}\Software\Microsoft\Internet Explorer\Main\FeatureControl\FEATURE_BROWSER_EMULATION";
            try
            {
                Registry.SetValue(regKey, applicationName, ieFeatureVersion);
            }
            catch (Exception)
            {
                // some config will hit access rights exceptions
                // this is why we try with both LOCAL_MACHINE and CURRENT_USER
                // Ignore
            }
        }
    }
}
