// Copyright (c) Dapplo and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
using Dapplo.Log;
using Dapplo.Log.XUnit;
using Dapplo.Windows.Kernel32;
using Dapplo.Windows.Kernel32.Structs;
using Xunit;

namespace Dapplo.Windows.Tests;

public class Kernel32Tests
{
    public Kernel32Tests(ITestOutputHelper testOutputHelper)
    {
        LogSettings.RegisterDefaultLogger<XUnitLogger>(LogLevels.Verbose, testOutputHelper);
    }

    [Fact]
    private void Test_IsRunningAsUwp()
    {
        Assert.False(PackageInfo.IsRunningOnUwp);
    }

    [Fact]
    private void Test_GetOsVersionInfoEx()
    {
        var osVersionInfoEx= OsVersionInfoEx.Create();
        Assert.True(Kernel32Api.GetVersionEx(ref osVersionInfoEx));
        //Assert.NotEmpty(osVersionInfoEx.ServicePackVersion);
    }

}