// Copyright (c) Dapplo and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Linq;
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
        (bool)typeof(UiAutomationAreas).GetMethod("HasLargeEmptyArea", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static).Invoke(null, new object[] { root });

    private static IEnumerable<UiAutomationArea> Flatten(UiAutomationArea area) => new[] { area }.Concat(area.Children.SelectMany(Flatten));

    private static UiAutomationArea Find(UiAutomationArea root, string name) => Flatten(root).FirstOrDefault(area => area.Name == name);

    private static bool IsInside(NativeRect outer, NativeRect inner) =>
        inner.Left >= outer.Left && inner.Top >= outer.Top && inner.Right <= outer.Right && inner.Bottom <= outer.Bottom;

    [Fact]
    public void FindAreas_HasThePanelsAndButtons_InsideTheWindow()
    {
        using var testWindow = new AreasTestWindow();

        var root = UiAutomationAreas.FindAreas(testWindow.WindowHandle);

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
    public void FindAreas_MinimumSize_DropsSmallAreas()
    {
        using var testWindow = new AreasTestWindow();

        Assert.NotNull(Find(UiAutomationAreas.FindAreas(testWindow.WindowHandle), "Tiny"));
        var root = UiAutomationAreas.FindAreas(testWindow.WindowHandle, minimumSize: 20);
        Assert.Null(Find(root, "Tiny"));
        Assert.NotNull(Find(root, "Deep"));
        Assert.All(Flatten(root), area => Assert.True(area.Bounds.Width >= 20 && area.Bounds.Height >= 20, area.ToString()));
    }

    [Fact]
    public void FindAreas_AreaWithTheBoundsOfItsParent_IsReplacedByItsChildren()
    {
        using var testWindow = new AreasTestWindow();

        var root = UiAutomationAreas.FindAreas(testWindow.WindowHandle);

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

    [Fact]
    public void FindAreas_NoWindow_ReturnsNull()
    {
        Assert.Null(UiAutomationAreas.FindAreas(IntPtr.Zero));
        Assert.Null(UiAutomationAreas.FindAreas((Dapplo.Windows.Desktop.IInteropWindow)null));
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
}
