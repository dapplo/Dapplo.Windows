// Copyright (c) Dapplo and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
using Dapplo.Log;
using Dapplo.Log.XUnit;
using Dapplo.Windows.Kernel32;
using Dapplo.Windows.Kernel32.Structs;
using Xunit;

namespace Dapplo.Windows.Tests;

public class VersionTests
{
    public VersionTests(ITestOutputHelper testOutputHelper)
    {
        LogSettings.RegisterDefaultLogger<XUnitLogger>(LogLevels.Verbose, testOutputHelper);
    }

    /// <summary>
    ///     Test GetVersionEx
    /// </summary>
    [Fact]
    public void TestOsVersionInfoEx()
    {
        var osVersionInfoEx = OsVersionInfoEx.Create();
        Assert.True(Kernel32Api.GetVersionEx(ref osVersionInfoEx));
        Assert.True(osVersionInfoEx.MajorVersion >= 6);
    }
}