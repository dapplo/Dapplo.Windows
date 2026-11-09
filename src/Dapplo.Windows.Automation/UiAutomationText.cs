// Copyright (c) Dapplo and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Dapplo.Log;
using Dapplo.Windows.Automation.Interop;
using Dapplo.Windows.Common.Extensions;
using Dapplo.Windows.Common.Structs;
using Dapplo.Windows.Desktop;

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
    ///     The default for the connection timeout of <see cref="ReadVisibleTextAsync(IntPtr, NativeRect, TimeSpan?, TimeSpan?, CancellationToken)"/>:
    ///     2 seconds
    /// </summary>
    public static readonly TimeSpan DefaultConnectionTimeout = TimeSpan.FromSeconds(2);

    /// <summary>
    ///     The default for the transaction timeout (one request to the application) of
    ///     <see cref="ReadVisibleTextAsync(IntPtr, NativeRect, TimeSpan?, TimeSpan?, CancellationToken)"/>: 2 seconds
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
    ///     The most links per element with a TextPattern
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
    ///     Read the text the window shows inside <paramref name="area"/>, as lines from top to bottom with the words and their bounds.
    /// </summary>
    /// <remarks>
    ///     <para>
    ///     The UI Automation calls run on a background (MTA) thread of the thread pool, the returned task completes with a snapshot.
    ///     The control view of the window is read level by level (one FindAllBuildCache per element whose children are read, only the
    ///     children which aren't offscreen, with a cache for the bounds, control type, name, password and TextPattern state).
    ///     </para>
    ///     <para>
    ///     An element with a TextPattern (a document in a browser, an edit field, a WPF RichTextBox) is read through it: its visible ranges are
    ///     split into lines and the lines into words, each with the bounds UI Automation reports; its children aren't read. A word belongs
    ///     to a link when its middle lies in the text of a hyperlink element inside it (TextPattern.RangeFromChild), the link target is the
    ///     value of the hyperlink (ValuePattern, the URL in a browser).
    ///     Other elements: a Text element is one word with its name and bounds (a hyperlink inside it is read too and, when it is the
    ///     whole text, replaces it), a Hyperlink element one word with its name, bounds and value as link target, a single-line Edit element
    ///     without TextPattern one word with its value. Words of elements without TextPattern which overlap vertically are put on one line.
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
    ///     <see cref="UiAutomationAreas.DefaultContentWait"/> has passed. <see cref="UiAutomationTextSnapshot.IsComplete"/> is false when it gave
    ///     up, a request timed out, a limit was reached, the window is gone or UI Automation isn't available.
    ///     </para>
    ///     <para>
    ///     Where IUIAutomation2 is available (Windows 8+) the connection and transaction timeouts are set, so a hanging application can't
    ///     block for the default 20 seconds. Neither text nor link targets are logged, only counts and timings.
    ///     </para>
    /// </remarks>
    /// <param name="windowHandle">IntPtr with the handle of the window</param>
    /// <param name="area">NativeRect in screen pixels (physical pixels for a per-monitor DPI aware process), <see cref="NativeRect.Empty"/> for the whole window</param>
    /// <param name="connectionTimeout">TimeSpan for the UI Automation connection timeout, default <see cref="DefaultConnectionTimeout"/></param>
    /// <param name="transactionTimeout">TimeSpan for the UI Automation transaction timeout (one request), default <see cref="DefaultTransactionTimeout"/></param>
    /// <param name="cancellationToken">CancellationToken, checked before every element and line, and during every pause</param>
    /// <returns>Task with the UiAutomationTextSnapshot, never null</returns>
    /// <exception cref="OperationCanceledException">When <paramref name="cancellationToken"/> was canceled</exception>
    public static Task<UiAutomationTextSnapshot> ReadVisibleTextAsync(IntPtr windowHandle, NativeRect area, TimeSpan? connectionTimeout = null,
        TimeSpan? transactionTimeout = null, CancellationToken cancellationToken = default)
    {
        if (connectionTimeout <= TimeSpan.Zero)
        {
            throw new ArgumentOutOfRangeException(nameof(connectionTimeout), connectionTimeout, "The connection timeout must be positive.");
        }
        if (transactionTimeout <= TimeSpan.Zero)
        {
            throw new ArgumentOutOfRangeException(nameof(transactionTimeout), transactionTimeout, "The transaction timeout must be positive.");
        }
        if (windowHandle == IntPtr.Zero)
        {
            return Task.FromResult(Incomplete());
        }
        // The thread pool threads are MTA, UI Automation must not run on the UI thread which owns the window
        return Task.Run(() => ReadVisibleText(windowHandle, area, connectionTimeout ?? DefaultConnectionTimeout, transactionTimeout ?? DefaultTransactionTimeout,
            cancellationToken), cancellationToken);
    }

    /// <summary>
    ///     Read the text the window shows inside the area, see <see cref="ReadVisibleTextAsync(IntPtr, NativeRect, TimeSpan?, TimeSpan?, CancellationToken)"/>
    /// </summary>
    /// <param name="window">IInteropWindow</param>
    /// <param name="area">NativeRect in screen pixels, <see cref="NativeRect.Empty"/> for the whole window</param>
    /// <param name="connectionTimeout">TimeSpan for the UI Automation connection timeout, default <see cref="DefaultConnectionTimeout"/></param>
    /// <param name="transactionTimeout">TimeSpan for the UI Automation transaction timeout, default <see cref="DefaultTransactionTimeout"/></param>
    /// <param name="cancellationToken">CancellationToken</param>
    /// <returns>Task with the UiAutomationTextSnapshot, never null</returns>
    public static Task<UiAutomationTextSnapshot> ReadVisibleTextAsync(IInteropWindow window, NativeRect area, TimeSpan? connectionTimeout = null,
        TimeSpan? transactionTimeout = null, CancellationToken cancellationToken = default)
        => window is null
            ? Task.FromResult(Incomplete())
            : ReadVisibleTextAsync(window.Handle, area, connectionTimeout, transactionTimeout, cancellationToken);

    private static UiAutomationTextSnapshot Incomplete() => new UiAutomationTextSnapshot(null, false);

    private static UiAutomationTextSnapshot ReadVisibleText(IntPtr windowHandle, NativeRect area, TimeSpan connectionTimeout, TimeSpan transactionTimeout,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var stopwatch = Stopwatch.StartNew();
        var automation = UiAutomationScroller.CreateAutomation(connectionTimeout, transactionTimeout);
        if (automation is null)
        {
            return Incomplete();
        }
        try
        {
            var looksIncomplete = false;
            var gone = false;
            var attempts = 0;
            TextReader ReadOnce()
            {
                attempts++;
                var reader = new TextReader(automation, area, cancellationToken);
                if (reader.Read(windowHandle))
                {
                    return reader;
                }
                gone = true;
                return null;
            }

            var result = UiAutomationAreas.ReadUntil(() =>
            {
                var reader = ReadOnce();
                if (reader is null || reader.WordCount > 0)
                {
                    looksIncomplete = false;
                    return (reader, false);
                }
                cancellationToken.ThrowIfCancellationRequested();
                if (UiAutomationAreas.LooksIncomplete(automation, windowHandle, cancellationToken))
                {
                    looksIncomplete = true;
                    return (reader, true);
                }
                looksIncomplete = false;
                // Complete now, but the read itself makes Chromium build the tree: it may have been completed during the read, after the
                // part with the page was read. A read after the check is reliable.
                cancellationToken.ThrowIfCancellationRequested();
                return (ReadOnce() ?? reader, false);
            }, UiAutomationAreas.DefaultContentWait, UiAutomationAreas.ContentPause, windowHandle, cancellationToken);

            if (result is null)
            {
                Log.Verbose().WriteLine("Reading the text of window 0x{0:X} failed, the window is gone or has no UI Automation element.", windowHandle.ToInt64());
                return Incomplete();
            }
            var isComplete = !looksIncomplete && !gone && !result.IsTruncated && !result.TimedOut;
            var snapshot = new UiAutomationTextSnapshot(result.BuildLines(), isComplete);
            Log.Verbose().WriteLine("Read {0} lines with {1} words ({2} links) of window 0x{3:X} from {4} elements in {5} ms with {6} attempts, complete: {7} (truncated: {8}, timed out: {9})",
                snapshot.Lines.Count, result.WordCount, result.LinkWordCount, windowHandle.ToInt64(), result.ElementCount, (long)stopwatch.Elapsed.TotalMilliseconds,
                attempts, isComplete, result.IsTruncated, result.TimedOut);
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
    ///     A link inside an element with a TextPattern: where its text is, and its target
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
    ///     Reads the text of a window once
    /// </summary>
    private sealed class TextReader
    {
        private readonly IUIAutomation _automation;
        private readonly NativeRect _area;
        private readonly CancellationToken _cancellationToken;
        private readonly List<UiAutomationTextLine> _textPatternLines = new List<UiAutomationTextLine>();
        private readonly List<UiAutomationTextWord> _elementWords = new List<UiAutomationTextWord>();

        private IUIAutomationCacheRequest _cacheRequest;
        private IUIAutomationCacheRequest _boundsCacheRequest;
        private IUIAutomationCondition _controlView;
        private IUIAutomationCondition _notOffscreen;
        private IUIAutomationCondition _visibleChildren;
        private IUIAutomationCondition _hyperlink;
        private IUIAutomationCondition _visibleHyperlink;
        private IUIAutomationCondition _password;
        private IUIAutomationCondition _linkOrPassword;
        private IUIAutomationCondition _linksAndPasswords;

        public TextReader(IUIAutomation automation, NativeRect area, CancellationToken cancellationToken)
        {
            _automation = automation;
            _area = area;
            _cancellationToken = cancellationToken;
        }

        public int WordCount { get; private set; }

        public int LinkWordCount { get; private set; }

        public int ElementCount { get; private set; }

        /// <summary>
        ///     A limit was reached, not all text was read
        /// </summary>
        public bool IsTruncated { get; private set; }

        /// <summary>
        ///     A request timed out, not all text was read
        /// </summary>
        public bool TimedOut { get; private set; }

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
                var hResult = _automation.ElementFromHandleBuildCache(windowHandle, _cacheRequest, out rootElement);
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
                    Visit(rootElement, clip, 0, _elementWords);
                }
                return true;
            }
            finally
            {
                UiAutomationScroller.Release(rootElement);
                UiAutomationScroller.Release(_linksAndPasswords);
                UiAutomationScroller.Release(_linkOrPassword);
                UiAutomationScroller.Release(_password);
                UiAutomationScroller.Release(_visibleHyperlink);
                UiAutomationScroller.Release(_hyperlink);
                UiAutomationScroller.Release(_visibleChildren);
                UiAutomationScroller.Release(_notOffscreen);
                UiAutomationScroller.Release(_controlView);
                UiAutomationScroller.Release(_boundsCacheRequest);
                UiAutomationScroller.Release(_cacheRequest);
                _linksAndPasswords = _linkOrPassword = _password = _visibleHyperlink = _hyperlink = _visibleChildren = _notOffscreen = _controlView = null;
                _boundsCacheRequest = _cacheRequest = null;
            }
        }

        private bool CreateRequests() =>
            _automation.CreateCacheRequest(out _cacheRequest) == UiaConstants.S_OK && _cacheRequest is not null
            && _cacheRequest.AddProperty(UiaConstants.BoundingRectanglePropertyId) == UiaConstants.S_OK
            && _cacheRequest.AddProperty(UiaConstants.ControlTypePropertyId) == UiaConstants.S_OK
            && _cacheRequest.AddProperty(UiaConstants.NamePropertyId) == UiaConstants.S_OK
            && _cacheRequest.AddProperty(UiaConstants.IsOffscreenPropertyId) == UiaConstants.S_OK
            && _cacheRequest.AddProperty(UiaConstants.IsPasswordPropertyId) == UiaConstants.S_OK
            && _cacheRequest.AddProperty(UiaConstants.IsTextPatternAvailablePropertyId) == UiaConstants.S_OK
            && _automation.get_ControlViewCondition(out _controlView) == UiaConstants.S_OK && _controlView is not null
            && _cacheRequest.put_TreeFilter(_controlView) == UiaConstants.S_OK
            // The links and password fields inside an element with a TextPattern: the bounds and the control type, never the value
            && _automation.CreateCacheRequest(out _boundsCacheRequest) == UiaConstants.S_OK && _boundsCacheRequest is not null
            && _boundsCacheRequest.AddProperty(UiaConstants.BoundingRectanglePropertyId) == UiaConstants.S_OK
            && _boundsCacheRequest.AddProperty(UiaConstants.ControlTypePropertyId) == UiaConstants.S_OK
            && _boundsCacheRequest.AddProperty(UiaConstants.IsPasswordPropertyId) == UiaConstants.S_OK
            && _boundsCacheRequest.put_TreeFilter(_controlView) == UiaConstants.S_OK
            // Offscreen elements are left out anyway: filtered in the application, a long web page has thousands of them
            && _automation.CreatePropertyCondition(UiaConstants.IsOffscreenPropertyId, false, out _notOffscreen) == UiaConstants.S_OK && _notOffscreen is not null
            && _automation.CreateAndCondition(_controlView, _notOffscreen, out _visibleChildren) == UiaConstants.S_OK && _visibleChildren is not null
            && _automation.CreatePropertyCondition(UiaConstants.ControlTypePropertyId, UiaConstants.HyperlinkControlTypeId, out _hyperlink) == UiaConstants.S_OK && _hyperlink is not null
            && _automation.CreateAndCondition(_hyperlink, _visibleChildren, out _visibleHyperlink) == UiaConstants.S_OK && _visibleHyperlink is not null
            && _automation.CreatePropertyCondition(UiaConstants.IsPasswordPropertyId, true, out _password) == UiaConstants.S_OK && _password is not null
            && _automation.CreateOrCondition(_visibleHyperlink, _password, out _linkOrPassword) == UiaConstants.S_OK && _linkOrPassword is not null
            && _automation.CreateAndCondition(_controlView, _linkOrPassword, out _linksAndPasswords) == UiaConstants.S_OK && _linksAndPasswords is not null;

        /// <summary>
        ///     Note a timeout, the snapshot is incomplete
        /// </summary>
        private int Check(int hResult)
        {
            if (hResult == UiaConstants.Timeout)
            {
                TimedOut = true;
            }
            return hResult;
        }

        /// <summary>
        ///     Read the text of the element (from its cache) and of its children
        /// </summary>
        /// <param name="element">IUIAutomationElement with the cache of the request</param>
        /// <param name="parentClip">NativeRect which the text is clipped to: the parent, the window and the area</param>
        /// <param name="nesting">int with the depth, a guard</param>
        /// <param name="words">List for the words of elements without TextPattern</param>
        private void Visit(IUIAutomationElement element, NativeRect parentClip, int nesting, List<UiAutomationTextWord> words)
        {
            if (IsTruncated)
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
            if (IsTextPatternAvailable(element) && ReadTextPattern(element, clip))
            {
                return;
            }
            switch (GetControlType(element))
            {
                case UiaConstants.HyperlinkControlTypeId:
                    AddWord(words, clip, GetName(element), GetValue(element));
                    return;
                case UiaConstants.EditControlTypeId:
                    // Without TextPattern the value is all text of the field, only a single line can be placed
                    var value = GetValue(element);
                    if (value is not null && value.IndexOfAny(new[] { '\r', '\n' }) < 0)
                    {
                        AddWord(words, clip, value, null);
                    }
                    return;
                case UiaConstants.TextControlTypeId:
                    // A link inside the text (a WPF TextBlock with a Hyperlink) is a child: when it is the whole text it replaces the text,
                    // so the text isn't there twice
                    var childWords = new List<UiAutomationTextWord>();
                    VisitChildren(element, clip, nesting, childWords);
                    var name = GetName(element).Trim();
                    if (name.Length > 0 && !childWords.Any(word => word.Text == name))
                    {
                        AddWord(words, clip, name, null);
                    }
                    words.AddRange(childWords);
                    return;
                default:
                    VisitChildren(element, clip, nesting, words);
                    return;
            }
        }

        private void VisitChildren(IUIAutomationElement element, NativeRect clip, int nesting, List<UiAutomationTextWord> words)
        {
            if (nesting >= MaxNesting)
            {
                IsTruncated = true;
                return;
            }
            _cancellationToken.ThrowIfCancellationRequested();
            if (Check(element.FindAllBuildCache(UiaConstants.TreeScopeChildren, _visibleChildren, _cacheRequest, out var children)) != UiaConstants.S_OK || children is null)
            {
                return;
            }
            try
            {
                if (children.get_Length(out var length) != UiaConstants.S_OK)
                {
                    return;
                }
                for (var index = 0; index < length && !IsTruncated; index++)
                {
                    if (children.GetElement(index, out var child) != UiaConstants.S_OK || child is null)
                    {
                        continue;
                    }
                    try
                    {
                        Visit(child, clip, nesting + 1, words);
                    }
                    finally
                    {
                        UiAutomationScroller.Release(child);
                    }
                }
            }
            finally
            {
                UiAutomationScroller.Release(children);
            }
        }

        private void AddWord(List<UiAutomationTextWord> words, NativeRect bounds, string text, string linkTarget)
        {
            text = text?.Trim();
            if (string.IsNullOrEmpty(text))
            {
                return;
            }
            if (WordCount >= MaxWords)
            {
                IsTruncated = true;
                return;
            }
            var word = new UiAutomationTextWord(bounds, text, linkTarget);
            WordCount++;
            if (word.IsLink)
            {
                LinkWordCount++;
            }
            words.Add(word);
        }

        /// <summary>
        ///     Read the visible text of an element through its TextPattern
        /// </summary>
        /// <returns>false when the element has no usable TextPattern, it is then read like an element without one</returns>
        private bool ReadTextPattern(IUIAutomationElement element, NativeRect clip)
        {
            _cancellationToken.ThrowIfCancellationRequested();
            if (Check(element.GetCurrentPattern(UiaConstants.TextPatternId, out var patternObject)) != UiaConstants.S_OK || patternObject is not IUIAutomationTextPattern pattern)
            {
                UiAutomationScroller.Release(patternObject);
                return false;
            }
            IUIAutomationTextRangeArray ranges = null;
            try
            {
                if (Check(pattern.GetVisibleRanges(out ranges)) != UiaConstants.S_OK || ranges is null || ranges.get_Length(out var length) != UiaConstants.S_OK)
                {
                    return false;
                }
                if (length == 0)
                {
                    return true;
                }
                ReadLinksAndPasswords(element, pattern, out var links, out var passwords);
                for (var index = 0; index < length && !IsTruncated; index++)
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
            _cancellationToken.ThrowIfCancellationRequested();
            if (Check(element.FindAllBuildCache(UiaConstants.TreeScopeDescendants, _linksAndPasswords, _boundsCacheRequest, out var found)) != UiaConstants.S_OK || found is null)
            {
                return;
            }
            try
            {
                if (found.get_Length(out var length) != UiaConstants.S_OK)
                {
                    return;
                }
                for (var index = 0; index < length; index++)
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
                        var target = GetValue(child);
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
        ///     Where the text of a child element (a link) is: the rectangles of its range, empty when the provider has no range for it
        /// </summary>
        private List<NativeRect> GetRectanglesOfChild(IUIAutomationTextPattern pattern, IUIAutomationElement child)
        {
            if (Check(pattern.RangeFromChild(child, out var range)) != UiaConstants.S_OK || range is null)
            {
                return new List<NativeRect>();
            }
            try
            {
                return Check(range.GetBoundingRectangles(out var rectangles)) == UiaConstants.S_OK ? ToRectangles(rectangles) : new List<NativeRect>();
            }
            finally
            {
                UiAutomationScroller.Release(range);
            }
        }

        /// <summary>
        ///     Split a visible range into lines and read their words
        /// </summary>
        private void ReadLines(IUIAutomationTextRange visibleRange, NativeRect clip, List<LinkArea> links, List<NativeRect> passwords)
        {
            if (Check(visibleRange.Clone(out var line)) != UiaConstants.S_OK || line is null)
            {
                return;
            }
            try
            {
                if (line.MoveEndpointByRange(TextPatternRangeEndpoint.End, line, TextPatternRangeEndpoint.Start) != UiaConstants.S_OK)
                {
                    return;
                }
                for (var count = 0; count < MaxUnitsPerRange && !IsTruncated; count++)
                {
                    _cancellationToken.ThrowIfCancellationRequested();
                    if (Check(line.ExpandToEnclosingUnit(TextUnit.Line)) != UiaConstants.S_OK || !ClampTo(line, visibleRange))
                    {
                        return;
                    }
                    ReadWords(line, clip, links, passwords);
                    if (!MoveToNext(line, visibleRange, TextUnit.Line))
                    {
                        return;
                    }
                }
                // The guard was reached with text left
                if (!IsTruncated && HasRest(line, visibleRange))
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
        ///     Read the words of a line and add the line
        /// </summary>
        private void ReadWords(IUIAutomationTextRange line, NativeRect clip, List<LinkArea> links, List<NativeRect> passwords)
        {
            // A line which isn't visible (scrolled away inside the visible range, or clipped away) needs no word ranges
            if (Check(line.GetBoundingRectangles(out var lineRectangles)) != UiaConstants.S_OK
                || !ToRectangles(lineRectangles).Any(rectangle => !rectangle.Intersect(clip).IsEmpty))
            {
                return;
            }
            if (Check(line.Clone(out var wordRange)) != UiaConstants.S_OK || wordRange is null)
            {
                return;
            }
            var words = new List<UiAutomationTextWord>();
            try
            {
                if (wordRange.MoveEndpointByRange(TextPatternRangeEndpoint.End, wordRange, TextPatternRangeEndpoint.Start) != UiaConstants.S_OK)
                {
                    return;
                }
                for (var count = 0; count < MaxUnitsPerRange && !IsTruncated; count++)
                {
                    if (Check(wordRange.ExpandToEnclosingUnit(TextUnit.Word)) != UiaConstants.S_OK || !ClampTo(wordRange, line))
                    {
                        break;
                    }
                    ReadWord(wordRange, clip, links, passwords, words);
                    if (!MoveToNext(wordRange, line, TextUnit.Word))
                    {
                        break;
                    }
                }
            }
            finally
            {
                UiAutomationScroller.Release(wordRange);
                if (words.Count > 0)
                {
                    var bounds = NativeRect.Empty;
                    foreach (var word in words)
                    {
                        bounds = bounds.Union(word.Bounds);
                    }
                    _textPatternLines.Add(new UiAutomationTextLine(bounds, words));
                }
            }
        }

        private void ReadWord(IUIAutomationTextRange wordRange, NativeRect clip, List<LinkArea> links, List<NativeRect> passwords, List<UiAutomationTextWord> words)
        {
            if (Check(wordRange.GetText(-1, out var text)) != UiaConstants.S_OK || string.IsNullOrWhiteSpace(text))
            {
                return;
            }
            if (Check(wordRange.GetBoundingRectangles(out var rectangles)) != UiaConstants.S_OK)
            {
                return;
            }
            var bounds = NativeRect.Empty;
            foreach (var rectangle in ToRectangles(rectangles))
            {
                bounds = bounds.Union(rectangle.Intersect(clip));
            }
            if (bounds.IsEmpty)
            {
                return;
            }
            var middle = Middle(bounds);
            if (passwords.Any(password => Contains(password, middle)))
            {
                return;
            }
            var link = links.FirstOrDefault(candidate => candidate.Rectangles.Any(rectangle => Contains(rectangle, middle)));
            AddWord(words, bounds, text, link?.Target);
        }

        /// <summary>
        ///     Limit the range to the outer range
        /// </summary>
        /// <returns>false when a call failed</returns>
        private static bool ClampTo(IUIAutomationTextRange range, IUIAutomationTextRange outer)
        {
            if (range.CompareEndpoints(TextPatternRangeEndpoint.Start, outer, TextPatternRangeEndpoint.Start, out var start) != UiaConstants.S_OK)
            {
                return false;
            }
            if (start < 0 && range.MoveEndpointByRange(TextPatternRangeEndpoint.Start, outer, TextPatternRangeEndpoint.Start) != UiaConstants.S_OK)
            {
                return false;
            }
            if (range.CompareEndpoints(TextPatternRangeEndpoint.End, outer, TextPatternRangeEndpoint.End, out var end) != UiaConstants.S_OK)
            {
                return false;
            }
            return end <= 0 || range.MoveEndpointByRange(TextPatternRangeEndpoint.End, outer, TextPatternRangeEndpoint.End) == UiaConstants.S_OK;
        }

        /// <summary>
        ///     Make the range the (empty) start of the next unit
        /// </summary>
        /// <returns>false when the outer range ends there, or the range can't move on</returns>
        private static bool MoveToNext(IUIAutomationTextRange range, IUIAutomationTextRange outer, TextUnit unit)
        {
            if (range.CompareEndpoints(TextPatternRangeEndpoint.Start, range, TextPatternRangeEndpoint.End, out var length) != UiaConstants.S_OK)
            {
                return false;
            }
            if (length < 0)
            {
                // Not empty: continue at its end
                if (range.MoveEndpointByRange(TextPatternRangeEndpoint.Start, range, TextPatternRangeEndpoint.End) != UiaConstants.S_OK)
                {
                    return false;
                }
            }
            // Empty (a provider whose unit didn't expand): move on by one unit, else ExpandToEnclosingUnit gives the same range again
            else if (range.Move(unit, 1, out var moved) != UiaConstants.S_OK || moved == 0)
            {
                return false;
            }
            return HasRest(range, outer);
        }

        /// <summary>
        ///     True when the start of the range lies before the end of the outer range
        /// </summary>
        private static bool HasRest(IUIAutomationTextRange range, IUIAutomationTextRange outer) =>
            range.CompareEndpoints(TextPatternRangeEndpoint.Start, outer, TextPatternRangeEndpoint.End, out var rest) == UiaConstants.S_OK && rest < 0;
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
    ///     The value of the ValuePattern (a link target, the text of an edit field), null when there is none; only asked after the
    ///     element was checked not to be a password field
    /// </summary>
    private static string GetValue(IUIAutomationElement element)
    {
        if (element.GetCurrentPropertyValue(UiaConstants.ValueValuePropertyId, out var value) != UiaConstants.S_OK)
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
