// Copyright (c) Dapplo and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Automation;
using System.Windows.Automation.Peers;
using System.Windows.Automation.Provider;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Threading;
using Dapplo.Log;
using Dapplo.Log.XUnit;
using Dapplo.Windows.Automation;
using Dapplo.Windows.Common.Structs;
using Dapplo.Windows.User32;
using Dapplo.Windows.User32.Structs;
using Xunit;
using WpfWindows = System.Windows;

namespace Dapplo.Windows.Tests;

/// <summary>
///     A WPF Hyperlink whose automation peer has a ValuePattern with the target, like the links of a browser
/// </summary>
internal sealed class ValueHyperlink : Hyperlink
{
    public ValueHyperlink(string text, string target) : base(new Run(text))
    {
        NavigateUri = new Uri(target);
    }

    protected override AutomationPeer OnCreateAutomationPeer() => new ValueHyperlinkAutomationPeer(this);

    private sealed class ValueHyperlinkAutomationPeer : HyperlinkAutomationPeer, IValueProvider
    {
        private readonly ValueHyperlink _owner;

        public ValueHyperlinkAutomationPeer(ValueHyperlink owner) : base(owner)
        {
            _owner = owner;
        }

        public override object GetPattern(PatternInterface patternInterface) =>
            patternInterface == PatternInterface.Value ? this : base.GetPattern(patternInterface);

        public bool IsReadOnly => true;

        public string Value => _owner.Dispatcher.Invoke(() => _owner.NavigateUri?.ToString());

        public void SetValue(string value) => throw new InvalidOperationException("Read only");
    }
}

/// <summary>
///     A WPF window with a TextBlock, a TextBlock with a link, a RichTextBox with a link, a PasswordBox and a ScrollViewer whose second text
///     is scrolled away
/// </summary>
internal sealed class TextTestWindow : ScrollTestWindow
{
    public const string Password = "Secret4711";
    public const string LinkTarget = "https://www.dapplo.net/";
    public const string RichLinkTarget = "https://www.dapplo.net/greenshot";

    private WpfWindows.Window _window;
    private Dispatcher _dispatcher;
    private TextBlock _plain;

    public TextTestWindow() : base(nameof(TextTestWindow), start: false)
    {
        Start();
    }

    protected override void Run()
    {
        _dispatcher = Dispatcher.CurrentDispatcher;
        _plain = new TextBlock { Text = "Plain text block", Margin = new WpfWindows.Thickness(10), HorizontalAlignment = WpfWindows.HorizontalAlignment.Left };
        var linkBlock = new TextBlock { Margin = new WpfWindows.Thickness(10) };
        linkBlock.Inlines.Add(new ValueHyperlink("Dapplo homepage", LinkTarget));

        var paragraph = new Paragraph();
        paragraph.Inlines.Add(new Run("Rich text about "));
        paragraph.Inlines.Add(new ValueHyperlink("Greenshot", RichLinkTarget));
        paragraph.Inlines.Add(new Run(" here"));
        var richTextBox = new RichTextBox
        {
            Document = new FlowDocument(paragraph),
            IsReadOnly = true,
            IsDocumentEnabled = true,
            Height = 50,
            Margin = new WpfWindows.Thickness(10)
        };

        var passwordBox = new PasswordBox { Password = Password, Width = 200, Margin = new WpfWindows.Thickness(10), HorizontalAlignment = WpfWindows.HorizontalAlignment.Left };

        var scrolled = new StackPanel();
        scrolled.Children.Add(new TextBlock { Text = "Visible inside" });
        scrolled.Children.Add(new Border { Height = 2000 });
        scrolled.Children.Add(new TextBlock { Text = "Offscreen words" });
        var scrollViewer = new ScrollViewer { Content = scrolled, Height = 60, Margin = new WpfWindows.Thickness(10) };

        var root = new StackPanel();
        root.Children.Add(_plain);
        root.Children.Add(linkBlock);
        root.Children.Add(richTextBox);
        root.Children.Add(passwordBox);
        root.Children.Add(scrollViewer);
        _window = new WpfWindows.Window
        {
            Title = "Dapplo.Windows UI Automation text test",
            Left = 170,
            Top = 140,
            Width = 420,
            Height = 380,
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

    /// <summary>The plain TextBlock in screen coordinates</summary>
    public NativeRect PlainBounds => Invoke(() =>
    {
        var topLeft = _plain.PointToScreen(new WpfWindows.Point(0, 0));
        var bottomRight = _plain.PointToScreen(new WpfWindows.Point(_plain.ActualWidth, _plain.ActualHeight));
        return new NativeRect((int)Math.Round(topLeft.X), (int)Math.Round(topLeft.Y), (int)Math.Round(bottomRight.X - topLeft.X), (int)Math.Round(bottomRight.Y - topLeft.Y));
    });

    public override NativeRect ScrollingBounds => NativeRect.Empty;

    public override T Invoke<T>(Func<T> func) => _dispatcher.Invoke(func);

    public override void Close() => _dispatcher.Invoke(_window.Close);
}

public class UiAutomationTextTests
{
    public UiAutomationTextTests(ITestOutputHelper testOutputHelper)
    {
        LogSettings.RegisterDefaultLogger<XUnitLogger>(LogLevels.Verbose, testOutputHelper);
    }

    private static bool IsInside(NativeRect outer, NativeRect inner) =>
        inner.Left >= outer.Left && inner.Top >= outer.Top && inner.Right <= outer.Right && inner.Bottom <= outer.Bottom;

    // Internal (the test assembly can't see the internals of the signed library): reflection
    private static List<UiAutomationTextLine> GroupIntoLines(IEnumerable<UiAutomationTextWord> words) =>
        (List<UiAutomationTextLine>)typeof(UiAutomationText).GetMethod("GroupIntoLines", BindingFlags.NonPublic | BindingFlags.Static).Invoke(null, new object[] { words });

    private static List<NativeRect> ToRectangles(double[] rectangles) =>
        (List<NativeRect>)typeof(UiAutomationText).GetMethod("ToRectangles", BindingFlags.NonPublic | BindingFlags.Static).Invoke(null, new object[] { rectangles });

    private static List<UiAutomationTextWord> SplitIntoWords(string text, IReadOnlyList<NativeRect> rectangles, NativeRect clip, string linkTarget) =>
        (List<UiAutomationTextWord>)typeof(UiAutomationText).GetMethod("SplitIntoWords", BindingFlags.NonPublic | BindingFlags.Static)
            .Invoke(null, new object[] { text, rectangles, clip, linkTarget });

    private static List<NativeRect> SplitIntoLineBands(NativeRect bounds, double lineHeight) =>
        (List<NativeRect>)typeof(UiAutomationText).GetMethod("SplitIntoLineBands", BindingFlags.NonPublic | BindingFlags.Static).Invoke(null, new object[] { bounds, lineHeight });

    /// <summary>
    ///     The internal read, with readDocumentSubtrees false: elements with a TextPattern are read level by level (the fallback when the
    ///     request for the subtree fails)
    /// </summary>
    private static Task<UiAutomationTextSnapshot> ReadLevelByLevelAsync(IntPtr windowHandle) =>
        Task.Run(() => (UiAutomationTextSnapshot)typeof(UiAutomationText).GetMethod("ReadVisibleText", BindingFlags.NonPublic | BindingFlags.Static)
            .Invoke(null, new object[] { windowHandle, NativeRect.Empty, TimeSpan.FromSeconds(2), TimeSpan.FromSeconds(2), null, false, CancellationToken.None }));

    /// <summary>
    ///     What every read of a <see cref="TextTestWindow"/> must have
    /// </summary>
    private static void AssertTextOfTestWindow(UiAutomationTextSnapshot snapshot, IntPtr windowHandle)
    {
        Assert.True(snapshot.IsComplete);
        Assert.False(snapshot.TimedOut);
        var words = snapshot.Words.ToList();
        Assert.NotEmpty(words);
        var text = snapshot.Text;
        Assert.Contains("Plain", text);
        Assert.Contains("block", text);
        // The link in a TextBlock, with its target
        Assert.Contains(words, word => word.Text.Contains("Dapplo") && word.LinkTarget == TextTestWindow.LinkTarget);
        // The RichTextBox through its TextPattern: words, the link with its target, the other words without
        Assert.Contains(words, word => word.Text == "Rich" && word.LinkTarget is null);
        Assert.Contains(words, word => word.Text.StartsWith("Greenshot", StringComparison.Ordinal) && word.LinkTarget == TextTestWindow.RichLinkTarget);
        Assert.Contains("Visible inside", text);
        Assert.DoesNotContain("Offscreen", text);
        Assert.DoesNotContain("Secret", text);
        var windowBounds = WindowBounds(windowHandle);
        Assert.All(words, word => Assert.True(IsInside(windowBounds, word.Bounds), $"{word} is not inside {windowBounds}"));
    }

    private static NativeRect WindowBounds(IntPtr windowHandle)
    {
        var info = WindowInfo.Create();
        Assert.True(User32Api.GetWindowInfo(windowHandle, ref info));
        return info.Bounds;
    }

    [Fact]
    public async Task ReadVisibleText_HasTheTextAndLinks_WithoutPasswordAndOffscreenText()
    {
        using var testWindow = new TextTestWindow();

        var snapshot = await UiAutomationText.ReadVisibleTextAsync(testWindow.WindowHandle, NativeRect.Empty, cancellationToken: TestContext.Current.CancellationToken);

        Assert.NotNull(snapshot);
        Assert.True(snapshot.IsComplete);
        var words = snapshot.Words.ToList();
        Assert.NotEmpty(words);
        var text = snapshot.Text;
        // The TextBlock (one word with its name when it has no TextPattern, else its words)
        Assert.Contains("Plain", text);
        Assert.Contains("block", text);
        // The link in a TextBlock, with its target
        Assert.Contains(words, word => word.Text.Contains("Dapplo") && word.LinkTarget == TextTestWindow.LinkTarget);
        // The RichTextBox through its TextPattern: words, the link with its target, the other words without
        Assert.Contains(words, word => word.Text == "Rich" && word.LinkTarget is null);
        Assert.Contains(words, word => word.Text.StartsWith("Greenshot", StringComparison.Ordinal) && word.LinkTarget == TextTestWindow.RichLinkTarget);
        // The visible text of the scroll viewer, not the text which is scrolled away
        Assert.Contains("Visible inside", text);
        Assert.DoesNotContain("Offscreen", text);
        // Never the password, not even the bullets of the PasswordBox
        Assert.DoesNotContain(TextTestWindow.Password, text);
        Assert.DoesNotContain("Secret", text);
        Assert.DoesNotContain(words, word => word.LinkTarget?.Contains("Secret") == true);

        var windowBounds = WindowBounds(testWindow.WindowHandle);
        Assert.All(words, word => Assert.True(IsInside(windowBounds, word.Bounds), $"{word} is not inside {windowBounds}"));
        Assert.All(snapshot.Lines, line => Assert.All(line.Words, word => Assert.True(IsInside(line.Bounds, word.Bounds), $"{word} is not inside {line}")));
        // Top to bottom
        for (var index = 1; index < snapshot.Lines.Count; index++)
        {
            Assert.True(snapshot.Lines[index - 1].Bounds.Top <= snapshot.Lines[index].Bounds.Top, $"{snapshot.Lines[index - 1]} is below {snapshot.Lines[index]}");
        }
    }

    [Fact]
    public async Task ReadVisibleText_Area_LeavesOutTheTextOutside()
    {
        using var testWindow = new TextTestWindow();
        var area = testWindow.PlainBounds;

        var snapshot = await UiAutomationText.ReadVisibleTextAsync(testWindow.WindowHandle, area, cancellationToken: TestContext.Current.CancellationToken);

        Assert.True(snapshot.IsComplete);
        Assert.Contains("Plain", snapshot.Text);
        Assert.DoesNotContain("Rich", snapshot.Text);
        Assert.DoesNotContain("Visible inside", snapshot.Text);
        Assert.All(snapshot.Words, word => Assert.True(IsInside(area, word.Bounds), $"{word} is not inside {area}"));
    }

    [Fact]
    public async Task ReadVisibleText_LevelByLevel_HasTheSameText()
    {
        using var testWindow = new TextTestWindow();

        var snapshot = await ReadLevelByLevelAsync(testWindow.WindowHandle);

        AssertTextOfTestWindow(snapshot, testWindow.WindowHandle);
    }

    [Fact]
    public async Task ReadVisibleText_MaxDurationPassed_ReturnsWhatItHasWithoutThrowing()
    {
        using var testWindow = new TextTestWindow();

        var snapshot = await UiAutomationText.ReadVisibleTextAsync(testWindow.WindowHandle, NativeRect.Empty, maxDuration: TimeSpan.Zero,
            cancellationToken: TestContext.Current.CancellationToken);

        Assert.True(snapshot.TimedOut);
        Assert.False(snapshot.IsComplete);

        // With time enough the same window is complete
        var complete = await UiAutomationText.ReadVisibleTextAsync(testWindow.WindowHandle, NativeRect.Empty, maxDuration: TimeSpan.FromSeconds(30),
            cancellationToken: TestContext.Current.CancellationToken);
        AssertTextOfTestWindow(complete, testWindow.WindowHandle);
    }

    [Fact]
    public void SplitIntoWords_OneRectangle_GivesEachWordItsShare()
    {
        var words = SplitIntoWords("Hello big world", new[] { new NativeRect(0, 0, 150, 10) }, NativeRect.Empty, "https://example.com/");

        Assert.Equal(new[] { "Hello", "big", "world" }, words.Select(word => word.Text));
        Assert.Equal(new NativeRect(0, 0, 50, 10), words[0].Bounds);
        Assert.Equal(new NativeRect(60, 0, 30, 10), words[1].Bounds);
        Assert.Equal(new NativeRect(100, 0, 50, 10), words[2].Bounds);
        Assert.All(words, word => Assert.Equal("https://example.com/", word.LinkTarget));
    }

    [Fact]
    public void SplitIntoWords_TwoLines_SplitsAtWhiteSpaceAndClips()
    {
        var first = new NativeRect(0, 0, 90, 10);
        var second = new NativeRect(0, 10, 90, 10);

        var words = SplitIntoWords("one two three four", new[] { first, second }, NativeRect.Empty, null);

        Assert.Equal(new[] { "one", "two", "three", "four" }, words.Select(word => word.Text));
        Assert.All(words.Take(2), word => Assert.Equal(0, word.Bounds.Top));
        Assert.All(words.Skip(2), word => Assert.Equal(10, word.Bounds.Top));
        Assert.All(words, word => Assert.Null(word.LinkTarget));

        // Only the first line is visible
        var clipped = SplitIntoWords("one two three four", new[] { first, second }, new NativeRect(0, 0, 200, 10), null);
        Assert.Equal(new[] { "one", "two" }, clipped.Select(word => word.Text));
        Assert.Empty(SplitIntoWords("   ", new[] { first }, NativeRect.Empty, null));
    }

    [Fact]
    public void SplitIntoLineBands_CutsIntoLines()
    {
        var bands = SplitIntoLineBands(new NativeRect(10, 100, 200, 61), 20);

        Assert.Equal(3, bands.Count);
        Assert.Equal(100, bands[0].Top);
        Assert.Equal(161, bands[2].Bottom);
        Assert.All(bands, band => Assert.InRange(band.Height, 20, 21));
        Assert.Single(SplitIntoLineBands(new NativeRect(0, 0, 10, 10), 0));
    }

    [Fact]
    public async Task ReadVisibleText_NoWindow_IsEmptyAndIncomplete()
    {
        var snapshot = await UiAutomationText.ReadVisibleTextAsync(IntPtr.Zero, NativeRect.Empty, cancellationToken: TestContext.Current.CancellationToken);

        Assert.Empty(snapshot.Lines);
        Assert.False(snapshot.IsComplete);
    }

    [Fact]
    public void GroupIntoLines_OverlappingWords_AreOneLineFromLeftToRight()
    {
        var right = new UiAutomationTextWord(new NativeRect(200, 12, 50, 20), "right");
        var left = new UiAutomationTextWord(new NativeRect(10, 10, 50, 20), "left", "https://example.com/");
        var below = new UiAutomationTextWord(new NativeRect(10, 40, 50, 20), "below");

        var lines = GroupIntoLines(new[] { right, below, left });

        Assert.Equal(2, lines.Count);
        Assert.Equal(new[] { left, right }, lines[0].Words);
        Assert.Equal(new NativeRect(10, 10, 240, 22), lines[0].Bounds);
        Assert.Equal("left right", lines[0].Text);
        Assert.Same(below, Assert.Single(lines[1].Words));
        Assert.True(left.IsLink);
        Assert.False(right.IsLink);
    }

    [Fact]
    public void ToRectangles_RoundsOutwardsAndSkipsEmpty()
    {
        var rectangles = ToRectangles(new[] { 10.5, 20.25, 30.0, 10.5, 0, 0, 0, 0, double.NaN, 1, 1, 1 });

        Assert.Equal(new NativeRect(10, 20, 31, 11), Assert.Single(rectangles));
        Assert.Empty(ToRectangles(null));
    }

    [Fact]
    public void Word_ToString_HasNoText()
    {
        var word = new UiAutomationTextWord(new NativeRect(1, 2, 3, 4), "confidential", "https://example.com/secret");

        Assert.DoesNotContain("confidential", word.ToString());
        Assert.DoesNotContain("secret", word.ToString());
        Assert.Null(new UiAutomationTextWord(NativeRect.Empty, null, string.Empty).LinkTarget);
    }
}
