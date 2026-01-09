// Copyright (c) Dapplo and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
using Dapplo.Log;
using Dapplo.Log.XUnit;
using Dapplo.Windows.DesktopWindowsManager;
using Xunit;

namespace Dapplo.Windows.Tests;

public class DwmTest
{
    public DwmTest(ITestOutputHelper testOutputHelper)
    {
        LogSettings.RegisterDefaultLogger<XUnitLogger>(LogLevels.Verbose, testOutputHelper);
    }

    /// <summary>
    ///     Test is Dwm is Enabled
    /// </summary>
    /// <returns></returns>
    //[Fact]
    private void TestDwmEnabled()
    {
        Assert.True(DwmApi.IsDwmEnabled);
    }
}