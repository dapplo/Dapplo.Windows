// Copyright (c) Dapplo and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Dapplo.Log;
using Dapplo.Windows.Automation.Interop;
using Dapplo.Windows.Common.Extensions;
using Dapplo.Windows.Common.Structs;
using Dapplo.Windows.Desktop;
using Dapplo.Windows.Enums;

namespace Dapplo.Windows.Automation;

/// <summary>
///     Read the text a window shows on the screen through UI Automation, as lines and words with their bounds and the targets of links:
///     for windows whose text should not go through OCR (browsers, Electron, WPF, UWP, Office). Generic, nothing is specific to an
///     application. Works on a window handle, so also while another window (a full-screen selection window) covers the window.
/// </summary>
public static class UiAutomationText
{
    private static readonly LogSource Log = new LogSource();

    /// <summary>
    ///     The default for the connection timeout of
    ///     <see cref="ReadVisibleTextAsync(IntPtr, NativeRect, TimeSpan?, TimeSpan?, TimeSpan?, CancellationToken)"/>: 2 seconds
    /// </summary>
    public static readonly TimeSpan DefaultConnectionTimeout = TimeSpan.FromSeconds(2);

    /// <summary>
    ///     The default for the transaction timeout (one request to the application) of
    ///     <see cref="ReadVisibleTextAsync(IntPtr, NativeRect, TimeSpan?, TimeSpan?, TimeSpan?, CancellationToken)"/>: 2 seconds
    /// </summary>
    public static readonly TimeSpan DefaultTransactionTimeout = TimeSpan.FromSeconds(2);

    /// <summary>
    ///     The most words of a snapshot, a guard against a provider which reports endless text; the snapshot is incomplete when it is reached
    /// </summary>
    internal const int MaxWords = 20000;

    /// <summary>
    ///     The most elements visited, a guard like <see cref="MaxWords"/>
    /// </summary>
    private const int MaxElements = 20000;

    /// <summary>
    ///     The most links per element which is read through its TextPattern
    /// </summary>
    private const int MaxLinks = 2000;

    /// <summary>
    ///     A guard against a provider which reports a (nearly) endless chain of elements
    /// </summary>
    private const int MaxNesting = 256;

    /// <summary>
    ///     A guard against a text range which doesn't move on: the most lines of a visible range, and the most words of a line
    /// </summary>
    private const int MaxUnitsPerRange = 5000;

    /// <summary>
    ///     A run of text whose bounds are higher than this many times the usual line height wraps over several lines
    /// </summary>
    private const double MultiLineFactor = 1.5;

    /// <summary>
    ///     How far (in characters) the split of a text over its rectangles moves to the next white space, so a word isn't cut in two
    /// </summary>
    private const int MaxSnapDistance = 15;

    /// <summary>
    ///     Read the text the window shows inside <paramref name="area"/>, as lines from top to bottom with the words and their bounds.
    /// </summary>
    /// <remarks>
    ///     <para>
    ///     The UI Automation calls run on a background (MTA) thread of the thread pool, the returned task completes with a snapshot.
    ///     The control view of the window is read level by level (one FindAllBuildCache per element whose children are read, only the
    ///     children which aren't offscreen, with a cache for the bounds, control type, name, value, password and TextPattern state).
    ///     </para>
    ///     <para>
    ///     An element with a TextPattern (a document of a browser, an edit field, a WPF RichTextBox, Word) is read with one request for its
    ///     whole subtree (BuildUpdatedCache with TreeScope_Subtree). When that subtree has Text elements (browsers, Electron and WebView2
    ///     expose every run of text as one) the text comes from the elements: a few requests for the whole page instead of several per word
    ///     through the TextPattern, which is slow in Chromium. Else the TextPattern is read line by line: per line its text and its
    ///     rectangles, the words get their share of the line by characters. Lines with a link (a hyperlink element inside the element,
    ///     located with TextPattern.RangeFromChild) are read word by word, so the words of the link get its target exactly.
    ///     </para>
    ///     <para>
    ///     Elements: a Text element is a run with its name and bounds (a hyperlink inside it is read too and, when it covers the whole text,
    ///     replaces it), the Text elements inside a Hyperlink get the hyperlink's value (ValuePattern, the URL in a browser) as link target,
    ///     a Hyperlink without Text elements is a run with its name, a single-line Edit element without TextPattern a run with its value.
    ///     A run is split into words which get their share of the run's bounds by characters; a run which is higher than one and a half
    ///     lines wraps: inside a document its line rectangles come from TextPattern.RangeFromChild, else it is cut into bands of a line.
    ///     Words which overlap vertically are put on one line. The boxes are estimates, good for selections and labels.
    ///     </para>
    ///     <para>
    ///     Password elements (IsPassword) are left out with their children, also words of a TextPattern which lie in a password element.
    ///     Offscreen elements are left out, and everything is clipped to its element, the window and <paramref name="area"/>; a word with
    ///     nothing left is left out.
    ///     </para>
    ///     <para>
    ///     Some applications (e.g. Chromium based browsers) build their accessibility tree only when a UI Automation client asks for it:
    ///     while no text is found and the tree looks incomplete (the check of <see cref="UiAutomationAreas.FindAreasAsync(IntPtr, int, int, TimeSpan?, TimeSpan?, CancellationToken)"/>),
    ///     the text is read again with a pause of a quarter of a second, until it is found, the tree is complete or
    ///     <see cref="UiAutomationAreas.DefaultContentWait"/> (at most <paramref name="maxDuration"/>) has passed.
    ///     </para>
    ///     <para>
    ///     When <paramref name="maxDuration"/> has passed the reading stops and the snapshot has the lines read until then, with
    ///     <see cref="UiAutomationTextSnapshot.TimedOut"/> true. <see cref="UiAutomationTextSnapshot.IsComplete"/> is false when it stopped,
    ///     gave up waiting for the tree, a request timed out, a limit was reached, the window is gone or UI Automation isn't available.
    ///     </para>
    ///     <para>
    ///     A window of a remote display client (Citrix Workspace app, Remote Desktop, Horizon, ..., see
    ///     <see cref="RemoteDisplayWindow.GetKind(IntPtr)"/>) is a picture for Windows, UI Automation has no text for the remote content: it is
    ///     read once without waiting for the tree, <see cref="UiAutomationTextSnapshot.IsComplete"/> is false and
    ///     <see cref="UiAutomationTextSnapshot.RemoteDisplay"/> says which client it is, so the caller knows it has to use OCR.
    ///     </para>
    ///     <para>
    ///     Where IUIAutomation2 is available (Windows 8+) the connection and transaction timeouts are set, so a hanging application can't
    ///     block for the default 20 seconds. Neither text nor link targets are logged, only counts and timings (at verbose level, per phase,
    ///     with the number of UI Automation requests).
    ///     </para>
    /// </remarks>
    /// <param name="windowHandle">IntPtr with the handle of the window</param>
    /// <param name="area">NativeRect in screen pixels (physical pixels for a per-monitor DPI aware process), <see cref="NativeRect.Empty"/> for the whole window</param>
    /// <param name="connectionTimeout">TimeSpan for the UI Automation connection timeout, default <see cref="DefaultConnectionTimeout"/></param>
    /// <param name="transactionTimeout">TimeSpan for the UI Automation transaction timeout (one request), default <see cref="DefaultTransactionTimeout"/></param>
    /// <param name="maxDuration">TimeSpan after which the reading stops and returns what it has, null (default) for no limit</param>
    /// <param name="cancellationToken">CancellationToken, checked before every element and line, and during every pause; it throws, use
    ///     <paramref name="maxDuration"/> to get what was read so far</param>
    /// <returns>Task with the UiAutomationTextSnapshot, never null</returns>
    /// <exception cref="OperationCanceledException">When <paramref name="cancellationToken"/> was canceled</exception>
    public static Task<UiAutomationTextSnapshot> ReadVisibleTextAsync(IntPtr windowHandle, NativeRect area, TimeSpan? connectionTimeout = null,
        TimeSpan? transactionTimeout = null, TimeSpan? maxDuration = null, CancellationToken cancellationToken = default)
    {
        if (connectionTimeout <= TimeSpan.Zero)
        {
            throw new ArgumentOutOfRangeException(nameof(connectionTimeout), connectionTimeout, "The connection timeout must be positive.");
        }
        if (transactionTimeout <= TimeSpan.Zero)
        {
            throw new ArgumentOutOfRangeException(nameof(transactionTimeout), transactionTimeout, "The transaction timeout must be positive.");
        }
        if (maxDuration < TimeSpan.Zero)
        {
            throw new ArgumentOutOfRangeException(nameof(maxDuration), maxDuration, "The maximum duration can't be negative.");
        }
        if (windowHandle == IntPtr.Zero)
        {
            return Task.FromResult(Incomplete());
        }
        // The thread pool threads are MTA, UI Automation must not run on the UI thread which owns the window
        return Task.Run(() => ReadVisibleText(windowHandle, area, connectionTimeout ?? DefaultConnectionTimeout, transactionTimeout ?? DefaultTransactionTimeout,
            maxDuration, true, cancellationToken), cancellationToken);
    }

    /// <summary>
    ///     Read the text the window shows inside the area, see
    ///     <see cref="ReadVisibleTextAsync(IntPtr, NativeRect, TimeSpan?, TimeSpan?, TimeSpan?, CancellationToken)"/>
    /// </summary>
    /// <param name="window">IInteropWindow</param>
    /// <param name="area">NativeRect in screen pixels, <see cref="NativeRect.Empty"/> for the whole window</param>
    /// <param name="connectionTimeout">TimeSpan for the UI Automation connection timeout, default <see cref="DefaultConnectionTimeout"/></param>
    /// <param name="transactionTimeout">TimeSpan for the UI Automation transaction timeout, default <see cref="DefaultTransactionTimeout"/></param>
    /// <param name="maxDuration">TimeSpan after which the reading stops and returns what it has, null (default) for no limit</param>
    /// <param name="cancellationToken">CancellationToken</param>
    /// <returns>Task with the UiAutomationTextSnapshot, never null</returns>
    public static Task<UiAutomationTextSnapshot> ReadVisibleTextAsync(IInteropWindow window, NativeRect area, TimeSpan? connectionTimeout = null,
        TimeSpan? transactionTimeout = null, TimeSpan? maxDuration = null, CancellationToken cancellationToken = default)
        => window is null
            ? Task.FromResult(Incomplete())
            : ReadVisibleTextAsync(window.Handle, area, connectionTimeout, transactionTimeout, maxDuration, cancellationToken);

    /// <summary>
    ///     Kept for binary compatibility, see <see cref="ReadVisibleTextAsync(IntPtr, NativeRect, TimeSpan?, TimeSpan?, TimeSpan?, CancellationToken)"/>
    /// </summary>
    [EditorBrowsable(EditorBrowsableState.Never)]
    public static Task<UiAutomationTextSnapshot> ReadVisibleTextAsync(IntPtr windowHandle, NativeRect area, TimeSpan? connectionTimeout, TimeSpan? transactionTimeout,
        CancellationToken cancellationToken)
        => ReadVisibleTextAsync(windowHandle, area, connectionTimeout, transactionTimeout, null, cancellationToken);

    /// <summary>
    ///     Kept for binary compatibility, see <see cref="ReadVisibleTextAsync(IInteropWindow, NativeRect, TimeSpan?, TimeSpan?, TimeSpan?, CancellationToken)"/>
    /// </summary>
    [EditorBrowsable(EditorBrowsableState.Never)]
    public static Task<UiAutomationTextSnapshot> ReadVisibleTextAsync(IInteropWindow window, NativeRect area, TimeSpan? connectionTimeout, TimeSpan? transactionTimeout,
        CancellationToken cancellationToken)
        => ReadVisibleTextAsync(window, area, connectionTimeout, transactionTimeout, null, cancellationToken);

    private static UiAutomationTextSnapshot Incomplete(RemoteDisplayKinds remoteDisplay = RemoteDisplayKinds.None, bool timedOut = false) =>
        new UiAutomationTextSnapshot(null, false, remoteDisplay, timedOut);

    /// <summary>
    ///     Read the text on the calling (MTA) thread
    /// </summary>
    /// <param name="windowHandle">IntPtr</param>
    /// <param name="area">NativeRect, empty for the whole window</param>
    /// <param name="connectionTimeout">TimeSpan</param>
    /// <param name="transactionTimeout">TimeSpan</param>
    /// <param name="maxDuration">TimeSpan?, null for no limit</param>
    /// <param name="readDocumentSubtrees">false to read elements with a TextPattern level by level instead of with one request for their subtree
    ///     (for the tests of the fallback)</param>
    /// <param name="cancellationToken">CancellationToken</param>
    internal static UiAutomationTextSnapshot ReadVisibleText(IntPtr windowHandle, NativeRect area, TimeSpan connectionTimeout, TimeSpan transactionTimeout,
        TimeSpan? maxDuration, bool readDocumentSubtrees, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var stopwatch = Stopwatch.StartNew();
        // The content of a remote display window is a picture, waiting for a tree to be built (Chromium) would only cost time
        var remoteDisplay = RemoteDisplayWindow.GetKind(windowHandle);
        var contentWait = remoteDisplay == RemoteDisplayKinds.None ? UiAutomationAreas.DefaultContentWait : TimeSpan.Zero;
        if (maxDuration is { } max && max < contentWait)
        {
            contentWait = max;
        }
        var automation = UiAutomationScroller.CreateAutomation(connectionTimeout, transactionTimeout);
        if (automation is null)
        {
            return Incomplete(remoteDisplay);
        }
        try
        {
            var looksIncomplete = false;
            var gone = false;
            var attempts = 0;
            var calls = 0;
            bool DeadlinePassed() => maxDuration is { } limit && stopwatch.Elapsed >= limit;

            TextReader ReadOnce()
            {
                attempts++;
                var reader = new TextReader(automation, area, readDocumentSubtrees, stopwatch, maxDuration, cancellationToken);
                var ok = reader.Read(windowHandle);
                calls += reader.Calls;
                if (ok)
                {
                    return reader;
                }
                gone = true;
                return null;
            }

            var result = UiAutomationAreas.ReadUntil(() =>
            {
                var reader = ReadOnce();
                if (reader is null || reader.WordCount > 0 || reader.DeadlineReached)
                {
                    looksIncomplete = false;
                    return (reader, false);
                }
                cancellationToken.ThrowIfCancellationRequested();
                if (DeadlinePassed() || UiAutomationAreas.LooksIncomplete(automation, windowHandle, cancellationToken))
                {
                    looksIncomplete = true;
                    return (reader, !DeadlinePassed());
                }
                looksIncomplete = false;
                // Complete now, but the read itself makes Chromium build the tree: it may have been completed during the read, after the
                // part with the page was read. A read after the check is reliable.
                cancellationToken.ThrowIfCancellationRequested();
                if (DeadlinePassed())
                {
                    looksIncomplete = true;
                    return (reader, false);
                }
                return (ReadOnce() ?? reader, false);
            }, contentWait, UiAutomationAreas.ContentPause, windowHandle, cancellationToken);

            if (result is null)
            {
                Log.Verbose().WriteLine("Reading the text of window 0x{0:X} failed, the window is gone or has no UI Automation element.", windowHandle.ToInt64());
                return Incomplete(remoteDisplay);
            }
            var timedOut = result.TimedOut || result.DeadlineReached || (looksIncomplete && DeadlinePassed());
            var isComplete = remoteDisplay == RemoteDisplayKinds.None && !looksIncomplete && !gone && !result.IsTruncated && !timedOut;
            var snapshot = new UiAutomationTextSnapshot(result.BuildLines(), isComplete, remoteDisplay, timedOut);
            var elapsed = (long)stopwatch.Elapsed.TotalMilliseconds;
            Log.Verbose().WriteLine("Read {0} lines with {1} words ({2} links) of window 0x{3:X} in {4} ms with {5} attempts and {6} UI Automation requests, complete: {7} (truncated: {8}, timed out: {9}, deadline: {10}, remote display: {11})",
                snapshot.Lines.Count, result.WordCount, result.LinkWordCount, windowHandle.ToInt64(), elapsed, attempts, calls, isComplete,
                result.IsTruncated, result.TimedOut, result.DeadlineReached, remoteDisplay);
            Log.Verbose().WriteLine("Phases of window 0x{0:X} (last attempt): first word after {1} ms; {2} elements, {3} runs; {4} documents from their subtree in {5} ms ({6} failed), {7} through the TextPattern in {8} ms ({9} lines, {10} of them word by word); {11} requests",
                windowHandle.ToInt64(), result.FirstWordMilliseconds, result.ElementCount, result.RunCount, result.SubtreeDocuments, result.SubtreeMilliseconds,
                result.FailedSubtrees, result.TextPatternDocuments, result.TextPatternMilliseconds, result.TextPatternLines, result.WordByWordLines, result.Calls);
            return snapshot;
        }
        finally
        {
            UiAutomationScroller.Release(automation);
        }
    }

    /// <summary>
    ///     Group words of elements without TextPattern into lines: words which overlap vertically by at least half the smaller height,
    ///     each line from left to right. Pure geometry.
    /// </summary>
    /// <param name="words">the words</param>
    /// <returns>List with the lines from top to bottom</returns>
    internal static List<UiAutomationTextLine> GroupIntoLines(IEnumerable<UiAutomationTextWord> words)
    {
        var groups = new List<List<UiAutomationTextWord>>();
        var groupBounds = new List<NativeRect>();
        foreach (var word in words.OrderBy(word => word.Bounds.Top).ThenBy(word => word.Bounds.Left))
        {
            var index = -1;
            for (var candidate = groups.Count - 1; candidate >= 0; candidate--)
            {
                var bounds = groupBounds[candidate];
                var overlap = Math.Min(bounds.Bottom, word.Bounds.Bottom) - Math.Max(bounds.Top, word.Bounds.Top);
                if (overlap > 0 && overlap * 2 >= Math.Min(bounds.Height, word.Bounds.Height))
                {
                    index = candidate;
                    break;
                }
            }
            if (index < 0)
            {
                groups.Add(new List<UiAutomationTextWord> { word });
                groupBounds.Add(word.Bounds);
                continue;
            }
            groups[index].Add(word);
            groupBounds[index] = groupBounds[index].Union(word.Bounds);
        }
        var lines = new List<UiAutomationTextLine>(groups.Count);
        for (var index = 0; index < groups.Count; index++)
        {
            lines.Add(new UiAutomationTextLine(groupBounds[index], groups[index].OrderBy(word => word.Bounds.Left).ToList()));
        }
        return lines;
    }

    /// <summary>
    ///     Split a text which is drawn in the rectangles (one per line, in the order of the text) into words, each with its share of the
    ///     rectangles by characters: the characters are distributed over the rectangles by their widths (the split moves to white space nearby,
    ///     so a word stays in one rectangle), each character of a rectangle gets the same width. An estimate, pure geometry.
    /// </summary>
    /// <param name="text">string with the text</param>
    /// <param name="rectangles">the rectangles of the text in screen pixels, unclipped</param>
    /// <param name="clip">NativeRect the words are clipped to, words with nothing left are left out; <see cref="NativeRect.Empty"/> for no clip</param>
    /// <param name="linkTarget">string with the link target of all words, null for none</param>
    /// <returns>List with the words in the order of the text</returns>
    internal static List<UiAutomationTextWord> SplitIntoWords(string text, IReadOnlyList<NativeRect> rectangles, NativeRect clip, string linkTarget)
    {
        var words = new List<UiAutomationTextWord>();
        if (string.IsNullOrWhiteSpace(text) || rectangles is null)
        {
            return words;
        }
        var usable = rectangles.Where(rectangle => !rectangle.IsEmpty).ToList();
        if (usable.Count == 0)
        {
            return words;
        }
        long totalWidth = usable.Sum(rectangle => (long)rectangle.Width);
        var length = text.Length;
        var start = 0;
        long widthBefore = 0;
        for (var index = 0; index < usable.Count; index++)
        {
            var rectangle = usable[index];
            widthBefore += rectangle.Width;
            var end = index == usable.Count - 1 ? length : SnapToWhiteSpace(text, (int)Math.Round((double)length * widthBefore / totalWidth), start);
            if (end > start)
            {
                AddWordsOfSlice(text, start, end, rectangle, clip, linkTarget, words);
            }
            start = Math.Max(start, end);
        }
        return words;
    }

    /// <summary>
    ///     Move a split position to white space nearby, so no word is cut; it stays at or after <paramref name="minimum"/>
    /// </summary>
    private static int SnapToWhiteSpace(string text, int position, int minimum)
    {
        position = Math.Max(minimum, Math.Min(text.Length, position));
        if (position <= 0 || position >= text.Length || char.IsWhiteSpace(text[position]) || char.IsWhiteSpace(text[position - 1]))
        {
            return position;
        }
        for (var distance = 1; distance <= MaxSnapDistance; distance++)
        {
            var after = position + distance;
            if (after < text.Length && char.IsWhiteSpace(text[after]))
            {
                return after;
            }
            var before = position - distance;
            if (before > minimum && char.IsWhiteSpace(text[before]))
            {
                return before;
            }
        }
        return position;
    }

    private static void AddWordsOfSlice(string text, int start, int end, NativeRect rectangle, NativeRect clip, string linkTarget, List<UiAutomationTextWord> words)
    {
        // White space at the ends of the slice takes no room: line rectangles usually don't include it
        var first = start;
        while (first < end && char.IsWhiteSpace(text[first]))
        {
            first++;
        }
        var last = end;
        while (last > first && char.IsWhiteSpace(text[last - 1]))
        {
            last--;
        }
        if (last <= first)
        {
            return;
        }
        var characterWidth = (double)rectangle.Width / (last - first);
        var position = first;
        while (position < last)
        {
            while (position < last && char.IsWhiteSpace(text[position]))
            {
                position++;
            }
            var wordStart = position;
            while (position < last && !char.IsWhiteSpace(text[position]))
            {
                position++;
            }
            if (position <= wordStart)
            {
                continue;
            }
            var left = rectangle.Left + (int)Math.Floor((wordStart - first) * characterWidth);
            var right = rectangle.Left + (int)Math.Ceiling((position - first) * characterWidth);
            var bounds = new NativeRect(left, rectangle.Top, Math.Max(1, right - left), rectangle.Height);
            if (!clip.IsEmpty)
            {
                bounds = bounds.Intersect(clip);
            }
            if (!bounds.IsEmpty)
            {
                words.Add(new UiAutomationTextWord(bounds, text.Substring(wordStart, position - wordStart), linkTarget));
            }
        }
    }

    /// <summary>
    ///     Cut the bounds of a text which wraps into bands of one line
    /// </summary>
    /// <param name="bounds">NativeRect of the text</param>
    /// <param name="lineHeight">double with the usual line height</param>
    internal static List<NativeRect> SplitIntoLineBands(NativeRect bounds, double lineHeight)
    {
        var count = lineHeight <= 0 ? 1 : Math.Max(1, (int)Math.Round(bounds.Height / lineHeight));
        var bands = new List<NativeRect>(count);
        for (var index = 0; index < count; index++)
        {
            var top = bounds.Top + (int)Math.Round((double)bounds.Height * index / count);
            var bottom = bounds.Top + (int)Math.Round((double)bounds.Height * (index + 1) / count);
            if (bottom > top)
            {
                bands.Add(new NativeRect(bounds.Left, top, bounds.Width, bottom - top));
            }
        }
        return bands;
    }

    /// <summary>
    ///     The bounds of the rectangles of IUIAutomationTextRange.GetBoundingRectangles (left, top, width, height as doubles), each one
    ///     rounded outwards
    /// </summary>
    /// <param name="rectangles">double array, may be null</param>
    /// <returns>List with the rectangles which aren't empty</returns>
    internal static List<NativeRect> ToRectangles(double[] rectangles)
    {
        var result = new List<NativeRect>();
        if (rectangles is null)
        {
            return result;
        }
        for (var index = 0; index + 3 < rectangles.Length; index += 4)
        {
            var left = rectangles[index];
            var top = rectangles[index + 1];
            var width = rectangles[index + 2];
            var height = rectangles[index + 3];
            if (double.IsNaN(left) || double.IsNaN(top) || double.IsNaN(width) || double.IsNaN(height) || width <= 0 || height <= 0
                || Math.Abs(left) > int.MaxValue / 2.0 || Math.Abs(top) > int.MaxValue / 2.0 || width > int.MaxValue / 2.0 || height > int.MaxValue / 2.0)
            {
                continue;
            }
            var x = (int)Math.Floor(left);
            var y = (int)Math.Floor(top);
            var right = (int)Math.Ceiling(left + width);
            var bottom = (int)Math.Ceiling(top + height);
            var rectangle = new NativeRect(x, y, right - x, bottom - y);
            if (!rectangle.IsEmpty)
            {
                result.Add(rectangle);
            }
        }
        return result;
    }

    private static NativePoint Middle(NativeRect bounds) => new NativePoint(bounds.Left + bounds.Width / 2, bounds.Top + bounds.Height / 2);

    private static bool Contains(NativeRect bounds, NativePoint point) =>
        point.X >= bounds.Left && point.X < bounds.Right && point.Y >= bounds.Top && point.Y < bounds.Bottom;

    /// <summary>
    ///     A link inside an element which is read through its TextPattern: where its text is, and its target
    /// </summary>
    private sealed class LinkArea
    {
        public LinkArea(List<NativeRect> rectangles, string target)
        {
            Rectangles = rectangles;
            Target = target;
        }

        public List<NativeRect> Rectangles { get; }

        public string Target { get; }
    }

    /// <summary>
    ///     An element with a TextPattern whose text is read from its elements: its pattern is only needed for runs which wrap
    /// </summary>
    private sealed class DocumentContext
    {
        public DocumentContext(IUIAutomationElement element)
        {
            Element = element;
        }

        public IUIAutomationElement Element { get; }

        public IUIAutomationTextPattern Pattern { get; set; }

        public bool PatternRequested { get; set; }
    }

    /// <summary>
    ///     A run of text of an element: its bounds, where it is visible, its text and its link target
    /// </summary>
    private sealed class TextRun
    {
        public TextRun(NativeRect bounds, NativeRect clip, string text, string linkTarget, IUIAutomationElement element, DocumentContext document)
        {
            Bounds = bounds;
            Clip = clip;
            Text = text;
            LinkTarget = linkTarget;
            Element = element;
            Document = document;
        }

        /// <summary>The bounds of the element, not clipped</summary>
        public NativeRect Bounds { get; }

        /// <summary>The visible part: the bounds clipped to the parents, the window and the area</summary>
        public NativeRect Clip { get; }

        public string Text { get; }

        public string LinkTarget { get; }

        /// <summary>The element, kept only inside a document (for RangeFromChild)</summary>
        public IUIAutomationElement Element { get; }

        public DocumentContext Document { get; }
    }

    /// <summary>
    ///     Reads the text of a window once
    /// </summary>
    private sealed class TextReader
    {
        private readonly IUIAutomation _automation;
        private readonly NativeRect _area;
        private readonly bool _readDocumentSubtrees;
        private readonly Stopwatch _stopwatch;
        private readonly TimeSpan? _maxDuration;
        private readonly CancellationToken _cancellationToken;
        private readonly List<UiAutomationTextLine> _textPatternLines = new List<UiAutomationTextLine>();
        private readonly List<UiAutomationTextWord> _elementWords = new List<UiAutomationTextWord>();
        private readonly List<TextRun> _runs = new List<TextRun>();
        private readonly List<DocumentContext> _documents = new List<DocumentContext>();
        // Elements which are used after their visit (runs and documents), released at the end; reference equality of the RCWs
        private readonly HashSet<object> _kept = new HashSet<object>();

        private IUIAutomationCacheRequest _cacheRequest;
        private IUIAutomationCacheRequest _subtreeCacheRequest;
        private IUIAutomationCacheRequest _linkCacheRequest;
        private IUIAutomationCondition _controlView;
        private IUIAutomationCondition _notOffscreen;
        private IUIAutomationCondition _visibleChildren;
        private IUIAutomationCondition _hyperlink;
        private IUIAutomationCondition _visibleHyperlink;
        private IUIAutomationCondition _password;
        private IUIAutomationCondition _linkOrPassword;
        private IUIAutomationCondition _linksAndPasswords;
        private IUIAutomationCondition _text;
        private IUIAutomationCondition _visibleText;

        public TextReader(IUIAutomation automation, NativeRect area, bool readDocumentSubtrees, Stopwatch stopwatch, TimeSpan? maxDuration, CancellationToken cancellationToken)
        {
            _automation = automation;
            _area = area;
            _readDocumentSubtrees = readDocumentSubtrees;
            _stopwatch = stopwatch;
            _maxDuration = maxDuration;
            _cancellationToken = cancellationToken;
        }

        public int WordCount { get; private set; }

        public int LinkWordCount { get; private set; }

        public int ElementCount { get; private set; }

        public int RunCount => _runs.Count;

        /// <summary>The number of requests to UI Automation (which may go to the application)</summary>
        public int Calls { get; private set; }

        public long FirstWordMilliseconds { get; private set; } = -1;

        public int SubtreeDocuments { get; private set; }

        public int FailedSubtrees { get; private set; }

        public long SubtreeMilliseconds { get; private set; }

        public int TextPatternDocuments { get; private set; }

        public long TextPatternMilliseconds { get; private set; }

        public int TextPatternLines { get; private set; }

        public int WordByWordLines { get; private set; }

        /// <summary>
        ///     A limit was reached, not all text was read
        /// </summary>
        public bool IsTruncated { get; private set; }

        /// <summary>
        ///     A request timed out, not all text was read
        /// </summary>
        public bool TimedOut { get; private set; }

        /// <summary>
        ///     The maximum duration passed, the reading stopped
        /// </summary>
        public bool DeadlineReached { get; private set; }

        private bool IsStopped => IsTruncated || DeadlineReached;

        /// <summary>
        ///     Check the maximum duration (and the cancellation token)
        /// </summary>
        /// <returns>true when the reading has to stop</returns>
        private bool CheckStop()
        {
            _cancellationToken.ThrowIfCancellationRequested();
            if (!DeadlineReached && _maxDuration is { } maxDuration && _stopwatch.Elapsed >= maxDuration)
            {
                DeadlineReached = true;
            }
            return IsStopped;
        }

        /// <summary>
        ///     The lines from top to bottom
        /// </summary>
        public List<UiAutomationTextLine> BuildLines() =>
            _textPatternLines.Concat(GroupIntoLines(_elementWords))
                .OrderBy(line => line.Bounds.Top)
                .ThenBy(line => line.Bounds.Left)
                .ToList();

        /// <summary>
        ///     Read the text of the window
        /// </summary>
        /// <returns>false when the window's element can't be read (gone, UI Automation failed)</returns>
        public bool Read(IntPtr windowHandle)
        {
            IUIAutomationElement rootElement = null;
            try
            {
                if (!CreateRequests())
                {
                    return false;
                }
                _cancellationToken.ThrowIfCancellationRequested();
                var hResult = Call(_automation.ElementFromHandleBuildCache(windowHandle, _cacheRequest, out rootElement));
                if (hResult != UiaConstants.S_OK || rootElement is null)
                {
                    Log.Verbose().WriteLine("Reading the UI Automation element of 0x{0:X} failed with 0x{1:X8}", windowHandle.ToInt64(), hResult);
                    return false;
                }
                if (rootElement.get_CachedBoundingRectangle(out var rootBounds) != UiaConstants.S_OK || rootBounds.IsEmpty)
                {
                    return true;
                }
                var clip = _area.IsEmpty ? rootBounds : rootBounds.Intersect(_area);
                if (!clip.IsEmpty)
                {
                    Visit(rootElement, clip, 0, false, null, null, _runs);
                    AddRunWords();
                }
                return true;
            }
            finally
            {
                foreach (var document in _documents)
                {
                    UiAutomationScroller.Release(document.Pattern);
                    document.Pattern = null;
                }
                if (rootElement is not null && !_kept.Contains(rootElement))
                {
                    UiAutomationScroller.Release(rootElement);
                }
                foreach (var kept in _kept)
                {
                    UiAutomationScroller.Release(kept);
                }
                _kept.Clear();
                UiAutomationScroller.Release(_visibleText);
                UiAutomationScroller.Release(_text);
                UiAutomationScroller.Release(_linksAndPasswords);
                UiAutomationScroller.Release(_linkOrPassword);
                UiAutomationScroller.Release(_password);
                UiAutomationScroller.Release(_visibleHyperlink);
                UiAutomationScroller.Release(_hyperlink);
                UiAutomationScroller.Release(_visibleChildren);
                UiAutomationScroller.Release(_notOffscreen);
                UiAutomationScroller.Release(_controlView);
                UiAutomationScroller.Release(_linkCacheRequest);
                UiAutomationScroller.Release(_subtreeCacheRequest);
                UiAutomationScroller.Release(_cacheRequest);
                _visibleText = _text = _linksAndPasswords = _linkOrPassword = _password = _visibleHyperlink = _hyperlink = _visibleChildren = _notOffscreen = _controlView = null;
                _linkCacheRequest = _subtreeCacheRequest = _cacheRequest = null;
            }
        }

        private bool AddElementProperties(IUIAutomationCacheRequest cacheRequest) =>
            cacheRequest.AddProperty(UiaConstants.BoundingRectanglePropertyId) == UiaConstants.S_OK
            && cacheRequest.AddProperty(UiaConstants.ControlTypePropertyId) == UiaConstants.S_OK
            && cacheRequest.AddProperty(UiaConstants.NamePropertyId) == UiaConstants.S_OK
            && cacheRequest.AddProperty(UiaConstants.IsOffscreenPropertyId) == UiaConstants.S_OK
            && cacheRequest.AddProperty(UiaConstants.IsPasswordPropertyId) == UiaConstants.S_OK
            && cacheRequest.AddProperty(UiaConstants.IsTextPatternAvailablePropertyId) == UiaConstants.S_OK
            // The value of links and edit fields; it is only read from elements which aren't password fields
            && cacheRequest.AddProperty(UiaConstants.ValueValuePropertyId) == UiaConstants.S_OK;

        private bool CreateRequests() =>
            _automation.get_ControlViewCondition(out _controlView) == UiaConstants.S_OK && _controlView is not null
            // Offscreen elements are left out anyway: filtered in the application, a long web page has thousands of them
            && _automation.CreatePropertyCondition(UiaConstants.IsOffscreenPropertyId, false, out _notOffscreen) == UiaConstants.S_OK && _notOffscreen is not null
            && _automation.CreateAndCondition(_controlView, _notOffscreen, out _visibleChildren) == UiaConstants.S_OK && _visibleChildren is not null
            // The children of an element, level by level
            && _automation.CreateCacheRequest(out _cacheRequest) == UiaConstants.S_OK && _cacheRequest is not null
            && AddElementProperties(_cacheRequest)
            && _cacheRequest.put_TreeFilter(_controlView) == UiaConstants.S_OK
            // The whole subtree of an element with a TextPattern in one request, only the visible elements of the control view
            && _automation.CreateCacheRequest(out _subtreeCacheRequest) == UiaConstants.S_OK && _subtreeCacheRequest is not null
            && AddElementProperties(_subtreeCacheRequest)
            && _subtreeCacheRequest.put_TreeScope(UiaConstants.TreeScopeSubtree) == UiaConstants.S_OK
            && _subtreeCacheRequest.put_TreeFilter(_visibleChildren) == UiaConstants.S_OK
            // The links and password fields inside an element which is read through its TextPattern
            && _automation.CreateCacheRequest(out _linkCacheRequest) == UiaConstants.S_OK && _linkCacheRequest is not null
            && _linkCacheRequest.AddProperty(UiaConstants.BoundingRectanglePropertyId) == UiaConstants.S_OK
            && _linkCacheRequest.AddProperty(UiaConstants.ControlTypePropertyId) == UiaConstants.S_OK
            && _linkCacheRequest.AddProperty(UiaConstants.IsPasswordPropertyId) == UiaConstants.S_OK
            && _linkCacheRequest.AddProperty(UiaConstants.ValueValuePropertyId) == UiaConstants.S_OK
            && _linkCacheRequest.put_TreeFilter(_controlView) == UiaConstants.S_OK
            && _automation.CreatePropertyCondition(UiaConstants.ControlTypePropertyId, UiaConstants.HyperlinkControlTypeId, out _hyperlink) == UiaConstants.S_OK && _hyperlink is not null
            && _automation.CreateAndCondition(_hyperlink, _visibleChildren, out _visibleHyperlink) == UiaConstants.S_OK && _visibleHyperlink is not null
            && _automation.CreatePropertyCondition(UiaConstants.IsPasswordPropertyId, true, out _password) == UiaConstants.S_OK && _password is not null
            && _automation.CreateOrCondition(_visibleHyperlink, _password, out _linkOrPassword) == UiaConstants.S_OK && _linkOrPassword is not null
            && _automation.CreateAndCondition(_controlView, _linkOrPassword, out _linksAndPasswords) == UiaConstants.S_OK && _linksAndPasswords is not null
            && _automation.CreatePropertyCondition(UiaConstants.ControlTypePropertyId, UiaConstants.TextControlTypeId, out _text) == UiaConstants.S_OK && _text is not null
            && _automation.CreateAndCondition(_text, _visibleChildren, out _visibleText) == UiaConstants.S_OK && _visibleText is not null;

        /// <summary>
        ///     Count a request and note a timeout, the snapshot is then incomplete
        /// </summary>
        private int Call(int hResult)
        {
            Calls++;
            if (hResult == UiaConstants.Timeout)
            {
                TimedOut = true;
            }
            return hResult;
        }

        private void Keep(object comObject)
        {
            if (comObject is not null)
            {
                _kept.Add(comObject);
            }
        }

        /// <summary>
        ///     Read the text of the element (from its cache) and of its children
        /// </summary>
        /// <param name="element">IUIAutomationElement with the cache of the request</param>
        /// <param name="parentClip">NativeRect which the text is clipped to: the parent, the window and the area</param>
        /// <param name="nesting">int with the depth, a guard</param>
        /// <param name="inSubtree">true when the element has its subtree cached: its children are read from the cache, without a request</param>
        /// <param name="document">DocumentContext of the element with a TextPattern the element is in, null for none</param>
        /// <param name="linkTarget">string with the target of the hyperlink the element is in, null for none</param>
        /// <param name="runs">List for the runs of text</param>
        private void Visit(IUIAutomationElement element, NativeRect parentClip, int nesting, bool inSubtree, DocumentContext document, string linkTarget, List<TextRun> runs)
        {
            if (CheckStop())
            {
                return;
            }
            if (++ElementCount > MaxElements)
            {
                IsTruncated = true;
                return;
            }
            // Never anything of a password field: not its value, not its children
            if (element.get_CachedIsPassword(out var isPassword) == UiaConstants.S_OK && isPassword != 0)
            {
                return;
            }
            if (element.get_CachedIsOffscreen(out var isOffscreen) == UiaConstants.S_OK && isOffscreen != 0)
            {
                return;
            }
            if (element.get_CachedBoundingRectangle(out var bounds) != UiaConstants.S_OK || bounds.IsEmpty)
            {
                return;
            }
            var clip = bounds.Intersect(parentClip);
            if (clip.IsEmpty)
            {
                return;
            }
            if (IsTextPatternAvailable(element) && document is null)
            {
                if (!inSubtree && _readDocumentSubtrees && ReadSubtree(element, out var updated))
                {
                    element = updated;
                    inSubtree = true;
                }
                if (!HasTextDescendant(element, inSubtree))
                {
                    if (ReadTextPattern(element, clip))
                    {
                        return;
                    }
                }
                else
                {
                    // The text is in the elements (a browser page): read them, the pattern is only needed for runs which wrap
                    Keep(element);
                    document = new DocumentContext(element);
                    _documents.Add(document);
                }
            }
            switch (GetControlType(element))
            {
                case UiaConstants.HyperlinkControlTypeId:
                    var target = GetCachedValue(element) ?? linkTarget;
                    var linkRuns = new List<TextRun>();
                    VisitChildren(element, clip, nesting, inSubtree, document, target, linkRuns);
                    if (linkRuns.Count > 0)
                    {
                        runs.AddRange(linkRuns);
                    }
                    else
                    {
                        AddRun(runs, element, bounds, clip, GetName(element), target, document);
                    }
                    return;
                case UiaConstants.EditControlTypeId:
                    // Without TextPattern the value is all text of the field, only a single line can be placed
                    var value = GetCachedValue(element);
                    if (value is not null && value.IndexOfAny(new[] { '\r', '\n' }) < 0)
                    {
                        AddRun(runs, element, bounds, clip, value, null, null);
                    }
                    return;
                case UiaConstants.TextControlTypeId:
                    // A link inside the text (a WPF TextBlock with a Hyperlink) is a child: when the children cover the whole text they replace
                    // the text, so it isn't there twice
                    var childRuns = new List<TextRun>();
                    VisitChildren(element, clip, nesting, inSubtree, document, linkTarget, childRuns);
                    var name = GetName(element).Trim();
                    if (name.Length > 0 && !IsCoveredBy(name, childRuns))
                    {
                        AddRun(runs, element, bounds, clip, name, linkTarget, document);
                    }
                    runs.AddRange(childRuns);
                    return;
                default:
                    VisitChildren(element, clip, nesting, inSubtree, document, linkTarget, runs);
                    return;
            }
        }

        /// <summary>
        ///     True when the runs have the same text as the name, or one of them has
        /// </summary>
        private static bool IsCoveredBy(string name, List<TextRun> runs)
        {
            if (runs.Count == 0)
            {
                return false;
            }
            if (runs.Any(run => run.Text == name))
            {
                return true;
            }
            static string WithoutWhiteSpace(string text) => new string(text.Where(character => !char.IsWhiteSpace(character)).ToArray());
            return WithoutWhiteSpace(string.Concat(runs.Select(run => run.Text))) == WithoutWhiteSpace(name);
        }

        /// <summary>
        ///     Fetch the subtree of an element with a TextPattern with one request
        /// </summary>
        /// <returns>false when the request failed (e.g. timed out on a huge page), the element is then read level by level</returns>
        private bool ReadSubtree(IUIAutomationElement element, out IUIAutomationElement updated)
        {
            var started = _stopwatch.ElapsedMilliseconds;
            Calls++;
            // Not counted as a timeout of the snapshot: the elements are then read level by level
            var hResult = element.BuildUpdatedCache(_subtreeCacheRequest, out updated);
            SubtreeMilliseconds += _stopwatch.ElapsedMilliseconds - started;
            if (hResult != UiaConstants.S_OK || updated is null)
            {
                FailedSubtrees++;
                Log.Verbose().WriteLine("Reading the subtree of an element with a TextPattern failed with 0x{0:X8} after {1} ms, reading it level by level.",
                    hResult, _stopwatch.ElapsedMilliseconds - started);
                UiAutomationScroller.Release(updated);
                updated = null;
                return false;
            }
            SubtreeDocuments++;
            Keep(updated);
            return true;
        }

        /// <summary>
        ///     True when the element has a Text element inside it: from the cache for a cached subtree, else with one request
        /// </summary>
        private bool HasTextDescendant(IUIAutomationElement element, bool inSubtree)
        {
            if (!inSubtree)
            {
                if (Call(element.FindFirst(UiaConstants.TreeScopeDescendants, _visibleText, out var found)) != UiaConstants.S_OK || found is null)
                {
                    return false;
                }
                UiAutomationScroller.Release(found);
                return true;
            }
            var visited = 0;
            return HasCachedTextDescendant(element, ref visited);
        }

        private static bool HasCachedTextDescendant(IUIAutomationElement element, ref int visited)
        {
            if (element.GetCachedChildren(out var children) != UiaConstants.S_OK || children is null)
            {
                return false;
            }
            try
            {
                if (children.get_Length(out var length) != UiaConstants.S_OK)
                {
                    return false;
                }
                for (var index = 0; index < length && visited < MaxElements; index++)
                {
                    if (children.GetElement(index, out var child) != UiaConstants.S_OK || child is null)
                    {
                        continue;
                    }
                    visited++;
                    try
                    {
                        if (GetControlType(child) == UiaConstants.TextControlTypeId || HasCachedTextDescendant(child, ref visited))
                        {
                            return true;
                        }
                    }
                    finally
                    {
                        UiAutomationScroller.Release(child);
                    }
                }
                return false;
            }
            finally
            {
                UiAutomationScroller.Release(children);
            }
        }

        private void VisitChildren(IUIAutomationElement element, NativeRect clip, int nesting, bool inSubtree, DocumentContext document, string linkTarget,
            List<TextRun> runs)
        {
            if (nesting >= MaxNesting)
            {
                IsTruncated = true;
                return;
            }
            if (CheckStop())
            {
                return;
            }
            IUIAutomationElementArray children;
            if (inSubtree)
            {
                // From the cache, no request
                if (element.GetCachedChildren(out children) != UiaConstants.S_OK || children is null)
                {
                    return;
                }
            }
            else if (Call(element.FindAllBuildCache(UiaConstants.TreeScopeChildren, _visibleChildren, _cacheRequest, out children)) != UiaConstants.S_OK || children is null)
            {
                return;
            }
            try
            {
                if (children.get_Length(out var length) != UiaConstants.S_OK)
                {
                    return;
                }
                for (var index = 0; index < length && !IsStopped; index++)
                {
                    if (children.GetElement(index, out var child) != UiaConstants.S_OK || child is null)
                    {
                        continue;
                    }
                    try
                    {
                        Visit(child, clip, nesting + 1, inSubtree, document, linkTarget, runs);
                    }
                    finally
                    {
                        if (!_kept.Contains(child))
                        {
                            UiAutomationScroller.Release(child);
                        }
                    }
                }
            }
            finally
            {
                UiAutomationScroller.Release(children);
            }
        }

        private void AddRun(List<TextRun> runs, IUIAutomationElement element, NativeRect bounds, NativeRect clip, string text, string linkTarget, DocumentContext document)
        {
            text = text?.Trim();
            if (string.IsNullOrEmpty(text))
            {
                return;
            }
            if (document is not null)
            {
                // For RangeFromChild, when the run wraps
                Keep(element);
            }
            runs.Add(new TextRun(bounds, clip, text, linkTarget, document is null ? null : element, document));
        }

        /// <summary>
        ///     Split the runs into words. A run which is higher than one and a half lines wraps: its line rectangles come from the TextPattern
        ///     of its document, else it is cut into bands of a line.
        /// </summary>
        private void AddRunWords()
        {
            if (_runs.Count == 0)
            {
                return;
            }
            var heights = _runs.Select(run => run.Bounds.Height).Where(height => height > 0).OrderBy(height => height).ToList();
            var lineHeight = heights.Count == 0 ? 0 : heights[heights.Count / 2];
            foreach (var run in _runs)
            {
                if (WordCount >= MaxWords)
                {
                    IsTruncated = true;
                    return;
                }
                IReadOnlyList<NativeRect> rectangles = null;
                if (lineHeight > 0 && run.Bounds.Height > lineHeight * MultiLineFactor)
                {
                    // After the deadline the runs which were read still become words, without further requests
                    if (run.Document is not null && run.Element is not null && !CheckStop())
                    {
                        rectangles = GetRectanglesOfChild(GetPattern(run.Document), run.Element);
                    }
                    if (rectangles is null || rectangles.Count == 0)
                    {
                        rectangles = SplitIntoLineBands(run.Bounds, lineHeight);
                    }
                }
                AddWords(_elementWords, SplitIntoWords(run.Text, rectangles ?? new[] { run.Bounds }, run.Clip, run.LinkTarget), null);
            }
        }

        private IUIAutomationTextPattern GetPattern(DocumentContext document)
        {
            if (!document.PatternRequested)
            {
                document.PatternRequested = true;
                if (Call(document.Element.GetCurrentPattern(UiaConstants.TextPatternId, out var patternObject)) == UiaConstants.S_OK && patternObject is IUIAutomationTextPattern pattern)
                {
                    document.Pattern = pattern;
                }
                else
                {
                    UiAutomationScroller.Release(patternObject);
                }
            }
            return document.Pattern;
        }

        /// <summary>
        ///     Add the words, leaving out those in a password field
        /// </summary>
        private void AddWords(List<UiAutomationTextWord> target, List<UiAutomationTextWord> words, List<NativeRect> passwords)
        {
            foreach (var word in words)
            {
                if (WordCount >= MaxWords)
                {
                    IsTruncated = true;
                    return;
                }
                if (passwords is { Count: > 0 })
                {
                    var middle = Middle(word.Bounds);
                    if (passwords.Any(password => Contains(password, middle)))
                    {
                        continue;
                    }
                }
                if (FirstWordMilliseconds < 0)
                {
                    FirstWordMilliseconds = _stopwatch.ElapsedMilliseconds;
                }
                WordCount++;
                if (word.IsLink)
                {
                    LinkWordCount++;
                }
                target.Add(word);
            }
        }

        private void AddLine(List<UiAutomationTextWord> words)
        {
            if (words.Count == 0)
            {
                return;
            }
            var bounds = NativeRect.Empty;
            foreach (var word in words)
            {
                bounds = bounds.Union(word.Bounds);
            }
            _textPatternLines.Add(new UiAutomationTextLine(bounds, words));
        }

        /// <summary>
        ///     Read the visible text of an element through its TextPattern
        /// </summary>
        /// <returns>false when the element has no usable TextPattern, it is then read like an element without one</returns>
        private bool ReadTextPattern(IUIAutomationElement element, NativeRect clip)
        {
            if (CheckStop())
            {
                return true;
            }
            var started = _stopwatch.ElapsedMilliseconds;
            if (Call(element.GetCurrentPattern(UiaConstants.TextPatternId, out var patternObject)) != UiaConstants.S_OK || patternObject is not IUIAutomationTextPattern pattern)
            {
                UiAutomationScroller.Release(patternObject);
                return false;
            }
            IUIAutomationTextRangeArray ranges = null;
            try
            {
                if (Call(pattern.GetVisibleRanges(out ranges)) != UiaConstants.S_OK || ranges is null || ranges.get_Length(out var length) != UiaConstants.S_OK)
                {
                    return false;
                }
                TextPatternDocuments++;
                if (length == 0)
                {
                    return true;
                }
                ReadLinksAndPasswords(element, pattern, out var links, out var passwords);
                for (var index = 0; index < length && !IsStopped; index++)
                {
                    if (ranges.GetElement(index, out var range) != UiaConstants.S_OK || range is null)
                    {
                        continue;
                    }
                    try
                    {
                        ReadLines(range, clip, links, passwords);
                    }
                    finally
                    {
                        UiAutomationScroller.Release(range);
                    }
                }
                return true;
            }
            finally
            {
                TextPatternMilliseconds += _stopwatch.ElapsedMilliseconds - started;
                UiAutomationScroller.Release(ranges);
                UiAutomationScroller.Release(pattern);
            }
        }

        /// <summary>
        ///     The links (with a target) and the password fields inside an element with a TextPattern, one request
        /// </summary>
        private void ReadLinksAndPasswords(IUIAutomationElement element, IUIAutomationTextPattern pattern, out List<LinkArea> links, out List<NativeRect> passwords)
        {
            links = new List<LinkArea>();
            passwords = new List<NativeRect>();
            if (CheckStop())
            {
                return;
            }
            if (Call(element.FindAllBuildCache(UiaConstants.TreeScopeDescendants, _linksAndPasswords, _linkCacheRequest, out var found)) != UiaConstants.S_OK || found is null)
            {
                return;
            }
            try
            {
                if (found.get_Length(out var length) != UiaConstants.S_OK)
                {
                    return;
                }
                for (var index = 0; index < length && !CheckStop(); index++)
                {
                    if (found.GetElement(index, out var child) != UiaConstants.S_OK || child is null)
                    {
                        continue;
                    }
                    try
                    {
                        child.get_CachedBoundingRectangle(out var bounds);
                        if (child.get_CachedIsPassword(out var isPassword) != UiaConstants.S_OK || isPassword != 0)
                        {
                            if (!bounds.IsEmpty)
                            {
                                passwords.Add(bounds);
                            }
                            continue;
                        }
                        if (links.Count >= MaxLinks)
                        {
                            continue;
                        }
                        var target = GetCachedValue(child);
                        if (target is null)
                        {
                            continue;
                        }
                        var rectangles = GetRectanglesOfChild(pattern, child);
                        if (rectangles.Count == 0 && !bounds.IsEmpty)
                        {
                            rectangles.Add(bounds);
                        }
                        if (rectangles.Count > 0)
                        {
                            links.Add(new LinkArea(rectangles, target));
                        }
                    }
                    finally
                    {
                        UiAutomationScroller.Release(child);
                    }
                }
            }
            finally
            {
                UiAutomationScroller.Release(found);
            }
        }

        /// <summary>
        ///     Where the text of a child element (a link, a run of text) is: the rectangles of its range, empty when the provider has no range for it
        /// </summary>
        private List<NativeRect> GetRectanglesOfChild(IUIAutomationTextPattern pattern, IUIAutomationElement child)
        {
            if (pattern is null || Call(pattern.RangeFromChild(child, out var range)) != UiaConstants.S_OK || range is null)
            {
                return new List<NativeRect>();
            }
            try
            {
                return Call(range.GetBoundingRectangles(out var rectangles)) == UiaConstants.S_OK ? ToRectangles(rectangles) : new List<NativeRect>();
            }
            finally
            {
                UiAutomationScroller.Release(range);
            }
        }

        /// <summary>
        ///     Split a visible range into lines and read them: per line its rectangles and its text, the words get their share of the line;
        ///     a line with a link is read word by word
        /// </summary>
        private void ReadLines(IUIAutomationTextRange visibleRange, NativeRect clip, List<LinkArea> links, List<NativeRect> passwords)
        {
            if (Call(visibleRange.Clone(out var line)) != UiaConstants.S_OK || line is null)
            {
                return;
            }
            try
            {
                if (Call(line.MoveEndpointByRange(TextPatternRangeEndpoint.End, line, TextPatternRangeEndpoint.Start)) != UiaConstants.S_OK)
                {
                    return;
                }
                var isFirst = true;
                for (var count = 0; count < MaxUnitsPerRange; count++)
                {
                    if (CheckStop())
                    {
                        return;
                    }
                    if (Call(line.ExpandToEnclosingUnit(TextUnit.Line)) != UiaConstants.S_OK || !ClampTo(line, visibleRange, isFirst))
                    {
                        return;
                    }
                    isFirst = false;
                    if (!ReadLine(line, clip, links, passwords, out var isEmpty) || !MoveToNext(line, visibleRange, TextUnit.Line, isEmpty))
                    {
                        return;
                    }
                }
                // The guard was reached with text left
                if (!IsStopped && HasRest(line, visibleRange))
                {
                    IsTruncated = true;
                }
            }
            finally
            {
                UiAutomationScroller.Release(line);
            }
        }

        /// <summary>
        ///     Read one line
        /// </summary>
        /// <param name="line">the line range</param>
        /// <param name="clip">NativeRect of the element</param>
        /// <param name="links">the links of the element</param>
        /// <param name="passwords">the password fields of the element</param>
        /// <param name="isEmpty">true when the line range is empty, null when it isn't known (not visible, its text wasn't read)</param>
        /// <returns>false when a request failed</returns>
        private bool ReadLine(IUIAutomationTextRange line, NativeRect clip, List<LinkArea> links, List<NativeRect> passwords, out bool? isEmpty)
        {
            isEmpty = null;
            if (Call(line.GetBoundingRectangles(out var lineRectangles)) != UiaConstants.S_OK)
            {
                return false;
            }
            var rectangles = ToRectangles(lineRectangles);
            // A line which isn't visible (scrolled away inside the visible range, or clipped away) needs no text
            if (!rectangles.Any(rectangle => !rectangle.Intersect(clip).IsEmpty))
            {
                return true;
            }
            if (Call(line.GetText(-1, out var text)) != UiaConstants.S_OK)
            {
                return false;
            }
            isEmpty = string.IsNullOrEmpty(text);
            if (string.IsNullOrWhiteSpace(text))
            {
                return true;
            }
            TextPatternLines++;
            if (links.Any(link => link.Rectangles.Any(linkRectangle => rectangles.Any(rectangle => !rectangle.Intersect(linkRectangle).IsEmpty))))
            {
                // The words of a link get its target: read the words, so their bounds are exact
                WordByWordLines++;
                ReadWords(line, clip, links, passwords);
                return true;
            }
            var words = new List<UiAutomationTextWord>();
            AddWords(words, SplitIntoWords(text, rectangles, clip, null), passwords);
            AddLine(words);
            return true;
        }

        /// <summary>
        ///     Read the words of a line with a range per word, and add the line
        /// </summary>
        private void ReadWords(IUIAutomationTextRange line, NativeRect clip, List<LinkArea> links, List<NativeRect> passwords)
        {
            if (Call(line.Clone(out var wordRange)) != UiaConstants.S_OK || wordRange is null)
            {
                return;
            }
            var words = new List<UiAutomationTextWord>();
            try
            {
                if (Call(wordRange.MoveEndpointByRange(TextPatternRangeEndpoint.End, wordRange, TextPatternRangeEndpoint.Start)) != UiaConstants.S_OK)
                {
                    return;
                }
                var isFirst = true;
                for (var count = 0; count < MaxUnitsPerRange && !CheckStop(); count++)
                {
                    if (Call(wordRange.ExpandToEnclosingUnit(TextUnit.Word)) != UiaConstants.S_OK || !ClampTo(wordRange, line, isFirst))
                    {
                        break;
                    }
                    isFirst = false;
                    var isEmpty = ReadWord(wordRange, clip, links, passwords, words);
                    if (!MoveToNext(wordRange, line, TextUnit.Word, isEmpty))
                    {
                        break;
                    }
                }
            }
            finally
            {
                UiAutomationScroller.Release(wordRange);
                AddLine(words);
            }
        }

        /// <returns>true when the word range is empty, null when it isn't known</returns>
        private bool? ReadWord(IUIAutomationTextRange wordRange, NativeRect clip, List<LinkArea> links, List<NativeRect> passwords, List<UiAutomationTextWord> words)
        {
            if (Call(wordRange.GetText(-1, out var text)) != UiaConstants.S_OK)
            {
                return null;
            }
            if (string.IsNullOrWhiteSpace(text))
            {
                return string.IsNullOrEmpty(text);
            }
            if (Call(wordRange.GetBoundingRectangles(out var rectangles)) != UiaConstants.S_OK)
            {
                return false;
            }
            var bounds = NativeRect.Empty;
            foreach (var rectangle in ToRectangles(rectangles))
            {
                bounds = bounds.Union(rectangle.Intersect(clip));
            }
            if (bounds.IsEmpty)
            {
                return false;
            }
            var middle = Middle(bounds);
            var link = links.FirstOrDefault(candidate => candidate.Rectangles.Any(rectangle => Contains(rectangle, middle)));
            AddWords(words, new List<UiAutomationTextWord> { new UiAutomationTextWord(bounds, text.Trim(), link?.Target) }, passwords);
            return false;
        }

        /// <summary>
        ///     Limit the range to the outer range; the start only for the first unit (only it can start before the outer range)
        /// </summary>
        /// <returns>false when a request failed</returns>
        private bool ClampTo(IUIAutomationTextRange range, IUIAutomationTextRange outer, bool clampStart)
        {
            if (clampStart)
            {
                if (Call(range.CompareEndpoints(TextPatternRangeEndpoint.Start, outer, TextPatternRangeEndpoint.Start, out var start)) != UiaConstants.S_OK)
                {
                    return false;
                }
                if (start < 0 && Call(range.MoveEndpointByRange(TextPatternRangeEndpoint.Start, outer, TextPatternRangeEndpoint.Start)) != UiaConstants.S_OK)
                {
                    return false;
                }
            }
            if (Call(range.CompareEndpoints(TextPatternRangeEndpoint.End, outer, TextPatternRangeEndpoint.End, out var end)) != UiaConstants.S_OK)
            {
                return false;
            }
            return end <= 0 || Call(range.MoveEndpointByRange(TextPatternRangeEndpoint.End, outer, TextPatternRangeEndpoint.End)) == UiaConstants.S_OK;
        }

        /// <summary>
        ///     Make the range the (empty) start of the next unit
        /// </summary>
        /// <param name="range">the range of the current unit</param>
        /// <param name="outer">the range which is split</param>
        /// <param name="unit">TextUnit</param>
        /// <param name="isEmpty">whether the range is empty, null when it isn't known (asked then)</param>
        /// <returns>false when the outer range ends there, or the range can't move on</returns>
        private bool MoveToNext(IUIAutomationTextRange range, IUIAutomationTextRange outer, TextUnit unit, bool? isEmpty)
        {
            if (isEmpty is null)
            {
                if (Call(range.CompareEndpoints(TextPatternRangeEndpoint.Start, range, TextPatternRangeEndpoint.End, out var length)) != UiaConstants.S_OK)
                {
                    return false;
                }
                isEmpty = length >= 0;
            }
            if (isEmpty == false)
            {
                // Not empty: continue at its end
                if (Call(range.MoveEndpointByRange(TextPatternRangeEndpoint.Start, range, TextPatternRangeEndpoint.End)) != UiaConstants.S_OK)
                {
                    return false;
                }
            }
            // Empty (a provider whose unit didn't expand): move on by one unit, else ExpandToEnclosingUnit gives the same range again
            else if (Call(range.Move(unit, 1, out var moved)) != UiaConstants.S_OK || moved == 0)
            {
                return false;
            }
            return HasRest(range, outer);
        }

        /// <summary>
        ///     True when the start of the range lies before the end of the outer range
        /// </summary>
        private bool HasRest(IUIAutomationTextRange range, IUIAutomationTextRange outer) =>
            Call(range.CompareEndpoints(TextPatternRangeEndpoint.Start, outer, TextPatternRangeEndpoint.End, out var rest)) == UiaConstants.S_OK && rest < 0;
    }

    private static bool IsTextPatternAvailable(IUIAutomationElement element)
    {
        if (element.GetCachedPropertyValue(UiaConstants.IsTextPatternAvailablePropertyId, out var value) != UiaConstants.S_OK)
        {
            return false;
        }
        if (value is bool isAvailable)
        {
            return isAvailable;
        }
        // The "not supported" value is a COM object
        UiAutomationScroller.Release(value);
        return false;
    }

    /// <summary>
    ///     The cached value of the ValuePattern (a link target, the text of an edit field), null when there is none; only asked after the
    ///     element was checked not to be a password field
    /// </summary>
    private static string GetCachedValue(IUIAutomationElement element)
    {
        if (element.GetCachedPropertyValue(UiaConstants.ValueValuePropertyId, out var value) != UiaConstants.S_OK)
        {
            return null;
        }
        if (value is string text)
        {
            return text.Length == 0 ? null : text;
        }
        UiAutomationScroller.Release(value);
        return null;
    }

    private static int GetControlType(IUIAutomationElement element) =>
        element.get_CachedControlType(out var controlType) == UiaConstants.S_OK ? controlType : 0;

    private static string GetName(IUIAutomationElement element) =>
        element.get_CachedName(out var name) == UiaConstants.S_OK ? name ?? string.Empty : string.Empty;
}
