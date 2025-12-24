// Copyright (c) Dapplo and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
using System.Diagnostics;
using System.Linq;
using System.Windows.Media.Imaging;
using Dapplo.Log;
using Dapplo.Log.XUnit;
using Dapplo.Windows.Desktop;
using Dapplo.Windows.Icons;
using Dapplo.Windows.User32;
using Xunit;
using Xunit.Abstractions;

namespace Dapplo.Windows.Tests;

public class IconTests
{
    public IconTests(ITestOutputHelper testOutputHelper)
    {
        LogSettings.RegisterDefaultLogger<XUnitLogger>(LogLevels.Verbose, testOutputHelper);
    }

    /// <summary>
    ///     Test getting an Icon for a top level window
    /// </summary>
    [WpfFact]
    public void TestIcon_GetIcon()
    {
        // Start a process to test against
        using (var process = Process.Start("notepad.exe"))
        {
            // Make sure it's started
            Assert.NotNull(process);
            // Wait until the process started it's message pump (listening for input)
            process.WaitForInputIdle();
            User32Api.SetWindowText(process.MainWindowHandle, "TestIcon_GetIcon");

            var window = InteropWindowQuery.GetTopLevelWindows().First();
            var icon = window.GetIcon<BitmapSource>();
            Assert.NotNull(icon);

            // Kill the process
            process.Kill();
        }
    }

    /// <summary>
    ///     Test getting an Icon for the desktop, which doesn't have one
    /// </summary>
    [WpfFact]
    public void TestIcon_GetIcon_Null()
    {
        var window = InteropWindowQuery.GetDesktopWindow();
        var icon = window.GetIcon<BitmapSource>();

        Assert.Null(icon);
    }

    /// <summary>
    ///     Test getting system-preferred icon sizes
    /// </summary>
    [Fact]
    public void TestIcon_GetSystemIconSizes()
    {
        // Test small icon metrics
        var smallWidth = IconHelper.GetSmallIconWidth();
        var smallHeight = IconHelper.GetSmallIconHeight();
        Assert.True(smallWidth > 0, "Small icon width should be greater than 0");
        Assert.True(smallHeight > 0, "Small icon height should be greater than 0");

        // Test standard/large icon metrics
        var standardWidth = IconHelper.GetStandardIconWidth();
        var standardHeight = IconHelper.GetStandardIconHeight();
        Assert.True(standardWidth > 0, "Standard icon width should be greater than 0");
        Assert.True(standardHeight > 0, "Standard icon height should be greater than 0");

        // Standard icons should typically be larger than or equal to small icons
        Assert.True(standardWidth >= smallWidth, "Standard icon width should be >= small icon width");
        Assert.True(standardHeight >= smallHeight, "Standard icon height should be >= small icon height");

        // Test icon spacing metrics
        var spacingWidth = IconHelper.GetIconSpacingWidth();
        var spacingHeight = IconHelper.GetIconSpacingHeight();
        Assert.True(spacingWidth > 0, "Icon spacing width should be greater than 0");
        Assert.True(spacingHeight > 0, "Icon spacing height should be greater than 0");

        // Icon spacing should be greater than or equal to standard icon size
        Assert.True(spacingWidth >= standardWidth, "Icon spacing width should be >= standard icon width");
        Assert.True(spacingHeight >= standardHeight, "Icon spacing height should be >= standard icon height");
    }

    /// <summary>
    ///     Test getting system icon size using the helper method
    /// </summary>
    [Fact]
    public void TestIcon_GetSystemIconSize()
    {
        var smallSize = IconHelper.GetSystemIconSize(Icons.Enums.IconMetricSize.SmallIcon);
        Assert.True(smallSize.Width > 0, "Small icon size width should be greater than 0");
        Assert.True(smallSize.Height > 0, "Small icon size height should be greater than 0");

        var standardSize = IconHelper.GetSystemIconSize(Icons.Enums.IconMetricSize.StandardIcon);
        Assert.True(standardSize.Width > 0, "Standard icon size width should be greater than 0");
        Assert.True(standardSize.Height > 0, "Standard icon size height should be greater than 0");

        // Standard should be >= small
        Assert.True(standardSize.Width >= smallSize.Width, "Standard size width should be >= small size width");
        Assert.True(standardSize.Height >= smallSize.Height, "Standard size height should be >= small size height");
    }

    /// <summary>
    ///     Test LoadIconMetric with system stock icons
    /// </summary>
    [WpfFact]
    public void TestIcon_LoadIconMetric()
    {
        // IDI_APPLICATION is 32512
        var idiApplication = new IntPtr(32512);

        // Load small icon using LoadIconMetric
        var smallIcon = IconHelper.LoadIconWithSystemMetrics<BitmapSource>(IntPtr.Zero, idiApplication, Icons.Enums.IconMetricSize.SmallIcon);
        Assert.NotNull(smallIcon);

        var expectedSmallSize = IconHelper.GetSystemIconSize(Icons.Enums.IconMetricSize.SmallIcon);
        Assert.Equal(expectedSmallSize.Width, smallIcon.PixelWidth);
        Assert.Equal(expectedSmallSize.Height, smallIcon.PixelHeight);

        // Load standard icon using LoadIconMetric
        var standardIcon = IconHelper.LoadIconWithSystemMetrics<BitmapSource>(IntPtr.Zero, idiApplication, Icons.Enums.IconMetricSize.StandardIcon);
        Assert.NotNull(standardIcon);

        var expectedStandardSize = IconHelper.GetSystemIconSize(Icons.Enums.IconMetricSize.StandardIcon);
        Assert.Equal(expectedStandardSize.Width, standardIcon.PixelWidth);
        Assert.Equal(expectedStandardSize.Height, standardIcon.PixelHeight);
    }

    /// <summary>
    ///     Test LoadIconWithScaleDown with system stock icons
    /// </summary>
    [WpfFact]
    public void TestIcon_LoadIconWithScaleDown()
    {
        // IDI_QUESTION is 32514
        var idiQuestion = new IntPtr(32514);

        // Load icon at a specific size
        const int targetSize = 24;
        var icon = IconHelper.LoadIconWithScaleDown<BitmapSource>(IntPtr.Zero, idiQuestion, targetSize, targetSize);
        Assert.NotNull(icon);

        // The icon should be scaled to the requested size
        Assert.Equal(targetSize, icon.PixelWidth);
        Assert.Equal(targetSize, icon.PixelHeight);
    }
}