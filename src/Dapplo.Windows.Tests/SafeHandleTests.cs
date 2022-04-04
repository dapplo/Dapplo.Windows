// Copyright (c) Dapplo and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
using System;
using Dapplo.Log;
using Dapplo.Log.XUnit;
using Dapplo.Windows.Gdi32;
using Dapplo.Windows.Gdi32.SafeHandles;
using Xunit;
using Xunit.Abstractions;

namespace Dapplo.Windows.Tests;

public class SafeHandleTests
{
    public SafeHandleTests(ITestOutputHelper testOutputHelper)
    {
        LogSettings.RegisterDefaultLogger<XUnitLogger>(LogLevels.Verbose, testOutputHelper);
    }

    [Fact]
    public void TestCreateCompatibleDc()
    {
        using (var desktopDcHandle = SafeWindowDcHandle.FromDesktop())
        {
            Assert.False(desktopDcHandle.IsInvalid);

            // create a device context we can copy to
            using (var safeCompatibleDcHandle = Gdi32Api.CreateCompatibleDC(desktopDcHandle))
            {
                Assert.False(safeCompatibleDcHandle.IsInvalid);
            }
        }
    }

    [Fact]
    public void TestSafeWindowDcHandle_IntPtrZero()
    {
        using (var safeDctHandle = SafeWindowDcHandle.FromWindow(IntPtr.Zero))
        {
            Assert.Null(safeDctHandle);
        }
    }
}