// Copyright (c) Dapplo and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
using Dapplo.Log;
using Dapplo.Log.XUnit;
using Xunit;
using Dapplo.Windows.Citrix;

namespace Dapplo.Windows.Tests;

public class CitrixTests
{
    public CitrixTests(ITestOutputHelper testOutputHelper)
    {
        LogSettings.RegisterDefaultLogger<XUnitLogger>(LogLevels.Verbose, testOutputHelper);
    }

    /// <summary>
    ///     Assume that we are not running on Citrix
    /// </summary>
    /// <returns></returns>
    [Fact]
    public void TestCitrix_NotAvailable()
    {
        Assert.False(WinFrame.IsAvailabe, "We are running on Citrix????");
            
    }
}