// Copyright (c) Dapplo and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
using System;
using System.Linq;
using Dapplo.Log;
using Dapplo.Log.XUnit;
using Xunit;
using Dapplo.Windows.Desktop;
using Dapplo.Windows.Enums;

namespace Dapplo.Windows.Tests;

public class InteropWindowTests
{
    public InteropWindowTests(ITestOutputHelper testOutputHelper)
    {
        LogSettings.RegisterDefaultLogger<XUnitLogger>(LogLevels.Verbose, testOutputHelper);
    }

    /// <summary>
    ///    Fill without ForceUpdate must use the cached values, and only retrieve what was requested
    /// </summary>
    [Fact]
    public void Test_Fill_UsesCachedValues()
    {
        var interopWindow = InteropWindowFactory.CreateFor(IntPtr.Zero);
        interopWindow.Caption = "cached";

        interopWindow.Fill(InteropWindowRetrieveSettings.Caption);

        Assert.Equal("cached", interopWindow.Caption);
        // Maximized was not requested, so it should not have been retrieved
        Assert.Null(interopWindow.IsMaximized);
    }

    /// <summary>
    ///    Fill with ForceUpdate must refresh the cached values
    /// </summary>
    [Fact]
    public void Test_Fill_ForceUpdate()
    {
        var interopWindow = InteropWindowFactory.CreateFor(IntPtr.Zero);
        interopWindow.Caption = "cached";

        interopWindow.Fill(InteropWindowRetrieveSettings.Caption | InteropWindowRetrieveSettings.ForceUpdate);

        Assert.NotEqual("cached", interopWindow.Caption);
    }

    /// <summary>
    ///    Test some of the InteropWindowQuery logic by finding the taskbar and the clock on it.
    /// </summary>
    /// <remarks>Interactive: this needs a desktop with a taskbar which shows the clock.</remarks>
    [Fact]
    [Trait("Category", "Interactive")]
    public void TestTaskbarInfo()
    {
        var systray = InteropWindowQuery.GetTopWindows().FirstOrDefault(window => window.GetClassname() == "Shell_TrayWnd");
        Assert.NotNull(systray);
        // The notification area exists on Windows 10 and 11 (the Windows 10 TrayClockWClass is XAML content on Windows 11)
        var notificationArea = systray.GetDescendants().FirstOrDefault(window => window.GetClassname() == "TrayNotifyWnd");
        Assert.NotNull(notificationArea);

        var info = notificationArea.GetInfo();
        Assert.True(info.ClientBounds.Width * info.ClientBounds.Height > 0);
    }

}