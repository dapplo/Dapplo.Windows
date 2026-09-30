// Copyright (c) Dapplo and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using Dapplo.Windows.AppRestartManager;
using Dapplo.Windows.AppRestartManager.Enums;
using Dapplo.Windows.Messages.Enums;
using Dapplo.Windows.Messages;
using Xunit;

namespace Dapplo.Windows.Tests;

/// <summary>
/// Tests for the EndSessionMessage, which answers WM_QUERYENDSESSION via the WindowMessage
/// </summary>
public class EndSessionMessageTests
{
    [Fact]
    public void QueryEndSession_DefaultAllows_WithoutHandlingTheMessage()
    {
        var windowMessage = new WindowMessage(0, WindowsMessages.WM_QUERYENDSESSION, 0, (nint)(long)(uint)EndSessionReasons.ENDSESSION_CLOSEAPP);
        var endSessionMessage = new EndSessionMessage(windowMessage);

        Assert.True(endSessionMessage.IsQuery);
        Assert.True(endSessionMessage.CanEndSession);
        Assert.Equal(EndSessionReasons.ENDSESSION_CLOSEAPP, endSessionMessage.EndSessionReason);
        // Not handled: DefWindowProc answers TRUE
        Assert.False(windowMessage.Handled);
    }

    [Fact]
    public void QueryEndSession_Allow_ReturnsTrue()
    {
        var windowMessage = new WindowMessage(0, WindowsMessages.WM_QUERYENDSESSION, 0, 0);
        var endSessionMessage = new EndSessionMessage(windowMessage)
        {
            CanEndSession = true
        };

        Assert.True(windowMessage.Handled);
        // WM_QUERYENDSESSION is answered with TRUE (1) to allow, not with S_OK (0)
        Assert.Equal((nint)1, windowMessage.Result);
        Assert.True(endSessionMessage.CanEndSession);
    }

    [Fact]
    public void QueryEndSession_Veto_ReturnsFalse_AndIsSharedBetweenSubscribers()
    {
        var windowMessage = new WindowMessage(0, WindowsMessages.WM_QUERYENDSESSION, 0, 0);
        var firstSubscriber = new EndSessionMessage(windowMessage);
        var secondSubscriber = new EndSessionMessage(windowMessage);

        Assert.True(firstSubscriber.Veto());

        Assert.True(windowMessage.Handled);
        Assert.Equal((nint)0, windowMessage.Result);
        Assert.False(secondSubscriber.CanEndSession);
    }

    [Fact]
    public void EndSession_ReportsIfTheSessionEnds_AndCannotBeAnswered()
    {
        var logoff = unchecked((nint)(long)(uint)EndSessionReasons.ENDSESSION_LOGOFF);
        var ending = new EndSessionMessage(new WindowMessage(0, WindowsMessages.WM_ENDSESSION, 1, logoff));
        var cancelled = new EndSessionMessage(new WindowMessage(0, WindowsMessages.WM_ENDSESSION, 0, 0));

        Assert.False(ending.IsQuery);
        Assert.True(ending.IsSessionEnding);
        Assert.Equal(EndSessionReasons.ENDSESSION_LOGOFF, ending.EndSessionReason);
        Assert.False(cancelled.IsSessionEnding);
        Assert.True(ending.CanEndSession);
        Assert.Throws<InvalidOperationException>(() => ending.CanEndSession = false);
        Assert.False(ending.WindowMessage.Handled);
    }

    [Fact]
    public void EndSessionMessage_RejectsOtherMessages()
    {
        Assert.Throws<ArgumentException>(() => new EndSessionMessage(new WindowMessage(0, WindowsMessages.WM_CLOSE, 0, 0)));
        Assert.Throws<ArgumentNullException>(() => new EndSessionMessage(null));
    }
}
