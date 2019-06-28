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

// ReSharper disable InconsistentNaming
// ReSharper disable UnusedMember.Global
// ReSharper disable UnusedAutoPropertyAccessor.Global
// ReSharper disable CommentTypo
#pragma warning disable 1591

namespace Dapplo.Windows.Software
{
    /// <summary>
    /// This class contains all known fields which describe installed software, details can be found hee: <a href="https://docs.microsoft.com/en-us/windows/desktop/msi/uninstall-registry-key">Uninstall Registry Key</a>
    /// </summary>
    public class SoftwareDetails
    {
        /// <summary>
        /// Application's product code GUID
        /// </summary>
        public Guid? Id { get; set; }

        /// <summary>
        /// See <a href="https://docs.microsoft.com/en-us/windows/desktop/msi/arpauthorizedcdfprefix">ARPAUTHORIZEDCDFPREFIX property</a>
        /// </summary>
        public string AuthorizedCDFPrefix { get; set; }
 
        /// <summary>
        /// See <a href="https://docs.microsoft.com/en-us/windows/desktop/msi/arpcontact">ARPCONTACT property</a>
        /// </summary>
        public string Contact { get; set; }

        /// <summary>
        /// Comments provided to the Add or Remove Programs control panel.
        /// </summary>
        public string Comments { get; set; }

        /// <summary>
        /// ProductName property
        /// </summary>
        public string DisplayName { get; set; }

        /// <summary>
        /// Derived from ProductVersion property
        /// See <a href="https://docs.microsoft.com/en-us/windows/desktop/msi/productversion">ProductVersion property</a>
        /// </summary>
        public string DisplayVersion { get; set; }

        /// <summary>
        /// Determined and set by the Windows Installer.
        /// </summary>
        public int EstimatedSize { get; set; }

        /// <summary>
        /// See <a href="https://docs.microsoft.com/en-us/windows/desktop/msi/arphelplink">ARPHELPLINK property</a>
        /// </summary>
        public string HelpLink { get; set; }

        /// <summary>
        /// See <a href="https://docs.microsoft.com/en-us/windows/desktop/msi/arphelptelephone">ARPHELPTELEPHONE property</a>
        /// </summary>
        public string HelpTelephone { get; set; }

        /// <summary>
        /// The last time this product received service.
        /// The value of this property is replaced each time a patch is applied or removed from the product or the /v Command-Line Option is used to repair the product.
        /// If the product has received no repairs or patches this property contains the time this product was installed on this computer.
        /// </summary>
        public string InstallDate { get; set; }

        public bool SystemComponent { get; set; }

        /// <summary>
        /// See <a href="https://docs.microsoft.com/en-us/windows/desktop/msi/arpinstalllocation">ARPINSTALLLOCATION property</a>
        /// </summary>
        public string InstallLocation { get; set; }
        /// <summary>
        /// See <a href="https://docs.microsoft.com/en-us/windows/desktop/msi/sourcedir">SourceDir property</a>
        /// </summary>
        public string InstallSource { get; set; }

        /// <summary>
        /// See <a href="https://docs.microsoft.com/en-us/windows/desktop/msi/productlanguage">ProductLanguage property</a>
        /// </summary>
        public int Language { get; set; }

        /// <summary>
        /// Determined and set by the Windows Installer.
        /// </summary>
        public string ModifyPath { get; set; }

        /// <summary>
        /// The Manufacturer property is the name of the manufacturer for the product. It is advertised as a product property.
        /// </summary>
        public string Publisher { get; set; }

        public string Readme { get; set; }

        public long Size { get; set; }

        /// <summary>
        /// Determined and set by Windows Installer.
        /// </summary>
        public string UninstallString { get; set; }

        /// <summary>
        /// See <a href="https://docs.microsoft.com/en-us/windows/desktop/msi/arpurlinfoabout">ARPURLINFOABOUT property</a>
        /// </summary>
        public string URLInfoAbout { get; set; }

        /// <summary>
        /// See <a href="https://docs.microsoft.com/en-us/windows/desktop/msi/arpurlupdateinfo">ARPURLUPDATEINFO property</a>
        /// </summary>
        public string URLUpdateInfo { get; set; }

        /// <summary>
        /// Derived from ProductVersion property
        /// See <a href="https://docs.microsoft.com/en-us/windows/desktop/msi/productversion">ProductVersion property</a>
        /// </summary>
        public int Version { get; set; }
        /// <summary>
        /// Derived from ProductVersion property
        /// See <a href="https://docs.microsoft.com/en-us/windows/desktop/msi/productversion">ProductVersion property</a>
        /// </summary>
        public int VersionMajor { get; set; }
        /// <summary>
        /// Derived from ProductVersion property
        /// See <a href="https://docs.microsoft.com/en-us/windows/desktop/msi/productversion">ProductVersion property</a>
        /// </summary>
        public int VersionMinor { get; set; }

        public bool WindowsInstaller { get; set; }

        public bool NoModify { get; set; }

        public bool NoRepair { get; set; }
        
        /// <inheritdoc />
        public override string ToString()
        {
            return $"{Id ?? default}: {DisplayName} - {DisplayVersion}";
        }
    }
}
