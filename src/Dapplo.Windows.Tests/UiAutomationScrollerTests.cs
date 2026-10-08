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
