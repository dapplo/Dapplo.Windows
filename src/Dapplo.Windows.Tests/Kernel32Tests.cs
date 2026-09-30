// Copyright (c) Dapplo and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
using Dapplo.Log;
using Dapplo.Log.XUnit;
using System;
using Dapplo.Windows.Kernel32;
using Dapplo.Windows.Kernel32.Enums;
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
    public void Test_IsRunningAsUwp()
    {
        Assert.False(PackageInfo.HasPackageIdentity);
    }

    [Fact]
    public void Test_GetOsVersionInfoEx()
    {
        var osVersionInfoEx= OsVersionInfoEx.Create();
        Assert.True(Kernel32Api.GetVersionEx(ref osVersionInfoEx));
        //Assert.NotEmpty(osVersionInfoEx.ServicePackVersion);
    }

    [Fact]
    public void Test_ProcessAccessRights_Values()
    {
        Assert.Equal(0x1000u, (uint)ProcessAccessRights.QueryLimitedInformation);
        Assert.Equal(0x400u, (uint)ProcessAccessRights.QueryInformation);
        Assert.Equal(0x800u, (uint)ProcessAccessRights.SuspendResume);
        Assert.Equal(0x1FFFFFu, (uint)ProcessAccessRights.All);
    }

    [Fact]
    public void Test_LoadLibrary()
    {
        // kernel32 is always loaded, so this only verifies that the file name is marshalled correctly (as UTF-16)
        Assert.NotEqual(IntPtr.Zero, Kernel32Api.LoadLibrary("kernel32.dll"));
    }

}