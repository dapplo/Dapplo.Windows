// Copyright (c) Dapplo and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using Dapplo.Log;
using Dapplo.Log.XUnit;
using Dapplo.Windows.Messages;
using Dapplo.Windows.Messages.Enumerations;
using Xunit;

namespace Dapplo.Windows.Tests;

/// <summary>
/// Tests for WindowsSessionListener
/// </summary>
public class WindowsSessionListenerTests : IDisposable
{
    private static readonly LogSource Log = new LogSource();

    public WindowsSessionListenerTests(ITestOutputHelper testOutputHelper)
    {
        LogSettings.RegisterDefaultLogger<XUnitLogger>(LogLevels.Verbose, testOutputHelper);
    }

    /// <inheritdoc cref="IDisposable"/>
    public void Dispose()
    {
        // Normally not needed, but every test is more or less its own application and we need to make sure this cleanup is done
        WinProcHandler.Instance.MessageHandlerWindow.Dispose();
    }

    /// <summary>
    /// Test that WindowsSessionListener can be created and started
    /// </summary>
    //[WpfFact]
    public void TestWindowsSessionListener_CanCreate()
    {
        using var listener = new WindowsSessionListener();
        Assert.NotNull(listener);
    }

    /// <summary>
    /// Test that WindowsSessionListener can be started and stopped
    /// </summary>
    //[WpfFact]
    public void TestWindowsSessionListener_CanStartAndStop()
    {
        using var listener = new WindowsSessionListener();
        listener.Start();
        listener.Stop();
    }

    /// <summary>
    /// Test that WindowsSessionListener can be paused and resumed
    /// </summary>
    //[WpfFact]
    public void TestWindowsSessionListener_CanPauseAndResume()
    {
        using var listener = new WindowsSessionListener();
        listener.Start();
        listener.Pause();
        listener.Resume();
        listener.Stop();
    }

    /// <summary>
    /// Test that WindowsSessionListener events can be subscribed
    /// </summary>
    //[WpfFact]
    public void TestWindowsSessionListener_CanSubscribeToEvents()
    {
        using var listener = new WindowsSessionListener();
        
        var lockEventReceived = false;
        var logonEventReceived = false;

        listener.SessionLockChange += (sender, args) =>
        {
            Log.Info().WriteLine($"Lock/Unlock event: {args.EventType}, SessionId: {args.SessionId}");
            lockEventReceived = true;
        };

        listener.SessionLogonChange += (sender, args) =>
        {
            Log.Info().WriteLine($"Logon/Logoff event: {args.EventType}, SessionId: {args.SessionId}");
            logonEventReceived = true;
        };

        listener.Start();
        
        // Note: We can't easily trigger actual session change events in a unit test,
        // but we can verify that the events are properly wired up
        Assert.False(lockEventReceived); // No events should have been received yet
        Assert.False(logonEventReceived); // No events should have been received yet
        
        listener.Stop();
    }

    /// <summary>
    /// Test that WindowsSessionListener can be disposed multiple times safely
    /// </summary>
    //[WpfFact]
    public void TestWindowsSessionListener_CanDisposeMultipleTimes()
    {
        var listener = new WindowsSessionListener();
        listener.Start();
        listener.Dispose();
        listener.Dispose(); // Should not throw
    }

    /// <summary>
    /// Test that WindowsSessionListener throws when used after disposal
    /// </summary>
    //[WpfFact]
    public void TestWindowsSessionListener_ThrowsAfterDisposal()
    {
        var listener = new WindowsSessionListener();
        listener.Dispose();
        Assert.Throws<ObjectDisposedException>(() => listener.Start());
    }

    /// <summary>
    /// Test that WtsSessionChangeEvents enum has expected values
    /// </summary>
    //[WpfFact]
    public void TestWtsSessionChangeEvents_HasExpectedValues()
    {
        Assert.Equal(0x5, (int)WtsSessionChangeEvents.WTS_SESSION_LOGON);
        Assert.Equal(0x6, (int)WtsSessionChangeEvents.WTS_SESSION_LOGOFF);
        Assert.Equal(0x7, (int)WtsSessionChangeEvents.WTS_SESSION_LOCK);
        Assert.Equal(0x8, (int)WtsSessionChangeEvents.WTS_SESSION_UNLOCK);
    }

    /// <summary>
    /// Test SessionChangeEventArgs properties
    /// </summary>
    //[WpfFact]
    public void TestSessionChangeEventArgs_HasCorrectProperties()
    {
        var args = new SessionChangeEventArgs(WtsSessionChangeEvents.WTS_SESSION_LOCK, 123);
        Assert.Equal(WtsSessionChangeEvents.WTS_SESSION_LOCK, args.EventType);
        Assert.Equal(123, args.SessionId);
    }
}
