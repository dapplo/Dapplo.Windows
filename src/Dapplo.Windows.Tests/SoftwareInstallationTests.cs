// Copyright (c) Dapplo and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
using System.Linq;
using Dapplo.Log;
using Dapplo.Log.XUnit;
using Dapplo.Windows.Software;
using Xunit;
using Xunit.Abstractions;

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