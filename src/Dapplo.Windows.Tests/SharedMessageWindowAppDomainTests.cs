// Copyright (c) Dapplo and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Runtime.InteropServices;
using System.Threading;
using Dapplo.Windows.Messages;
using Xunit;

namespace Dapplo.Windows.Tests;

/// <summary>
/// Tests for the lifetime of the SharedMessageWindow in an AppDomain which is unloaded (.NET Framework only, e.g. test hosts),
/// and that nothing changes in the default AppDomain. These only use hidden windows.
/// </summary>
public class SharedMessageWindowAppDomainTests
{
    [DllImport("user32")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool IsWindow(nint hWnd);

    [Fact]
    public void DefaultAppDomain_WindowLivesAsBefore()
    {
        // On .NET (Core) there is only the default AppDomain, on .NET Framework the xunit.v3 test exe runs in it: DomainUnload is not used
        Assert.True(AppDomain.CurrentDomain.IsDefaultAppDomain());
        var handle = SharedMessageWindow.Handle;
        Assert.True(IsWindow(handle));
        Assert.False(SharedMessageWindow.IsProcessExiting);
        Assert.Equal(handle, SharedMessageWindow.Handle);
    }

#if NETFRAMEWORK
    private const uint Synchronize = 0x00100000;
    private const uint WaitObject0 = 0;

    [DllImport("kernel32", SetLastError = true)]
    private static extern nint OpenThread(uint dwDesiredAccess, [MarshalAs(UnmanagedType.Bool)] bool bInheritHandle, uint dwThreadId);

    [DllImport("kernel32", SetLastError = true)]
    private static extern uint WaitForSingleObject(nint hHandle, uint dwMilliseconds);

    [DllImport("kernel32", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool CloseHandle(nint hObject);

    [Fact]
    public void AppDomainUnload_ShutsDownTheWindowAndItsThread()
    {
        var recorder = new UnloadRecorder();
        var domain = CreateTestDomain();
        WindowInfo info;
        nint threadHandle;
        try
        {
            var helper = CreateHelper(domain);
            info = helper.CreateWindow(recorder);
            Assert.True(IsWindow(info.Hwnd));
            threadHandle = OpenThread(Synchronize, false, info.NativeThreadId);
            Assert.NotEqual((nint)0, threadHandle);
        }
        catch
        {
            AppDomain.Unload(domain);
            throw;
        }

        try
        {
            AppDomain.Unload(domain);

            // Still here: the process survived the unload
            Assert.False(IsWindow(info.Hwnd));
            Assert.Equal(WaitObject0, WaitForSingleObject(threadHandle, 5000));
        }
        finally
        {
            CloseHandle(threadHandle);
        }

        // Recorded by a DomainUnload handler in the unloaded domain, which ran after the one of the SharedMessageWindow
        Assert.True(recorder.Recorded);
        Assert.True(recorder.IsProcessExiting);
        Assert.False(recorder.WasWindowAlive);
        Assert.True(recorder.ThrewObjectDisposed, recorder.Error);
    }

    [Fact]
    public void AppDomainUnload_WhileTheWindowThreadIsBusy_DoesNotCrash()
    {
        var domain = CreateTestDomain();
        WindowInfo info;
        nint threadHandle;
        try
        {
            var helper = CreateHelper(domain);
            // The shutdown times out after 100 ms, the window procedure is busy for 1 second: the CLR aborts the window thread while it's in the window procedure
            info = helper.CreateBusyWindow(TimeSpan.FromMilliseconds(100), 1000);
            threadHandle = OpenThread(Synchronize, false, info.NativeThreadId);
            Assert.NotEqual((nint)0, threadHandle);
        }
        catch
        {
            AppDomain.Unload(domain);
            throw;
        }

        try
        {
            AppDomain.Unload(domain);

            // Still here: the thread abort didn't escape through the window procedure
            Assert.Equal(WaitObject0, WaitForSingleObject(threadHandle, 5000));
            Assert.False(IsWindow(info.Hwnd));
        }
        finally
        {
            CloseHandle(threadHandle);
        }
    }

    private static AppDomain CreateTestDomain()
    {
        var current = AppDomain.CurrentDomain.SetupInformation;
        var setup = new AppDomainSetup
        {
            ApplicationBase = current.ApplicationBase,
            ConfigurationFile = current.ConfigurationFile
        };
        return AppDomain.CreateDomain("Dapplo.Windows.Tests.SharedMessageWindow." + Guid.NewGuid().ToString("N"), null, setup);
    }

    private static AppDomainWindowHelper CreateHelper(AppDomain domain)
    {
        var helperType = typeof(AppDomainWindowHelper);
        return (AppDomainWindowHelper)domain.CreateInstanceAndUnwrap(helperType.Assembly.FullName, helperType.FullName);
    }

    /// <summary>
    /// The window handle and the native thread ID of the window thread in the other domain
    /// </summary>
    [Serializable]
    public sealed class WindowInfo
    {
        public nint Hwnd { get; set; }
        public uint NativeThreadId { get; set; }
    }

    /// <summary>
    /// Lives in the default domain, is called from a DomainUnload handler in the unloaded domain
    /// </summary>
    public sealed class UnloadRecorder : MarshalByRefObject
    {
        public bool Recorded { get; private set; }
        public bool IsProcessExiting { get; private set; }
        public bool WasWindowAlive { get; private set; }
        public bool ThrewObjectDisposed { get; private set; }
        public string Error { get; private set; }

        public void Record(bool isProcessExiting, bool wasWindowAlive, bool threwObjectDisposed, string error)
        {
            IsProcessExiting = isProcessExiting;
            WasWindowAlive = wasWindowAlive;
            ThrewObjectDisposed = threwObjectDisposed;
            Error = error;
            Recorded = true;
        }
    }

    /// <summary>
    /// Created in the other domain, uses the SharedMessageWindow of that domain
    /// </summary>
    public sealed class AppDomainWindowHelper : MarshalByRefObject
    {
        [DllImport("kernel32")]
        private static extern uint GetCurrentThreadId();

        [DllImport("user32", EntryPoint = "SendNotifyMessageW", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool SendNotifyMessage(nint hWnd, uint msg, nint wParam, nint lParam);

        [DllImport("user32", EntryPoint = "RegisterWindowMessageW", CharSet = CharSet.Unicode, SetLastError = true)]
        private static extern uint RegisterWindowMessage(string lpString);

        private static WindowInfo GetWindowInfo()
        {
            var info = new WindowInfo();
            SharedMessageWindow.Invoke(hwnd =>
            {
                info.Hwnd = hwnd;
                info.NativeThreadId = GetCurrentThreadId();
            });
            return info;
        }

        public WindowInfo CreateWindow(UnloadRecorder recorder)
        {
            var info = GetWindowInfo();
            // Registered after the SharedMessageWindow registered its handler, so this runs after its shutdown
            AppDomain.CurrentDomain.DomainUnload += (_, _) =>
            {
                var threwObjectDisposed = false;
                string error = null;
                try
                {
                    var unused = SharedMessageWindow.Handle;
                    error = "The window was created again during the unload";
                }
                catch (ObjectDisposedException)
                {
                    threwObjectDisposed = true;
                }
                catch (Exception ex)
                {
                    error = ex.ToString();
                }
                recorder.Record(SharedMessageWindow.IsProcessExiting, IsWindow(info.Hwnd), threwObjectDisposed, error);
            };
            return info;
        }

        public WindowInfo CreateBusyWindow(TimeSpan shutdownTimeout, int busyMilliseconds)
        {
            SharedMessageWindow.ProcessExitShutdownTimeout = shutdownTimeout;
            var busyMessage = RegisterWindowMessage("Dapplo.Windows.Tests.SharedMessageWindowAppDomainTests.Busy");
            var started = new ManualResetEventSlim(false);
            SharedMessageWindow.Messages.Subscribe(m =>
            {
                if ((uint)m.Msg == busyMessage)
                {
                    started.Set();
                    Thread.Sleep(busyMilliseconds);
                }
            });
            var info = GetWindowInfo();
            // No xunit in the other domain: exceptions are passed to the test
            // A sent message (not posted): GetMessage calls the window procedure from a user32 callback, like SendMessage from another thread
            if (!SendNotifyMessage(info.Hwnd, busyMessage, 0, 0))
            {
                throw new InvalidOperationException($"SendNotifyMessage failed with error {Marshal.GetLastWin32Error()}");
            }
            if (!started.Wait(5000))
            {
                throw new TimeoutException("The window procedure didn't get the busy message");
            }
            return info;
        }
    }
#endif
}
