// Copyright (c) Dapplo and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Dapplo.Log;
using Dapplo.Log.XUnit;
using Dapplo.Windows.Automation;
using Dapplo.Windows.Common.Structs;
using Dapplo.Windows.Desktop;
using Dapplo.Windows.User32;
using Xunit;

namespace Dapplo.Windows.Tests;

/// <summary>
///     Tests for the UI Automation based scroller, against windows the tests create on their own UI thread.
///     A WPF ScrollViewer has no Win32 scroll bar, so GetWindowScroller can't scroll it, UI Automation can.
/// </summary>
public class UiAutomationScrollerTests
{
    public UiAutomationScrollerTests(ITestOutputHelper testOutputHelper)
    {
        LogSettings.RegisterDefaultLogger<XUnitLogger>(LogLevels.Verbose, testOutputHelper);
    }

    private static UiAutomationScroller CreateScroller(WpfScrollTestWindow testWindow, bool horizontal = false)
    {
        var scroller = UiAutomationScroller.FromWindow(testWindow.WindowHandle, horizontal);
        Assert.NotNull(scroller);
        return scroller;
    }

    [Fact]
    public void Wpf_HasNoWin32ScrollBar()
    {
        using var testWindow = new WpfScrollTestWindow();
        Assert.Null(InteropWindowFactory.CreateFor(testWindow.WindowHandle).GetWindowScroller(forceUpdate: true));
    }

    [Fact]
    public void FromWindow_FindsTheScrollViewer()
    {
        using var testWindow = new WpfScrollTestWindow();
        using var scroller = CreateScroller(testWindow);

        Assert.True(scroller.IsAvailable);
        Assert.False(scroller.Horizontal);
        Assert.True(scroller.IsAtStart);
        Assert.False(scroller.IsAtEnd);
        Assert.Equal(0, scroller.ScrollPercent, 3);
        Assert.InRange(scroller.VisibleFraction, 0.01, 0.99);

        // The viewport is the ScrollViewer (without its scroll bars), inside the window
        var viewport = scroller.ViewportBounds;
        var expected = testWindow.ScrollingBounds;
        Assert.False(viewport.IsEmpty);
        Assert.InRange(viewport.X, expected.X - 2, expected.X + 2);
        Assert.InRange(viewport.Y, expected.Y - 2, expected.Y + 2);
        Assert.InRange(viewport.Width, 1, expected.Width + 2);
        Assert.InRange(viewport.Height, 1, expected.Height + 2);
    }

    [Fact]
    public void FromPoint_FindsTheScrollableAncestor()
    {
        using var testWindow = new WpfScrollTestWindow();
        var center = ScrollTestWindow.CenterOf(testWindow.ScrollingBounds);
        testWindow.SkipWhenNotVisibleAt(center);

        // The point is on a TextBlock inside the ScrollViewer, the scroller walks up to the ScrollViewer
        using var scroller = UiAutomationScroller.FromPoint(center);

        Assert.NotNull(scroller);
        Assert.True(scroller.IsAtStart);
        Assert.True(scroller.Next());
        Assert.True(testWindow.VerticalOffset > 0);
    }

    [Fact]
    public void Next_HalfPage_ScrollsHalfTheViewport()
    {
        using var testWindow = new WpfScrollTestWindow();
        using var scroller = CreateScroller(testWindow);
        scroller.StepFraction = 0.5;
        var viewportHeight = testWindow.Invoke(() => testWindow.ScrollingBounds.Height);

        Assert.True(scroller.Next());

        // In device independent pixels, the viewport height in pixels depends on the DPI: compare relative to a page
        var offset = testWindow.VerticalOffset;
        var page = testWindow.Invoke(() => GetViewportHeight(testWindow));
        Assert.InRange(offset, page * 0.5 - 2, page * 0.5 + 2);

        Assert.True(scroller.Previous());
        Assert.True(scroller.IsAtStart);
        Assert.InRange(testWindow.VerticalOffset, 0, 0.5);
        Assert.True(viewportHeight > 0);
    }

    private static double GetViewportHeight(WpfScrollTestWindow testWindow)
    {
        // ViewportHeight of the ScrollViewer, read via the visual tree on the UI thread
        var window = (System.Windows.Window)System.Windows.Interop.HwndSource.FromHwnd(testWindow.WindowHandle).RootVisual;
        return ((System.Windows.Controls.ScrollViewer)window.Content).ViewportHeight;
    }

    [Fact]
    public void HalfPageSteps_ReachTheEnd_AndOverlap()
    {
        using var testWindow = new WpfScrollTestWindow();
        using var scroller = CreateScroller(testWindow);
        scroller.StepFraction = 0.5;
        var pages = 1 / scroller.VisibleFraction;
        // From the start to the end is (pages - 1) pages, half a page per step
        var expectedSteps = (int)Math.Ceiling((pages - 1) * 2);

        var steps = 0;
        var previousPercent = scroller.ScrollPercent;
        while (!scroller.IsAtEnd && steps < 200)
        {
            Assert.True(scroller.Next());
            var percent = scroller.ScrollPercent;
            Assert.True(percent > previousPercent, $"Step {steps}: {percent} after {previousPercent}");
            previousPercent = percent;
            steps++;
        }

        Assert.True(scroller.IsAtEnd);
        Assert.InRange(steps, expectedSteps - 1, expectedSteps + 1);
        Assert.False(scroller.Next());   // nothing left
    }

    [Fact]
    public void StartEndReset_MoveToThePositions()
    {
        using var testWindow = new WpfScrollTestWindow();
        using var scroller = CreateScroller(testWindow);

        Assert.True(scroller.End());
        Assert.True(scroller.IsAtEnd);
        Assert.Equal(100, scroller.ScrollPercent, 1);

        Assert.True(scroller.Start());
        Assert.True(scroller.IsAtStart);

        Assert.True(scroller.Next());
        Assert.True(scroller.Reset());   // created at 0 percent
        Assert.True(scroller.IsAtStart);
    }

    [Fact]
    public void Horizontal_ScrollsTheOtherDirection()
    {
        using var testWindow = new WpfScrollTestWindow();
        using var scroller = CreateScroller(testWindow, horizontal: true);
        Assert.True(scroller.Horizontal);
        scroller.StepFraction = 0.5;

        Assert.True(scroller.Next());

        Assert.True(testWindow.HorizontalOffset > 0);
        Assert.Equal(0, testWindow.VerticalOffset, 3);
    }

    [Fact]
    public void AsIScroller_WorksTheSameAsAWindowScroller()
    {
        using var testWindow = new WpfScrollTestWindow();
        // The pattern of the sample: the Win32 scroller first, UI Automation as fallback
        IScroller scroller = InteropWindowFactory.CreateFor(testWindow.WindowHandle).GetWindowScroller(forceUpdate: true);
        scroller ??= UiAutomationScroller.FromWindow(testWindow.WindowHandle);
        Assert.IsType<UiAutomationScroller>(scroller);
        scroller.StepFraction = 0.5;

        Assert.True(scroller.Start());
        var steps = 0;
        while (!scroller.IsAtEnd && steps < 200 && scroller.Next())
        {
            steps++;
        }

        Assert.True(scroller.IsAtEnd);
        ((IDisposable)scroller).Dispose();
    }

    [Fact]
    public void Win32EditControl_IsScrollableWithUiAutomationToo()
    {
        using var testWindow = new TextBoxScrollTestWindow();
        using var scroller = UiAutomationScroller.FromWindow(testWindow.ScrollingHandle);
        Assert.SkipWhen(scroller is null, "UI Automation exposes no ScrollPattern for the EDIT control on this system");
        scroller.StepFraction = 0.5;

        Assert.True(scroller.Next());

        Assert.True(scroller.ScrollPercent > 0);
    }

    /// <summary>
    ///     A capture loop ends when the window goes away. For another process UI Automation reports the element as gone; for a window
    ///     of this process (like here) the provider object lives on and may still answer with its last values, but nothing moves,
    ///     so Next() returns false.
    /// </summary>
    [Fact]
    public void ClosedWindow_EndsLoops()
    {
        var testWindow = new WpfScrollTestWindow();
        using var scroller = CreateScroller(testWindow);
        testWindow.Dispose();

        var steps = 0;
        while (!scroller.IsAtEnd && scroller.Next())
        {
            steps++;
            Assert.True(steps < 3, "The loop didn't end after the window was closed");
        }
        Assert.Equal(0, steps);
    }

    [Fact]
    public void Disposed_Throws()
    {
        using var testWindow = new WpfScrollTestWindow();
        var scroller = CreateScroller(testWindow);
        scroller.Dispose();
        Assert.Throws<ObjectDisposedException>(() => scroller.Next());
    }

    [Fact]
    public void StepFraction_OutOfRange_Throws()
    {
        using var testWindow = new WpfScrollTestWindow();
        using var scroller = CreateScroller(testWindow);
        Assert.Throws<ArgumentOutOfRangeException>(() => scroller.StepFraction = 0);
        Assert.Throws<ArgumentOutOfRangeException>(() => scroller.StepFraction = 1.01);
    }

    [Fact]
    public void FromPoint_NothingScrollable_ReturnsNull()
    {
        using var testWindow = new TextBoxScrollTestWindow();
        // The title bar of the form: nothing above it scrolls vertically
        var bounds = InteropWindowFactory.CreateFor(testWindow.WindowHandle).GetInfo(true).Bounds;
        var titleBar = new Dapplo.Windows.Common.Structs.NativePoint(bounds.X + bounds.Width / 2, bounds.Y + 8);
        testWindow.SkipWhenNotVisibleAt(titleBar);
        Assert.Null(UiAutomationScroller.FromPoint(titleBar));
    }

    private static bool IsNear(NativeRect actual, NativeRect expected, int tolerance = 2) =>
        Math.Abs(actual.X - expected.X) <= tolerance && Math.Abs(actual.Y - expected.Y) <= tolerance &&
        Math.Abs(actual.Width - expected.Width) <= tolerance && Math.Abs(actual.Height - expected.Height) <= tolerance;

    private static string Describe(IEnumerable<NativeRect> areas) => string.Join(", ", areas.Select(a => a.ToString()));

    private static void AssertInside(NativeRect outer, IEnumerable<NativeRect> areas)
    {
        foreach (var area in areas)
        {
            Assert.True(area.X >= outer.X - 1 && area.Y >= outer.Y - 1 && area.Right <= outer.Right + 1 && area.Bottom <= outer.Bottom + 1,
                $"{area} is not inside the window {outer}");
        }
    }

    [Fact]
    public void FindScrollableAreas_ReturnsBothAreasInsideTheWindow()
    {
        using var testWindow = new TwoAreasScrollTestWindow();
        var left = testWindow.LeftBounds;
        var right = testWindow.RightBounds;
        var windowBounds = InteropWindowFactory.CreateFor(testWindow.WindowHandle).GetInfo(true).Bounds;

        var areas = UiAutomationScroller.FindScrollableAreas(testWindow.WindowHandle);

        Assert.NotNull(areas);
        Assert.True(areas.Any(a => IsNear(a, left)), $"The left area {left} is missing: {Describe(areas)}");
        Assert.True(areas.Any(a => IsNear(a, right)), $"The right area {right} is missing: {Describe(areas)}");
        Assert.All(areas, a => Assert.False(a.IsEmpty));
        AssertInside(windowBounds, areas);
    }

    [Fact]
    public void FindScrollableAreas_Horizontal_ReturnsOnlyTheWideArea()
    {
        using var testWindow = new TwoAreasScrollTestWindow();
        var left = testWindow.LeftBounds;
        var right = testWindow.RightBounds;

        var areas = UiAutomationScroller.FindScrollableAreas(testWindow.WindowHandle, horizontal: true);

        Assert.True(areas.Any(a => IsNear(a, left)), $"The left area {left} is missing: {Describe(areas)}");
        Assert.DoesNotContain(areas, a => IsNear(a, right));
    }

    /// <summary>
    ///     Greenshot's use case: another window covers the whole window (its selection window), no hit testing on the screen is needed
    /// </summary>
    [Fact]
    public void FindScrollableAreas_WorksWhileTheWindowIsCovered()
    {
        using var testWindow = new TwoAreasScrollTestWindow();
        var windowInfo = InteropWindowFactory.CreateFor(testWindow.WindowHandle).GetInfo(true);
        using var cover = new CoverTestWindow(windowInfo.Bounds);

        var areas = UiAutomationScroller.FindScrollableAreas(InteropWindowFactory.CreateFor(testWindow.WindowHandle));

        Assert.True(areas.Any(a => IsNear(a, testWindow.LeftBounds)), Describe(areas));
        Assert.True(areas.Any(a => IsNear(a, testWindow.RightBounds)), Describe(areas));
    }

    [Fact]
    public void FindScrollableAreas_NothingScrollable_ReturnsAnEmptyList()
    {
        using var cover = new CoverTestWindow(new NativeRect(50, 50, 200, 100));

        Assert.Empty(UiAutomationScroller.FindScrollableAreas(cover.WindowHandle));
        Assert.Empty(UiAutomationScroller.FindScrollableAreas(IntPtr.Zero));
        Assert.Empty(UiAutomationScroller.FindScrollableAreas((IInteropWindow)null));
    }

    [Fact]
    public void FindScrollableAreas_EdgeControl_IsFoundInTheForm()
    {
        using var testWindow = new TextBoxScrollTestWindow();
        var areas = UiAutomationScroller.FindScrollableAreas(testWindow.WindowHandle);
        Assert.SkipWhen(areas.Count == 0, "UI Automation exposes no ScrollPattern for the EDIT control on this system");
        // The client area of the EDIT control, without its border and scroll bar, lies inside the found area
        var client = testWindow.ScrollingBounds;
        Assert.Contains(areas, a => a.X <= client.X && a.Y <= client.Y && a.Right >= client.Right && a.Bottom >= client.Bottom);
        AssertInside(InteropWindowFactory.CreateFor(testWindow.WindowHandle).GetInfo(true).Bounds, areas);
    }

    [Theory]
    [InlineData(ScrollBarExposure.RangeValue)]
    [InlineData(ScrollBarExposure.ThumbOnly)]
    [InlineData(ScrollBarExposure.Nothing)]
    public void FindScrollableAreas_IncludesTheParentOfAScrollBar(ScrollBarExposure exposure)
    {
        using var testWindow = new ScrollBarOnlyTestWindow(exposure);
        // The control without its scroll bar: a capture of the area doesn't show it
        var expected = testWindow.ContentBounds;
        var control = testWindow.ControlBounds;
        Assert.True(expected.Width < control.Width);
        var windowBounds = InteropWindowFactory.CreateFor(testWindow.WindowHandle).GetInfo(true).Bounds;

        var areas = UiAutomationScroller.FindScrollableAreas(testWindow.WindowHandle);

        Assert.True(areas.Any(a => IsNear(a, expected)), $"The control {expected} is missing: {Describe(areas)}");
        AssertInside(windowBounds, areas);
        // Only ScrollPattern elements: nothing here; and the scroll bar is vertical
        Assert.DoesNotContain(areas, a => IsNear(a, control));
        Assert.DoesNotContain(UiAutomationScroller.FindScrollableAreas(testWindow.WindowHandle, false, null, false), a => IsNear(a, expected));
        Assert.DoesNotContain(UiAutomationScroller.FindScrollableAreas(testWindow.WindowHandle, horizontal: true), a => IsNear(a, expected));
    }

    [Fact]
    public void FindScrollableAreas_ScrollViewerAndItsScrollBars_AreReturnedOnce()
    {
        using var testWindow = new TwoAreasScrollTestWindow();
        var left = testWindow.LeftBounds;

        var areas = UiAutomationScroller.FindScrollableAreas(testWindow.WindowHandle);

        Assert.Single(areas, a => IsNear(a, left));
    }

    [Theory]
    [InlineData(ScrollBarExposure.RangeValue)]
    [InlineData(ScrollBarExposure.ThumbOnly)]
    public void FromPoint_ScrollBarOnly_ReturnsAWheelScroller(ScrollBarExposure exposure)
    {
        using var testWindow = new ScrollBarOnlyTestWindow(exposure);
        var bounds = testWindow.ContentBounds;
        var center = ScrollTestWindow.CenterOf(bounds);
        testWindow.SkipWhenNotVisibleAt(center);

        using var scroller = UiAutomationScroller.FromPoint(center);

        Assert.NotNull(scroller);
        Assert.True(scroller.IsScrollBarFallback);
        Assert.Equal(UiAutomationScrollModes.MouseWheel, scroller.ScrollMode);
        Assert.Throws<InvalidOperationException>(() => scroller.ScrollMode = UiAutomationScrollModes.ScrollPattern);
        Assert.True(scroller.IsPositionKnown);
        Assert.True(scroller.IsAtStart);
        Assert.False(scroller.IsAtEnd);
        Assert.Equal(0, scroller.InitialScrollPercent, 3);
        // About 260 of 2000 pixels are visible
        Assert.InRange(scroller.VisibleFraction, 0.05, 0.3);
        Assert.True(IsNear(scroller.ViewportBounds, bounds), $"{scroller.ViewportBounds} is not {bounds}");
    }

    [Fact]
    public void FromPoint_ScrollBarOnly_HorizontalFindsNothing()
    {
        using var testWindow = new ScrollBarOnlyTestWindow();
        var center = ScrollTestWindow.CenterOf(testWindow.ControlBounds);
        testWindow.SkipWhenNotVisibleAt(center);

        Assert.Null(UiAutomationScroller.FromPoint(center, horizontal: true));
    }

    /// <summary>
    ///     The wheel moves the content, the position comes from the RangeValue or the thumb, the loop ends at the end
    /// </summary>
    [Theory]
    [InlineData(ScrollBarExposure.RangeValue)]
    [InlineData(ScrollBarExposure.ThumbOnly)]
    public void ScrollBarOnly_HalfPageSteps_ReachTheEnd(ScrollBarExposure exposure)
    {
        using var testWindow = new ScrollBarOnlyTestWindow(exposure);
        var center = ScrollTestWindow.CenterOf(testWindow.ControlBounds);
        testWindow.SkipWhenNotVisibleAt(center);
        using var scroller = UiAutomationScroller.FromPoint(center);
        Assert.NotNull(scroller);
        scroller.StepFraction = 0.5;
        scroller.RestoreCursorAfterWheel = true;

        var steps = 0;
        var previousOffset = testWindow.Offset;
        while (!scroller.IsAtEnd && steps < 100)
        {
            Assert.True(scroller.Next(), $"Step {steps} at {scroller.ScrollPercent} percent");
            var offset = testWindow.Offset;
            Assert.True(offset > previousOffset, $"Step {steps}: {offset} after {previousOffset}");
            previousOffset = offset;
            steps++;
        }

        Assert.True(scroller.IsAtEnd);
        Assert.Equal(testWindow.MaxOffset, testWindow.Offset, 1);
        Assert.InRange(steps, 5, 30);
        Assert.False(scroller.Next());

        Assert.True(scroller.Reset());   // created at the start
        Assert.True(scroller.IsAtStart);
        Assert.Equal(0, testWindow.Offset, 1);
    }

    /// <summary>
    ///     A horizontal scroll bar at the bottom of a vertically scrolled area is cut off too
    /// </summary>
    [Fact]
    public void ScrollBarOnly_AreaExcludesBothScrollBars()
    {
        using var testWindow = new ScrollBarOnlyTestWindow(withHorizontalScrollBar: true);
        var expected = testWindow.ContentBounds;
        var control = testWindow.ControlBounds;
        Assert.True(expected.Height < control.Height && expected.Width < control.Width);

        var areas = UiAutomationScroller.FindScrollableAreas(testWindow.WindowHandle);
        Assert.True(areas.Any(a => IsNear(a, expected)), $"The content {expected} is missing: {Describe(areas)}");
        // The horizontal scroll bar gives an area too, without both scroll bars as well
        Assert.True(UiAutomationScroller.FindScrollableAreas(testWindow.WindowHandle, horizontal: true).Any(a => IsNear(a, expected)));

        var center = ScrollTestWindow.CenterOf(expected);
        testWindow.SkipWhenNotVisibleAt(center);
        using var scroller = UiAutomationScroller.FromPoint(center);
        Assert.NotNull(scroller);
        Assert.True(IsNear(scroller.ViewportBounds, expected), $"{scroller.ViewportBounds} is not {expected}");
    }

    private static NativeRect CutScrollBar(NativeRect area, NativeRect scrollBar, bool scrollBarIsHorizontal)
    {
        var method = typeof(UiAutomationScroller).GetMethod("CutScrollBar", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
        Assert.NotNull(method);
        return (NativeRect)method.Invoke(null, [area, scrollBar, scrollBarIsHorizontal]);
    }

    [Fact]
    public void CutScrollBar_OnlyAtAnEdge()
    {
        var area = new NativeRect(100, 100, 400, 300);
        // Vertical at the right, at the left, in the middle
        Assert.Equal(new NativeRect(100, 100, 383, 300), CutScrollBar(area, new NativeRect(483, 100, 17, 300), false));
        Assert.Equal(new NativeRect(117, 100, 383, 300), CutScrollBar(area, new NativeRect(100, 100, 17, 300), false));
        Assert.Equal(area, CutScrollBar(area, new NativeRect(300, 100, 17, 300), false));
        // Horizontal at the bottom, outside the area, too short
        Assert.Equal(new NativeRect(100, 100, 400, 283), CutScrollBar(area, new NativeRect(100, 383, 383, 17), true));
        Assert.Equal(area, CutScrollBar(area, new NativeRect(100, 400, 400, 17), true));
        Assert.Equal(area, CutScrollBar(area, new NativeRect(100, 383, 100, 17), true));
    }

    /// <summary>
    ///     Start from far down: many more than 100 steps of half a page; with the RangeValue it is set, with only a thumb it wheels
    ///     several pages at a time
    /// </summary>
    [Theory]
    [InlineData(ScrollBarExposure.RangeValue, true)]
    [InlineData(ScrollBarExposure.ThumbOnly, true)]
    // Like the Visual Studio editor: setting the scroll bar's value moves only the scroll bar
    [InlineData(ScrollBarExposure.RangeValue, false)]
    public void ScrollBarOnly_StartAndEnd_ReachTheLimitsOfLongContent(ScrollBarExposure exposure, bool followsValueChanges)
    {
        using var testWindow = new ScrollBarOnlyTestWindow(exposure, lineCount: 3000, followsValueChanges: followsValueChanges);
        var center = ScrollTestWindow.CenterOf(testWindow.ContentBounds);
        testWindow.SkipWhenNotVisibleAt(center);
        testWindow.ScrollTo(double.MaxValue);
        var halfPage = testWindow.ContentBounds.Height / 2.0;
        using var scroller = UiAutomationScroller.FromPoint(center);
        Assert.NotNull(scroller);
        Assert.True(scroller.IsAtEnd);
        scroller.StepFraction = 0.5;
        scroller.RestoreCursorAfterWheel = true;
        // In device independent pixels against physical ones, at 100% or more scaling this is at least the number of half pages
        Assert.True(testWindow.Offset / halfPage > 100, $"Only {testWindow.Offset / halfPage} half pages");

        Assert.True(scroller.Start());

        Assert.True(scroller.IsAtStart);
        Assert.Equal(0, testWindow.Offset, 1);
        // A step from the start moves the content and the position as expected
        Assert.True(scroller.Next());
        Assert.InRange(testWindow.Offset, halfPage * 0.25, halfPage * 2);
        Assert.True(scroller.Start());
        Assert.Equal(0, testWindow.Offset, 1);

        Assert.True(scroller.End());

        Assert.True(scroller.IsAtEnd);
        Assert.Equal(testWindow.MaxOffset, testWindow.Offset, 1);
    }

    /// <summary>
    ///     With a writable RangeValue Start and End set the value, one wheel notch confirms that the content followed
    /// </summary>
    [Fact]
    public void FromWindow_ScrollBarOnly_StartEndUseTheRangeValue()
    {
        using var testWindow = new ScrollBarOnlyTestWindow();
        testWindow.SkipWhenNotVisibleAt(ScrollTestWindow.CenterOf(testWindow.ContentBounds));
        using var scroller = UiAutomationScroller.FromWindow(testWindow.WindowHandle);
        Assert.NotNull(scroller);
        scroller.RestoreCursorAfterWheel = true;
        Assert.True(scroller.IsScrollBarFallback);

        Assert.True(scroller.End());
        Assert.True(scroller.IsAtEnd);
        Assert.Equal(testWindow.MaxOffset, testWindow.Offset, 1);

        Assert.True(scroller.Start());
        Assert.True(scroller.IsAtStart);
        Assert.Equal(0, testWindow.Offset, 1);
    }

    /// <summary>
    ///     Without RangeValue and thumb the position is unknown: one notch per step, the caller detects the end
    /// </summary>
    [Fact]
    public async Task ScrollBarOnly_UnknownPosition_StepsOneNotch()
    {
        using var testWindow = new ScrollBarOnlyTestWindow(ScrollBarExposure.Nothing);
        var center = ScrollTestWindow.CenterOf(testWindow.ControlBounds);
        testWindow.SkipWhenNotVisibleAt(center);
        using var scroller = UiAutomationScroller.FromPoint(center);
        Assert.NotNull(scroller);
        Assert.True(scroller.IsScrollBarFallback);
        Assert.False(scroller.IsPositionKnown);
        Assert.Equal(-1, scroller.ScrollPercent);
        Assert.Equal(-1, scroller.InitialScrollPercent);
        // WPF (Aero2) disables both line buttons while the mouse isn't over the scroll bar: that tells nothing
        Assert.False(scroller.IsAtStart);
        Assert.False(scroller.IsAtEnd);
        Assert.False(scroller.Start());
        scroller.RestoreCursorAfterWheel = true;

        Assert.True(scroller.Next());

        var expected = ScrollBarOnlyControl.LinesPerNotch * ScrollBarOnlyControl.LineHeight;
        await TestWait.UntilAsync(() => Math.Abs(testWindow.Offset - expected) < 0.5, $"The offset is not {expected}");
        Assert.False(scroller.Reset());
    }

    /// <summary>
    ///     Mouse wheel mode: wheel at the viewport centre until half a page moved, the cursor is put back
    /// </summary>
    /// <remarks>Interactive: moves the cursor and injects mouse wheel input.</remarks>
    [Fact]
    [Trait("Category", "Interactive")]
    public async Task MouseWheel_ScrollsAndRestoresTheCursor()
    {
        using var testWindow = new WpfScrollTestWindow();
        testWindow.SkipWhenNotVisibleAt(ScrollTestWindow.CenterOf(testWindow.ScrollingBounds));
        using var scroller = CreateScroller(testWindow);
        scroller.ScrollMode = UiAutomationScrollModes.MouseWheel;
        scroller.RestoreCursorAfterWheel = true;
        scroller.StepFraction = 0.5;
        var cursorBefore = User32Api.GetCursorLocation();

        Assert.True(scroller.Next());

        Assert.True(testWindow.VerticalOffset > 0);
        await TestWait.UntilAsync(() => User32Api.GetCursorLocation() == cursorBefore, "The cursor was not restored");
        Assert.True(scroller.End());
        Assert.True(scroller.IsAtEnd);
    }
}
