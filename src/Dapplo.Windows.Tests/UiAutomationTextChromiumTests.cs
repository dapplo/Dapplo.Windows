// Copyright (c) Dapplo and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
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
///     The text model of <see cref="ChromiumLikeDocument"/>: lines of words. A position is (line, word, offset in the word), like the
///     (node, offset) positions of Chromium: the end of a word and the start of the next one are the same place in the text, but not the
///     same position. That is what makes the backward expansion possible.
/// </summary>
internal sealed class ChromiumLikeText
{
    public const double CharWidth = 8;
    public const double LineHeight = 20;

    private readonly int[] _lineStarts;
    private readonly int[][] _wordStarts;
    private readonly int[] _rows;

    /// <param name="lines">the lines; hidden ones have no rectangles (outside the clip, like most visible ranges of a Chromium page)</param>
    /// <param name="visibleRanges">the visible ranges as (first line, line count)</param>
    public ChromiumLikeText(IReadOnlyList<(string Text, bool Hidden)> lines, IReadOnlyList<(int First, int Count)> visibleRanges)
    {
        Lines = lines;
        VisibleRanges = visibleRanges;
        Words = lines.Select(line => Regex.Matches(line.Text, @"\S+\s*").Cast<Match>().Select(match => match.Value).ToArray()).ToArray();
        _lineStarts = new int[lines.Count];
        _wordStarts = new int[lines.Count][];
        _rows = new int[lines.Count];
        var offset = 0;
        var row = 0;
        for (var line = 0; line < lines.Count; line++)
        {
            _lineStarts[line] = offset;
            _wordStarts[line] = new int[Words[line].Length];
            var inLine = 0;
            for (var word = 0; word < Words[line].Length; word++)
            {
                _wordStarts[line][word] = inLine;
                inLine += Words[line][word].Length;
            }
            offset += inLine;
            _rows[line] = lines[line].Hidden ? -1 : row++;
        }
        Text = string.Concat(Words.SelectMany(words => words));
        Rows = row;
    }

    public IReadOnlyList<(string Text, bool Hidden)> Lines { get; }

    public IReadOnlyList<(int First, int Count)> VisibleRanges { get; }

    public string[][] Words { get; }

    public string Text { get; }

    /// <summary>The number of rows which are shown</summary>
    public int Rows { get; }

    /// <summary>The ExpandToEnclosingUnit calls for lines, the steps of the reader</summary>
    public int LineExpansions { get; set; }

    /// <summary>The ExpandToEnclosingUnit calls for words</summary>
    public int WordExpansions { get; set; }

    public int Global(Position position) => _lineStarts[position.Line] + _wordStarts[position.Line][position.Word] + position.Offset;

    public int LineLength(int line) => Lines[line].Text.TrimEnd().Length == 0 ? 0 : _wordStarts[line].Last() + Words[line].Last().Length;

    public int Row(int line) => _rows[line];

    public int LineStart(int line) => _lineStarts[line];

    public Position StartOfLine(int line) => new(line, 0, 0);

    public Position EndOfLine(int line) => new(line, Words[line].Length - 1, Words[line].Last().Length);

    public Position EndOfWord(int line, int word) => new(line, word, Words[line][word].Length);

    /// <summary>Chromium gives a short line (a label, a number) again when its end is expanded</summary>
    public bool IsShortLine(int line) => Lines[line].Text.Trim().Length <= 4;

    public bool IsShortWord(int line, int word) => Words[line][word].Trim().Length <= 4;

    public readonly struct Position
    {
        public Position(int line, int word, int offset)
        {
            Line = line;
            Word = word;
            Offset = offset;
        }

        public int Line { get; }

        public int Word { get; }

        public int Offset { get; }
    }
}

/// <summary>
///     An element with a TextPattern which behaves like a Chromium document: expanding the empty range at the end of a short line (or word)
///     to a line (word) gives that line (word) again, its start lies before the end which was expanded. Move by one unit moves on.
/// </summary>
internal sealed class ChromiumLikeDocument : Canvas
{
    public ChromiumLikeDocument(ChromiumLikeText text)
    {
        Text = text;
    }

    public ChromiumLikeText Text { get; }

    protected override AutomationPeer OnCreateAutomationPeer() => new ChromiumLikeDocumentPeer(this);

    /// <summary>A text rectangle (device independent, relative to the element) in screen coordinates, like UI Automation has them</summary>
    public WpfWindows.Rect ToScreen(double x, double y, double width, double height)
    {
        var topLeft = PointToScreen(new WpfWindows.Point(x, y));
        var bottomRight = PointToScreen(new WpfWindows.Point(x + width, y + height));
        return new WpfWindows.Rect(topLeft, bottomRight);
    }

    private sealed class ChromiumLikeDocumentPeer : FrameworkElementAutomationPeer, ITextProvider
    {
        private readonly ChromiumLikeDocument _owner;

        public ChromiumLikeDocumentPeer(ChromiumLikeDocument owner) : base(owner)
        {
            _owner = owner;
        }

        protected override AutomationControlType GetAutomationControlTypeCore() => AutomationControlType.Document;

        protected override string GetClassNameCore() => nameof(ChromiumLikeDocument);

        public override object GetPattern(PatternInterface patternInterface) =>
            patternInterface == PatternInterface.Text ? this : base.GetPattern(patternInterface);

        private ChromiumLikeText Text => _owner.Text;

        private ChromiumLikeRange Range(ChromiumLikeText.Position start, ChromiumLikeText.Position end) =>
            new(_owner, () => ProviderFromPeer(this), start, end);

        public ITextRangeProvider[] GetSelection() => Array.Empty<ITextRangeProvider>();

        public ITextRangeProvider[] GetVisibleRanges() =>
            Text.VisibleRanges.Select(range => (ITextRangeProvider)Range(Text.StartOfLine(range.First), Text.EndOfLine(range.First + range.Count - 1))).ToArray();

        // No range for a child: the reader uses the bounds of the link
        public ITextRangeProvider RangeFromChild(IRawElementProviderSimple childElement) => null;

        public ITextRangeProvider RangeFromPoint(WpfWindows.Point screenLocation) => Range(Text.StartOfLine(0), Text.StartOfLine(0));

        public ITextRangeProvider DocumentRange => Range(Text.StartOfLine(0), Text.EndOfLine(Text.Lines.Count - 1));

        public SupportedTextSelection SupportedTextSelection => SupportedTextSelection.None;
    }

    private sealed class ChromiumLikeRange : ITextRangeProvider
    {
        private readonly ChromiumLikeDocument _owner;
        private readonly Func<IRawElementProviderSimple> _enclosingElement;
        private ChromiumLikeText.Position _start;
        private ChromiumLikeText.Position _end;

        public ChromiumLikeRange(ChromiumLikeDocument owner, Func<IRawElementProviderSimple> enclosingElement, ChromiumLikeText.Position start, ChromiumLikeText.Position end)
        {
            _owner = owner;
            _enclosingElement = enclosingElement;
            _start = start;
            _end = end;
        }

        private ChromiumLikeText Text => _owner.Text;

        private bool IsDegenerate => Text.Global(_start) == Text.Global(_end);

        private static ChromiumLikeRange Of(ITextRangeProvider range) =>
            range as ChromiumLikeRange ?? throw new ArgumentException("Not a range of this document", nameof(range));

        public ITextRangeProvider Clone() => new ChromiumLikeRange(_owner, _enclosingElement, _start, _end);

        public bool Compare(ITextRangeProvider range)
        {
            var other = Of(range);
            return Text.Global(_start) == Text.Global(other._start) && Text.Global(_end) == Text.Global(other._end);
        }

        public int CompareEndpoints(TextPatternRangeEndpoint endpoint, ITextRangeProvider targetRange, TextPatternRangeEndpoint targetEndpoint)
        {
            var target = Of(targetRange);
            var own = endpoint == TextPatternRangeEndpoint.Start ? _start : _end;
            var other = targetEndpoint == TextPatternRangeEndpoint.Start ? target._start : target._end;
            return Math.Sign(Text.Global(own) - Text.Global(other));
        }

        public void ExpandToEnclosingUnit(TextUnit unit)
        {
            var position = _start;
            switch (unit)
            {
                case TextUnit.Character:
                case TextUnit.Format:
                case TextUnit.Word:
                    Text.WordExpansions++;
                    var word = position.Word;
                    // A long word expands forwards at its end, a short one gives itself again (Chromium)
                    if (position.Offset == Text.Words[position.Line][word].Length && !Text.IsShortWord(position.Line, word) && word + 1 < Text.Words[position.Line].Length)
                    {
                        word++;
                    }
                    _start = new ChromiumLikeText.Position(position.Line, word, 0);
                    _end = Text.EndOfWord(position.Line, word);
                    return;
                case TextUnit.Document:
                    _start = Text.StartOfLine(0);
                    _end = Text.EndOfLine(Text.Lines.Count - 1);
                    return;
                default:
                    Text.LineExpansions++;
                    var line = position.Line;
                    // A long line expands forwards at its end (to the next line), a short one gives itself again (Chromium)
                    var atEnd = Text.Global(position) == Text.Global(Text.EndOfLine(line));
                    if (atEnd && !Text.IsShortLine(line) && line + 1 < Text.Lines.Count)
                    {
                        line++;
                    }
                    _start = Text.StartOfLine(line);
                    _end = Text.EndOfLine(line);
                    return;
            }
        }

        public ITextRangeProvider FindAttribute(int attribute, object value, bool backward) => null;

        public ITextRangeProvider FindText(string text, bool backward, bool ignoreCase) => null;

        public object GetAttributeValue(int attribute) => AutomationElementIdentifiers.NotSupported;

        public double[] GetBoundingRectangles()
        {
            var start = Text.Global(_start);
            var end = Text.Global(_end);
            var rectangles = new List<double>();
            for (var line = _start.Line; line <= _end.Line; line++)
            {
                var lineStart = Text.LineStart(line);
                var from = Math.Max(start, lineStart);
                var to = Math.Min(end, lineStart + Text.LineLength(line));
                if (to <= from || Text.Row(line) < 0)
                {
                    continue;
                }
                var rectangle = _owner.ToScreen((from - lineStart) * ChromiumLikeText.CharWidth, Text.Row(line) * ChromiumLikeText.LineHeight,
                    (to - from) * ChromiumLikeText.CharWidth, ChromiumLikeText.LineHeight - 2);
                rectangles.AddRange(new[] { rectangle.X, rectangle.Y, rectangle.Width, rectangle.Height });
            }
            return rectangles.ToArray();
        }

        public IRawElementProviderSimple GetEnclosingElement() => _enclosingElement();

        public string GetText(int maxLength)
        {
            var start = Text.Global(_start);
            var text = Text.Text.Substring(start, Text.Global(_end) - start);
            return maxLength >= 0 && text.Length > maxLength ? text.Substring(0, maxLength) : text;
        }

        public int Move(TextUnit unit, int count)
        {
            if (count <= 0)
            {
                return 0;
            }
            var wasDegenerate = IsDegenerate;
            var moved = 0;
            for (; moved < count; moved++)
            {
                if (unit == TextUnit.Word)
                {
                    if (_start.Word + 1 < Text.Words[_start.Line].Length)
                    {
                        _start = new ChromiumLikeText.Position(_start.Line, _start.Word + 1, 0);
                    }
                    else if (_start.Line + 1 < Text.Lines.Count)
                    {
                        _start = Text.StartOfLine(_start.Line + 1);
                    }
                    else
                    {
                        break;
                    }
                    _end = wasDegenerate ? _start : Text.EndOfWord(_start.Line, _start.Word);
                    continue;
                }
                if (_start.Line + 1 >= Text.Lines.Count)
                {
                    break;
                }
                _start = Text.StartOfLine(_start.Line + 1);
                _end = wasDegenerate ? _start : Text.EndOfLine(_start.Line);
            }
            return moved;
        }

        public int MoveEndpointByUnit(TextPatternRangeEndpoint endpoint, TextUnit unit, int count) => 0;

        public void MoveEndpointByRange(TextPatternRangeEndpoint endpoint, ITextRangeProvider targetRange, TextPatternRangeEndpoint targetEndpoint)
        {
            var target = Of(targetRange);
            var position = targetEndpoint == TextPatternRangeEndpoint.Start ? target._start : target._end;
            if (endpoint == TextPatternRangeEndpoint.Start)
            {
                _start = position;
                if (Text.Global(_start) > Text.Global(_end))
                {
                    _end = _start;
                }
            }
            else
            {
                _end = position;
                if (Text.Global(_end) < Text.Global(_start))
                {
                    _start = _end;
                }
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

/// <summary>
///     A link inside the <see cref="ChromiumLikeDocument"/>, over one line: that line is read word by word
/// </summary>
internal sealed class ChromiumLikeLink : Border
{
    private readonly string _name;
    private readonly string _target;

    public ChromiumLikeLink(string name, string target)
    {
        _name = name;
        _target = target;
        Background = WpfWindows.Media.Brushes.Transparent;
    }

    protected override AutomationPeer OnCreateAutomationPeer() => new ChromiumLikeLinkPeer(this);

    private sealed class ChromiumLikeLinkPeer : FrameworkElementAutomationPeer, IValueProvider
    {
        private readonly ChromiumLikeLink _owner;

        public ChromiumLikeLinkPeer(ChromiumLikeLink owner) : base(owner)
        {
            _owner = owner;
        }

        protected override AutomationControlType GetAutomationControlTypeCore() => AutomationControlType.Hyperlink;

        protected override string GetNameCore() => _owner._name;

        public override object GetPattern(PatternInterface patternInterface) =>
            patternInterface == PatternInterface.Value ? this : base.GetPattern(patternInterface);

        public bool IsReadOnly => true;

        public string Value => _owner._target;

        public void SetValue(string value) => throw new InvalidOperationException("Read only");
    }
}

/// <summary>
///     A WPF window with only a <see cref="ChromiumLikeDocument"/>
/// </summary>
internal sealed class ChromiumLikeTestWindow : ScrollTestWindow
{
    public const string LinkTarget = "https://www.dapplo.net/chromium";

    private readonly ChromiumLikeText _text;
    private readonly int _linkLine;
    private WpfWindows.Window _window;
    private Dispatcher _dispatcher;

    /// <param name="text">ChromiumLikeText</param>
    /// <param name="linkLine">the line with a link over it, -1 for none</param>
    public ChromiumLikeTestWindow(ChromiumLikeText text, int linkLine = -1) : base(nameof(ChromiumLikeTestWindow), start: false)
    {
        _text = text;
        _linkLine = linkLine;
        Start();
    }

    protected override void Run()
    {
        _dispatcher = Dispatcher.CurrentDispatcher;
        var document = new ChromiumLikeDocument(_text)
        {
            Width = 400,
            Height = _text.Rows * ChromiumLikeText.LineHeight + 10,
            Margin = new WpfWindows.Thickness(10),
            HorizontalAlignment = WpfWindows.HorizontalAlignment.Left,
            VerticalAlignment = WpfWindows.VerticalAlignment.Top
        };
        if (_linkLine >= 0)
        {
            var link = new ChromiumLikeLink(_text.Lines[_linkLine].Text, LinkTarget)
            {
                Width = _text.LineLength(_linkLine) * ChromiumLikeText.CharWidth,
                Height = ChromiumLikeText.LineHeight - 2
            };
            Canvas.SetLeft(link, 0);
            Canvas.SetTop(link, _text.Row(_linkLine) * ChromiumLikeText.LineHeight);
            document.Children.Add(link);
        }
        _window = new WpfWindows.Window
        {
            Title = "Dapplo.Windows Chromium like text test",
            Left = 170,
            Top = 140,
            Width = 440,
            Height = document.Height + 80,
            Topmost = true,
            ShowInTaskbar = false,
            Content = document
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

/// <summary>
///     The reading of a TextPattern whose provider expands backwards, like Chromium does for short lines and words
/// </summary>
public class UiAutomationTextChromiumTests
{
    private const int LinkLine = 9;

    public UiAutomationTextChromiumTests(ITestOutputHelper testOutputHelper)
    {
        LogSettings.RegisterDefaultLogger<XUnitLogger>(LogLevels.Verbose, testOutputHelper);
    }

    /// <summary>
    ///     Short lines ("1791", "?") which Chromium gives again, a visible range whose lines have no rectangles (outside the clip) and a line
    ///     with a link which is read word by word
    /// </summary>
    private static ChromiumLikeText CreateText() => new(new (string, bool)[]
    {
        ("Chromium like document", false),
        ("1791", false),
        ("?", false),
        ("A", false),
        ("Second long line of text", false),
        // A visible range which is outside the clip: no rectangles
        ("x", true),
        ("y", true),
        ("42", true),
        ("Hidden longer line", true),
        // The line with the link, its words are short
        ("Go to 1791 now", false),
        ("B", false),
        ("Last line here", false),
        ("7", false)
    }, new[] { (0, 5), (5, 4), (9, 3), (12, 1) });

    private static string[] VisibleLines(ChromiumLikeText text) => text.Lines.Where(line => !line.Hidden).Select(line => line.Text).ToArray();

    [Fact]
    public async Task ReadVisibleText_BackwardExpandingLines_ReadsEveryLineOnceAndCompletes()
    {
        var text = CreateText();
        using var testWindow = new ChromiumLikeTestWindow(text);

        var snapshot = await UiAutomationText.ReadVisibleTextAsync(testWindow.WindowHandle, NativeRect.Empty, maxDuration: TimeSpan.FromSeconds(30),
            cancellationToken: TestContext.Current.CancellationToken);
        var lineExpansions = testWindow.Invoke(() => text.LineExpansions);

        Assert.True(snapshot.IsComplete);
        Assert.False(snapshot.TimedOut);
        var lines = snapshot.Lines.Select(line => line.Text).ToList();
        var allTexts = text.Lines.Select(line => line.Text).ToList();
        // Every visible line exactly once, from top to bottom, no repeats, nothing of the lines without rectangles
        Assert.Equal(VisibleLines(text), lines.Where(allTexts.Contains));
        Assert.DoesNotContain(lines, line => line.Contains("Hidden") || line == "x" || line == "42");
        // Each line is expanded once, a short one a second time after the move, the lines without rectangles too: the reader doesn't run
        // up to its guard of 5000 per visible range (per read attempt)
        Assert.InRange(lineExpansions, text.Lines.Count, 2 * text.Lines.Count * 2);
    }

    [Fact]
    public async Task ReadVisibleText_BackwardExpandingWords_ReadsEveryWordOnce()
    {
        var text = CreateText();
        using var testWindow = new ChromiumLikeTestWindow(text, LinkLine);

        var snapshot = await UiAutomationText.ReadVisibleTextAsync(testWindow.WindowHandle, NativeRect.Empty, maxDuration: TimeSpan.FromSeconds(30),
            cancellationToken: TestContext.Current.CancellationToken);
        var wordExpansions = testWindow.Invoke(() => text.WordExpansions);

        Assert.True(snapshot.IsComplete);
        var linkLine = Assert.Single(snapshot.Lines, line => line.Words.Any(word => word.LinkTarget == ChromiumLikeTestWindow.LinkTarget));
        Assert.Equal(new[] { "Go", "to", "1791", "now" }, linkLine.Words.Select(word => word.Text));
        Assert.All(linkLine.Words, word => Assert.Equal(ChromiumLikeTestWindow.LinkTarget, word.LinkTarget));
        // The other lines once each too
        var allTexts = text.Lines.Select(line => line.Text).ToList();
        Assert.Equal(VisibleLines(text), snapshot.Lines.Select(line => line.Text).Where(allTexts.Contains));
        var words = text.Words[LinkLine].Length;
        Assert.InRange(wordExpansions, words, 2 * words * 2);
    }
}
