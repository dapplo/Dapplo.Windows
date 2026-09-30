// Copyright (c) Dapplo and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
using Dapplo.Log;
using Dapplo.Log.XUnit;
using Xunit;
using Dapplo.Windows.Shell32;

namespace Dapplo.Windows.Tests;

public class Shell32Tests
{
    public Shell32Tests(ITestOutputHelper testOutputHelper)
    {
        LogSettings.RegisterDefaultLogger<XUnitLogger>(LogLevels.Verbose, testOutputHelper);
    }

    /// <summary>
    ///     Test AppBar, this needs a desktop with a taskbar
    /// </summary>
    [Fact]
    [Trait("Category", "Interactive")]
    public void TestAppBar()
    {
        Assert.True(Shell32Api.TryGetTaskbarPosition(out var appBarData));
        Assert.False(appBarData.Bounds.IsEmpty);
    }
}