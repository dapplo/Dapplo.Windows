// Copyright (c) Dapplo and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Threading;
using Dapplo.Windows.Messages;
using Dapplo.Windows.Messages.Enums;
using Xunit;

namespace Dapplo.Windows.Tests;

/// <summary>
/// Tests for the SharedMessageWindow and WindowMessage, these only use the hidden shared window and a private registered message
/// </summary>
public class SharedMessageWindowTests
{
    [DllImport("user32", EntryPoint = "RegisterWindowMessageW", CharSet = CharSet.Unicode, SetLastError = true)]
    private static extern uint RegisterWindowMessage(string lpString);

    [DllImport("user32", EntryPoint = "SendMessageW")]
    private static extern nint SendMessage(nint hWnd, uint msg, nint wParam, nint lParam);

    private static readonly uint TestMessage = RegisterWindowMessage("Dapplo.Windows.Tests.SharedMessageWindowTests");

    private static bool IsTestMessage(WindowMessage windowMessage, nint marker)
    {
        return (uint)windowMessage.Msg == TestMessage && windowMessage.WParam == marker;
    }

    [Fact]
    public void WindowMessage_IsSharedBetweenSubscribers()
    {
        var windowMessage = new WindowMessage(1, WindowsMessages.WM_USER, 2, 3);
        Action<WindowMessage> subscriber = m =>
        {
            m.Handled = true;
            m.Result = -1;
        };
        subscriber(windowMessage);
        Assert.True(windowMessage.Handled);
        Assert.Equal((nint)(-1), windowMessage.Result);
        Assert.Equal((nint)1, windowMessage.Hwnd);
        Assert.Equal(WindowsMessages.WM_USER, windowMessage.Msg);
        Assert.Equal((nint)2, windowMessage.WParam);
        Assert.Equal((nint)3, windowMessage.LParam);
        Assert.Contains("WM_USER", windowMessage.ToString());
    }

    [Fact]
    public void Handle_IsNonZeroAndStable()
    {
        var handle = SharedMessageWindow.Handle;
        Assert.NotEqual((nint)0, handle);
        Assert.Equal(handle, SharedMessageWindow.Handle);
        using (SharedMessageWindow.Messages.Subscribe(_ => { }))
        {
            Assert.Equal(handle, SharedMessageWindow.Handle);
        }
        Assert.Equal(handle, SharedMessageWindow.Handle);
    }

    [Fact]
    public void Invoke_RunsOnWindowThread()
    {
        Assert.False(SharedMessageWindow.IsWindowThread);
        var callerThreadId = Environment.CurrentManagedThreadId;
        var invokeThreadId = 0;
        var isWindowThread = false;
        nint invokeHandle = 0;
        var nestedRan = false;
        SharedMessageWindow.Invoke(hwnd =>
        {
            invokeThreadId = Environment.CurrentManagedThreadId;
            isWindowThread = SharedMessageWindow.IsWindowThread;
            invokeHandle = hwnd;
            // A nested invoke on the window thread must run directly, and not dead-lock
            SharedMessageWindow.Invoke(_ => nestedRan = true);
        });
        Assert.True(isWindowThread);
        Assert.True(nestedRan);
        Assert.NotEqual(callerThreadId, invokeThreadId);
        Assert.Equal(SharedMessageWindow.Handle, invokeHandle);
    }

    [Fact]
    public void Invoke_RethrowsExceptionToCaller()
    {
        var exception = Assert.Throws<InvalidOperationException>(() => SharedMessageWindow.Invoke(_ => throw new InvalidOperationException("from the window thread")));
        Assert.Equal("from the window thread", exception.Message);
    }

    [Fact]
    public void Listen_SetupAndTeardownRunOnceOnWindowThread()
    {
        var setupCount = 0;
        var teardownCount = 0;
        var setupOnWindowThread = false;
        var teardownOnWindowThread = false;
        nint setupHandle = 0;
        nint teardownHandle = 0;

        var subscription = SharedMessageWindow.Listen(
            onSetup: hwnd =>
            {
                setupCount++;
                setupHandle = hwnd;
                setupOnWindowThread = SharedMessageWindow.IsWindowThread;
            },
            onTeardown: hwnd =>
            {
                teardownCount++;
                teardownHandle = hwnd;
                teardownOnWindowThread = SharedMessageWindow.IsWindowThread;
            }).Subscribe(_ => { });

        Assert.Equal(1, setupCount);
        Assert.Equal(0, teardownCount);
        Assert.True(setupOnWindowThread);
        Assert.Equal(SharedMessageWindow.Handle, setupHandle);

        subscription.Dispose();
        subscription.Dispose();

        Assert.Equal(1, setupCount);
        Assert.Equal(1, teardownCount);
        Assert.True(teardownOnWindowThread);
        Assert.Equal(setupHandle, teardownHandle);
    }

    [Fact]
    public void Listen_ReceivesMessagesSentToHandle()
    {
        const int marker = 11;
        var received = 0;
        using (SharedMessageWindow.Listen(onSetup: _ => { }).Subscribe(m =>
        {
            if (IsTestMessage(m, marker))
            {
                received++;
            }
        }))
        {
            SendMessage(SharedMessageWindow.Handle, TestMessage, marker, 0);
        }
        // After disposing, nothing is received anymore
        SendMessage(SharedMessageWindow.Handle, TestMessage, marker, 0);
        Assert.Equal(1, received);
    }

    [Fact]
    public void ThrowingSubscriber_DoesNotStopOtherSubscribers()
    {
        const int marker = 22;
        var thrown = new InvalidOperationException("Subscriber failure");
        var errors = new List<Exception>();
        var received = 0;

        using (SharedMessageWindow.SubscriberErrors.Subscribe(ex =>
        {
            lock (errors)
            {
                errors.Add(ex);
            }
        }))
        using (SharedMessageWindow.Messages.Subscribe(m =>
        {
            if (IsTestMessage(m, marker))
            {
                throw thrown;
            }
        }))
        using (SharedMessageWindow.Messages.Subscribe(m =>
        {
            if (IsTestMessage(m, marker))
            {
                received++;
            }
        }))
        {
            SendMessage(SharedMessageWindow.Handle, TestMessage, marker, 0);
            // The message pump is still alive
            SendMessage(SharedMessageWindow.Handle, TestMessage, marker, 0);
            var invoked = false;
            SharedMessageWindow.Invoke(_ => invoked = true);
            Assert.True(invoked);
        }

        Assert.Equal(2, received);
        lock (errors)
        {
            Assert.Contains(thrown, errors);
        }
    }

    [Fact]
    public void HandledAndResult_AreReturnedFromSendMessage()
    {
        const int handledMarker = 33;
        const int unhandledMarker = 34;
        var threadIds = new List<int>();
        using (SharedMessageWindow.Messages.Subscribe(m =>
        {
            if (IsTestMessage(m, handledMarker))
            {
                threadIds.Add(Environment.CurrentManagedThreadId);
                m.Handled = true;
                m.Result = -1234;
            }
        }))
        {
            Assert.Equal((nint)(-1234), SendMessage(SharedMessageWindow.Handle, TestMessage, handledMarker, 0));
            // Not handled: DefWindowProc returns 0 for a registered message
            Assert.Equal((nint)0, SendMessage(SharedMessageWindow.Handle, TestMessage, unhandledMarker, 0));
        }
        Assert.Single(threadIds);
        Assert.NotEqual(Environment.CurrentManagedThreadId, threadIds[0]);
    }
}
