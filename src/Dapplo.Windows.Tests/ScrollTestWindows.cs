// Copyright (c) Dapplo and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Controls;
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
    private readonly Thread _thread;
    private readonly TaskCompletionSource<bool> _ready = new(TaskCreationOptions.RunContinuationsAsynchronously);

    protected ScrollTestWindow(string name)
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
            Name = name
        };
        _thread.SetApartmentState(ApartmentState.STA);
        _thread.Start();
        if (!_ready.Task.Wait(TimeSpan.FromSeconds(10)))
        {
            throw new TimeoutException($"The test window {name} didn't start");
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
        _thread.Join(TimeSpan.FromSeconds(5));
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
        _window.ContentRendered += (_, _) =>
        {
            WindowHandle = new WpfWindows.Interop.WindowInteropHelper(_window).Handle;
            ScrollingHandle = WindowHandle;
            SignalReady();
        };
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

internal static class NativeTestMethods
{
    [System.Runtime.InteropServices.DllImport("user32.dll")]
    internal static extern IntPtr WindowFromPoint(NativePoint point);

    [System.Runtime.InteropServices.DllImport("user32.dll")]
    [return: System.Runtime.InteropServices.MarshalAs(System.Runtime.InteropServices.UnmanagedType.Bool)]
    internal static extern bool IsChild(IntPtr parentWindowHandle, IntPtr windowHandle);
}
