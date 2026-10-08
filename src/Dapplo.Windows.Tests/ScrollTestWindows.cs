// Copyright (c) Dapplo and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Windows.Automation.Peers;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Media;
using System.Windows.Threading;
using Dapplo.Windows.Common.Structs;
using Xunit;
using WinForms = System.Windows.Forms;
using WpfWindows = System.Windows;

namespace Dapplo.Windows.Tests;

/// <summary>
///     Hosts a window on its own UI thread with a message loop, so the test thread can scroll it with window messages or UI Automation
///     (UI Automation must not be used on the thread which owns the target window).
/// </summary>
internal abstract class ScrollTestWindow : IDisposable
{
    private readonly string _name;
    private Thread _thread;
    private readonly TaskCompletionSource<bool> _ready = new(TaskCreationOptions.RunContinuationsAsynchronously);

    /// <summary>Starts the window right away</summary>
    protected ScrollTestWindow(string name) : this(name, start: true)
    {
    }

    /// <summary>With start false a derived class sets its fields first, then calls Start</summary>
    protected ScrollTestWindow(string name, bool start)
    {
        _name = name;
        if (start)
        {
            Start();
        }
    }

    /// <summary>Start the UI thread and wait until the window is shown</summary>
    protected void Start()
    {
        _thread = new Thread(() =>
        {
            try
            {
                Run();
            }
            catch (Exception ex)
            {
                _ready.TrySetException(ex);
            }
        })
        {
            IsBackground = true,
            Name = _name
        };
        _thread.SetApartmentState(ApartmentState.STA);
        _thread.Start();
        if (!_ready.Task.Wait(TimeSpan.FromSeconds(30)))
        {
            throw new TimeoutException($"The test window {_name} didn't start");
        }
    }

    /// <summary>The handle of the top-level window</summary>
    public IntPtr WindowHandle { get; protected set; }

    /// <summary>The handle of the scrolling control (WinForms), or the window (WPF)</summary>
    public IntPtr ScrollingHandle { get; protected set; }

    /// <summary>The scrolling area in screen coordinates</summary>
    public abstract NativeRect ScrollingBounds { get; }

    protected void SignalReady() => _ready.TrySetResult(true);

    /// <summary>Create and show the window, call SignalReady, then run the message loop until it closes</summary>
    protected abstract void Run();

    /// <summary>Run an action on the UI thread and wait for it</summary>
    public abstract T Invoke<T>(Func<T> func);

    public abstract void Close();

    public void Dispose()
    {
        try
        {
            Close();
        }
        catch (Exception)
        {
            // already closed
        }
        _thread?.Join(TimeSpan.FromSeconds(5));
    }

    /// <summary>
    ///     Skip the test when the window isn't on the screen at the point (no interactive desktop, or another window covers it)
    /// </summary>
    public void SkipWhenNotVisibleAt(NativePoint point)
    {
        var atPoint = NativeTestMethods.WindowFromPoint(point);
        Assert.SkipWhen(atPoint != WindowHandle && !NativeTestMethods.IsChild(WindowHandle, atPoint),
            "The test window is not visible at its location (no interactive desktop, or it is covered)");
    }

    public static NativePoint CenterOf(NativeRect rect) => new(rect.X + rect.Width / 2, rect.Y + rect.Height / 2);
}

/// <summary>
///     A WinForms window with a multi-line TextBox which has a Win32 vertical scroll bar (an EDIT control)
/// </summary>
internal sealed class TextBoxScrollTestWindow : ScrollTestWindow
{
    public const int LineCount = 200;
    private WinForms.Form _form;
    private WinForms.TextBox _textBox;

    public TextBoxScrollTestWindow() : base(nameof(TextBoxScrollTestWindow))
    {
    }

    protected override void Run()
    {
        _form = new WinForms.Form
        {
            Text = "Dapplo.Windows scroll test",
            StartPosition = WinForms.FormStartPosition.Manual,
            Location = new System.Drawing.Point(100, 100),
            Size = new System.Drawing.Size(400, 300),
            TopMost = true,
            ShowInTaskbar = false
        };
        _textBox = new WinForms.TextBox
        {
            Multiline = true,
            ScrollBars = WinForms.ScrollBars.Vertical,
            WordWrap = false,
            Dock = WinForms.DockStyle.Fill,
            Text = string.Join(Environment.NewLine, Enumerable.Range(1, LineCount).Select(i => $"Line {i}"))
        };
        _form.Controls.Add(_textBox);
        _form.Shown += (_, _) =>
        {
            WindowHandle = _form.Handle;
            ScrollingHandle = _textBox.Handle;
            SignalReady();
        };
        WinForms.Application.Run(_form);
    }

    public override NativeRect ScrollingBounds => Invoke(() =>
    {
        var rect = _textBox.RectangleToScreen(_textBox.ClientRectangle);
        return new NativeRect(rect.X, rect.Y, rect.Width, rect.Height);
    });

    public override T Invoke<T>(Func<T> func) => (T)_form.Invoke(func);

    public override void Close() => _form.Invoke(new Action(_form.Close));
}

/// <summary>
///     A WPF window with a ScrollViewer: no Win32 scroll bar, only UI Automation can scroll it
/// </summary>
internal sealed class WpfScrollTestWindow : ScrollTestWindow
{
    public const int ItemCount = 100;
    public const double ItemHeight = 20;
    public const double ItemWidth = 2000;
    private WpfWindows.Window _window;
    private ScrollViewer _scrollViewer;
    private Dispatcher _dispatcher;

    public WpfScrollTestWindow() : base(nameof(WpfScrollTestWindow))
    {
    }

    protected override void Run()
    {
        _dispatcher = Dispatcher.CurrentDispatcher;
        var panel = new StackPanel();
        for (var i = 0; i < ItemCount; i++)
        {
            panel.Children.Add(new TextBlock { Text = $"Item {i}", Height = ItemHeight, Width = ItemWidth });
        }
        _scrollViewer = new ScrollViewer
        {
            Content = panel,
            VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
            HorizontalScrollBarVisibility = ScrollBarVisibility.Auto
        };
        _window = new WpfWindows.Window
        {
            Title = "Dapplo.Windows UI Automation scroll test",
            Left = 150,
            Top = 150,
            Width = 400,
            Height = 300,
            Topmost = true,
            ShowInTaskbar = false,
            Content = _scrollViewer
        };
        // Ready after the layout pass, at ContextIdle priority (after layout and input, before idle), without depending on the render
        // thread: ContentRendered sometimes didn't come within the timeout on a busy CI runner (.NET Framework)
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

    /// <summary>The vertical offset of the ScrollViewer in device independent pixels</summary>
    public double VerticalOffset => Invoke(() => _scrollViewer.VerticalOffset);

    /// <summary>The horizontal offset of the ScrollViewer in device independent pixels</summary>
    public double HorizontalOffset => Invoke(() => _scrollViewer.HorizontalOffset);

    public override NativeRect ScrollingBounds => Invoke(() =>
    {
        var topLeft = _scrollViewer.PointToScreen(new WpfWindows.Point(0, 0));
        var bottomRight = _scrollViewer.PointToScreen(new WpfWindows.Point(_scrollViewer.ActualWidth, _scrollViewer.ActualHeight));
        return new NativeRect((int)topLeft.X, (int)topLeft.Y, (int)(bottomRight.X - topLeft.X), (int)(bottomRight.Y - topLeft.Y));
    });

    public override T Invoke<T>(Func<T> func) => _dispatcher.Invoke(func);

    public override void Close() => _dispatcher.Invoke(_window.Close);
}

/// <summary>
///     A WPF window with two ScrollViewers side by side in one HWND, like the panes of Visual Studio: the left one scrolls in both
///     directions, the right one only vertically
/// </summary>
internal sealed class TwoAreasScrollTestWindow : ScrollTestWindow
{
    private WpfWindows.Window _window;
    private ScrollViewer _left;
    private ScrollViewer _right;
    private Dispatcher _dispatcher;
    private readonly int _left0;
    private readonly int _top0;

    public TwoAreasScrollTestWindow() : this(120, 120)
    {
    }

    public TwoAreasScrollTestWindow(int left, int top) : base(nameof(TwoAreasScrollTestWindow), start: false)
    {
        _left0 = left;
        _top0 = top;
        Start();
    }

    private static ScrollViewer CreateScrollViewer(double itemWidth)
    {
        var panel = new StackPanel();
        for (var i = 0; i < 100; i++)
        {
            panel.Children.Add(new TextBlock { Text = $"Item {i}", Height = 20, Width = itemWidth });
        }
        return new ScrollViewer
        {
            Content = panel,
            VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
            HorizontalScrollBarVisibility = ScrollBarVisibility.Auto
        };
    }

    protected override void Run()
    {
        _dispatcher = Dispatcher.CurrentDispatcher;
        _left = CreateScrollViewer(2000);
        _right = CreateScrollViewer(50);
        var grid = new Grid();
        grid.ColumnDefinitions.Add(new ColumnDefinition());
        grid.ColumnDefinitions.Add(new ColumnDefinition());
        Grid.SetColumn(_left, 0);
        Grid.SetColumn(_right, 1);
        grid.Children.Add(_left);
        grid.Children.Add(_right);
        _window = new WpfWindows.Window
        {
            Title = "Dapplo.Windows scrollable areas test",
            Left = _left0,
            Top = _top0,
            Width = 500,
            Height = 300,
            Topmost = true,
            ShowInTaskbar = false,
            Content = grid
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

    private NativeRect BoundsOf(ScrollViewer scrollViewer) => Invoke(() =>
    {
        var topLeft = scrollViewer.PointToScreen(new WpfWindows.Point(0, 0));
        var bottomRight = scrollViewer.PointToScreen(new WpfWindows.Point(scrollViewer.ActualWidth, scrollViewer.ActualHeight));
        return new NativeRect((int)Math.Round(topLeft.X), (int)Math.Round(topLeft.Y), (int)Math.Round(bottomRight.X - topLeft.X), (int)Math.Round(bottomRight.Y - topLeft.Y));
    });

    /// <summary>The left ScrollViewer in screen coordinates</summary>
    public NativeRect LeftBounds => BoundsOf(_left);

    /// <summary>The right ScrollViewer in screen coordinates</summary>
    public NativeRect RightBounds => BoundsOf(_right);

    public override NativeRect ScrollingBounds => LeftBounds;

    public override T Invoke<T>(Func<T> func) => _dispatcher.Invoke(func);

    public override void Close() => _dispatcher.Invoke(_window.Close);
}

/// <summary>
///     An empty, topmost WinForms window, e.g. to cover another window like Greenshot's selection window does
/// </summary>
internal sealed class CoverTestWindow : ScrollTestWindow
{
    private WinForms.Form _form;
    private readonly System.Drawing.Rectangle _bounds;

    public CoverTestWindow(NativeRect bounds) : base(nameof(CoverTestWindow), start: false)
    {
        _bounds = new System.Drawing.Rectangle(bounds.X, bounds.Y, bounds.Width, bounds.Height);
        Start();
    }

    protected override void Run()
    {
        _form = new WinForms.Form
        {
            FormBorderStyle = WinForms.FormBorderStyle.None,
            StartPosition = WinForms.FormStartPosition.Manual,
            Bounds = _bounds,
            TopMost = true,
            ShowInTaskbar = false
        };
        _form.Shown += (_, _) =>
        {
            WindowHandle = _form.Handle;
            ScrollingHandle = WindowHandle;
            SignalReady();
        };
        WinForms.Application.Run(_form);
    }

    public override NativeRect ScrollingBounds => new(_bounds.X, _bounds.Y, _bounds.Width, _bounds.Height);

    public override T Invoke<T>(Func<T> func) => (T)_form.Invoke(func);

    public override void Close() => _form.Invoke(new Action(_form.Close));
}

/// <summary>
///     What the scroll bar of a <see cref="ScrollBarOnlyTestWindow"/> tells UI Automation about its position
/// </summary>
public enum ScrollBarExposure
{
    /// <summary>The RangeValue pattern (Value, Minimum, Maximum, LargeChange) and the thumb</summary>
    RangeValue,

    /// <summary>No RangeValue, only the thumb between the line buttons</summary>
    ThumbOnly,

    /// <summary>Neither: only the line and page buttons</summary>
    Nothing
}

/// <summary>
///     A WPF window with a control which scrolls by itself, like the Visual Studio editor: no ScrollPattern anywhere, a separate
///     ScrollBar next to the content moves it, and the mouse wheel over the control moves the ScrollBar
/// </summary>
internal sealed class ScrollBarOnlyTestWindow : ScrollTestWindow
{
    private readonly ScrollBarExposure _exposure;
    private readonly int _lineCount;
    private readonly bool _withHorizontalScrollBar;
    private readonly bool _followsValueChanges;
    private WpfWindows.Window _window;
    private ScrollBarOnlyControl _control;
    private Dispatcher _dispatcher;

    /// <param name="exposure">ScrollBarExposure, what the vertical scroll bar tells UI Automation</param>
    /// <param name="lineCount">int with the number of lines of 20 pixels</param>
    /// <param name="withHorizontalScrollBar">true for a horizontal scroll bar at the bottom of the content too</param>
    /// <param name="followsValueChanges">false: the content only follows the Scroll events of the scroll bar and the wheel, like the
    ///     Visual Studio editor, so setting the scroll bar's value via UI Automation moves only the scroll bar</param>
    public ScrollBarOnlyTestWindow(ScrollBarExposure exposure = ScrollBarExposure.RangeValue, int lineCount = ScrollBarOnlyControl.DefaultLineCount, bool withHorizontalScrollBar = false,
        bool followsValueChanges = true)
        : base(nameof(ScrollBarOnlyTestWindow), start: false)
    {
        _exposure = exposure;
        _lineCount = lineCount;
        _withHorizontalScrollBar = withHorizontalScrollBar;
        _followsValueChanges = followsValueChanges;
        Start();
    }

    protected override void Run()
    {
        _dispatcher = Dispatcher.CurrentDispatcher;
        _control = new ScrollBarOnlyControl(_exposure, _lineCount, _withHorizontalScrollBar, _followsValueChanges);
        _window = new WpfWindows.Window
        {
            Title = "Dapplo.Windows scroll bar only test",
            Left = 140,
            Top = 140,
            Width = 400,
            Height = 300,
            Topmost = true,
            ShowInTaskbar = false,
            Content = _control
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

    /// <summary>The control (content and scroll bars) in screen coordinates</summary>
    public NativeRect ControlBounds => Invoke(() => BoundsOf(_control));

    /// <summary>The content without the scroll bars in screen coordinates</summary>
    public NativeRect ContentBounds => Invoke(() => BoundsOf(_control.Viewport));

    public override NativeRect ScrollingBounds => ContentBounds;

    private static NativeRect BoundsOf(WpfWindows.FrameworkElement element)
    {
        var topLeft = element.PointToScreen(new WpfWindows.Point(0, 0));
        var bottomRight = element.PointToScreen(new WpfWindows.Point(element.ActualWidth, element.ActualHeight));
        return new NativeRect((int)Math.Round(topLeft.X), (int)Math.Round(topLeft.Y), (int)Math.Round(bottomRight.X - topLeft.X), (int)Math.Round(bottomRight.Y - topLeft.Y));
    }

    /// <summary>Scroll the content, in device independent pixels</summary>
    public void ScrollTo(double offset) => Invoke(() =>
    {
        _control.MoveContent(Math.Max(0, Math.Min(_control.ScrollBar.Maximum, offset)));
        return true;
    });

    /// <summary>How far the content is scrolled, in device independent pixels (not the scroll bar's value, which may differ)</summary>
    public double Offset => Invoke(() => _control.ContentOffset);

    /// <summary>The offset at the end</summary>
    public double MaxOffset => Invoke(() => _control.ScrollBar.Maximum);

    public override T Invoke<T>(Func<T> func) => _dispatcher.Invoke(func);

    public override void Close() => _dispatcher.Invoke(_window.Close);
}

/// <summary>
///     Content in a clipping canvas, moved by a ScrollBar beside it; the UserControl has no ScrollPattern, the ScrollBar is its child
///     in the UI Automation tree
/// </summary>
internal sealed class ScrollBarOnlyControl : UserControl
{
    public const int DefaultLineCount = 100;
    public const double LineHeight = 20;
    public const int LinesPerNotch = 3;
    private readonly TranslateTransform _transform = new();

    public ScrollBarOnlyControl(ScrollBarExposure exposure, int lineCount = DefaultLineCount, bool withHorizontalScrollBar = false, bool followsValueChanges = true)
    {
        var lines = new StackPanel { RenderTransform = _transform };
        for (var i = 0; i < lineCount; i++)
        {
            lines.Children.Add(new TextBlock { Text = $"Line {i}", Height = LineHeight });
        }
        var viewport = new Canvas { ClipToBounds = true, Background = Brushes.White };
        Viewport = viewport;
        viewport.Children.Add(lines);
        ScrollBar = new ExposingScrollBar(exposure) { Orientation = Orientation.Vertical, SmallChange = LineHeight };
        var grid = new Grid();
        grid.ColumnDefinitions.Add(new ColumnDefinition());
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = WpfWindows.GridLength.Auto });
        grid.RowDefinitions.Add(new RowDefinition());
        grid.RowDefinitions.Add(new RowDefinition { Height = WpfWindows.GridLength.Auto });
        Grid.SetColumn(viewport, 0);
        Grid.SetColumn(ScrollBar, 1);
        grid.Children.Add(viewport);
        grid.Children.Add(ScrollBar);
        if (withHorizontalScrollBar)
        {
            // Only there to be cut off the area, like the horizontal scroll bar of an editor
            var horizontalScrollBar = new ScrollBar { Orientation = Orientation.Horizontal, Maximum = 100, ViewportSize = 50, LargeChange = 50 };
            Grid.SetRow(horizontalScrollBar, 1);
            grid.Children.Add(horizontalScrollBar);
        }
        Content = grid;
        Background = Brushes.White;

        if (followsValueChanges)
        {
            ScrollBar.ValueChanged += (_, e) => _transform.Y = -e.NewValue;
        }
        else
        {
            // Like the Visual Studio editor: only what the user does with the scroll bar (buttons, thumb) moves the content
            ScrollBar.Scroll += (_, e) => _transform.Y = -e.NewValue;
        }
        viewport.SizeChanged += (_, _) =>
        {
            var height = viewport.ActualHeight;
            ScrollBar.Maximum = Math.Max(0, lineCount * LineHeight - height);
            ScrollBar.ViewportSize = height;
            ScrollBar.LargeChange = height;
        };
        // Like an editor: the wheel moves a few lines per notch
        // The wheel moves the content from where the content is, and the scroll bar follows it
        PreviewMouseWheel += (_, e) =>
        {
            var offset = ContentOffset - e.Delta / 120.0 * LinesPerNotch * LineHeight;
            MoveContent(Math.Max(ScrollBar.Minimum, Math.Min(ScrollBar.Maximum, offset)));
            e.Handled = true;
        };
    }

    public ScrollBar ScrollBar { get; }

    /// <summary>How far the content is scrolled</summary>
    public double ContentOffset => -_transform.Y;

    /// <summary>Scroll the content and put the scroll bar there</summary>
    public void MoveContent(double offset)
    {
        _transform.Y = -offset;
        ScrollBar.Value = offset;
    }

    /// <summary>The content area, without the scroll bars</summary>
    public WpfWindows.FrameworkElement Viewport { get; }
}

/// <summary>
///     A ScrollBar which exposes the RangeValue pattern and its thumb to UI Automation, or not
/// </summary>
internal sealed class ExposingScrollBar(ScrollBarExposure exposure) : ScrollBar
{
    protected override AutomationPeer OnCreateAutomationPeer() => new ExposingScrollBarAutomationPeer(this, exposure);

    private sealed class ExposingScrollBarAutomationPeer(ScrollBar owner, ScrollBarExposure exposure) : ScrollBarAutomationPeer(owner)
    {
        public override object GetPattern(PatternInterface patternInterface)
        {
            if (patternInterface == PatternInterface.RangeValue)
            {
                // RangeBaseAutomationPeer implements IRangeValueProvider itself
                return exposure == ScrollBarExposure.RangeValue ? this : null;
            }
            return base.GetPattern(patternInterface);
        }

        protected override List<AutomationPeer> GetChildrenCore()
        {
            var children = base.GetChildrenCore();
            if (exposure != ScrollBarExposure.Nothing || children is null)
            {
                return children;
            }
            return children.Where(child => child is not ThumbAutomationPeer).ToList();
        }
    }
}

internal static class NativeTestMethods
{
    [System.Runtime.InteropServices.DllImport("user32.dll")]
    internal static extern IntPtr WindowFromPoint(NativePoint point);

    [System.Runtime.InteropServices.DllImport("user32.dll")]
    [return: System.Runtime.InteropServices.MarshalAs(System.Runtime.InteropServices.UnmanagedType.Bool)]
    internal static extern bool IsChild(IntPtr parentWindowHandle, IntPtr windowHandle);
}
