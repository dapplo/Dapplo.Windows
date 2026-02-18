// Copyright (c) Dapplo and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
using Dapplo.Log;
using Dapplo.Log.XUnit;
using Dapplo.Windows.Common.Structs;
using Dapplo.Windows.Dpi;
using Xunit;

namespace Dapplo.Windows.Tests;

public class DpiTests
{
    public DpiTests(ITestOutputHelper testOutputHelper)
    {
        LogSettings.RegisterDefaultLogger<XUnitLogger>(LogLevels.Verbose, testOutputHelper);
    }

    /// <summary>
    ///     Test ScaleWithDpi
    /// </summary>
    [Fact]
    public void Test_ScaleWithDpi()
    {
        var size96 = DpiCalculator.ScaleWithDpi(16, 96);
        Assert.Equal(16, size96);
        var size120 = DpiCalculator.ScaleWithDpi(16, 120);
        Assert.Equal(20, size120);
        var size144 = DpiCalculator.ScaleWithDpi(16, 144);
        Assert.Equal(24, size144);
        var size192 = DpiCalculator.ScaleWithDpi(16, 192);
        Assert.Equal(32, size192);
    }

    /// <summary>
    ///     Test UnscaleWithDpi
    /// </summary>
    [Fact]
    public void Test_UnscaleWithDpi()
    {
        var size96 = DpiCalculator.UnscaleWithDpi(16, 96);
        Assert.Equal(16, size96);
        var size120 = DpiCalculator.UnscaleWithDpi(16, 120);
        Assert.Equal(12, size120);
        var size144 = DpiCalculator.UnscaleWithDpi(16, 144);
        Assert.Equal(10, size144);
        var size192 = DpiCalculator.UnscaleWithDpi(16, 192);
        Assert.Equal(8, size192);
    }

    /// <summary>
    ///     Test scale -> unscale
    /// </summary>
    [Fact]
    public void Test_ScaleWithDpi_UnscaleWithDpi()
    {
        var testSize = new NativeSize(16, 16);
        var size96 = DpiCalculator.ScaleWithDpi(testSize, 96);
        var resultSize96 = DpiCalculator.UnscaleWithDpi(size96, 96);
        Assert.Equal(testSize, resultSize96);

        var size120 = DpiCalculator.ScaleWithDpi(testSize, 120);
        var resultSize120 = DpiCalculator.UnscaleWithDpi(size120, 120);
        Assert.Equal(testSize, resultSize120);

        var size144 = DpiCalculator.ScaleWithDpi(testSize, 144);
        var resultSize144 = DpiCalculator.UnscaleWithDpi(size144, 144);
        Assert.Equal(testSize, resultSize144);
    }

    /// <summary>
    ///     Test GetSystemMetrics
    /// </summary>
    [Fact]
    public void Test_GetSystemMetrics()
    {
        // Test with default DPI
        var screenWidth = DpiApi.GetSystemMetrics(User32.Enums.SystemMetric.SM_CXSCREEN);
        Assert.True(screenWidth > 0, "Screen width should be positive");

        // Test with specific DPI values
        var screenWidth96 = DpiApi.GetSystemMetrics(User32.Enums.SystemMetric.SM_CXSCREEN, 96);
        Assert.True(screenWidth96 > 0, "Screen width at 96 DPI should be positive");

        var screenWidth144 = DpiApi.GetSystemMetrics(User32.Enums.SystemMetric.SM_CXSCREEN, 144);
        Assert.True(screenWidth144 > 0, "Screen width at 144 DPI should be positive");

        // Higher DPI should generally result in larger values for most metrics
        // Note: This may not always be true depending on the system configuration
    }

    /// <summary>
    ///     Test AdjustWindowRect
    /// </summary>
    [Fact]
    public void Test_AdjustWindowRect()
    {
        // Create a client rectangle
        var clientRect = new NativeRect(0, 0, 800, 600);

        // Adjust for a standard overlapped window with caption and sizing border
        var windowRect = DpiApi.AdjustWindowRect(
            clientRect,
            User32.Enums.WindowStyleFlags.WS_OVERLAPPEDWINDOW,
            hasMenu: false,
            User32.Enums.ExtendedWindowStyleFlags.WS_NONE,
            dpi: 96);

        Assert.NotNull(windowRect);
        
        // The window rect should be larger than the client rect to account for borders and title bar
        Assert.True(windowRect.Value.Width >= clientRect.Width, "Window width should be >= client width");
        Assert.True(windowRect.Value.Height >= clientRect.Height, "Window height should be >= client height");
    }

    /// <summary>
    ///     Test AdjustWindowRect with different DPI values
    /// </summary>
    [Fact]
    public void Test_AdjustWindowRect_DpiScaling()
    {
        var clientRect = new NativeRect(0, 0, 800, 600);

        var windowRect96 = DpiApi.AdjustWindowRect(
            clientRect,
            User32.Enums.WindowStyleFlags.WS_OVERLAPPEDWINDOW,
            hasMenu: false,
            User32.Enums.ExtendedWindowStyleFlags.WS_NONE,
            dpi: 96);

        var windowRect144 = DpiApi.AdjustWindowRect(
            clientRect,
            User32.Enums.WindowStyleFlags.WS_OVERLAPPEDWINDOW,
            hasMenu: false,
            User32.Enums.ExtendedWindowStyleFlags.WS_NONE,
            dpi: 144);

        Assert.NotNull(windowRect96);
        Assert.NotNull(windowRect144);

        // The border size should scale with DPI
        var border96 = windowRect96.Value.Width - clientRect.Width;
        var border144 = windowRect144.Value.Width - clientRect.Width;
        
        // At 144 DPI (150%), borders should be larger than at 96 DPI (100%)
        Assert.True(border144 >= border96, "Border at 144 DPI should be >= border at 96 DPI");
    }
}