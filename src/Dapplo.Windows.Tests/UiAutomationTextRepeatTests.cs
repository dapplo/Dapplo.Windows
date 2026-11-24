// Copyright (c) Dapplo and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Automation;
using System.Windows.Automation.Peers;
using System.Windows.Automation.Provider;
using System.Windows.Automation.Text;
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
///     A link over a word of a <see cref="ChromiumLikeText"/>: a Hyperlink element with a ValuePattern, without text children
/// </summary>
internal sealed class ChromiumLikeLink : WpfWindows.FrameworkElement
{
    public string Target { get; set; }

    protected override AutomationPeer OnCreateAutomationPeer() => new LinkAutomationPeer(this);

    private sealed class LinkAutomationPeer : FrameworkElementAutomationPeer, IValueProvider
    {
        private readonly string _target;

        public LinkAutomationPeer(ChromiumLikeLink owner) : base(owner)
        {
            _target = owner.Target;
        }

        protected override AutomationControlType GetAutomationControlTypeCore() => AutomationControlType.Hyperlink;

        protected override string GetClassNameCore() => nameof(ChromiumLikeLink);

        protected override bool IsControlElementCore() => true;

        protected override bool IsContentElementCore() => true;

        public override object GetPattern(PatternInterface patternInterface) =>
            patternInterface == PatternInterface.Value ? this : base.GetPattern(patternInterface);

        public bool IsReadOnly => true;

        public string Value => _target;

        public void SetValue(string value) => throw new InvalidOperationException("Read only");
    }
}

/// <summary>
///     A document with a TextPattern whose ranges behave like Chromium's: the empty range at the end of a short line (or word) expands to
///     that same line again, not to the next one; Move by one unit from there gets to the next one
/// </summary>
internal sealed class ChromiumLikeText : Canvas
{
    public const double LineHeight = 20;
    public const double CharacterWidth = 8;
    // Lines and words up to this length (without white space) expand backwards
    private const int ShortLength = 4;

    private readonly string _text;
    private readonly List<(int Start, int Length)> _lines = new();
    private readonly List<(int Start, int End)> _words = new();

    public ChromiumLikeText(IReadOnlyList<string> lines, string linkWord, string linkTarget)
    {
        _text = string.Join("\n", lines);
        var start = 0;
        foreach (var line in lines)
        {
            _lines.Add((start, line.Length));
            // A word with its trailing spaces, the line break is a unit of its own
            var offset = 0;
            while (offset < line.Length)
            {
                var end = line.IndexOf(' ', offset);
                end = end < 0 ? line.Length : end;
                while (end < line.Length && line[end] == ' ')
                {
                    end++;
                }
                _words.Add((start + offset, start + end));
                offset = end;
            }
            start += line.Length;
            if (start < _text.Length)
            {
                _words.Add((start, start + 1));
                start++;
            }
        }
        Width = 400;
        Height = lines.Count * LineHeight;
        var linkLine = lines.Select((line, index) => (line, index)).First(entry => entry.line.Contains(linkWord));
        LinkStart = _lines[linkLine.index].Start + linkLine.line.IndexOf(linkWord, StringComparison.Ordinal);
        LinkEnd = LinkStart + linkWord.Length;
        var link = new ChromiumLikeLink { Target = linkTarget, Width = linkWord.Length * CharacterWidth, Height = LineHeight - 4 };
        SetLeft(link, (LinkStart - _lines[linkLine.index].Start) * CharacterWidth);
        SetTop(link, linkLine.index * LineHeight + 2);
        Children.Add(link);
    }

    public int LinkStart { get; }

    public int LinkEnd { get; }

    public int TextLength => _text.Length;

    /// <summary>How often a unit was expanded, to see that nothing is read over and over</summary>
    public int Expansions { get; set; }

    protected override AutomationPeer OnCreateAutomationPeer() => new TextAutomationPeer(this);

    private List<(int Start, int End)> UnitsOf(TextUnit unit) => unit switch
    {
        TextUnit.Word => _words,
        TextUnit.Character => Enumerable.Range(0, _text.Length).Select(index => (index, index + 1)).ToList(),
        TextUnit.Document or TextUnit.Page => new List<(int, int)> { (0, _text.Length) },
        _ => _lines.Select((line, index) => (line.Start, index + 1 < _lines.Count ? _lines[index + 1].Start : _text.Length)).ToList()
    };

    private bool IsShort((int Start, int End) unit)
    {
        var text = _text.Substring(unit.Start, unit.End - unit.Start).Trim();
        return text.Length > 0 && text.Length <= ShortLength;
    }

    /// <summary>The index of a short unit which ends at the position, with another unit after it: there Chromium expands backwards</summary>
    private int ShortUnitEndingAt(List<(int Start, int End)> units, int position)
    {
        var index = units.FindIndex(unit => unit.End == position);
        return index >= 0 && index + 1 < units.Count && IsShort(units[index]) ? index : -1;
    }

    private double[] BoundingRectangles(int start, int end)
    {
        var result = new List<double>();
        for (var index = 0; index < _lines.Count; index++)
        {
            var (lineStart, length) = _lines[index];
            var from = Math.Max(start, lineStart);
            var to = Math.Min(end, lineStart + length);
            if (to <= from)
            {
                continue;
            }
            var topLeft = PointToScreen(new WpfWindows.Point((from - lineStart) * CharacterWidth, index * LineHeight + 2));
            var bottomRight = PointToScreen(new WpfWindows.Point((to - lineStart) * CharacterWidth, (index + 1) * LineHeight - 2));
            result.AddRange(new[] { topLeft.X, topLeft.Y, bottomRight.X - topLeft.X, bottomRight.Y - topLeft.Y });
        }
        return result.ToArray();
    }

    private sealed class TextAutomationPeer : FrameworkElementAutomationPeer, ITextProvider
    {
        private readonly ChromiumLikeText _owner;

        public TextAutomationPeer(ChromiumLikeText owner) : base(owner)
        {
            _owner = owner;
        }

        internal IRawElementProviderSimple Provider => ProviderFromPeer(this);

        protected override AutomationControlType GetAutomationControlTypeCore() => AutomationControlType.Document;

        protected override string GetClassNameCore() => nameof(ChromiumLikeText);

        protected override bool IsControlElementCore() => true;

        protected override bool IsContentElementCore() => true;

        public override object GetPattern(PatternInterface patternInterface) =>
            patternInterface == PatternInterface.Text ? this : base.GetPattern(patternInterface);

        public ITextRangeProvider[] GetSelection() => Array.Empty<ITextRangeProvider>();

        public ITextRangeProvider[] GetVisibleRanges() => new ITextRangeProvider[] { new Range(this, 0, _owner.TextLength) };

        public ITextRangeProvider RangeFromChild(IRawElementProviderSimple childElement) => new Range(this, _owner.LinkStart, _owner.LinkEnd);

        public ITextRangeProvider RangeFromPoint(WpfWindows.Point screenLocation) => new Range(this, 0, 0);

        public ITextRangeProvider DocumentRange => new Range(this, 0, _owner.TextLength);

        public SupportedTextSelection SupportedTextSelection => SupportedTextSelection.None;

        private sealed class Range : ITextRangeProvider
        {
            private readonly TextAutomationPeer _peer;
            private int _start;
            private int _end;
            // An empty range reached by Move: it lies at the start of the next unit, not at the end of the previous one
            private bool _atNextUnit;

            public Range(TextAutomationPeer peer, int start, int end)
            {
                _peer = peer;
                _start = start;
                _end = end;
            }

            private ChromiumLikeText Owner => _peer._owner;

            public ITextRangeProvider Clone() => new Range(_peer, _start, _end) { _atNextUnit = _atNextUnit };

            public bool Compare(ITextRangeProvider range) => range is Range other && other._start == _start && other._end == _end;

            public int CompareEndpoints(TextPatternRangeEndpoint endpoint, ITextRangeProvider targetRange, TextPatternRangeEndpoint targetEndpoint)
            {
                var target = (Range)targetRange;
                return Endpoint(endpoint).CompareTo(target.Endpoint(targetEndpoint));
            }

            private int Endpoint(TextPatternRangeEndpoint endpoint) => endpoint == TextPatternRangeEndpoint.Start ? _start : _end;

            public void ExpandToEnclosingUnit(TextUnit unit)
            {
                Owner.Expansions++;
                var units = Owner.UnitsOf(unit);
                var backwards = _start == _end && !_atNextUnit ? Owner.ShortUnitEndingAt(units, _start) : -1;
                var index = backwards >= 0 ? backwards : units.FindIndex(candidate => candidate.Start <= _start && _start < candidate.End);
                (_start, _end) = index >= 0 ? units[index] : units[units.Count - 1];
                _atNextUnit = false;
            }

            public ITextRangeProvider FindAttribute(int attribute, object value, bool backward) => null;

            public ITextRangeProvider FindText(string text, bool backward, bool ignoreCase) => null;

            public object GetAttributeValue(int attribute) => AutomationElementIdentifiers.NotSupported;

            public double[] GetBoundingRectangles() => Owner.BoundingRectangles(_start, _end);

            public IRawElementProviderSimple GetEnclosingElement() => _peer.Provider;

            public string GetText(int maxLength)
            {
                var text = Owner._text.Substring(_start, _end - _start);
                return maxLength >= 0 && text.Length > maxLength ? text.Substring(0, maxLength) : text;
            }

            public int Move(TextUnit unit, int count)
            {
                var units = Owner.UnitsOf(unit);
                var moved = 0;
                for (; moved < Math.Abs(count); moved++)
                {
                    if (count > 0 && _start == _end && !_atNextUnit && Owner.ShortUnitEndingAt(units, _start) >= 0)
                    {
                        // Chromium: from the end of the short unit to the start of the next one, at the same offset
                        _atNextUnit = true;
                        continue;
                    }
                    var position = _start;
                    var next = count > 0
                        ? units.Where(candidate => candidate.Start > position).Select(candidate => candidate.Start).DefaultIfEmpty(-1).Min()
                        : units.Where(candidate => candidate.Start < position).Select(candidate => candidate.Start).DefaultIfEmpty(-1).Max();
                    if (next < 0)
                    {
                        break;
                    }
                    _start = _end = next;
                    _atNextUnit = false;
                }
                return count > 0 ? moved : -moved;
            }

            public int MoveEndpointByUnit(TextPatternRangeEndpoint endpoint, TextUnit unit, int count)
            {
                var units = Owner.UnitsOf(unit);
                var position = Endpoint(endpoint);
                var moved = 0;
                for (; moved < Math.Abs(count); moved++)
                {
                    var current = position;
                    var next = count > 0
                        ? units.Where(candidate => candidate.End > current).Select(candidate => candidate.End).DefaultIfEmpty(-1).Min()
                        : units.Where(candidate => candidate.Start < current).Select(candidate => candidate.Start).DefaultIfEmpty(-1).Max();
                    if (next < 0)
                    {
                        break;
                    }
                    position = next;
                }
                SetEndpoint(endpoint, position);
                return count > 0 ? moved : -moved;
            }

            public void MoveEndpointByRange(TextPatternRangeEndpoint endpoint, ITextRangeProvider targetRange, TextPatternRangeEndpoint targetEndpoint) =>
                SetEndpoint(endpoint, ((Range)targetRange).Endpoint(targetEndpoint));

            /// <summary>Like every provider: an endpoint moved over the other one takes it along</summary>
            private void SetEndpoint(TextPatternRangeEndpoint endpoint, int position)
            {
                _atNextUnit = false;
                if (endpoint == TextPatternRangeEndpoint.Start)
                {
                    _start = position;
                    _end = Math.Max(_end, position);
                }
                else
                {
                    _end = position;
                    _start = Math.Min(_start, position);
                }
            }

            public void Select()
            {
            }

            public void AddToSelection()
            {
            }

            public void RemoveFromSelection()
            {
            }

            public void ScrollIntoView(bool alignToTop)
            {
            }

            public IRawElementProviderSimple[] GetChildren() => Array.Empty<IRawElementProviderSimple>();
        }
    }
}

/// <summary>
///     A WPF window with a <see cref="ChromiumLikeText"/>
/// </summary>
internal sealed class ChromiumLikeTextWindow : ScrollTestWindow
{
    public const string LinkTarget = "https://www.dapplo.net/docs";

    public static readonly string[] Lines = { "Messages", "A", "1791", "?", "Hello world from Dapplo", "Read the docs here", "ok", "The last line" };

    private WpfWindows.Window _window;
    private Dispatcher _dispatcher;
    private ChromiumLikeText _text;

    public ChromiumLikeTextWindow() : base(nameof(ChromiumLikeTextWindow), start: false)
    {
        Start();
    }

    protected override void Run()
    {
        _dispatcher = Dispatcher.CurrentDispatcher;
        _text = new ChromiumLikeText(Lines, "docs", LinkTarget) { Margin = new WpfWindows.Thickness(10) };
        _window = new WpfWindows.Window
        {
            Title = "Dapplo.Windows Chromium like text test",
            Left = 170,
            Top = 140,
            Width = 460,
            Height = 260,
            Topmost = true,
            ShowInTaskbar = false,
            Content = _text
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

    public int Expansions => Invoke(() => _text.Expansions);

    public override NativeRect ScrollingBounds => NativeRect.Empty;

    public override T Invoke<T>(Func<T> func) => _dispatcher.Invoke(func);

    public override void Close() => _dispatcher.Invoke(_window.Close);
}

public class UiAutomationTextRepeatTests
{
    public UiAutomationTextRepeatTests(ITestOutputHelper testOutputHelper)
    {
        LogSettings.RegisterDefaultLogger<XUnitLogger>(LogLevels.Verbose, testOutputHelper);
    }

    [Fact]
    public async Task ReadVisibleText_ProviderExpandsShortUnitsBackwards_ReadsEveryLineAndWordOnce()
    {
        using var testWindow = new ChromiumLikeTextWindow();

        var snapshot = await UiAutomationText.ReadVisibleTextAsync(testWindow.WindowHandle, NativeRect.Empty, maxDuration: TimeSpan.FromSeconds(30),
            cancellationToken: TestContext.Current.CancellationToken);

        Assert.True(snapshot.IsComplete, snapshot.ToString());
        var lines = snapshot.Lines.Select(line => line.Text).ToList();
        foreach (var expected in ChromiumLikeTextWindow.Lines)
        {
            Assert.Equal(1, lines.Count(line => line == expected));
        }
        // The line with the link is read word by word: each word once, the link word with its target
        var words = snapshot.Words.ToList();
        Assert.Equal(1, words.Count(word => word.Text == "Read"));
        Assert.Equal(1, words.Count(word => word.Text == "the"));
        Assert.Single(words, word => word.Text == "docs" && word.LinkTarget == ChromiumLikeTextWindow.LinkTarget);
        Assert.Equal(1, words.Count(word => word.Text == "here"));
        // A read expands every line and word at most twice (backwards, then after the move): far from the 5000 of the guard
        Assert.True(testWindow.Expansions < 200, $"{testWindow.Expansions} expansions");
    }
}
