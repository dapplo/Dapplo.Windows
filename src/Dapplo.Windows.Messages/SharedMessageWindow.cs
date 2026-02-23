// Copyright (c) Dapplo and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Dapplo.Windows.Messages.Enumerations;
using Dapplo.Windows.Messages.Native;
using Dapplo.Windows.Messages.Structs;
using System;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Runtime.InteropServices;
using System.Threading;

namespace Dapplo.Windows.Messages;

/// <summary>
/// Provides functionality to create an observable stream of Windows messages using a message-only window on a dedicated thread.
/// </summary>
public static class SharedMessageWindow
{
    #region PInvokes
    [DllImport("user32", SetLastError = true, CharSet = CharSet.Auto)]
    private static extern ushort RegisterClassEx(ref WNDCLASSEX lpwc);

    [DllImport("user32", SetLastError = true, CharSet = CharSet.Auto)]
    private static extern bool UnregisterClass(string lpClassName, nint hInstance);

    [DllImport("user32", SetLastError = true, CharSet = CharSet.Auto)]
    private static extern nint CreateWindowEx(uint dwExStyle, string lpClassName, string lpWindowName, uint dwStyle, int x, int y, int nWidth, int nHeight, nint hWndParent, nint hMenu, nint hInstance, nint lpParam);

    [DllImport("user32")]
    private static extern bool GetMessage(out Msg lpMsg, nint hWnd, uint wMsgFilterMin, uint wMsgFilterMax);

    [DllImport("user32")]
    private static extern bool TranslateMessage(ref Msg lpMsg);

    [DllImport("user32")]
    private static extern nint DispatchMessage(ref Msg lpMsg);

    [DllImport("user32")]
    private static extern nint DefWindowProc(nint hWnd, WindowsMessages msg, nint wParam, nint lParam);

    [DllImport("user32")]
    private static extern void PostQuitMessage(int nExitCode);

    [DllImport("user32")]
    private static extern bool PostMessage(nint hWnd, uint Msg, nint wParam, nint lParam);

    [DllImport("kernel32")]
    private static extern nint GetModuleHandle(string lpModuleName);
    #endregion

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
    private struct WNDCLASSEX
    {
        public uint cbSize; public uint style; public WndProc lpfnWndProc; public int cbClsExtra;
        public int cbWndExtra; public nint hInstance; public nint hIcon; public nint hCursor;
        public nint hbrBackground; public string lpszMenuName; public string lpszClassName; public nint hIconSm;
    }

    private static IObservable<WindowMessage> _sharedStream;
    private static readonly object _lock = new();
    private static readonly BehaviorSubject<nint> _handleSubject = new(0);

    /// <summary>
    /// Gets the current handle of the message window, or zero if no window is currently active.
    /// </summary>
    public static nint Handle => _handleSubject.Value;

    /// <summary>
    /// Gets an observable sequence of all window messages received by the application.
    /// </summary>
    /// <remarks>The returned observable is shared among all subscribers. Subscribing to this property allows
    /// monitoring of window messages as they occur. Unsubscribing from all observers will automatically release
    /// resources associated with the stream.</remarks>
    public static IObservable<WindowMessage> Messages
    {
        get
        {
            lock (_lock)
            {
                if (_sharedStream == null)
                {
                    _sharedStream = CreateBaseStream()
                        .Publish()
                        .RefCount();
                }
                return _sharedStream;
            }
        }
    }

    /// <summary>
    /// Subscribes to the shared message loop.
    /// </summary>
    /// <param name="onSetup">Invoked with the HWND when the window is created (or immediately if it already exists).</param>
    /// <param name="onTeardown">Invoked with the HWND when the window is destroyed OR when the subscription is disposed.</param>
    public static IObservable<WindowMessage> Listen(Action<nint> onSetup = null, Action<nint> onTeardown = null)
    {
        if (onSetup == null && onTeardown == null)
        {
            return Messages;
        }

        return Observable.Create<WindowMessage>(observer =>
        {
            nint activeHwnd = 0;
            object handleLock = new();

            // Manage the Handle Lifecycle
            var handleSubscription = _handleSubject.Subscribe(hwnd =>
            {
                lock (handleLock)
                {
                    if (hwnd != 0)
                    {
                        activeHwnd = hwnd;
                        onSetup?.Invoke(activeHwnd);
                    }
                    else if (activeHwnd != 0)
                    {
                        // Window was destroyed
                        onTeardown?.Invoke(activeHwnd);
                        activeHwnd = 0;
                    }
                }
            });

            // Pass through the messages
            var messageSubscription = Messages.Subscribe(observer);

            // Ensure teardown runs if the consumer drops the subscription early
            return Disposable.Create(() =>
            {
                lock (handleLock)
                {
                    if (activeHwnd != 0)
                    {
                        onTeardown?.Invoke(activeHwnd);
                        activeHwnd = 0;
                    }
                }
                handleSubscription.Dispose();
                messageSubscription.Dispose();
            });
        });
    }

    /// <summary>
    /// Creates an observable sequence that emits Windows messages for a message-only window with the specified class name.
    /// </summary>
    /// <remarks>The window is created on a dedicated background thread with a single-threaded apartment
    /// state. Disposal of the returned observable triggers destruction of the window and completion of the sequence.
    /// This method is useful for scenarios requiring low-level message processing without a visible UI window.</remarks>
    /// <returns>An observable sequence of Windows messages received by the created message-only window. The sequence completes when the window is destroyed.</returns>
    private static IObservable<WindowMessage> CreateBaseStream()
    {
        return Observable.Create<WindowMessage>(observer =>
        {
            nint hwnd = 0;
            string className = "MsgLoop_" + Guid.NewGuid().ToString("N");
            WndProc wndProcDelegate = null;

            var thread = new Thread(() =>
            {
                wndProcDelegate = (hWnd, msg, wParam, lParam) =>
                {
                    if (msg == WindowsMessages.WM_DESTROY)
                    {
                        PostQuitMessage(0);
                        return 0;
                    }

                    var windowMessage = new WindowMessage(hWnd, msg, wParam, lParam);
                    observer.OnNext(windowMessage);
                    // Check if any subscriber claimed the message
                    if (windowMessage.Handled)
                    {
                        return windowMessage.Result; // Return the custom value defined by the listener
                    }
                    return DefWindowProc(hWnd, msg, wParam, lParam);
                };

                // Register Class
                var wndClass = new WNDCLASSEX
                {
                    cbSize = (uint)Marshal.SizeOf<WNDCLASSEX>(),
                    lpfnWndProc = wndProcDelegate,
                    hInstance = GetModuleHandle(null),
                    lpszClassName = className
                };

                RegisterClassEx(ref wndClass);

                hwnd = CreateWindowEx(0, className, "NativeMessageWindow", 0, 0, 0, 0, 0, -3, 0, wndClass.hInstance, 0);

                // Trigger External Registrations
                if (hwnd != 0)
                {
                    _handleSubject.OnNext(hwnd);
                }

                while (GetMessage(out var msg, 0, 0, 0))
                {
                    TranslateMessage(ref msg);
                    DispatchMessage(ref msg);
                }

                // Trigger External Cleanup
                if (hwnd != 0)
                {
                    _handleSubject.OnNext(0);
                }
                
                UnregisterClass(className, wndClass.hInstance);
            });

            thread.SetApartmentState(ApartmentState.STA);
            thread.IsBackground = true;
            thread.Start();

            return Disposable.Create(() =>
            {
                if (hwnd != 0)
                {
                    PostMessage(hwnd, (uint)WindowsMessages.WM_DESTROY, 0, 0);
                }
            });
        });
    }
}
