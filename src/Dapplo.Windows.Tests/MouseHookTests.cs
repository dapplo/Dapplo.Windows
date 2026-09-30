// Copyright (c) Dapplo and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
using System;
using System.Reactive.Linq;
using System.Threading.Tasks;
using Dapplo.Log;
using Dapplo.Log.XUnit;
using Dapplo.Windows.Input.Mouse;
using Dapplo.Windows.Messages.Enumerations;
using Dapplo.Windows.User32;
using Xunit;

namespace Dapplo.Windows.Tests;

/// <summary>
///     Test mouse hooking
/// </summary>
/// <remarks>Interactive: these tests change the real desktop (input, clipboard or registry). They are excluded by default, run them with --filter Category=Interactive.</remarks>
[Trait("Category", "Interactive")]
public class MouseHookTests
{
    public MouseHookTests(ITestOutputHelper testOutputHelper)
    {
        LogSettings.RegisterDefaultLogger<XUnitLogger>(LogLevels.Verbose, testOutputHelper);
    }

    /// <summary>
    ///     An injected mouse move, to the current location, must be reported by the hook
    /// </summary>
    [StaFact]
    public async Task Test_MouseMove_IsReported()
    {
        var mouseMoved = new TaskCompletionSource<MouseHookEventArgs>(TaskCreationOptions.RunContinuationsAsynchronously);
        using (MouseHook.MouseEvents.Where(args => args.WindowsMessage == WindowsMessages.WM_MOUSEMOVE && args.IsInjectedByProcess).Subscribe(args => mouseMoved.TrySetResult(args)))
        {
            MouseInputGenerator.MoveMouse(User32Api.GetCursorLocation());
            var mouseHookEventArgs = await TestWait.ForAsync(mouseMoved.Task, "The injected mouse move was not reported");
            Assert.Equal(WindowsMessages.WM_MOUSEMOVE, mouseHookEventArgs.WindowsMessage);
        }
    }
}