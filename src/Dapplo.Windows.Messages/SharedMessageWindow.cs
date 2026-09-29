// Copyright (c) Dapplo and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Dapplo.Windows.Messages.Enumerations;
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
/// and then lives until the process exits. If it is destroyed externally, it is recreated on the next use.
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

    [DllImport("user32")]
    private static extern void PostQuitMessage(int nExitCode);

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

    [ThreadStatic]
    private static WindowState _threadState;

    /// <summary>
    /// The handle of the shared window. Creates the window if it doesn't exist yet and blocks until it does, this never returns 0.
    /// </summary>
    /// <exception cref="Win32Exception">When registering the window class or creating the window failed</exception>
    /// <exception cref="TimeoutException">When the window was not created in time</exception>
    public static nint Handle => EnsureWindow().Hwnd;

    /// <summary>
    /// True when the current thread is the thread of the shared window.
    /// </summary>
    public static bool IsWindowThread => _threadState != null;

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
            var id = Interlocked.Increment(ref _invokeCounter);
            PendingInvokes[id] = invokeItem;
            try
            {
                SendMessage(state.Hwnd, InvokeMessageId, id, 0);
            }
            finally
            {
                PendingInvokes.TryRemove(id, out _);
            }

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
    /// Subscribes to the messages of the shared window, with optional registration actions which run on the window thread.
    /// </summary>
    /// <param name="onSetup">Invoked on the window thread with the HWND when subscribing. If it throws, the exception is passed to the subscriber and onTeardown is not called.</param>
    /// <param name="onTeardown">Invoked on the window thread with the same HWND when the subscription is disposed, exactly once per subscription.
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
            try
            {
                Invoke(hwnd =>
                {
                    setupHwnd = hwnd;
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
                    Invoke(_ => onTeardown(setupHwnd));
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
    /// The window procedure of the shared window, this must never throw.
    /// </summary>
    private static nint WindowProcedure(nint hWnd, WindowsMessages msg, nint wParam, nint lParam)
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
