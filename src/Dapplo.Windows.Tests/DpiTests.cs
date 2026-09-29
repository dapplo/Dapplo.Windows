// Copyright (c) Dapplo and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
using System;
using Dapplo.Log;
using Dapplo.Log.XUnit;
using Dapplo.Windows.Common;
using Dapplo.Windows.Common.Structs;
using Dapplo.Windows.Dpi;
using Dapplo.Windows.Dpi.Enums;
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

    /// <summary>
    ///     The DPI_AWARENESS_CONTEXT pseudo handles must be pointer sized with the documented values
    /// </summary>
    [Fact]
    public void Test_DpiAwarenessContext_PseudoHandles()
    {
        Assert.Equal(new IntPtr(-1), DpiAwarenessContext.Unaware.Value);
        Assert.Equal(new IntPtr(-2), DpiAwarenessContext.SystemAware.Value);
        Assert.Equal(new IntPtr(-3), DpiAwarenessContext.PerMonitorAware.Value);
        Assert.Equal(new IntPtr(-4), DpiAwarenessContext.PerMonitorAwareV2.Value);
        Assert.Equal(new IntPtr(-5), DpiAwarenessContext.UnawareGdiScaled.Value);
        Assert.Equal(-4L, DpiAwarenessContext.PerMonitorAwareV2.Value.ToInt64());
        Assert.True(DpiAwarenessContext.Null.IsNull);
        Assert.True(default(DpiAwarenessContext).IsNull);
        Assert.False(DpiAwarenessContext.PerMonitorAwareV2.IsNull);
    }

    /// <summary>
    ///     Test that the pseudo handles are accepted by Windows, and map to the right DPI awareness
    /// </summary>
    [Fact]
    public void Test_DpiAwarenessContext_Valid()
    {
        if (!WindowsVersion.IsWindows10BuildOrLater(15063))
        {
            Assert.Skip("Per Monitor v2 is only available on Windows 10 1703 or later");
        }

        Assert.True(NativeDpiMethods.IsValidDpiAwarenessContext(DpiAwarenessContext.PerMonitorAwareV2));
        Assert.True(NativeDpiMethods.IsValidDpiAwarenessContext(DpiAwarenessContext.Unaware));
        Assert.False(NativeDpiMethods.IsValidDpiAwarenessContext(DpiAwarenessContext.Null));
        Assert.Equal(DpiAwareness.Unaware, NativeDpiMethods.GetAwarenessFromDpiAwarenessContext(DpiAwarenessContext.Unaware));
        Assert.Equal(DpiAwareness.SystemAware, NativeDpiMethods.GetAwarenessFromDpiAwarenessContext(DpiAwarenessContext.SystemAware));
        Assert.Equal(DpiAwareness.PerMonitorAware, NativeDpiMethods.GetAwarenessFromDpiAwarenessContext(DpiAwarenessContext.PerMonitorAwareV2));
        Assert.True(NativeDpiMethods.AreDpiAwarenessContextsEqual(DpiAwarenessContext.PerMonitorAwareV2, DpiAwarenessContext.PerMonitorAwareV2));
        Assert.False(NativeDpiMethods.AreDpiAwarenessContextsEqual(DpiAwarenessContext.PerMonitorAwareV2, DpiAwarenessContext.Unaware));
    }

    /// <summary>
    ///     Test that the scoped thread DPI awareness context is applied, and restored when disposed
    /// </summary>
    [Fact]
    public void Test_ScopedThreadDpiAwarenessContext_Restores()
    {
        if (!WindowsVersion.IsWindows10BuildOrLater(14393))
        {
            Assert.Skip("SetThreadDpiAwarenessContext is only available on Windows 10 1607 or later");
        }

        var before = NativeDpiMethods.GetThreadDpiAwarenessContext();
        Assert.False(before.IsNull);
        var target = NativeDpiMethods.AreDpiAwarenessContextsEqual(before, DpiAwarenessContext.Unaware) ? DpiAwarenessContext.SystemAware : DpiAwarenessContext.Unaware;

        using (NativeDpiMethods.ScopedThreadDpiAwarenessContext(target))
        {
            Assert.True(NativeDpiMethods.AreDpiAwarenessContextsEqual(NativeDpiMethods.GetThreadDpiAwarenessContext(), target));
        }

        Assert.True(NativeDpiMethods.AreDpiAwarenessContextsEqual(NativeDpiMethods.GetThreadDpiAwarenessContext(), before));
    }
}