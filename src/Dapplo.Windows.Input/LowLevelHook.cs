// Copyright (c) Dapplo and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Reactive.Concurrency;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Runtime.InteropServices;
using System.Threading;
using Dapplo.Windows.Input.Enums;
using static Dapplo.Windows.Input.LowLevelHookNativeMethods;

namespace Dapplo.Windows.Input;

/// <summary>
/// Hosts a low-level hook (WH_KEYBOARD_LL or WH_MOUSE_LL) on a dedicated background thread with its own message loop,
/// and dispatches the events to the subscribers.
/// </summary>
/// <remarks>
/// <list type="bullet">
/// <item>The hook is installed when the first subscriber subscribes, and removed when the last subscriber unsubscribes.</item>
/// <item>The hook runs on its own thread with a message loop, so it works no matter which thread subscribes and a busy UI thread doesn't stall the input of the whole system.</item>
/// <item>OnNext is called synchronously inside the hook callback, on the hook thread. An exception thrown by a subscriber never leaves the hook callback,
/// it doesn't affect the other subscribers and is published on <see cref="SubscriberErrors"/> (and written to <see cref="Trace"/>).</item>
/// <item>If the hook cannot be installed, the subscriber gets OnError with a <see cref="Win32Exception"/>.</item>
/// </list>
/// </remarks>
/// <typeparam name="TEventArgs">Type of the event args</typeparam>
internal sealed class LowLevelHook<TEventArgs> where TEventArgs : class
{
    private const int InstallTimeoutMilliseconds = 5000;
    private const long SlowSubscriberWarningMilliseconds = 200;

    private readonly HookTypes _hookType;
    private readonly string _name;
    private readonly Func<IntPtr, IntPtr, TEventArgs> _createEventArgs;
    private readonly Func<TEventArgs, bool> _isHandled;
    private readonly object _lock = new();
    private readonly ISubject<Exception> _errorSubject = Subject.Synchronize(new Subject<Exception>());
    private readonly Lazy<EventLoopScheduler> _nonBlockingScheduler;
    // Copy-on-write array of the current observers, the hook callback dispatches to a snapshot
    private IObserver<TEventArgs>[] _observers = Array.Empty<IObserver<TEventArgs>>();
    private HookThread _hookThread;

    /// <summary>
    /// Create the low-level hook
    /// </summary>
    /// <param name="hookType">HookTypes, WH_KEYBOARD_LL or WH_MOUSE_LL</param>
    /// <param name="name">string used for the thread names and tracing</param>
    /// <param name="createEventArgs">Function to create the event args from wParam and lParam</param>
    /// <param name="isHandled">Function to check if the event args were marked as handled</param>
    public LowLevelHook(HookTypes hookType, string name, Func<IntPtr, IntPtr, TEventArgs> createEventArgs, Func<TEventArgs, bool> isHandled)
    {
        _hookType = hookType;
        _name = name;
        _createEventArgs = createEventArgs;
        _isHandled = isHandled;
        _nonBlockingScheduler = new Lazy<EventLoopScheduler>(() => new EventLoopScheduler(threadStart => new Thread(threadStart)
        {
            IsBackground = true,
            Name = name + ".NonBlocking"
        }));
        Events = Observable.Create<TEventArgs>(Subscribe);
        NonBlockingEvents = Observable.Create<TEventArgs>(observer =>
            Events
                .ObserveOn(_nonBlockingScheduler.Value)
                .Subscribe(eventArgs =>
                    {
                        try
                        {
                            observer.OnNext(eventArgs);
                        }
                        catch (Exception ex)
                        {
                            ReportError(ex);
                        }
                    },
                    observer.OnError));
    }

    /// <summary>
    /// The events, OnNext is called synchronously inside the hook callback on the hook thread
    /// </summary>
    public IObservable<TEventArgs> Events { get; }

    /// <summary>
    /// The events, delivered in order on a separate background thread. The hook callback only enqueues them, so slow subscribers never delay the hook.
    /// Setting Handled has no effect here.
    /// </summary>
    public IObservable<TEventArgs> NonBlockingEvents { get; }

    /// <summary>
    /// Exceptions thrown by subscribers
    /// </summary>
    public IObservable<Exception> SubscriberErrors => _errorSubject.AsObservable();

    /// <summary>
    /// Add an observer, installs the hook for the first observer
    /// </summary>
    private IDisposable Subscribe(IObserver<TEventArgs> observer)
    {
        Exception installError = null;
        lock (_lock)
        {
            if (_hookThread == null)
            {
                var hookThread = new HookThread(this);
                if (hookThread.Start(out installError))
                {
                    _hookThread = hookThread;
                }
            }

            if (installError == null)
            {
                var current = _observers;
                var updated = new IObserver<TEventArgs>[current.Length + 1];
                Array.Copy(current, updated, current.Length);
                updated[current.Length] = observer;
                Volatile.Write(ref _observers, updated);
            }
        }

        if (installError != null)
        {
            // Outside of the lock, the observer might re-subscribe (Retry)
            observer.OnError(installError);
            return Disposable.Empty;
        }
        return Disposable.Create(() => RemoveObserver(observer));
    }

    /// <summary>
    /// Remove an observer, removes the hook when it was the last one
    /// </summary>
    private void RemoveObserver(IObserver<TEventArgs> observer)
    {
        lock (_lock)
        {
            var current = _observers;
            var index = Array.IndexOf(current, observer);
            if (index < 0)
            {
                return;
            }
            var updated = new IObserver<TEventArgs>[current.Length - 1];
            Array.Copy(current, 0, updated, 0, index);
            Array.Copy(current, index + 1, updated, index, current.Length - index - 1);
            Volatile.Write(ref _observers, updated);
            if (updated.Length == 0 && _hookThread != null)
            {
                // Never wait for the hook thread here, this is often called from the hook thread itself (e.g. FirstAsync)
                _hookThread.Stop();
                _hookThread = null;
            }
        }
    }

    /// <summary>
    /// Called from the hook callback, dispatches the event to all observers. This never throws.
    /// </summary>
    /// <returns>true if the event was marked as handled</returns>
    private bool Dispatch(IntPtr wParam, IntPtr lParam)
    {
        var observers = Volatile.Read(ref _observers);
        if (observers.Length == 0)
        {
            return false;
        }

        var startTimestamp = Stopwatch.GetTimestamp();
        var isHandled = false;
        try
        {
            var eventArgs = _createEventArgs(wParam, lParam);
            foreach (var observer in observers)
            {
                try
                {
                    observer.OnNext(eventArgs);
                }
                catch (Exception ex)
                {
                    ReportError(ex);
                }
            }
            isHandled = _isHandled(eventArgs);
        }
        catch (Exception ex)
        {
            ReportError(ex);
        }

        var elapsedMilliseconds = (Stopwatch.GetTimestamp() - startTimestamp) * 1000 / Stopwatch.Frequency;
        if (elapsedMilliseconds > SlowSubscriberWarningMilliseconds)
        {
            Trace.TraceWarning("{0}: the subscribers took {1} ms to process an event, Windows silently removes a low-level hook which takes longer than the LowLevelHooksTimeout. Use the NonBlocking events or ObserveOn for slow work.", _name, elapsedMilliseconds);
        }
        return isHandled;
    }

    /// <summary>
    /// Publish an exception, without ever throwing
    /// </summary>
    private void ReportError(Exception exception)
    {
        Trace.TraceError("{0}: a subscriber threw an exception: {1}", _name, exception);
        try
        {
            _errorSubject.OnNext(exception);
        }
        catch (Exception ex)
        {
            Trace.TraceError("{0}: a SubscriberErrors subscriber threw an exception: {1}", _name, ex);
        }
    }

    /// <summary>
    /// A thread which installs the hook and runs a message loop, as long as the hook is needed
    /// </summary>
    private sealed class HookThread
    {
        private readonly LowLevelHook<TEventArgs> _owner;
        private readonly ManualResetEventSlim _installed = new(false);
        // Keep a reference to the delegate, so the function pointer handed to Windows is not garbage collected while the hook is installed
        private readonly HookProc _hookProc;
        private volatile bool _isStopping;
        private volatile uint _threadId;
        private IntPtr _hookHandle;
        private Exception _installError;

        public HookThread(LowLevelHook<TEventArgs> owner)
        {
            _owner = owner;
            _hookProc = HookCallback;
        }

        /// <summary>
        /// Start the thread and wait until the hook is installed
        /// </summary>
        /// <param name="error">Exception when the hook couldn't be installed</param>
        /// <returns>true if the hook was installed</returns>
        public bool Start(out Exception error)
        {
            var thread = new Thread(Run)
            {
                IsBackground = true,
                Name = _owner._name
            };
            thread.Start();
            if (!_installed.Wait(InstallTimeoutMilliseconds))
            {
                Stop();
                error = new TimeoutException($"{_owner._name}: the {_owner._hookType} hook was not installed within {InstallTimeoutMilliseconds} ms.");
                return false;
            }
            error = _installError;
            return error == null;
        }

        /// <summary>
        /// Request the thread to remove the hook and stop, this doesn't wait
        /// </summary>
        public void Stop()
        {
            _isStopping = true;
            var threadId = _threadId;
            if (threadId != 0)
            {
                PostThreadMessage(threadId, WmQuit, IntPtr.Zero, IntPtr.Zero);
            }
        }

        private void Run()
        {
            try
            {
                // Make sure the thread has a message queue before the thread id is published, so PostThreadMessage works
                PeekMessage(out _, IntPtr.Zero, 0, 0, PmNoRemove);
                _threadId = GetCurrentThreadId();

                var hookHandle = SetWindowsHookEx(_owner._hookType, _hookProc, GetModuleHandle(null), 0);
                if (hookHandle == IntPtr.Zero)
                {
                    var errorCode = Marshal.GetLastWin32Error();
                    _installError = new Win32Exception(errorCode, $"{_owner._name}: SetWindowsHookEx for {_owner._hookType} failed: {new Win32Exception(errorCode).Message}");
                    return;
                }
                _hookHandle = hookHandle;
                _installed.Set();

                try
                {
                    while (!_isStopping)
                    {
                        var result = GetMessage(out var msg, IntPtr.Zero, 0, 0);
                        if (result == 0)
                        {
                            // WM_QUIT
                            break;
                        }
                        if (result == -1)
                        {
                            var errorCode = Marshal.GetLastWin32Error();
                            _owner.ReportError(new Win32Exception(errorCode, $"{_owner._name}: GetMessage failed, the {_owner._hookType} hook is removed."));
                            break;
                        }
                        TranslateMessage(ref msg);
                        DispatchMessage(ref msg);
                    }
                }
                finally
                {
                    UnhookWindowsHookEx(hookHandle);
                    GC.KeepAlive(_hookProc);
                }
            }
            catch (Exception ex)
            {
                _installError ??= ex;
                Trace.TraceError("{0}: the hook thread failed: {1}", _owner._name, ex);
            }
            finally
            {
                _installed.Set();
            }
        }

        private IntPtr HookCallback(int nCode, IntPtr wParam, IntPtr lParam)
        {
            if (nCode >= 0 && !_isStopping && _owner.Dispatch(wParam, lParam))
            {
                // Swallow the event
                return (IntPtr)1;
            }
            return CallNextHookEx(_hookHandle, nCode, wParam, lParam);
        }
    }
}
