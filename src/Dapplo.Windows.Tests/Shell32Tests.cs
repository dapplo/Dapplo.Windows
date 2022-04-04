// Copyright (c) Dapplo and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
using Dapplo.Log;
using Dapplo.Log.XUnit;
using Xunit;
using Xunit.Abstractions;
using Dapplo.Windows.Shell32;

namespace Dapplo.Windows.Tests;

public class Shell32Tests
{
    public Shell32Tests(ITestOutputHelper testOutputHelper)
    {
        LogSettings.RegisterDefaultLogger<XUnitLogger>(LogLevels.Verbose, testOutputHelper);
    }

    /// <summary>
    ///     Test AppBarr
    /// </summary>
    /// <returns></returns>
    [Fact]
    private void TestAppBar()
    {
        var appBarData = Shell32Api.TaskbarPosition;
        Assert.False(appBarData.Bounds.IsEmpty);
    }
}