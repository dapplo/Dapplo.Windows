// Copyright (c) Dapplo and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
using System;
using System.Runtime.InteropServices;
using System.Threading;
using Dapplo.Log;
using Dapplo.Windows.Automation.Interop;
using Dapplo.Windows.Common.Structs;
using Dapplo.Windows.Desktop;
using Dapplo.Windows.Input.Mouse;

namespace Dapplo.Windows.Automation;

/// <summary>
///     Scrolls an element which supports the UI Automation <c>ScrollPattern</c>. This works for nearly every application which draws
///     its own scroll bars, where <see cref="InteropWindowExtensions.GetWindowScroller"/> returns null: Chromium / Electron, Firefox,
///     WPF, WinUI, Office and the Explorer file list.
///     Create it with <see cref="FromPoint"/> or <see cref="FromWindow(IntPtr, bool)"/>; it has the same shape as
///     <see cref="WindowScroller"/>, both implement <see cref="IScroller"/>.
/// </summary>
/// <remarks>
///     <para>
///     Threading: use it from a background thread (MTA, e.g. the thread pool); calls block while the target application answers.
///     Never use it on the UI thread which owns the target window (e.g. your own window), UI Automation would wait for that thread.
///     An STA thread is not needed.
///     </para>
///     <para>
///     Every property and method calls into the target process, so they always reflect the current state; a step needs about three
///     calls. When the element is gone (the page navigated, the window closed), <see cref="IsAvailable"/> becomes false,
///     <see cref="IsAtEnd"/> and <see cref="IsAtStart"/> return true so loops end, and the methods return false;
///     <see cref="Refresh"/> finds the element again at the original location.
///     </para>
///     <para>
///     Dispose it to release the COM objects right away instead of waiting for the garbage collector.
///     </para>
/// </remarks>
public sealed class UiAutomationScroller : IScroller, IDisposable
{
    private static readonly LogSource Log = new LogSource();

    /// <summary>
    ///     How often, and with which interval in milliseconds, the position is checked after a mouse wheel movement
    /// </summary>
    private const int PositionChangeChecks = 15;
    private const int PositionChangeCheckInterval = 10;

    private readonly IUIAutomation _automation;
    private readonly NativePoint? _point;
    private readonly IntPtr _windowHandle;
    private IUIAutomationElement _element;
    private IUIAutomationScrollPattern _scrollPattern;
    private double _stepFraction = 1.0;
    private bool _isDisposed;

    private UiAutomationScroller(IUIAutomation automation, IUIAutomationElement element, IUIAutomationScrollPattern scrollPattern, bool horizontal, NativePoint? point, IntPtr windowHandle)
    {
        _automation = automation;
        _element = element;
        _scrollPattern = scrollPattern;
        Horizontal = horizontal;
        _point = point;
        _windowHandle = windowHandle;
        TryGetScrollPercent(out var percent);
        InitialScrollPercent = percent;
    }

    /// <summary>
    ///     Find the scrollable element under a screen point: the element at the point, or the first of its ancestors with a
    ///     <c>ScrollPattern</c> which can scroll in the requested direction.
    /// </summary>
    /// <param name="screenPoint">NativePoint in screen coordinates, e.g. where the user clicked</param>
    /// <param name="horizontal">false (default) for vertical scrolling, true for horizontal scrolling</param>
    /// <returns>UiAutomationScroller, or null when nothing at that point can scroll in that direction</returns>
    public static UiAutomationScroller FromPoint(NativePoint screenPoint, bool horizontal = false)
    {
        var automation = CreateAutomation();
        if (automation is null)
        {
            return null;
        }
        var scroller = FindAtPoint(automation, screenPoint, horizontal, screenPoint, IntPtr.Zero);
        if (scroller is null)
        {
            Release(automation);
        }
        return scroller;
    }

    /// <summary>
    ///     Find the scrollable element of a window: the window element itself when it can scroll, else the element under the middle of
    ///     the window (walking up to a scrollable ancestor), else the first scrollable descendant of the window.
    /// </summary>
    /// <param name="windowHandle">IntPtr with the handle of the window</param>
    /// <param name="horizontal">false (default) for vertical scrolling, true for horizontal scrolling</param>
    /// <returns>UiAutomationScroller, or null when the window has nothing which can scroll in that direction</returns>
    public static UiAutomationScroller FromWindow(IntPtr windowHandle, bool horizontal = false)
    {
        if (windowHandle == IntPtr.Zero)
        {
            return null;
        }
        var automation = CreateAutomation();
        if (automation is null)
        {
            return null;
        }
        var scroller = FindInWindow(automation, windowHandle, horizontal);
        if (scroller is null)
        {
            Release(automation);
        }
        return scroller;
    }

    /// <summary>
    ///     Find the scrollable element of a window, see <see cref="FromWindow(IntPtr, bool)"/>
    /// </summary>
    /// <param name="window">IInteropWindow</param>
    /// <param name="horizontal">false (default) for vertical scrolling, true for horizontal scrolling</param>
    /// <returns>UiAutomationScroller, or null when the window has nothing which can scroll in that direction</returns>
    public static UiAutomationScroller FromWindow(IInteropWindow window, bool horizontal = false) => window is null ? null : FromWindow(window.Handle, horizontal);

    /// <summary>
    ///     True when this scrolls horizontally, false for vertical scrolling
    /// </summary>
    public bool Horizontal { get; }

    /// <summary>
    ///     How <see cref="Next"/>, <see cref="Previous"/>, <see cref="Start"/> and <see cref="End"/> scroll, default is
    ///     <see cref="UiAutomationScrollModes.ScrollPattern"/>
    /// </summary>
    public UiAutomationScrollModes ScrollMode { get; set; } = UiAutomationScrollModes.ScrollPattern;

    /// <summary>
    ///     False when the element is not available anymore (e.g. the page navigated or the window closed), see <see cref="Refresh"/>
    /// </summary>
    public bool IsAvailable { get; private set; } = true;

    /// <summary>
    ///     The scroll percentage when this scroller was created (or refreshed), used by <see cref="Reset"/>; -1 when it was unknown
    /// </summary>
    public double InitialScrollPercent { get; private set; }

    /// <summary>
    ///     The tolerance in percentage points for <see cref="IsAtStart"/> and <see cref="IsAtEnd"/>, some applications never report exactly 0 or 100.
    ///     Default is 0.5.
    /// </summary>
    public double PercentTolerance { get; set; } = 0.5;

    /// <summary>
    ///     The part of a page that <see cref="Next"/> and <see cref="Previous"/> scroll, greater than 0 and at most 1.
    ///     The default 1.0 scrolls a full page; a scrolling capture uses e.g. 0.5 so consecutive frames overlap.
    /// </summary>
    /// <exception cref="ArgumentOutOfRangeException">The value is not greater than 0 and at most 1.</exception>
    public double StepFraction
    {
        get => _stepFraction;
        set
        {
            if (!(value > 0 && value <= 1))
            {
                throw new ArgumentOutOfRangeException(nameof(value), value, "The step fraction must be greater than 0 and at most 1.");
            }
            _stepFraction = value;
        }
    }

    /// <summary>
    ///     The maximum number of SmallIncrement scroll calls (when SetScrollPercent is not supported) or mouse wheel notches for one step,
    ///     and the maximum number of steps <see cref="Start"/> and <see cref="End"/> take in <see cref="UiAutomationScrollModes.MouseWheel"/>.
    ///     Default is 100.
    /// </summary>
    public int MaxIncrementsPerStep { get; set; } = 100;

    /// <summary>
    ///     Where the mouse wheel input goes in <see cref="UiAutomationScrollModes.MouseWheel"/>, in screen coordinates.
    ///     Null (the default) uses the middle of <see cref="ViewportBounds"/>. The cursor moves there, see <see cref="RestoreCursorAfterWheel"/>.
    /// </summary>
    public NativePoint? WheelLocation { get; set; }

    /// <summary>
    ///     In <see cref="UiAutomationScrollModes.MouseWheel"/> the cursor moves to the wheel location. When this is true the cursor is moved
    ///     back to where it was after every wheel movement. Default is false.
    /// </summary>
    public bool RestoreCursorAfterWheel { get; set; }

    /// <summary>
    ///     The visible, scrolling area: the bounding rectangle of the element in screen coordinates (physical pixels for a per-monitor
    ///     DPI aware process), e.g. to crop the frames of a scrolling capture. Empty when the element is not available.
    /// </summary>
    public NativeRect ViewportBounds
    {
        get
        {
            var element = _element;
            if (element is null || !Check(element.get_CurrentBoundingRectangle(out var bounds)))
            {
                return NativeRect.Empty;
            }
            return bounds;
        }
    }

    /// <summary>
    ///     The current scroll position in percent (0 to 100) in the scroll direction, -1 when it can't scroll or the element is not available
    /// </summary>
    public double ScrollPercent => TryGetScrollPercent(out var percent) ? percent : UiaConstants.NoScroll;

    /// <summary>
    ///     The visible part of the content in the scroll direction, between 0 and 1 (VerticalViewSize / 100), 0 when unknown.
    ///     E.g. 0.25 means a page shows a quarter of the content.
    /// </summary>
    public double VisibleFraction => TryGetViewSize(out var viewSize) ? viewSize / 100 : 0;

    /// <summary>
    ///     True when the content is at the start (0 percent, within <see cref="PercentTolerance"/>), when it can't scroll, or when the element is not available
    /// </summary>
    public bool IsAtStart => !TryGetScrollPercent(out var percent) || percent < 0 || percent <= PercentTolerance;

    /// <summary>
    ///     True when the content is at the end (100 percent, within <see cref="PercentTolerance"/>), when it can't scroll, or when the element is not available
    /// </summary>
    public bool IsAtEnd => !TryGetScrollPercent(out var percent) || percent < 0 || percent >= 100 - PercentTolerance;

    /// <summary>
    ///     Scroll to the start (0 percent)
    /// </summary>
    /// <returns>bool if this worked</returns>
    public bool Start() => ScrollTo(0, false);

    /// <summary>
    ///     Scroll to the end (100 percent)
    /// </summary>
    /// <returns>bool if this worked</returns>
    public bool End() => ScrollTo(100, true);

    /// <summary>
    ///     Scroll forward (down or right) by <see cref="StepFraction"/> of a page
    /// </summary>
    /// <returns>bool if this worked, false at the end, when the element is gone or when the position didn't change</returns>
    public bool Next() => Step(true, StepFraction);

    /// <summary>
    ///     Scroll back (up or left) by <see cref="StepFraction"/> of a page
    /// </summary>
    /// <returns>bool if this worked, false at the start, when the element is gone or when the position didn't change</returns>
    public bool Previous() => Step(false, StepFraction);

    /// <summary>
    ///     Scroll back to <see cref="InitialScrollPercent"/>
    /// </summary>
    /// <returns>bool if this worked</returns>
    public bool Reset()
    {
        if (InitialScrollPercent < 0 || !TryGetScrollPercent(out var before))
        {
            return false;
        }
        if (!SetScrollPercent(InitialScrollPercent))
        {
            return false;
        }
        WaitForPercentChange(before, out _);
        return true;
    }

    /// <summary>
    ///     Find the element again at the original point (or in the original window), e.g. after <see cref="IsAvailable"/> became false
    ///     because the page navigated. <see cref="InitialScrollPercent"/> is taken from the new element.
    /// </summary>
    /// <returns>true when a scrollable element was found</returns>
    public bool Refresh()
    {
        ThrowIfDisposed();
        var found = _point.HasValue && _windowHandle == IntPtr.Zero
            ? FindAtPoint(_automation, _point.Value, Horizontal, null, IntPtr.Zero)
            : FindInWindow(_automation, _windowHandle, Horizontal);
        if (found is null)
        {
            return false;
        }
        ReleaseElement();
        _element = found._element;
        _scrollPattern = found._scrollPattern;
        IsAvailable = true;
        InitialScrollPercent = found.InitialScrollPercent;
        return true;
    }

    /// <inheritdoc />
    public void Dispose()
    {
        if (_isDisposed)
        {
            return;
        }
        _isDisposed = true;
        ReleaseElement();
        Release(_automation);
    }

    // ── Scrolling ────────────────────────────────────────────────────────────

    private bool ScrollTo(double percent, bool toEnd)
    {
        ThrowIfDisposed();
        if (ScrollMode == UiAutomationScrollModes.MouseWheel)
        {
            for (var step = 0; step < MaxIncrementsPerStep; step++)
            {
                if (toEnd ? IsAtEnd : IsAtStart)
                {
                    return IsAvailable;
                }
                if (!Step(toEnd, 1.0))
                {
                    return false;
                }
            }
            return false;
        }
        if (TryGetScrollPercent(out var before) && SetScrollPercent(percent))
        {
            WaitForPercentChange(before, out _);
            return true;
        }
        // SetScrollPercent not supported: large increments until the end
        for (var step = 0; step < MaxIncrementsPerStep; step++)
        {
            if (toEnd ? IsAtEnd : IsAtStart)
            {
                return IsAvailable;
            }
            if (!ScrollAmountAndWait(toEnd ? ScrollAmount.LargeIncrement : ScrollAmount.LargeDecrement))
            {
                return false;
            }
        }
        return false;
    }

    private bool Step(bool forward, double stepFraction)
    {
        ThrowIfDisposed();
        if (!TryGetScrollPercent(out var percent) || percent < 0)
        {
            return false;
        }
        if (forward ? percent >= 100 - PercentTolerance : percent <= PercentTolerance)
        {
            return false;
        }
        // The view size is the visible part of the content in percent, a page in scroll percent is the view size relative to the rest
        // of the content: scrolling from 0 to 100 percent moves the content by (100 - viewSize) percent of its size.
        var pagePercent = TryGetViewSize(out var viewSize) && viewSize > 0 && viewSize < 100
            ? viewSize / (100 - viewSize) * 100
            : 0;
        var target = pagePercent > 0
            ? Math.Max(0, Math.Min(100, percent + (forward ? 1 : -1) * pagePercent * stepFraction))
            : -1;

        if (ScrollMode == UiAutomationScrollModes.MouseWheel)
        {
            return WheelUntil(forward, percent, target);
        }

        if (target >= 0 && SetScrollPercent(target))
        {
            // Some frameworks (e.g. WPF) apply the position at their next layout pass: wait a little, so a capture right after
            // this call sees the new position and IsAtEnd is up to date
            WaitForPercentChange(percent, out _);
            return true;
        }
        if (!IsAvailable)
        {
            return false;
        }

        // SetScrollPercent is not supported (or the view size is unknown): increments until the target is reached
        var amount = target < 0 || stepFraction >= 1
            ? forward ? ScrollAmount.LargeIncrement : ScrollAmount.LargeDecrement
            : forward ? ScrollAmount.SmallIncrement : ScrollAmount.SmallDecrement;
        for (var increment = 0; increment < MaxIncrementsPerStep; increment++)
        {
            if (!ScrollAmountAndWait(amount))
            {
                return increment > 0;
            }
            if (target < 0 || amount == ScrollAmount.LargeIncrement || amount == ScrollAmount.LargeDecrement)
            {
                return true;
            }
            if (!TryGetScrollPercent(out var current) || (forward ? current >= target : current <= target))
            {
                return true;
            }
        }
        return true;
    }

    /// <summary>
    ///     Wheel one notch at a time until the target percentage is reached (or a page when it's unknown), for controls which report
    ///     the ScrollPattern but don't move when it's used
    /// </summary>
    private bool WheelUntil(bool forward, double startPercent, double target)
    {
        var location = WheelLocation ?? CenterOf(ViewportBounds);
        var moved = false;
        var before = startPercent;
        for (var notch = 0; notch < MaxIncrementsPerStep; notch++)
        {
            // Negative is down (towards the user) for the vertical wheel, positive is right for the horizontal wheel
            var delta = Horizontal == forward ? WindowScroller.WheelDeltaPerNotch : -WindowScroller.WheelDeltaPerNotch;
            if (!MouseInputGenerator.MoveMouseWheelAt(delta, location, RestoreCursorAfterWheel, Horizontal))
            {
                return moved;
            }
            if (!WaitForPercentChange(before, out var after))
            {
                Log.Verbose().WriteLine("Scroll position didn't change after a mouse wheel notch, stopping.");
                return moved;
            }
            moved = true;
            if (target < 0 || (forward ? after >= target : after <= target) || (forward ? after >= 100 - PercentTolerance : after <= PercentTolerance))
            {
                return true;
            }
            before = after;
        }
        return moved;
    }

    private bool ScrollAmountAndWait(ScrollAmount amount)
    {
        if (!TryGetScrollPercent(out var before))
        {
            return false;
        }
        var pattern = _scrollPattern;
        var hResult = Horizontal ? pattern.Scroll(amount, ScrollAmount.NoAmount) : pattern.Scroll(ScrollAmount.NoAmount, amount);
        if (!Check(hResult))
        {
            return false;
        }
        return WaitForPercentChange(before, out _);
    }

    private bool SetScrollPercent(double percent)
    {
        var pattern = _scrollPattern;
        if (pattern is null)
        {
            return false;
        }
        var hResult = Horizontal ? pattern.SetScrollPercent(percent, UiaConstants.NoScroll) : pattern.SetScrollPercent(UiaConstants.NoScroll, percent);
        return Check(hResult);
    }

    /// <summary>
    ///     The position of most applications changes synchronously, input and some applications are asynchronous, so poll for a short time
    /// </summary>
    private bool WaitForPercentChange(double before, out double after)
    {
        for (var check = 0; check < PositionChangeChecks; check++)
        {
            if (!TryGetScrollPercent(out after))
            {
                return false;
            }
            if (Math.Abs(after - before) > 0.0001)
            {
                return true;
            }
            Thread.Sleep(PositionChangeCheckInterval);
        }
        after = before;
        return false;
    }

    // ── UI Automation calls ──────────────────────────────────────────────────

    private bool TryGetScrollPercent(out double percent)
    {
        percent = UiaConstants.NoScroll;
        var pattern = _scrollPattern;
        if (pattern is null)
        {
            return false;
        }
        return Check(Horizontal ? pattern.get_CurrentHorizontalScrollPercent(out percent) : pattern.get_CurrentVerticalScrollPercent(out percent));
    }

    private bool TryGetViewSize(out double viewSize)
    {
        viewSize = 0;
        var pattern = _scrollPattern;
        if (pattern is null)
        {
            return false;
        }
        return Check(Horizontal ? pattern.get_CurrentHorizontalViewSize(out viewSize) : pattern.get_CurrentVerticalViewSize(out viewSize));
    }

    /// <summary>
    ///     Check an HRESULT, a gone element makes this scroller unavailable
    /// </summary>
    private bool Check(int hResult)
    {
        if (hResult == UiaConstants.S_OK)
        {
            return true;
        }
        if (hResult == UiaConstants.ElementNotAvailable)
        {
            IsAvailable = false;
        }
        if (Log.IsVerboseEnabled())
        {
            Log.Verbose().WriteLine("UI Automation call failed with 0x{0:X8}", hResult);
        }
        return false;
    }

    private static IUIAutomation CreateAutomation()
    {
        try
        {
            var type = Type.GetTypeFromCLSID(UiaConstants.CUIAutomationClsid, true);
            return (IUIAutomation)Activator.CreateInstance(type);
        }
        catch (Exception ex) when (ex is COMException or InvalidCastException or TypeLoadException)
        {
            Log.Warn().WriteLine(ex, "UI Automation is not available.");
            return null;
        }
    }

    private static UiAutomationScroller FindAtPoint(IUIAutomation automation, NativePoint screenPoint, bool horizontal, NativePoint? rememberPoint, IntPtr rememberWindow)
    {
        if (automation.ElementFromPoint(screenPoint, out var element) != UiaConstants.S_OK || element is null)
        {
            return null;
        }
        return FindScrollableAncestor(automation, element, horizontal, rememberPoint, rememberWindow);
    }

    private static UiAutomationScroller FindInWindow(IUIAutomation automation, IntPtr windowHandle, bool horizontal)
    {
        if (automation.ElementFromHandle(windowHandle, out var windowElement) != UiaConstants.S_OK || windowElement is null)
        {
            return null;
        }
        try
        {
            // 1. The window itself
            var pattern = GetScrollPattern(windowElement, horizontal);
            if (pattern is not null)
            {
                var self = new UiAutomationScroller(automation, windowElement, pattern, horizontal, null, windowHandle);
                windowElement = null;
                return self;
            }

            // 2. The element under the middle of the window, or one of its ancestors
            if (windowElement.get_CurrentBoundingRectangle(out var bounds) == UiaConstants.S_OK && !bounds.IsEmpty
                && NativeMethods.WindowFromPoint(CenterOf(bounds)) is var hwndAtCenter
                && (hwndAtCenter == windowHandle || NativeMethods.IsChild(windowHandle, hwndAtCenter)))
            {
                var atCenter = FindAtPoint(automation, CenterOf(bounds), horizontal, null, windowHandle);
                if (atCenter is not null)
                {
                    return atCenter;
                }
            }

            // 3. The first descendant which can scroll in the direction (a tree search, this can take a while for large windows)
            var propertyId = horizontal ? UiaConstants.HorizontallyScrollablePropertyId : UiaConstants.VerticallyScrollablePropertyId;
            if (automation.CreatePropertyCondition(propertyId, true, out var condition) != UiaConstants.S_OK || condition is null)
            {
                return null;
            }
            try
            {
                if (windowElement.FindFirst(UiaConstants.TreeScopeDescendants, condition, out var descendant) != UiaConstants.S_OK || descendant is null)
                {
                    return null;
                }
                var descendantPattern = GetScrollPattern(descendant, horizontal);
                if (descendantPattern is null)
                {
                    Release(descendant);
                    return null;
                }
                return new UiAutomationScroller(automation, descendant, descendantPattern, horizontal, null, windowHandle);
            }
            finally
            {
                Release(condition);
            }
        }
        finally
        {
            Release(windowElement);
        }
    }

    /// <summary>
    ///     Walk from the element up to the first element with a ScrollPattern which can scroll in the direction
    /// </summary>
    private static UiAutomationScroller FindScrollableAncestor(IUIAutomation automation, IUIAutomationElement element, bool horizontal, NativePoint? rememberPoint, IntPtr rememberWindow)
    {
        IUIAutomationTreeWalker walker = null;
        try
        {
            // The depth is limited, a misbehaving provider can't make this loop forever
            for (var depth = 0; depth < 64 && element is not null; depth++)
            {
                var pattern = GetScrollPattern(element, horizontal);
                if (pattern is not null)
                {
                    var scroller = new UiAutomationScroller(automation, element, pattern, horizontal, rememberPoint, rememberWindow);
                    element = null;
                    return scroller;
                }
                if (walker is null && (automation.get_RawViewWalker(out walker) != UiaConstants.S_OK || walker is null))
                {
                    return null;
                }
                // The parent of the root is null
                var hResult = walker.GetParentElement(element, out var parent);
                Release(element);
                element = hResult == UiaConstants.S_OK ? parent : null;
            }
            return null;
        }
        finally
        {
            Release(element);
            Release(walker);
        }
    }

    /// <summary>
    ///     The ScrollPattern of the element when it can scroll in the direction, else null
    /// </summary>
    private static IUIAutomationScrollPattern GetScrollPattern(IUIAutomationElement element, bool horizontal)
    {
        if (element.GetCurrentPattern(UiaConstants.ScrollPatternId, out var patternObject) != UiaConstants.S_OK || patternObject is null)
        {
            return null;
        }
        if (patternObject is not IUIAutomationScrollPattern pattern)
        {
            Release(patternObject);
            return null;
        }
        var hResult = horizontal ? pattern.get_CurrentHorizontallyScrollable(out var scrollable) : pattern.get_CurrentVerticallyScrollable(out scrollable);
        if (hResult == UiaConstants.S_OK && scrollable != 0)
        {
            return pattern;
        }
        Release(pattern);
        return null;
    }

    private static NativePoint CenterOf(NativeRect bounds) => new NativePoint(bounds.X + bounds.Width / 2, bounds.Y + bounds.Height / 2);

    private void ReleaseElement()
    {
        var pattern = _scrollPattern;
        var element = _element;
        _scrollPattern = null;
        _element = null;
        Release(pattern);
        Release(element);
    }

    private static void Release(object comObject)
    {
        if (comObject is not null && Marshal.IsComObject(comObject))
        {
            Marshal.ReleaseComObject(comObject);
        }
    }

    private void ThrowIfDisposed()
    {
        if (_isDisposed)
        {
            throw new ObjectDisposedException(nameof(UiAutomationScroller));
        }
    }

    /// <inheritdoc />
    public override string ToString() => $"UiAutomationScroller {{Horizontal: {Horizontal}; ScrollMode: {ScrollMode}; Available: {IsAvailable}}}";
}
