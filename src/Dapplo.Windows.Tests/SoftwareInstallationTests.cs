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

#region using

using System.Linq;
using Dapplo.Log;
using Dapplo.Log.XUnit;
using Dapplo.Windows.Software;
using Xunit;
using Xunit.Abstractions;

#endregion

namespace Dapplo.Windows.Tests
{
    public class SoftwareInstallationTests
    {
        private static readonly LogSource Log = new LogSource();
        public SoftwareInstallationTests(ITestOutputHelper testOutputHelper)
        {
            LogSettings.RegisterDefaultLogger<XUnitLogger>(LogLevels.Verbose, testOutputHelper);
        }

        [Fact]
        private void Test_InstalledSoftware()
        {
            var software = InstallationInformation.InstalledSoftware().OrderBy(details => details.DisplayName).ToList();
            Assert.True(software.Count > 0);
            foreach (var softwareDetails in software)
            {
                Log.Debug().WriteLine(softwareDetails.ToString(), null);
                if (!string.IsNullOrEmpty(softwareDetails.HelpLink))
                {
                    Log.Debug().WriteLine("Help - {0}" , softwareDetails.HelpLink);
                }
            }
        }
    }
}