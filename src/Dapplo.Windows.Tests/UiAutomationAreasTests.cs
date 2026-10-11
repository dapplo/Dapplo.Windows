// Copyright (c) Dapplo and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Automation;
using System.Windows.Controls;
using System.Windows.Threading;
using Dapplo.Log;
using Dapplo.Log.XUnit;
using Dapplo.Windows.Automation;
using Dapplo.Windows.Common.Structs;
using Xunit;
using WpfWindows = System.Windows;

namespace Dapplo.Windows.Tests;

/// <summary>
///     A WPF window with nested group boxes and buttons, a tiny button, and a wrapper which contains a second wrapper with the same bounds
/// </summary>
internal sealed class AreasTestWindow : ScrollTestWindow
{
    private WpfWindows.Window _window;
    private Dispatcher _dispatcher;
    private Button _deep;

    public AreasTestWindow() : base(nameof(AreasTestWindow), start: false)
    {
        Start();
    }

    private static T Named<T>(T element, string name) where T : WpfWindows.DependencyObject
    {
        AutomationProperties.SetName(element, name);
        return element;
    }

    protected override void Run()
    {
        _dispatcher = Dispatcher.CurrentDispatcher;
        _deep = Named(new Button { Content = "Deep", Width = 120, Height = 40, Margin = new WpfWindows.Thickness(10) }, "Deep");
        var inner = Named(new GroupBox { Header = "Inner", Content = _deep, Margin = new WpfWindows.Thickness(10) }, "Inner");
        var tiny = Named(new Button { Width = 10, Height = 10, Margin = new WpfWindows.Thickness(10), HorizontalAlignment = WpfWindows.HorizontalAlignment.Left }, "Tiny");
        var outerPanel = new StackPanel();
        outerPanel.Children.Add(inner);
        outerPanel.Children.Add(tiny);
        var outer = Named(new GroupBox { Header = "Outer", Content = outerPanel, Margin = new WpfWindows.Thickness(10), Width = 240 }, "Outer");

        // A wrapper whose only content is a second wrapper with the same bounds
        var wrapped = Named(new Button { Content = "Wrapped", Margin = new WpfWindows.Thickness(15) }, "Wrapped");
        var sameBounds = Named(new UserControl { Content = wrapped }, "SameBounds");
        var wrapper = Named(new UserControl { Content = sameBounds, Width = 180, Height = 100, Margin = new WpfWindows.Thickness(10), VerticalAlignment = WpfWindows.VerticalAlignment.Top }, "Wrapper");

        var root = new StackPanel { Orientation = Orientation.Horizontal };
        root.Children.Add(outer);
        root.Children.Add(wrapper);
        _window = new WpfWindows.Window
        {
            Title = "Dapplo.Windows UI Automation areas test",
            Left = 160,
            Top = 160,
            Width = 500,
            Height = 320,
            Topmost = true,
            ShowInTaskbar = false,
            Content = root
        };
        _window.Loaded += (_, _) => _dispatcher.BeginInvoke(DispatcherPriority.ContextIdle, new Action(() =>
        {
            WindowHandle = new WpfWindows.Interop.WindowInteropHelper(_window).Handle;
            ScrollingHandle = WindowHandle;
            SignalReady();
        }));
        _window.Closed += (_, _) => _dispatcher.InvokeShutdown();
        _window.Show();
        Dispatcher.Run();
    }

    /// <summary>The deep button in screen coordinates</summary>
    public NativeRect DeepBounds => Invoke(() =>
    {
        var topLeft = _deep.PointToScreen(new WpfWindows.Point(0, 0));
        var bottomRight = _deep.PointToScreen(new WpfWindows.Point(_deep.ActualWidth, _deep.ActualHeight));
        return new NativeRect((int)Math.Round(topLeft.X), (int)Math.Round(topLeft.Y), (int)Math.Round(bottomRight.X - topLeft.X), (int)Math.Round(bottomRight.Y - topLeft.Y));
    });

    public override NativeRect ScrollingBounds => DeepBounds;

    public override T Invoke<T>(Func<T> func) => _dispatcher.Invoke(func);

    public override void Close() => _dispatcher.Invoke(_window.Close);
}

/// <summary>
///     What the large panel of a <see cref="LargePanelTestWindow"/> holds
/// </summary>
public enum LargePanelContent
{
    /// <summary>Only buttons of 6 x 6 pixels</summary>
    SmallControls,

    /// <summary>Nothing</summary>
    Nothing,

    /// <summary>Only an empty panel with the same bounds (merged away)</summary>
    EmptyPanelWithTheSameBounds
}

/// <summary>
///     A WPF window with one large panel, see <see cref="LargePanelContent"/>
/// </summary>
internal sealed class LargePanelTestWindow : ScrollTestWindow
{
    private readonly LargePanelContent _content;
    private WpfWindows.Window _window;
    private Dispatcher _dispatcher;

    public LargePanelTestWindow(LargePanelContent content) : base(nameof(LargePanelTestWindow), start: false)
    {
        _content = content;
        Start();
    }

    protected override void Run()
    {
        _dispatcher = Dispatcher.CurrentDispatcher;
        var large = new UserControl { Margin = new WpfWindows.Thickness(10) };
        AutomationProperties.SetName(large, "Large");
        if (_content == LargePanelContent.EmptyPanelWithTheSameBounds)
        {
            var inner = new UserControl();
            AutomationProperties.SetName(inner, "SameBounds");
            large.Content = inner;
        }
        if (_content == LargePanelContent.SmallControls)
        {
            var panel = new WrapPanel();
            for (var i = 0; i < 10; i++)
            {
                var button = new Button { Width = 6, Height = 6, Margin = new WpfWindows.Thickness(4) };
                AutomationProperties.SetName(button, $"Small {i}");
                panel.Children.Add(button);
            }
            large.Content = panel;
        }
        _window = new WpfWindows.Window
        {
            Title = "Dapplo.Windows large panel test",
            Left = 180,
            Top = 180,
            Width = 400,
            Height = 300,
            Topmost = true,
            ShowInTaskbar = false,
            Content = large
        };
        _window.Loaded += (_, _) => _dispatcher.BeginInvoke(DispatcherPriority.ContextIdle, new Action(() =>
        {
            WindowHandle = new WpfWindows.Interop.WindowInteropHelper(_window).Handle;
            ScrollingHandle = WindowHandle;
            SignalReady();
        }));
        _window.Closed += (_, _) => _dispatcher.InvokeShutdown();
        _window.Show();
        Dispatcher.Run();
    }

    public override NativeRect ScrollingBounds => NativeRect.Empty;

    public override T Invoke<T>(Func<T> func) => _dispatcher.Invoke(func);

    public override void Close() => _dispatcher.Invoke(_window.Close);
}

public class UiAutomationAreasTests
{
    private const int ButtonControlType = 50000;
    private const int GroupControlType = 50026;

    public UiAutomationAreasTests(ITestOutputHelper testOutputHelper)
    {
        LogSettings.RegisterDefaultLogger<XUnitLogger>(LogLevels.Verbose, testOutputHelper);
    }

    // Internal (the test assembly can't see the internals of the signed library), no spans: reflection works
    private static bool HasLargeEmptyArea(UiAutomationArea root) =>
        (bool)typeof(UiAutomationAreas).GetMethod("HasLargeEmptyArea", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static, null, new[] { typeof(UiAutomationArea) }, null).Invoke(null, new object[] { root });

    private static IEnumerable<UiAutomationArea> Flatten(UiAutomationArea area) => new[] { area }.Concat(area.Children.SelectMany(Flatten));

    private static UiAutomationArea Find(UiAutomationArea root, string name) => Flatten(root).FirstOrDefault(area => area.Name == name);

    private static bool IsInside(NativeRect outer, NativeRect inner) =>
        inner.Left >= outer.Left && inner.Top >= outer.Top && inner.Right <= outer.Right && inner.Bottom <= outer.Bottom;

    [Fact]
    public async Task FindAreas_HasThePanelsAndButtons_InsideTheWindow()
    {
        using var testWindow = new AreasTestWindow();

        var root = await UiAutomationAreas.FindAreasAsync(testWindow.WindowHandle, maxDepth: 10, cancellationToken: TestContext.Current.CancellationToken);

        Assert.NotNull(root);
        Assert.False(root.Bounds.IsEmpty);
        var outer = Find(root, "Outer");
        var inner = Find(root, "Inner");
        var deep = Find(root, "Deep");
        Assert.NotNull(outer);
        Assert.NotNull(inner);
        Assert.NotNull(deep);
        Assert.Equal(GroupControlType, outer.ControlType);
        Assert.Equal(ButtonControlType, deep.ControlType);
        // Nested: the deep button is in the inner group, which is in the outer group
        Assert.Contains(inner, Flatten(outer));
        Assert.Contains(deep, Flatten(inner));
        Assert.All(Flatten(root).Skip(1), area => Assert.True(IsInside(root.Bounds, area.Bounds), $"{area} is not inside {root.Bounds}"));
        // Every area lies inside its parent
        foreach (var area in Flatten(root))
        {
            Assert.All(area.Children, child => Assert.True(IsInside(area.Bounds, child.Bounds), $"{child} is not inside {area}"));
        }
        var expected = testWindow.DeepBounds;
        Assert.InRange(deep.Bounds.X, expected.X - 2, expected.X + 2);
        Assert.InRange(deep.Bounds.Y, expected.Y - 2, expected.Y + 2);
        Assert.InRange(deep.Bounds.Width, expected.Width - 2, expected.Width + 2);
        Assert.InRange(deep.Bounds.Height, expected.Height - 2, expected.Height + 2);

        // The chain at the middle of the deep button: deepest first (the text of the button, then the button), the window last
        var chain = root.GetAreasAt(new NativePoint(deep.Bounds.X + deep.Bounds.Width / 2, deep.Bounds.Y + deep.Bounds.Height / 2)).ToList();
        Assert.True(chain[0] == deep || deep.Children.Contains(chain[0]), chain[0].ToString());
        Assert.True(chain.IndexOf(deep) < chain.IndexOf(inner) && chain.IndexOf(inner) < chain.IndexOf(outer));
        Assert.Same(root, chain[chain.Count - 1]);
        Assert.False(HasLargeEmptyArea(root));
    }

    [Fact]
    public async Task FindAreas_MinimumSize_DropsSmallAreas()
    {
        using var testWindow = new AreasTestWindow();

        Assert.NotNull(Find(await UiAutomationAreas.FindAreasAsync(testWindow.WindowHandle, maxDepth: 10, cancellationToken: TestContext.Current.CancellationToken), "Tiny"));
        var root = await UiAutomationAreas.FindAreasAsync(testWindow.WindowHandle, maxDepth: 10, minimumSize: 20, cancellationToken: TestContext.Current.CancellationToken);
        Assert.Null(Find(root, "Tiny"));
        Assert.NotNull(Find(root, "Deep"));
        Assert.All(Flatten(root), area => Assert.True(area.Bounds.Width >= 20 && area.Bounds.Height >= 20, area.ToString()));
    }

    [Fact]
    public async Task FindAreas_AreaWithTheBoundsOfItsParent_IsReplacedByItsChildren()
    {
        using var testWindow = new AreasTestWindow();

        var root = await UiAutomationAreas.FindAreasAsync(testWindow.WindowHandle, maxDepth: 10, cancellationToken: TestContext.Current.CancellationToken);

        var wrapper = Find(root, "Wrapper");
        Assert.NotNull(wrapper);
        Assert.Null(Find(root, "SameBounds"));
        Assert.Contains(wrapper.Children, child => child.Name == "Wrapped");
        // No area has a child with exactly its bounds
        foreach (var area in Flatten(root))
        {
            Assert.DoesNotContain(area.Children, child => child.Bounds == area.Bounds);
        }
    }

    /// <summary>
    ///     maxDepth counts the levels of the result, after merging: the wrapper with the same bounds as its parent doesn't count
    /// </summary>
    [Fact]
    public async Task FindAreas_MaxDepth_CountsAfterMerging()
    {
        using var testWindow = new AreasTestWindow();
        var token = TestContext.Current.CancellationToken;

        var levelOne = await UiAutomationAreas.FindAreasAsync(testWindow.WindowHandle, maxDepth: 1, cancellationToken: token);
        Assert.NotNull(Find(levelOne, "Outer"));
        Assert.NotNull(Find(levelOne, "Wrapper"));
        Assert.Null(Find(levelOne, "Inner"));
        Assert.Null(Find(levelOne, "Wrapped"));
        Assert.All(levelOne.Children, child => Assert.Empty(child.Children));
        // Only the window
        Assert.Empty((await UiAutomationAreas.FindAreasAsync(testWindow.WindowHandle, maxDepth: 0, cancellationToken: token)).Children);

        // Wrapper (1) > SameBounds (merged) > Wrapped (2)
        var levelTwo = await UiAutomationAreas.FindAreasAsync(testWindow.WindowHandle, maxDepth: 2, cancellationToken: token);
        Assert.Contains(Find(levelTwo, "Wrapper").Children, child => child.Name == "Wrapped");
        Assert.NotNull(Find(levelTwo, "Inner"));
        Assert.Null(Find(levelTwo, "Deep"));
    }

    [Fact]
    public async Task FindAreas_Canceled_Throws()
    {
        using var testWindow = new AreasTestWindow();
        using var cancellationTokenSource = new CancellationTokenSource();
        cancellationTokenSource.Cancel();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => UiAutomationAreas.FindAreasAsync(testWindow.WindowHandle, cancellationToken: cancellationTokenSource.Token));
    }

    [Fact]
    public async Task FindAreas_NoWindow_ReturnsNull()
    {
        var token = TestContext.Current.CancellationToken;
        Assert.Null(await UiAutomationAreas.FindAreasAsync(IntPtr.Zero, cancellationToken: token));
        Assert.Null(await UiAutomationAreas.FindAreasAsync((Dapplo.Windows.Desktop.IInteropWindow)null, cancellationToken: token));
        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(() => UiAutomationAreas.FindAreasAsync(new IntPtr(1), maxDepth: -1, cancellationToken: token));
    }

    // ── GetAreasAt, on hand-built trees ───────────────────────────────────────

    private static readonly UiAutomationArea Deep = new(new NativeRect(20, 20, 20, 20), ButtonControlType, "Deep");
    private static readonly UiAutomationArea Lower = new(new NativeRect(10, 10, 60, 60), GroupControlType, "Lower", new[] { Deep });
    // Overlaps Lower and comes after it: drawn on top
    private static readonly UiAutomationArea Upper = new(new NativeRect(50, 50, 40, 40), GroupControlType, "Upper");
    private static readonly UiAutomationArea Root = new(new NativeRect(0, 0, 100, 100), 50032, "Window", new[] { Lower, Upper });

    [Fact]
    public void GetAreasAt_DeepestFirst()
    {
        Assert.Equal(new[] { Deep, Lower, Root }, Root.GetAreasAt(new NativePoint(25, 25)));
        Assert.Equal(new[] { Lower, Root }, Root.GetAreasAt(new NativePoint(15, 15)));
        Assert.Equal(new[] { Root }, Root.GetAreasAt(new NativePoint(95, 5)));
    }

    [Fact]
    public void GetAreasAt_TheTopMostSiblingWins()
    {
        // In Lower and Upper, Upper comes last
        Assert.Equal(new[] { Upper, Root }, Root.GetAreasAt(new NativePoint(60, 60)));
    }

    [Fact]
    public void GetAreasAt_OutsideTheRoot_IsEmpty()
    {
        Assert.Empty(Root.GetAreasAt(new NativePoint(100, 50)));
        Assert.Empty(Root.GetAreasAt(new NativePoint(-1, 50)));
    }

    // Internal, no spans: reflection works
    private static bool NeedsSecondRead(IntPtr windowHandle, int maxDepth, int minimumSize) =>
        (bool)typeof(UiAutomationAreas).GetMethod("NeedsSecondRead", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static)
            .Invoke(null, new object[] { windowHandle, maxDepth, minimumSize });

    /// <summary>
    ///     A large area whose children are all smaller than minimumSize has content: no second read (Gmail in Edge read twice on every call)
    /// </summary>
    [Fact]
    public async Task FindAreas_LargeAreaWithOnlySmallChildren_IsNotReadAgain()
    {
        using var testWindow = new LargePanelTestWindow(LargePanelContent.SmallControls);

        Assert.False(NeedsSecondRead(testWindow.WindowHandle, 3, 20));
        Assert.False(NeedsSecondRead(testWindow.WindowHandle, 3, 0));

        var root = await UiAutomationAreas.FindAreasAsync(testWindow.WindowHandle, minimumSize: 20, cancellationToken: TestContext.Current.CancellationToken);
        var large = Find(root, "Large");
        Assert.NotNull(large);
        // The small buttons were left out
        Assert.Empty(large.Children);
        Assert.NotNull(Find(await UiAutomationAreas.FindAreasAsync(testWindow.WindowHandle, cancellationToken: TestContext.Current.CancellationToken), "Small 0"));
    }

    /// <summary>
    ///     A large element which reports no children still is content which may not be there yet, unless it wasn't read because of maxDepth
    /// </summary>
    [Theory]
    [InlineData(LargePanelContent.Nothing)]
    [InlineData(LargePanelContent.EmptyPanelWithTheSameBounds)]
    public void FindAreas_LargeAreaWithoutContent_IsReadAgain(LargePanelContent content)
    {
        using var testWindow = new LargePanelTestWindow(content);

        Assert.True(NeedsSecondRead(testWindow.WindowHandle, 3, 0));
        // At maxDepth 1 the panel's children aren't read
        Assert.False(NeedsSecondRead(testWindow.WindowHandle, 1, 0));
    }

    [Fact]
    public void HasLargeEmptyArea_OnlyForAnEmptyAreaOfAQuarterOrMore()
    {
        Assert.False(HasLargeEmptyArea(Root));
        var emptyContent = new UiAutomationArea(new NativeRect(0, 20, 100, 30), 50033, "Content");
        Assert.True(HasLargeEmptyArea(new UiAutomationArea(new NativeRect(0, 0, 100, 100), 50032, "Window", new[] { Deep, emptyContent })));
        var smallEmpty = new UiAutomationArea(new NativeRect(0, 20, 100, 20), 50033, "Small");
        Assert.False(HasLargeEmptyArea(new UiAutomationArea(new NativeRect(0, 0, 100, 100), 50032, "Window", new[] { smallEmpty })));
        Assert.False(HasLargeEmptyArea(new UiAutomationArea(new NativeRect(0, 0, 100, 100), 50032, "Window")));
        // An empty overlay over a sibling with the same bounds and content (Edge has one) doesn't count, nested as well
        var overlay = new UiAutomationArea(new NativeRect(0, 0, 90, 90), 50033, "Overlay");
        var content = new UiAutomationArea(new NativeRect(0, 0, 90, 90), 50033, "Content", new[] { Deep });
        Assert.False(HasLargeEmptyArea(new UiAutomationArea(new NativeRect(0, 0, 100, 100), 50032, "Window", new[] { overlay, content })));
        var nestedEmpty = new UiAutomationArea(new NativeRect(0, 10, 90, 80), 50033, "NotYetBuilt");
        var frame = new UiAutomationArea(new NativeRect(0, 0, 90, 90), 50033, "Frame", new[] { nestedEmpty });
        Assert.True(HasLargeEmptyArea(new UiAutomationArea(new NativeRect(0, 0, 100, 100), 50032, "Window", new[] { overlay, frame })));
    }

    [Fact]
    public void Area_NullNameAndChildren_AreEmpty()
    {
        var area = new UiAutomationArea(new NativeRect(0, 0, 1, 1), 0, null);
        Assert.Equal(string.Empty, area.Name);
        Assert.NotNull(area.Children);
        Assert.Empty(area.Children);
    }

    private static UiAutomationArea ReadUntilComplete(Func<(UiAutomationArea Root, bool ReadAgain)> read, TimeSpan contentWait, TimeSpan pause, CancellationToken cancellationToken)
    {
        var method = typeof(UiAutomationAreas).GetMethod("ReadUntilComplete", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
        try
        {
            return (UiAutomationArea)method.Invoke(null, new object[] { read, contentWait, pause, IntPtr.Zero, cancellationToken });
        }
        catch (System.Reflection.TargetInvocationException ex) when (ex.InnerException != null)
        {
            System.Runtime.ExceptionServices.ExceptionDispatchInfo.Capture(ex.InnerException).Throw();
            throw;
        }
    }

    private static UiAutomationArea TreeNumber(int number) => new UiAutomationArea(new NativeRect(0, 0, 100, 100), 50032, $"Read {number}");

    [Fact]
    public void ReadUntilComplete_ReadsAgainUntilTheTreeIsComplete()
    {
        var reads = 0;
        var result = ReadUntilComplete(() =>
        {
            reads++;
            return (TreeNumber(reads), reads < 3);
        }, TimeSpan.FromSeconds(5), TimeSpan.FromMilliseconds(10), TestContext.Current.CancellationToken);

        Assert.Equal(3, reads);
        Assert.Equal("Read 3", result.Name);
    }

    [Fact]
    public void ReadUntilComplete_CompleteTree_ReadsOnce()
    {
        var reads = 0;
        var result = ReadUntilComplete(() => { reads++; return (TreeNumber(reads), false); }, TimeSpan.FromSeconds(5), TimeSpan.FromMilliseconds(10), TestContext.Current.CancellationToken);
        Assert.Equal(1, reads);
        Assert.Equal("Read 1", result.Name);
    }

    [Fact]
    public void ReadUntilComplete_ZeroWait_ReadsOnce()
    {
        var reads = 0;
        var result = ReadUntilComplete(() => { reads++; return (TreeNumber(reads), true); }, TimeSpan.Zero, TimeSpan.FromMilliseconds(10), TestContext.Current.CancellationToken);
        Assert.Equal(1, reads);
        Assert.Equal("Read 1", result.Name);
    }

    [Fact]
    public void ReadUntilComplete_StaysEmpty_StopsAfterTheWaitWithTheLastResult()
    {
        var reads = 0;
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();
        var result = ReadUntilComplete(() => { reads++; return (TreeNumber(reads), true); }, TimeSpan.FromMilliseconds(300), TimeSpan.FromMilliseconds(50), TestContext.Current.CancellationToken);
        stopwatch.Stop();

        // About 300 ms / 50 ms pauses plus the first read; generous bounds for a busy build agent
        Assert.InRange(reads, 2, 8);
        Assert.Equal($"Read {reads}", result.Name);
        Assert.InRange(stopwatch.ElapsedMilliseconds, 250, 2000);
    }

    [Fact]
    public void ReadUntilComplete_NullResult_StopsAtOnce()
    {
        var reads = 0;
        var result = ReadUntilComplete(() => { reads++; return (null, true); }, TimeSpan.FromSeconds(5), TimeSpan.FromMilliseconds(10), TestContext.Current.CancellationToken);
        Assert.Equal(1, reads);
        Assert.Null(result);
    }

    [Fact]
    public void ReadUntilComplete_CancelDuringPause_Throws()
    {
        using var cancellationTokenSource = new CancellationTokenSource();
        var reads = 0;
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();
        Assert.Throws<OperationCanceledException>(() => ReadUntilComplete(() =>
        {
            reads++;
            // Cancel while the loop is about to pause for a long time
            cancellationTokenSource.CancelAfter(100);
            return (TreeNumber(reads), true);
        }, TimeSpan.FromSeconds(30), TimeSpan.FromSeconds(10), cancellationTokenSource.Token));
        stopwatch.Stop();

        Assert.Equal(1, reads);
        Assert.True(stopwatch.ElapsedMilliseconds < 5000, $"Cancellation took {stopwatch.ElapsedMilliseconds} ms");
    }

    [Fact]
    public async Task FindAreasAsync_NegativeContentWait_Throws()
    {
        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(() => UiAutomationAreas.FindAreasAsync(IntPtr.Zero, contentWait: TimeSpan.FromSeconds(-1), cancellationToken: TestContext.Current.CancellationToken));
    }
    // ── FindScrollableAreas with a content wait ─────────────────────────────

    private static IReadOnlyList<NativeRect> ReadScrollableAreas(Func<IReadOnlyList<NativeRect>> find, Func<bool> looksIncomplete, TimeSpan contentWait, TimeSpan pause,
        CancellationToken cancellationToken)
    {
        var method = typeof(UiAutomationScroller).GetMethod("ReadScrollableAreas", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
        try
        {
            return (IReadOnlyList<NativeRect>)method.Invoke(null, new object[] { find, looksIncomplete, contentWait, pause, IntPtr.Zero, cancellationToken });
        }
        catch (System.Reflection.TargetInvocationException ex) when (ex.InnerException != null)
        {
            System.Runtime.ExceptionServices.ExceptionDispatchInfo.Capture(ex.InnerException).Throw();
            throw;
        }
    }

    private static readonly IReadOnlyList<NativeRect> NoAreas = Array.Empty<NativeRect>();
    private static readonly IReadOnlyList<NativeRect> OneArea = new[] { new NativeRect(10, 10, 100, 100) };

    [Fact]
    public void ScrollableAreas_ZeroWait_SearchesOnce()
    {
        var finds = 0;
        var checks = 0;
        var result = ReadScrollableAreas(() => { finds++; return NoAreas; }, () => { checks++; return true; }, TimeSpan.Zero, TimeSpan.FromMilliseconds(10),
            TestContext.Current.CancellationToken);
        Assert.Empty(result);
        Assert.Equal(1, finds);
        // Nothing to decide with a single search: no shallow read
        Assert.Equal(0, checks);
    }

    [Fact]
    public void ScrollableAreas_Found_DoesNotCheckTheTree()
    {
        var finds = 0;
        var checks = 0;
        var result = ReadScrollableAreas(() => { finds++; return OneArea; }, () => { checks++; return true; }, TimeSpan.FromSeconds(5), TimeSpan.FromMilliseconds(10),
            TestContext.Current.CancellationToken);
        Assert.Same(OneArea, result);
        Assert.Equal(1, finds);
        Assert.Equal(0, checks);
    }

    [Fact]
    public void ScrollableAreas_NothingAndCompleteTree_DoesNotWait()
    {
        var finds = 0;
        var checks = 0;
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();
        var result = ReadScrollableAreas(() => { finds++; return NoAreas; }, () => { checks++; return false; }, TimeSpan.FromSeconds(5), TimeSpan.FromSeconds(1),
            TestContext.Current.CancellationToken);
        Assert.Empty(result);
        // Once more after the check: the tree may have been completed during the first search
        Assert.Equal(2, finds);
        Assert.Equal(1, checks);
        Assert.True(stopwatch.ElapsedMilliseconds < 900, $"Took {stopwatch.ElapsedMilliseconds} ms");
    }

    /// <summary>
    ///     Cold Edge: the first search makes Chromium build the tree, the check right after it sees the complete tree; the search after the
    ///     check finds the page
    /// </summary>
    [Fact]
    public void ScrollableAreas_TreeCompletedDuringTheSearch_SearchesOnceMore()
    {
        var finds = 0;
        var checks = 0;
        var result = ReadScrollableAreas(() => ++finds == 1 ? NoAreas : OneArea, () => { checks++; return false; }, TimeSpan.FromSeconds(5), TimeSpan.FromSeconds(1),
            TestContext.Current.CancellationToken);
        Assert.Same(OneArea, result);
        Assert.Equal(2, finds);
        Assert.Equal(1, checks);
    }

    private static bool LooksIncomplete(UiAutomationArea root, bool rootHasContent, Func<UiAutomationArea, bool> isEmpty) =>
        (bool)typeof(UiAutomationAreas).GetMethod("LooksIncomplete", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static, null,
            new[] { typeof(UiAutomationArea), typeof(bool), typeof(Func<UiAutomationArea, bool>) }, null).Invoke(null, new object[] { root, rootHasContent, isEmpty });

    /// <summary>
    ///     The window's own element without content looks incomplete (Chromium's render widget window before its tree is built: the document
    ///     has the window's bounds and is merged into the root); with content (e.g. only small children, left out) it doesn't
    /// </summary>
    [Fact]
    public void LooksIncomplete_RootWithoutContent()
    {
        var renderWidget = new UiAutomationArea(new NativeRect(0, 0, 976, 696), 50033, "Render widget");
        Func<UiAutomationArea, bool> isEmpty = area => area.Children.Count == 0;
        Assert.True(LooksIncomplete(renderWidget, false, isEmpty));
        Assert.False(LooksIncomplete(renderWidget, true, isEmpty));
        // HasLargeEmptyArea alone only looks at the root's children
        Assert.False(HasLargeEmptyArea(renderWidget));
        // A large empty child still counts, a window which can't be read or has no size doesn't
        Assert.True(LooksIncomplete(new UiAutomationArea(new NativeRect(0, 0, 100, 100), 50032, "Window",
            new[] { new UiAutomationArea(new NativeRect(0, 20, 100, 60), 50030, "Document") }), true, isEmpty));
        Assert.False(LooksIncomplete(null, false, isEmpty));
        Assert.False(LooksIncomplete(new UiAutomationArea(NativeRect.Empty, 50032, "Window"), false, isEmpty));
    }

    [Fact]
    public void ScrollableAreas_IncompleteTree_SearchesAgainUntilFound()
    {
        var finds = 0;
        var result = ReadScrollableAreas(() => ++finds < 3 ? NoAreas : OneArea, () => true, TimeSpan.FromSeconds(5), TimeSpan.FromMilliseconds(10),
            TestContext.Current.CancellationToken);
        Assert.Same(OneArea, result);
        Assert.Equal(3, finds);
    }

    [Fact]
    public void ScrollableAreas_CancelDuringPause_Throws()
    {
        using var cancellationTokenSource = new CancellationTokenSource();
        var finds = 0;
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();
        Assert.Throws<OperationCanceledException>(() => ReadScrollableAreas(() =>
        {
            finds++;
            cancellationTokenSource.CancelAfter(100);
            return NoAreas;
        }, () => true, TimeSpan.FromSeconds(30), TimeSpan.FromSeconds(10), cancellationTokenSource.Token));
        Assert.Equal(1, finds);
        Assert.True(stopwatch.ElapsedMilliseconds < 5000, $"Cancellation took {stopwatch.ElapsedMilliseconds} ms");
    }

    [Fact]
    public void FindScrollableAreas_NegativeContentWait_Throws()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => UiAutomationScroller.FindScrollableAreas(IntPtr.Zero, false, null, true, TimeSpan.FromSeconds(-1),
            TestContext.Current.CancellationToken));
    }

    /// <summary>
    ///     Real windows with a complete tree return without waiting: one which can scroll, one with nothing to scroll, and one whose
    ///     large panel only holds small controls (left out by the shallow read, the panel has content)
    /// </summary>
    [Fact]
    public void FindScrollableAreas_CompleteTree_ReturnsWithoutWaiting()
    {
        var contentWait = TimeSpan.FromSeconds(10);
        using (var scrollWindow = new WpfScrollTestWindow())
        {
            var stopwatch = System.Diagnostics.Stopwatch.StartNew();
            var areas = UiAutomationScroller.FindScrollableAreas(scrollWindow.WindowHandle, false, null, true, contentWait, TestContext.Current.CancellationToken);
            Assert.NotEmpty(areas);
            Assert.True(stopwatch.Elapsed < TimeSpan.FromSeconds(5), $"Took {stopwatch.ElapsedMilliseconds} ms");
        }
        using (var areasWindow = new AreasTestWindow())
        {
            var stopwatch = System.Diagnostics.Stopwatch.StartNew();
            var areas = UiAutomationScroller.FindScrollableAreas(areasWindow.WindowHandle, false, null, true, contentWait, TestContext.Current.CancellationToken);
            Assert.Empty(areas);
            Assert.True(stopwatch.Elapsed < TimeSpan.FromSeconds(5), $"Took {stopwatch.ElapsedMilliseconds} ms");
        }
        using (var smallControlsWindow = new LargePanelTestWindow(LargePanelContent.SmallControls))
        {
            var stopwatch = System.Diagnostics.Stopwatch.StartNew();
            var areas = UiAutomationScroller.FindScrollableAreas(smallControlsWindow.WindowHandle, false, null, true, contentWait, TestContext.Current.CancellationToken);
            Assert.Empty(areas);
            Assert.True(stopwatch.Elapsed < TimeSpan.FromSeconds(5), $"Took {stopwatch.ElapsedMilliseconds} ms");
        }
    }

    /// <summary>
    ///     A child window whose element has no children (here a button) is complete: no content wait, the caller asks its parent at once.
    ///     Only Chromium's render widget window counts as incomplete without content.
    /// </summary>
    [Fact]
    public void FindScrollableAreas_ChildWindowWithoutChildren_ReturnsWithoutWaiting()
    {
        using var testWindow = new QueryTestWindow("Dapplo scrollable areas leaf");
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();
        var areas = UiAutomationScroller.FindScrollableAreas(testWindow.TopButtonHandle, false, null, true, TimeSpan.FromSeconds(10), TestContext.Current.CancellationToken);
        Assert.Empty(areas);
        Assert.True(stopwatch.Elapsed < TimeSpan.FromSeconds(5), $"Took {stopwatch.ElapsedMilliseconds} ms");
    }

    /// <summary>
    ///     A large element without content and nothing to scroll looks like a tree which isn't built yet: the whole content wait, once
    /// </summary>
    [Fact]
    public void FindScrollableAreas_LargeEmptyElement_WaitsTheContentWait()
    {
        using var testWindow = new LargePanelTestWindow(LargePanelContent.Nothing);
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();
        var areas = UiAutomationScroller.FindScrollableAreas(testWindow.WindowHandle, false, null, true, TimeSpan.FromMilliseconds(800), TestContext.Current.CancellationToken);
        stopwatch.Stop();
        Assert.Empty(areas);
        Assert.InRange(stopwatch.ElapsedMilliseconds, 700, 10000);

        // The overloads without a content wait search once
        stopwatch.Restart();
        Assert.Empty(UiAutomationScroller.FindScrollableAreas(testWindow.WindowHandle));
        Assert.True(stopwatch.ElapsedMilliseconds < 700, $"Took {stopwatch.ElapsedMilliseconds} ms");
    }

    /// <summary>
    ///     Cancellation with a real window during the wait for content
    /// </summary>
    [Fact]
    public void FindScrollableAreas_CancelDuringTheContentWait_Throws()
    {
        using var testWindow = new LargePanelTestWindow(LargePanelContent.Nothing);
        using var cancellationTokenSource = new CancellationTokenSource(TimeSpan.FromMilliseconds(500));
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();
        Assert.ThrowsAny<OperationCanceledException>(() =>
            UiAutomationScroller.FindScrollableAreas(testWindow.WindowHandle, false, null, true, TimeSpan.FromSeconds(30), cancellationTokenSource.Token));
        Assert.True(stopwatch.Elapsed < TimeSpan.FromSeconds(10), $"Cancellation took {stopwatch.ElapsedMilliseconds} ms");
    }
}
