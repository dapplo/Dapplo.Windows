// Copyright (c) Dapplo and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Dapplo.Windows.Messages.Enums;
using Dapplo.Windows.Messages.Native;
using Dapplo.Windows.Messages.Structs;
using System;
using System.Collections.Concurrent;
using System.ComponentModel;
using System.Diagnostics;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Runtime.ExceptionServices;
using System.Runtime.InteropServices;
using System.Threading;

namespace Dapplo.Windows.Messages;

/// <summary>
/// Provides a shared, hidden, top-level window on a dedicated background STA thread, and an observable stream of the Windows messages it receives.
/// </summary>
/// <remarks>
/// The window is a hidden top-level WS_POPUP window with WS_EX_TOOLWINDOW, it is deliberately NOT a message-only (HWND_MESSAGE) window:
/// only top-level windows receive broadcasts like WM_QUERYENDSESSION, WM_POWERBROADCAST, WM_DISPLAYCHANGE and WM_SETTINGCHANGE.
/// <para>
/// The window is created lazily on first use (the first access to <see cref="Handle"/>, <see cref="Messages"/>, <see cref="Listen"/> or <see cref="Invoke"/>)
/// and then lives until the process exits. If it is destroyed externally, or with <see cref="Shutdown"/>, it is recreated on the next use.
/// </para>
/// <para>
/// When the process exits (<see cref="AppDomain.ProcessExit"/>) the window is destroyed on its own thread with <see cref="Shutdown"/>,
/// so Windows sends WM_RENDERALLFORMATS and WM_DESTROYCLIPBOARD and delayed rendered clipboard content survives the process.
/// From then on the window is not created again, see <see cref="IsProcessExiting"/>.
/// </para>
/// <para>
/// On .NET Framework, in an AppDomain which is not the default AppDomain (e.g. a test host), the same happens on <see cref="AppDomain.DomainUnload"/>:
/// the window is destroyed and its thread ends before the CLR aborts the threads of the unloaded AppDomain.
/// On .NET (Core) there is only one AppDomain, there nothing changes.
/// </para>
/// <para>
/// Registrations which need a window handle and must happen on the thread of that window (clipboard format listener, session notifications, raw input, device notifications, ...)
/// should use <see cref="Listen"/> with an onSetup and onTeardown action, or <see cref="Invoke"/>.
/// </para>
/// </remarks>
public static class SharedMessageWindow
{
    #region PInvokes
    [DllImport("user32", EntryPoint = "RegisterClassExW", SetLastError = true, CharSet = CharSet.Unicode)]
    private static extern ushort RegisterClassEx(ref WNDCLASSEX lpwc);

    [DllImport("user32", EntryPoint = "UnregisterClassW", SetLastError = true, CharSet = CharSet.Unicode)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool UnregisterClass(string lpClassName, nint hInstance);

    [DllImport("user32", EntryPoint = "CreateWindowExW", SetLastError = true, CharSet = CharSet.Unicode)]
    private static extern nint CreateWindowEx(uint dwExStyle, string lpClassName, string lpWindowName, uint dwStyle, int x, int y, int nWidth, int nHeight, nint hWndParent, nint hMenu, nint hInstance, nint lpParam);

    [DllImport("user32", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool DestroyWindow(nint hWnd);

    [DllImport("user32", EntryPoint = "GetMessageW", SetLastError = true)]
    private static extern int GetMessage(out Msg lpMsg, nint hWnd, uint wMsgFilterMin, uint wMsgFilterMax);

    [DllImport("user32")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool TranslateMessage(ref Msg lpMsg);

    [DllImport("user32", EntryPoint = "DispatchMessageW")]
    private static extern nint DispatchMessage(ref Msg lpMsg);

    [DllImport("user32", EntryPoint = "DefWindowProcW")]
    private static extern nint DefWindowProc(nint hWnd, WindowsMessages msg, nint wParam, nint lParam);

    [DllImport("user32", EntryPoint = "SendMessageW")]
    private static extern nint SendMessage(nint hWnd, uint msg, nint wParam, nint lParam);

    [DllImport("user32", EntryPoint = "SendMessageTimeoutW", SetLastError = true)]
    private static extern nint SendMessageTimeout(nint hWnd, uint msg, nint wParam, nint lParam, uint flags, uint timeout, out nint result);

    [DllImport("user32")]
    private static extern void PostQuitMessage(int nExitCode);

    [DllImport("user32", EntryPoint = "PostMessageW", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool PostMessage(nint hWnd, WindowsMessages msg, nint wParam, nint lParam);

    [DllImport("user32", EntryPoint = "RegisterWindowMessageW", SetLastError = true, CharSet = CharSet.Unicode)]
    private static extern uint RegisterWindowMessage(string lpString);

    [DllImport("kernel32", EntryPoint = "GetModuleHandleW", CharSet = CharSet.Unicode)]
    private static extern nint GetModuleHandle(string lpModuleName);
    #endregion

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    private struct WNDCLASSEX
    {
        public uint cbSize; public uint style; public WndProc lpfnWndProc; public int cbClsExtra;
        public int cbWndExtra; public nint hInstance; public nint hIcon; public nint hCursor;
        public nint hbrBackground; public string lpszMenuName; public string lpszClassName; public nint hIconSm;
    }

    /// <summary>
    /// The state of one window (and its thread)
    /// </summary>
    private sealed class WindowState
    {
        public readonly ManualResetEventSlim Created = new(false);
        public Thread Thread;
        public nint Hwnd;
        public volatile bool IsDead;
        public volatile bool IsDestroyed;
        public int ErrorCode;
        public string ErrorMessage;

        public void Fail(int errorCode, string errorMessage)
        {
            ErrorCode = errorCode;
            ErrorMessage = errorMessage;
            IsDead = true;
            Created.Set();
        }
    }

    /// <summary>
    /// An action which is passed to the window thread by <see cref="Invoke"/>
    /// </summary>
    private sealed class InvokeItem
    {
        private readonly Action<nint> _action;

        public InvokeItem(Action<nint> action)
        {
            _action = action;
        }

        public bool Executed { get; private set; }

        public Exception Error { get; private set; }

        public void Run(nint hwnd)
        {
            Executed = true;
            try
            {
                _action(hwnd);
            }
            catch (Exception ex)
            {
                Error = ex;
            }
        }
    }

    private const int CreationTimeoutMilliseconds = 10000;
    // SendMessageTimeout flag: wait for the result, but not longer than the timeout
    private const uint SmtoNormal = 0x0000;
    private static readonly TimeSpan DefaultShutdownTimeout = TimeSpan.FromSeconds(5);
    private const string WindowName = "Dapplo.SharedMessageWindow";
    private static readonly object Lock = new();
    // Keep the delegate in a static field, so the function pointer handed to Windows is never garbage collected
    private static readonly WndProc WndProcDelegate = WindowProcedure;
    private static readonly uint InvokeMessageId = RegisterWindowMessage("Dapplo.Windows.Messages.SharedMessageWindow.Invoke");
    private static readonly ConcurrentDictionary<int, InvokeItem> PendingInvokes = new();
    private static readonly ISubject<Exception> ErrorSubject = Subject.Synchronize(new Subject<Exception>());
    private static readonly IObservable<WindowMessage> MessagesObservable = Observable.Create<WindowMessage>(observer => SubscribeToMessages(observer));
    // Copy-on-write array of the current observers, the window procedure dispatches to a snapshot
    private static IObserver<WindowMessage>[] _observers = Array.Empty<IObserver<WindowMessage>>();
    private static WindowState _current;
    private static int _invokeCounter;
    private static int _processExitRegistered;
    private static volatile bool _isProcessExiting;
    private static long _processExitShutdownTimeoutTicks = TimeSpan.FromMilliseconds(1500).Ticks;

    [ThreadStatic]
    private static WindowState _threadState;

    /// <summary>
    /// The handle of the shared window. Creates the window if it doesn't exist yet and blocks until it does, this never returns 0.
    /// </summary>
    /// <exception cref="Win32Exception">When registering the window class or creating the window failed</exception>
    /// <exception cref="TimeoutException">When the window was not created in time</exception>
    /// <exception cref="ObjectDisposedException">When the process is exiting and the window was already shut down, see <see cref="IsProcessExiting"/></exception>
    public static nint Handle => EnsureWindow().Hwnd;

    /// <summary>
    /// True when the current thread is the thread of the shared window.
    /// </summary>
    public static bool IsWindowThread => _threadState != null;

    /// <summary>
    /// True when the process is exiting: <see cref="AppDomain.ProcessExit"/> was raised, and the window is (being) shut down.
    /// From then on the window is not created again: using the SharedMessageWindow when it doesn't exist anymore throws an <see cref="ObjectDisposedException"/>.
    /// </summary>
    /// <remarks>
    /// On .NET Framework this is also true when the (non-default) AppDomain of the SharedMessageWindow is being unloaded (<see cref="AppDomain.DomainUnload"/>):
    /// for the code in that AppDomain it's the end of the process.
    /// </remarks>
    public static bool IsProcessExiting => _isProcessExiting;

    /// <summary>
    /// The maximum time the automatic <see cref="Shutdown"/> on <see cref="AppDomain.ProcessExit"/> waits, this includes the time the delayed clipboard renderers need (WM_RENDERALLFORMATS).
    /// Default is 1.5 seconds: on .NET Framework all ProcessExit handlers together only get about 2 seconds.
    /// The same timeout is used for the automatic shutdown on <see cref="AppDomain.DomainUnload"/> of a non-default AppDomain (.NET Framework).
    /// </summary>
    /// <exception cref="ArgumentOutOfRangeException">When the value is negative (<see cref="Timeout.InfiniteTimeSpan"/> is allowed)</exception>
    public static TimeSpan ProcessExitShutdownTimeout
    {
        get => TimeSpan.FromTicks(Interlocked.Read(ref _processExitShutdownTimeoutTicks));
        set
        {
            ValidateTimeout(value, nameof(value));
            Interlocked.Exchange(ref _processExitShutdownTimeoutTicks, value.Ticks);
        }
    }

    /// <summary>
    /// A hot observable sequence of all window messages received by the shared window.
    /// </summary>
    /// <remarks>
    /// OnNext is called synchronously inside the window procedure, on the window thread.
    /// <see cref="WindowMessage.Handled"/> and <see cref="WindowMessage.Result"/> are only honoured when set synchronously in OnNext,
    /// if any subscriber sets Handled the window procedure returns Result, otherwise the default window procedure is called.
    /// Subscribing makes sure the window exists, it never creates or destroys windows otherwise. The sequence never completes.
    /// An exception thrown by a subscriber does not affect other subscribers or the message pump, it is published on <see cref="SubscriberErrors"/>
    /// (and, as usual in Rx, the throwing subscription is detached).
    /// </remarks>
    public static IObservable<WindowMessage> Messages => MessagesObservable;

    /// <summary>
    /// Exceptions thrown by subscribers of <see cref="Messages"/> (or by onTeardown actions of <see cref="Listen"/>) are published here,
    /// they are also written to <see cref="Trace"/>.
    /// </summary>
    public static IObservable<Exception> SubscriberErrors => ErrorSubject.AsObservable();

    /// <summary>
    /// Runs the action synchronously on the window thread, passing the handle of the window.
    /// When called on the window thread the action is called directly, otherwise it is marshalled with SendMessage.
    /// Exceptions thrown by the action are rethrown to the caller.
    /// </summary>
    /// <remarks>Use this for registrations which must happen on the thread of the window.
    /// Don't call this from a thread which the window thread is (synchronously) waiting for, this would deadlock.</remarks>
    /// <param name="action">Action which gets the handle of the window</param>
    public static void Invoke(Action<nint> action)
    {
        if (action == null)
        {
            throw new ArgumentNullException(nameof(action));
        }
        if (InvokeMessageId == 0)
        {
            throw new InvalidOperationException("RegisterWindowMessage failed, cannot invoke on the shared message window.");
        }

        // Try twice: the window could be destroyed (externally) between looking it up and sending the message
        for (var attempt = 0; attempt < 2; attempt++)
        {
            var state = EnsureWindow();
            if (ReferenceEquals(_threadState, state))
            {
                action(state.Hwnd);
                return;
            }

            var invokeItem = new InvokeItem(action);
            SendInvoke(state, invokeItem, Timeout.InfiniteTimeSpan);

            if (!invokeItem.Executed)
            {
                continue;
            }
            if (invokeItem.Error != null)
            {
                ExceptionDispatchInfo.Capture(invokeItem.Error).Throw();
            }
            return;
        }
        throw new InvalidOperationException("The shared message window was destroyed before the action could be invoked.");
    }

    /// <summary>
    /// Destroys the shared window on its own thread, and waits until its message loop ended.
    /// </summary>
    /// <remarks>
    /// Destroying the window makes Windows send WM_RENDERALLFORMATS (when the window owns the clipboard) and WM_DESTROY / WM_NCDESTROY:
    /// the delayed clipboard renderers of Dapplo.Windows.Clipboard run synchronously on the window thread, before this returns.
    /// This is called automatically when the process exits (<see cref="AppDomain.ProcessExit"/>, with <see cref="ProcessExitShutdownTimeout"/>)
    /// and, on .NET Framework, when a non-default AppDomain is unloaded (<see cref="AppDomain.DomainUnload"/>),
    /// call it yourself when you want to control the moment, e.g. at the end of your Main.
    /// <para>
    /// After an explicit Shutdown the next use of the SharedMessageWindow creates a new window, but registrations which were made
    /// with <see cref="Listen"/> or <see cref="Invoke"/> (clipboard listener, hotkeys, session notifications, ...) belonged to the old window and are gone:
    /// their onTeardown actions are not called anymore. Shutdown is meant for the end of the application.
    /// During process exit the window is not created again (<see cref="IsProcessExiting"/>).
    /// </para>
    /// <para>
    /// Called on the window thread itself, the window is destroyed directly and the message loop ends when control returns to it.
    /// </para>
    /// </remarks>
    /// <param name="timeout">The maximum time to wait for the window thread, default 5 seconds. <see cref="Timeout.InfiniteTimeSpan"/> waits forever.</param>
    /// <returns>true when the window doesn't exist anymore and its thread ended (or when there was no window), false when the timeout elapsed</returns>
    /// <exception cref="ArgumentOutOfRangeException">When the timeout is negative (<see cref="Timeout.InfiniteTimeSpan"/> is allowed)</exception>
    public static bool Shutdown(TimeSpan? timeout = null)
    {
        var waitTime = timeout ?? DefaultShutdownTimeout;
        ValidateTimeout(waitTime, nameof(timeout));

        WindowState state;
        lock (Lock)
        {
            state = _current;
        }
        if (state == null)
        {
            return true;
        }

        if (ReferenceEquals(_threadState, state))
        {
            // On the window thread: WM_RENDERALLFORMATS etc. are processed synchronously inside DestroyWindow,
            // the message loop ends as soon as control returns to it (WM_QUIT is posted on WM_NCDESTROY).
            DestroyOwnWindow(state);
            return true;
        }

        var stopwatch = Stopwatch.StartNew();
        if (!state.Created.Wait(waitTime))
        {
            return false;
        }

        if (!state.IsDead)
        {
            var invokeItem = new InvokeItem(_ => DestroyOwnWindow(state));
            SendInvoke(state, invokeItem, Remaining(waitTime, stopwatch));
            if (invokeItem.Error != null)
            {
                Trace.TraceError("Dapplo.Windows.Messages.SharedMessageWindow: destroying the window failed: {0}", invokeItem.Error);
            }
        }

        var thread = state.Thread;
        return thread == null || thread.Join(Remaining(waitTime, stopwatch));
    }

    /// <summary>
    /// Called when the process exits: destroy the window, so WM_RENDERALLFORMATS is sent, and don't create a new one anymore.
    /// ProcessExit runs on another thread than the window thread (unless Environment.Exit was called on the window thread),
    /// the window thread is a background thread and is still running at that moment.
    /// </summary>
    private static void OnProcessExit(object sender, EventArgs e)
    {
        FinalShutdown("the process exits", false);
    }

    /// <summary>
    /// Called when a non-default AppDomain is unloaded (.NET Framework only, e.g. test hosts): ProcessExit is not raised for it,
    /// after DomainUnload the CLR aborts all threads of the AppDomain. The window thread must have ended before that:
    /// an abort while it's in GetMessage is raised in the window procedure, inside a user32 callback, and crashes the process.
    /// DomainUnload runs on the thread which unloads the AppDomain, the window thread is still running at that moment.
    /// </summary>
    private static void OnDomainUnload(object sender, EventArgs e)
    {
        FinalShutdown("the AppDomain is unloaded", true);
    }

    /// <summary>
    /// The shutdown for ProcessExit and DomainUnload: destroy the window with <see cref="ProcessExitShutdownTimeout"/>, and don't create a new one anymore.
    /// </summary>
    /// <param name="reason">string for the log</param>
    /// <param name="closeWhenTimedOut">true to post WM_CLOSE when the shutdown timed out, so the window thread ends as soon as it's back in its message loop</param>
    private static void FinalShutdown(string reason, bool closeWhenTimedOut)
    {
        lock (Lock)
        {
            // Under the lock: EnsureWindow either sees the flag, or created the window which Shutdown destroys
            _isProcessExiting = true;
        }
        try
        {
            var timeout = ProcessExitShutdownTimeout;
            if (Shutdown(timeout))
            {
                return;
            }
            Trace.TraceWarning("Dapplo.Windows.Messages.SharedMessageWindow: the window was not destroyed within {0} while {1}.", timeout, reason);
            if (!closeWhenTimedOut)
            {
                return;
            }
            WindowState state;
            lock (Lock)
            {
                state = _current;
            }
            // The window thread is still busy (e.g. a delayed renderer), the destroy request timed out and is dropped.
            // WM_CLOSE (DefWindowProc calls DestroyWindow) makes the window thread destroy the window and end its loop when it gets back to it.
            if (state != null && !state.IsDead && state.Hwnd != 0)
            {
                PostMessage(state.Hwnd, WindowsMessages.WM_CLOSE, 0, 0);
            }
        }
        catch (Exception ex)
        {
            Trace.TraceError("Dapplo.Windows.Messages.SharedMessageWindow: the shutdown while {0} failed: {1}", reason, ex);
        }
    }

    /// <summary>
    /// Destroy the window, this must be called on the window thread
    /// </summary>
    private static void DestroyOwnWindow(WindowState state)
    {
        if (state.IsDestroyed || state.Hwnd == 0)
        {
            return;
        }
        if (!DestroyWindow(state.Hwnd))
        {
            Trace.TraceWarning("Dapplo.Windows.Messages.SharedMessageWindow: DestroyWindow failed with error {0}", Marshal.GetLastWin32Error());
        }
    }

    /// <summary>
    /// Send the invoke item to the window of the state, and wait (at most the timeout) until it was processed.
    /// When the item was not executed (window destroyed, timeout) <see cref="InvokeItem.Executed"/> is false, the item is never executed afterwards.
    /// </summary>
    private static void SendInvoke(WindowState state, InvokeItem invokeItem, TimeSpan timeout)
    {
        var id = Interlocked.Increment(ref _invokeCounter);
        PendingInvokes[id] = invokeItem;
        try
        {
            if (timeout == Timeout.InfiniteTimeSpan)
            {
                SendMessage(state.Hwnd, InvokeMessageId, id, 0);
            }
            else
            {
                var milliseconds = (uint)Math.Min(Math.Ceiling(timeout.TotalMilliseconds), uint.MaxValue - 1);
                SendMessageTimeout(state.Hwnd, InvokeMessageId, id, 0, SmtoNormal, milliseconds, out _);
            }
        }
        finally
        {
            PendingInvokes.TryRemove(id, out _);
        }
    }

    /// <summary>
    /// Run the action on the thread of the window of the state, if that window still exists. Never creates a window.
    /// </summary>
    /// <returns>true if the action was executed, exceptions of the action are rethrown</returns>
    private static bool InvokeOnExistingWindow(WindowState state, Action<nint> action)
    {
        if (state == null || state.IsDead)
        {
            return false;
        }
        if (ReferenceEquals(_threadState, state))
        {
            action(state.Hwnd);
            return true;
        }
        var invokeItem = new InvokeItem(action);
        SendInvoke(state, invokeItem, Timeout.InfiniteTimeSpan);
        if (invokeItem.Error != null)
        {
            ExceptionDispatchInfo.Capture(invokeItem.Error).Throw();
        }
        return invokeItem.Executed;
    }

    /// <summary>
    /// Validate a timeout: not negative, except infinite
    /// </summary>
    private static void ValidateTimeout(TimeSpan timeout, string parameterName)
    {
        if (timeout < TimeSpan.Zero && timeout != Timeout.InfiniteTimeSpan)
        {
            throw new ArgumentOutOfRangeException(parameterName, timeout, "The timeout must not be negative, use Timeout.InfiniteTimeSpan to wait forever.");
        }
    }

    /// <summary>
    /// The rest of the timeout, which is left after the elapsed time of the stopwatch
    /// </summary>
    private static TimeSpan Remaining(TimeSpan timeout, Stopwatch stopwatch)
    {
        if (timeout == Timeout.InfiniteTimeSpan)
        {
            return timeout;
        }
        var remaining = timeout - stopwatch.Elapsed;
        return remaining < TimeSpan.Zero ? TimeSpan.Zero : remaining;
    }

    /// <summary>
    /// Subscribes to the messages of the shared window, with optional registration actions which run on the window thread.
    /// </summary>
    /// <param name="onSetup">Invoked on the window thread with the HWND when subscribing. If it throws, the exception is passed to the subscriber and onTeardown is not called.</param>
    /// <param name="onTeardown">Invoked on the window thread with the same HWND when the subscription is disposed, at most once per subscription.
    /// It is not called when the window was destroyed in the meantime (e.g. by <see cref="Shutdown"/>), as the registrations were removed with the window.
    /// Exceptions are published on <see cref="SubscriberErrors"/>.</param>
    /// <returns>IObservable of WindowMessage, see <see cref="Messages"/> for the semantics</returns>
    public static IObservable<WindowMessage> Listen(Action<nint> onSetup = null, Action<nint> onTeardown = null)
    {
        if (onSetup == null && onTeardown == null)
        {
            return Messages;
        }

        return Observable.Create<WindowMessage>(observer =>
        {
            // Subscribe before the setup, so messages which are caused by the setup are not missed
            var messageSubscription = Messages.Subscribe(observer);
            nint setupHwnd = 0;
            WindowState setupState = null;
            try
            {
                Invoke(hwnd =>
                {
                    setupHwnd = hwnd;
                    setupState = _threadState;
                    onSetup?.Invoke(hwnd);
                });
            }
            catch
            {
                messageSubscription.Dispose();
                throw;
            }

            // Disposable.Create only calls the action once
            return Disposable.Create(() =>
            {
                messageSubscription.Dispose();
                if (onTeardown == null)
                {
                    return;
                }
                try
                {
                    // Only on the window which was set up: never create a new window for a teardown, the registrations died with the old window
                    InvokeOnExistingWindow(setupState, _ => onTeardown(setupHwnd));
                }
                catch (Exception ex)
                {
                    ReportError(ex);
                }
            });
        });
    }

    /// <summary>
    /// Add an observer to the snapshot array
    /// </summary>
    private static IDisposable SubscribeToMessages(IObserver<WindowMessage> observer)
    {
        EnsureWindow();
        lock (Lock)
        {
            var current = _observers;
            var updated = new IObserver<WindowMessage>[current.Length + 1];
            Array.Copy(current, updated, current.Length);
            updated[current.Length] = observer;
            Volatile.Write(ref _observers, updated);
        }
        return Disposable.Create(() => RemoveObserver(observer));
    }

    /// <summary>
    /// Remove an observer from the snapshot array
    /// </summary>
    private static void RemoveObserver(IObserver<WindowMessage> observer)
    {
        lock (Lock)
        {
            var current = _observers;
            var index = Array.IndexOf(current, observer);
            if (index < 0)
            {
                return;
            }
            var updated = new IObserver<WindowMessage>[current.Length - 1];
            Array.Copy(current, 0, updated, 0, index);
            Array.Copy(current, index + 1, updated, index, current.Length - index - 1);
            Volatile.Write(ref _observers, updated);
        }
    }

    /// <summary>
    /// Make sure the window exists, create it if needed and wait until it is created.
    /// </summary>
    private static WindowState EnsureWindow()
    {
        var threadState = _threadState;
        if (threadState != null && !threadState.IsDead)
        {
            // Called on the window thread (from a subscriber, or while the window is being created): never wait for ourselves
            return threadState;
        }

        WindowState state;
        lock (Lock)
        {
            state = _current;
            if (state == null || state.IsDead)
            {
                if (_isProcessExiting)
                {
                    throw new ObjectDisposedException(nameof(SharedMessageWindow), "The process is exiting (or the AppDomain is unloaded), the shared message window was shut down and is not created again.");
                }
                if (Interlocked.Exchange(ref _processExitRegistered, 1) == 0)
                {
                    AppDomain.CurrentDomain.ProcessExit += OnProcessExit;
                    // A non-default AppDomain (.NET Framework only, e.g. xunit / vstest) is unloaded without ProcessExit, see OnDomainUnload.
                    // On .NET (Core) there is only the default AppDomain, DomainUnload is never raised there.
                    if (!AppDomain.CurrentDomain.IsDefaultAppDomain())
                    {
                        AppDomain.CurrentDomain.DomainUnload += OnDomainUnload;
                    }
                }
                state = new WindowState();
                var thread = new Thread(WindowThread)
                {
                    IsBackground = true,
                    Name = WindowName
                };
                thread.SetApartmentState(ApartmentState.STA);
                state.Thread = thread;
                _current = state;
                thread.Start(state);
            }
        }

        if (!state.Created.Wait(CreationTimeoutMilliseconds))
        {
            throw new TimeoutException($"The shared message window was not created within {CreationTimeoutMilliseconds} ms.");
        }
        if (state.ErrorMessage != null)
        {
            throw new Win32Exception(state.ErrorCode, state.ErrorMessage);
        }
        return state;
    }

    /// <summary>
    /// The thread which creates the window and runs its message loop
    /// </summary>
    private static void WindowThread(object parameter)
    {
        var state = (WindowState)parameter;
        _threadState = state;
        var className = WindowName + "." + Guid.NewGuid().ToString("N");
        nint hInstance = 0;
        var classRegistered = false;
        try
        {
            hInstance = GetModuleHandle(null);
            var wndClass = new WNDCLASSEX
            {
                cbSize = (uint)Marshal.SizeOf<WNDCLASSEX>(),
                lpfnWndProc = WndProcDelegate,
                hInstance = hInstance,
                lpszClassName = className
            };

            if (RegisterClassEx(ref wndClass) == 0)
            {
                var error = Marshal.GetLastWin32Error();
                state.Fail(error, $"RegisterClassEx for the shared message window failed: {new Win32Exception(error).Message}");
                return;
            }
            classRegistered = true;

            // A hidden top-level popup window (NOT HWND_MESSAGE), so broadcasts like WM_QUERYENDSESSION, WM_POWERBROADCAST and WM_SETTINGCHANGE arrive.
            // WS_EX_TOOLWINDOW keeps it out of the taskbar and ALT+TAB.
            const uint WS_EX_TOOLWINDOW = 0x00000080;
            const uint WS_POPUP = 0x80000000;

            var hwnd = CreateWindowEx(
                WS_EX_TOOLWINDOW,
                className,
                WindowName,     // A title helps with debugging
                WS_POPUP,       // POPUP creates a window without borders or controls, it is never shown
                0, 0, 0, 0,     // Position/Size
                0,              // No parent: top-level
                0,              // No Menu
                hInstance,
                0);

            if (hwnd == 0)
            {
                var error = Marshal.GetLastWin32Error();
                state.Fail(error, $"CreateWindowEx for the shared message window failed: {new Win32Exception(error).Message}");
                return;
            }
            state.Hwnd = hwnd;
            state.Created.Set();

            while (true)
            {
                var result = GetMessage(out var msg, 0, 0, 0);
                if (result == 0)
                {
                    // WM_QUIT, posted when the window got WM_NCDESTROY
                    break;
                }
                if (result == -1)
                {
                    Trace.TraceError("Dapplo.Windows.Messages.SharedMessageWindow: GetMessage failed with error {0}", Marshal.GetLastWin32Error());
                    break;
                }
                TranslateMessage(ref msg);
                DispatchMessage(ref msg);
            }
        }
#if NETFRAMEWORK
        catch (ThreadAbortException)
        {
            // The AppDomain is unloaded while the window was not shut down in time, the finally block destroys the window
            Trace.TraceWarning("Dapplo.Windows.Messages.SharedMessageWindow: the window thread was aborted.");
            if (!state.Created.IsSet)
            {
                state.Fail(0, "The shared message window thread was aborted.");
            }
        }
#endif
        catch (Exception ex)
        {
            Trace.TraceError("Dapplo.Windows.Messages.SharedMessageWindow: the window thread failed: {0}", ex);
            if (!state.Created.IsSet)
            {
                state.Fail(0, $"Creating the shared message window failed: {ex.Message}");
            }
        }
        finally
        {
            state.IsDead = true;
            if (state.Hwnd != 0 && !state.IsDestroyed)
            {
                DestroyWindow(state.Hwnd);
            }
            if (classRegistered)
            {
                UnregisterClass(className, hInstance);
            }
            _threadState = null;
            if (!state.Created.IsSet)
            {
                state.Fail(0, "The shared message window thread ended before the window was created.");
            }
        }
    }

    /// <summary>
    /// The window procedure of the shared window, this must never throw: it's called by user32, an exception can't pass that callback.
    /// </summary>
    private static nint WindowProcedure(nint hWnd, WindowsMessages msg, nint wParam, nint lParam)
    {
#if NETFRAMEWORK
        // A ThreadAbortException (AppDomain unload while the window thread is busy, e.g. the shutdown timed out) can't be swallowed:
        // it's raised again at the end of every catch, and Thread.ResetAbort doesn't cancel the abort of an AppDomain unload.
        // The CLR delays a (non-rude) thread abort while the thread runs a finally block, so the whole window procedure runs in one:
        // the abort is raised when the window thread is back in managed code of its message loop, which ends the thread normally.
        nint result = 0;
        try
        {
        }
        finally
        {
            result = WindowProcedureCore(hWnd, msg, wParam, lParam);
            if ((Thread.CurrentThread.ThreadState & System.Threading.ThreadState.AbortRequested) != 0)
            {
                // The thread is being aborted: end the message loop, so the abort isn't postponed until the next message arrives
                PostQuitMessage(0);
            }
        }
        return result;
#else
        return WindowProcedureCore(hWnd, msg, wParam, lParam);
#endif
    }

    /// <summary>
    /// The implementation of the window procedure, this never throws.
    /// </summary>
    private static nint WindowProcedureCore(nint hWnd, WindowsMessages msg, nint wParam, nint lParam)
    {
        try
        {
            var state = _threadState;
            if (state != null && state.Hwnd == 0)
            {
                // Messages like WM_NCCREATE arrive while CreateWindowEx is still running, make the handle available for code on this thread
                state.Hwnd = hWnd;
            }

            if (InvokeMessageId != 0 && (uint)msg == InvokeMessageId)
            {
                if (PendingInvokes.TryRemove(unchecked((int)wParam), out var invokeItem))
                {
                    invokeItem.Run(hWnd);
                }
                return 0;
            }

            var windowMessage = new WindowMessage(hWnd, msg, wParam, lParam);
            var observers = Volatile.Read(ref _observers);
            foreach (var observer in observers)
            {
                try
                {
                    observer.OnNext(windowMessage);
                }
                catch (Exception ex)
                {
                    ReportError(ex);
                }
            }

            if (msg == WindowsMessages.WM_NCDESTROY && state != null)
            {
                // Last message for this window: end the message loop, the window class is unregistered when the loop ends.
                // The next use of the SharedMessageWindow creates a new window.
                state.IsDestroyed = true;
                state.IsDead = true;
                PostQuitMessage(0);
            }

            if (windowMessage.Handled)
            {
                return windowMessage.Result;
            }
        }
        catch (Exception ex)
        {
            ReportError(ex);
        }
        return DefWindowProc(hWnd, msg, wParam, lParam);
    }

    /// <summary>
    /// Publish an exception from a subscriber, without ever throwing
    /// </summary>
    private static void ReportError(Exception exception)
    {
        Trace.TraceError("Dapplo.Windows.Messages.SharedMessageWindow: a subscriber threw an exception: {0}", exception);
        try
        {
            ErrorSubject.OnNext(exception);
        }
        catch (Exception ex)
        {
            Trace.TraceError("Dapplo.Windows.Messages.SharedMessageWindow: a SubscriberErrors subscriber threw an exception: {0}", ex);
        }
    }
}
