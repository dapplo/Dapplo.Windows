// Copyright (c) Dapplo and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
using System;
using System.Threading.Tasks;
using Dapplo.Windows.Common.Structs;
using Dapplo.Windows.Desktop;
using Dapplo.Windows.User32;
using Dapplo.Windows.User32.Enums;
using Xunit;

namespace Dapplo.Windows.Tests;

public class WindowScrollerTests
{
    [Fact]
    public void CalculateWheelDelta_ScrollsOnePage()
    {
        // 30 lines per page, 3 lines per notch = 10 notches
        Assert.Equal(1200, WindowScroller.CalculateWheelDelta(30, 3));
    }

    [Fact]
    public void CalculateWheelDelta_NoScrollLines_DoesNotThrow()
    {
        Assert.Equal(WindowScroller.WheelDeltaPerNotch, WindowScroller.CalculateWheelDelta(30, 0));
    }

    [Fact]
    public void CalculateWheelDelta_PageScroll_IsOneNotch()
    {
        Assert.Equal(WindowScroller.WheelDeltaPerNotch, WindowScroller.CalculateWheelDelta(30, uint.MaxValue));
    }

    [Fact]
    public void CalculateWheelDelta_SmallPage_IsAtLeastOneNotch()
    {
        Assert.Equal(WindowScroller.WheelDeltaPerNotch, WindowScroller.CalculateWheelDelta(2, 3));
        Assert.Equal(WindowScroller.WheelDeltaPerNotch, WindowScroller.CalculateWheelDelta(0, 3));
    }

    [Fact]
    public void CalculateWheelDelta_HugePage_DoesNotOverflow()
    {
        Assert.Equal(int.MaxValue, WindowScroller.CalculateWheelDelta(uint.MaxValue - 1, 1));
    }

    // ── Step fraction ────────────────────────────────────────────────────────

    [Theory]
    [InlineData(0.0)]
    [InlineData(-0.5)]
    [InlineData(1.5)]
    [InlineData(double.NaN)]
    [InlineData(double.PositiveInfinity)]
    public void StepFraction_OutOfRange_Throws(double stepFraction)
    {
        var windowScroller = new WindowScroller();
        Assert.Throws<ArgumentOutOfRangeException>(() => windowScroller.StepFraction = stepFraction);
        Assert.Equal(1.0, windowScroller.StepFraction);
    }

    [Theory]
    [InlineData(1200, 0.5, false, 600)]
    [InlineData(1200, 0.3, false, 360)]   // 3.6 notches round to 3
    [InlineData(1200, 1.0, false, 1200)]
    [InlineData(120, 0.5, false, 120)]    // at least one notch
    [InlineData(0, 0.5, false, 120)]      // no page delta: one notch
    [InlineData(1200, 0.25, true, 300)]
    [InlineData(120, 0.1, true, 12)]
    [InlineData(10, 0.01, true, 1)]       // at least 1
    public void ScaleWheelDelta_ScalesThePageDelta(int pageWheelDelta, double stepFraction, bool allowPartialNotches, int expected)
    {
        Assert.Equal(expected, WindowScroller.ScaleWheelDelta(pageWheelDelta, stepFraction, allowPartialNotches));
    }

    [Theory]
    [InlineData(30u, 0.5, 15)]
    [InlineData(30u, 1.0, 30)]
    [InlineData(1u, 0.5, 1)]
    [InlineData(0u, 0.5, 1)]
    [InlineData(uint.MaxValue, 1.0, WindowScroller.MaxLineSteps)]
    public void CalculateLineSteps_ApproximatesTheFraction(uint pageSize, double stepFraction, int expected)
    {
        Assert.Equal(expected, WindowScroller.CalculateLineSteps(pageSize, stepFraction));
    }

    [Theory]
    [InlineData(0, 0, 99, 20u, 0.5, true, 10)]
    [InlineData(0, 0, 99, 20u, 1.0, true, 20)]
    [InlineData(95, 0, 99, 20u, 0.5, true, 99)]    // clamped to the maximum
    [InlineData(50, 0, 99, 20u, 0.5, false, 40)]
    [InlineData(5, 0, 99, 20u, 0.5, false, 0)]     // clamped to the minimum
    [InlineData(10, 0, 99, 1u, 0.1, true, 11)]     // at least 1
    public void CalculateStepPosition_MovesByTheFractionAndClamps(int position, int minimum, int maximum, uint pageSize, double stepFraction, bool forward, int expected)
    {
        Assert.Equal(expected, WindowScroller.CalculateStepPosition(position, minimum, maximum, pageSize, stepFraction, forward));
    }

    // ── With a real window (an EDIT control with a Win32 scroll bar, on its own UI thread) ──

    private static WindowScroller CreateScroller(TextBoxScrollTestWindow testWindow, ScrollModes scrollMode, double stepFraction)
    {
        var windowScroller = InteropWindowFactory.CreateFor(testWindow.ScrollingHandle).GetWindowScroller();
        Assert.NotNull(windowScroller);
        windowScroller.ScrollMode = scrollMode;
        windowScroller.StepFraction = stepFraction;
        Assert.True(windowScroller.Start());
        return windowScroller;
    }

    [Fact]
    public void AbsoluteWindowMessage_HalfPage_MovesHalfAPage()
    {
        using var testWindow = new TextBoxScrollTestWindow();
        var windowScroller = CreateScroller(testWindow, ScrollModes.AbsoluteWindowMessage, 0.5);
        Assert.True(windowScroller.GetPosition(out var before));
        Assert.True(before.PageSize > 2, $"Page size {before.PageSize}");

        Assert.True(windowScroller.Next());

        Assert.True(windowScroller.GetPosition(out var after));
        Assert.Equal(WindowScroller.CalculateStepPosition(before.Position, before.Minimum, before.Maximum, before.PageSize, 0.5, true), after.Position);

        Assert.True(windowScroller.Previous());
        Assert.True(windowScroller.GetPosition(out var back));
        Assert.Equal(before.Position, back.Position);
    }

    [Fact]
    public void WindowsMessage_HalfPage_SendsLines()
    {
        using var testWindow = new TextBoxScrollTestWindow();
        var windowScroller = CreateScroller(testWindow, ScrollModes.WindowsMessage, 0.5);
        Assert.True(windowScroller.GetPosition(out var before));

        Assert.True(windowScroller.Next());

        // An EDIT control scrolls one line per SB_LINEDOWN and its page size is in lines
        Assert.True(windowScroller.GetPosition(out var after));
        Assert.Equal(before.Position + WindowScroller.CalculateLineSteps(before.PageSize, 0.5), after.Position);
    }

    [Fact]
    public void WindowsMessage_FullPage_StillScrollsAPage()
    {
        using var testWindow = new TextBoxScrollTestWindow();
        var windowScroller = CreateScroller(testWindow, ScrollModes.WindowsMessage, 1.0);
        Assert.True(windowScroller.GetPosition(out var before));

        Assert.True(windowScroller.Next());

        Assert.True(windowScroller.GetPosition(out var after));
        Assert.Equal(before.Position + (int)before.PageSize, after.Position);
    }

    [Fact]
    public void HalfPageSteps_ReachTheEnd()
    {
        using var testWindow = new TextBoxScrollTestWindow();
        var windowScroller = CreateScroller(testWindow, ScrollModes.AbsoluteWindowMessage, 0.5);
        Assert.True(windowScroller.GetPosition(out var scrollInfo));
        var expectedSteps = (int)Math.Ceiling((scrollInfo.Maximum - scrollInfo.PageSize + 1.0) / Math.Max(1, Math.Round(scrollInfo.PageSize * 0.5)));

        var steps = 0;
        while (!windowScroller.IsAtEnd && steps < 1000)
        {
            Assert.True(windowScroller.Next());
            steps++;
        }

        Assert.True(windowScroller.IsAtEnd);
        Assert.InRange(steps, expectedSteps - 1, expectedSteps + 1);
    }

    [Fact]
    public void ViewportBounds_IsTheClientAreaInScreenCoordinates()
    {
        using var testWindow = new TextBoxScrollTestWindow();
        var windowScroller = CreateScroller(testWindow, ScrollModes.WindowsMessage, 1.0);

        Assert.Equal(testWindow.ScrollingBounds, windowScroller.ViewportBounds);
        IScroller scroller = windowScroller;
        Assert.Equal(testWindow.ScrollingBounds, scroller.ViewportBounds);
    }

    /// <summary>
    ///     The wheel goes to WheelLocation (here the text box, while the scroll bar window is the text box too), and the cursor is put back
    /// </summary>
    /// <remarks>Interactive: moves the cursor and injects mouse wheel input.</remarks>
    [Fact]
    [Trait("Category", "Interactive")]
    public async Task MouseWheel_AtWheelLocation_ScrollsAndRestoresTheCursor()
    {
        using var testWindow = new TextBoxScrollTestWindow();
        var bounds = testWindow.ScrollingBounds;
        var wheelLocation = new NativePoint(bounds.X + 20, bounds.Y + 20);
        testWindow.SkipWhenNotVisibleAt(wheelLocation);
        var windowScroller = CreateScroller(testWindow, ScrollModes.MouseWheel, 0.5);
        windowScroller.WheelLocation = wheelLocation;
        windowScroller.RestoreCursorAfterWheel = true;
        var cursorBefore = User32Api.GetCursorLocation();
        Assert.True(windowScroller.GetPosition(out var before));

        Assert.True(windowScroller.Next());

        await TestWait.UntilAsync(() => windowScroller.GetPosition(out var current) && current.Position > before.Position, "The wheel input didn't scroll the text box");
        await TestWait.UntilAsync(() => User32Api.GetCursorLocation() == cursorBefore, "The cursor was not restored");
    }
}
