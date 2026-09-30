// Copyright (c) Dapplo and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Reactive.Linq;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using System.Threading;
using Dapplo.Windows.Desktop;
using Dapplo.Windows.Forms.Messages;
using Dapplo.Windows.Messages;
using Dapplo.Windows.Messages.Enumerations;
using Dapplo.Windows.Wpf.Messages;

namespace Dapplo.Windows.Example.DocSamples;

/// <summary>
/// Samples for doc/articles/window-messages.md, wiki/SharedMessageWindow.md
/// </summary>
public static class MessagesSamples
{
    public static void SimpleFilter()
    {
        #region SimpleFilter
        // Called on the SharedMessageWindow thread: keep it short, and never block it
        IDisposable subscription = SharedMessageWindow.Messages
            .Where(m => m.Msg == WindowsMessages.WM_DISPLAYCHANGE)
            .Subscribe(m => Console.WriteLine($"Display changed, new resolution {(int)m.LParam & 0xFFFF}x{((int)m.LParam >> 16) & 0xFFFF}"));

        // Stop listening, the window itself stays alive
        subscription.Dispose();
        #endregion
    }

    public static void ObserveOnUi(Label statusLabel)
    {
        #region ObserveOnUi
        // Filter on the window thread, then continue on the UI thread (call this on the UI thread)
        var subscription = SharedMessageWindow.Messages
            .Where(m => m.Msg == WindowsMessages.WM_SETTINGCHANGE)
            .ObserveOn(SynchronizationContext.Current)
            .Subscribe(m => statusLabel.Text = "Settings changed");
        #endregion
    }

    public static void HandleMessage()
    {
        #region HandleMessage
        // Handled and Result must be set synchronously in OnNext, before any ObserveOn
        var subscription = SharedMessageWindow.Messages
            .Where(m => m.Msg == WindowsMessages.WM_QUERYENDSESSION)
            .Subscribe(m =>
            {
                m.Result = 1;    // TRUE: the session may end
                m.Handled = true; // don't call DefWindowProc
            });
        #endregion
    }

    #region ListenHotkeyPInvoke
    [DllImport("user32", SetLastError = true)]
    private static extern bool RegisterHotKey(IntPtr hWnd, int id, uint modifiers, uint virtualKey);

    [DllImport("user32", SetLastError = true)]
    private static extern bool UnregisterHotKey(IntPtr hWnd, int id);
    #endregion

    public static void ListenHotkey()
    {
        #region ListenHotkey
        const int hotkeyId = 1;
        const uint modControl = 0x0002, modShift = 0x0004, modNoRepeat = 0x4000;

        // RegisterHotKey must be called on the thread of the window which gets WM_HOTKEY:
        // Listen runs onSetup and onTeardown on the SharedMessageWindow thread
        var subscription = SharedMessageWindow.Listen(
                onSetup: hwnd =>
                {
                    if (!RegisterHotKey(hwnd, hotkeyId, modControl | modShift | modNoRepeat, (uint)'P'))
                    {
                        // The exception goes to OnError of this subscription
                        throw new System.ComponentModel.Win32Exception();
                    }
                },
                onTeardown: hwnd => UnregisterHotKey(hwnd, hotkeyId))
            .Where(m => m.Msg == WindowsMessages.WM_HOTKEY && m.WParam == hotkeyId)
            .Subscribe(
                m => Console.WriteLine("Ctrl+Shift+P pressed"),
                ex => Console.WriteLine($"Registering the hotkey failed: {ex.Message}"));

        // Disposing the subscription runs onTeardown (UnregisterHotKey) on the window thread
        subscription.Dispose();
        #endregion
    }

    public static void Invoke()
    {
        #region Invoke
        // Run code on the window thread and wait for it, exceptions are rethrown here
        bool isWindowThread = false;
        SharedMessageWindow.Invoke(hwnd => isWindowThread = SharedMessageWindow.IsWindowThread);

        // The handle exists as soon as it's requested, and stays valid until the window is shut down (at process exit)
        IntPtr handle = SharedMessageWindow.Handle;
        #endregion
    }

    public static void Shutdown()
    {
        #region Shutdown
        // Happens automatically on AppDomain.ProcessExit, with this timeout (default 1.5 seconds)
        SharedMessageWindow.ProcessExitShutdownTimeout = TimeSpan.FromSeconds(1);

        // Or at the end of Main, to control the moment and the timeout yourself:
        // the window is destroyed on its own thread, delayed rendered clipboard formats are rendered (WM_RENDERALLFORMATS)
        bool isShutDown = SharedMessageWindow.Shutdown(TimeSpan.FromSeconds(5));
        if (!isShutDown)
        {
            Console.WriteLine("The window thread didn't end in time, e.g. a delayed renderer is still busy");
        }
        #endregion
    }

    public static void SubscriberErrors()
    {
        #region SubscriberErrors
        // An exception in a subscriber ends only that subscription, it is published here (and written to Trace)
        var errorSubscription = SharedMessageWindow.SubscriberErrors
            .Subscribe(ex => Console.Error.WriteLine($"A message subscriber failed: {ex}"));
        #endregion
    }

    public static void CustomMessage()
    {
        #region CustomMessage
        // A message which is unique for the whole desktop, e.g. to let a second instance talk to the first
        uint showMeMessage = WindowsMessage.RegisterWindowsMessage("MyApp.ShowMe");

        var subscription = SharedMessageWindow.Messages
            .Where(m => (uint)m.Msg == showMeMessage)
            .Subscribe(m => Console.WriteLine("Another instance asked me to show myself"));
        #endregion
    }

    public static void SessionListener()
    {
        #region SessionListener
        var sessionListener = new WindowsSessionListener();

        // The events are raised on the SharedMessageWindow thread
        sessionListener.SessionLockChange += (sender, args) =>
        {
            if (args.EventType == WtsSessionChangeEvents.WTS_SESSION_LOCK)
            {
                Console.WriteLine($"Session {args.SessionId} locked");
            }
            else if (args.EventType == WtsSessionChangeEvents.WTS_SESSION_UNLOCK)
            {
                Console.WriteLine($"Session {args.SessionId} unlocked");
            }
        };

        sessionListener.SessionLogonChange += (sender, args) =>
            Console.WriteLine(args.EventType == WtsSessionChangeEvents.WTS_SESSION_LOGON ? "Logged on" : "Logged off");

        // Early at logon the registration can fail, it's retried for about 2 minutes before this is raised
        sessionListener.RegistrationFailed += (sender, args) =>
            Console.WriteLine($"No session notifications: {args.GetException().Message}");

        sessionListener.Start();

        // Ignore events for a while, without unregistering
        sessionListener.Pause();
        sessionListener.Resume();

        // Stop listening and unregister
        sessionListener.Dispose();
        #endregion
    }

    public static void EnvironmentChanges()
    {
        #region EnvironmentChanges
        // WM_SETTINGCHANGE, e.g. when the user switches between light and dark mode ("ImmersiveColorSet")
        var subscription = EnvironmentMonitor.EnvironmentUpdateEvents
            .Where(e => e.Area == "ImmersiveColorSet")
            .Subscribe(e => Console.WriteLine("The color theme changed"));
        #endregion
    }

    public static void FormsMessages(Form form)
    {
        #region FormsMessages
        // Subclasses the form's window. Runs on the UI thread, you may set Handled / Result.
        // The sequence follows handle re-creation and completes when the form is disposed.
        var subscription = form.WinProcFormsMessages()
            .Where(m => m.Message == WindowsMessages.WM_NCHITTEST)
            .Subscribe(m =>
            {
                // HTCAPTION: the whole window can be dragged like its title bar
                m.Result = new IntPtr(2);
                m.Handled = true;
            });
        #endregion
    }

    public static void WpfMessages(System.Windows.Window window)
    {
        #region WpfMessages
        // Works before the window is shown, the hook is added when the HwndSource is created
        var subscription = window.WinProcMessages()
            .Where(m => m.Message == WindowsMessages.WM_DPICHANGED)
            .Subscribe(m => Console.WriteLine("DPI changed"));
        #endregion
    }
}
