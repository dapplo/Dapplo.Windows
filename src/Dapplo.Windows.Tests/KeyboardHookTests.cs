// Copyright (c) Dapplo and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
using System;
using System.Linq;
using System.Reactive.Concurrency;
using System.Reactive.Linq;
using System.Threading;
using System.Threading.Tasks;
using Dapplo.Log;
using Dapplo.Log.XUnit;
using Dapplo.Windows.Input.Enums;
using Dapplo.Windows.Input.Keyboard;
using Xunit;

namespace Dapplo.Windows.Tests;

/// <remarks>Interactive: these tests change the real desktop (input, clipboard or registry). They are excluded by default, run them with --filter Category=Interactive.</remarks>
[Trait("Category", "Interactive")]
public class KeyboardHookTests
{
    private static LogSource Log = new LogSource();
    public KeyboardHookTests(ITestOutputHelper testOutputHelper)
    {
        LogSettings.RegisterDefaultLogger<XUnitLogger>(LogLevels.Verbose, testOutputHelper);
    }

    [StaFact]
    public async Task TestKeyHandler_SingleCombination()
    {
        int pressCount = 0;
        var keyHandler = new KeyCombinationHandler(VirtualKeyCode.Back, VirtualKeyCode.RightShift)
        {
            IgnoreInjected = false,
            IsPassThrough = false
        };
        using (KeyboardHook.KeyboardEvents.Where(keyHandler).Subscribe(keyboardHookEventArgs => pressCount++))
        using (var counter = new InjectedKeyCounter())
        {
            KeyboardInputGenerator.KeyCombinationPress(VirtualKeyCode.Back, VirtualKeyCode.RightShift);
            await counter.WaitForAsync(4);
            KeyboardInputGenerator.KeyCombinationPress(VirtualKeyCode.Back, VirtualKeyCode.RightShift);
            await counter.WaitForAsync(8);
        }
        Assert.Equal(2, pressCount);

        // After the subscription is disposed, the handler must not be called anymore
        using (var counter = new InjectedKeyCounter())
        {
            KeyboardInputGenerator.KeyCombinationPress(VirtualKeyCode.Back, VirtualKeyCode.RightShift);
            await counter.WaitForAsync(4);
        }
        Assert.Equal(2, pressCount);
    }

    [StaFact]
    public async Task TestKeyHandler_Slow_Subscriber()
    {
        int pressCount = 0;
        const int pressHandlingTime = 500;

        var sequenceHandler = new KeyCombinationHandler(VirtualKeyCode.Shift, VirtualKeyCode.KeyA) { IgnoreInjected = false };

        // The handler decides synchronously on the hook thread, the slow work is moved away from the hook thread with ObserveOn
        using (KeyboardHook.KeyboardEvents.Where(sequenceHandler).ObserveOn(ThreadPoolScheduler.Instance).Subscribe(keyboardHookEventArgs =>
               {
                   Log.Info().WriteLine("Key combination was pressed, slow handling!", null);
                   Thread.Sleep(pressHandlingTime);
                   Log.Info().WriteLine("Key combination was pressed, finished!", null);
                   Interlocked.Increment(ref pressCount);
               }))
        {
            Log.Info().WriteLine("Pressing key combination", null);
            KeyboardInputGenerator.KeyCombinationPress(VirtualKeyCode.Shift, VirtualKeyCode.KeyA);
            Log.Info().WriteLine("Pressed key combination", null);
            Log.Info().WriteLine("Pressing key combination", null);
            KeyboardInputGenerator.KeyCombinationPress(VirtualKeyCode.Shift, VirtualKeyCode.KeyA);
            Log.Info().WriteLine("Pressed key combination", null);
            // Both presses are handled one after the other, this takes 2x the handling time
            await TestWait.UntilAsync(() => Volatile.Read(ref pressCount) == 2, "The slow subscriber didn't handle both key combinations", TimeSpan.FromSeconds(10));
        }
    }

    [StaFact]
    public async Task TestKeyHandler_Sequence_InputGenerator()
    {
        int pressCount = 0;
        var sequenceHandler = new KeySequenceHandler(
            new KeyCombinationHandler(VirtualKeyCode.Print) { IgnoreInjected = false },
            new KeyCombinationHandler(VirtualKeyCode.Shift, VirtualKeyCode.KeyA) { IgnoreInjected = false });

        using (KeyboardHook.KeyboardEvents.Where(sequenceHandler).Subscribe(keyboardHookEventArgs => pressCount++))
        using (var counter = new InjectedKeyCounter())
        {
            KeyboardInputGenerator.KeyCombinationPress(VirtualKeyCode.Print);
            await counter.WaitForAsync(2);
            Assert.Equal(0, pressCount);
            KeyboardInputGenerator.KeyCombinationPress(VirtualKeyCode.Shift, VirtualKeyCode.KeyB);
            await counter.WaitForAsync(6);
            Assert.Equal(0, pressCount);
            KeyboardInputGenerator.KeyCombinationPress(VirtualKeyCode.Print);
            await counter.WaitForAsync(8);
            KeyboardInputGenerator.KeyCombinationPress(VirtualKeyCode.Shift, VirtualKeyCode.KeyA);
            await counter.WaitForAsync(12);
            Assert.Equal(1, pressCount);
        }
    }

    [StaFact]
    public async Task TestKeyHandler_SequenceWithOptionalKeys_KeyboardInputGenerator()
    {
        int pressCount = 0;
        var sequenceHandler = new KeySequenceHandler(
            new KeyCombinationHandler(VirtualKeyCode.Print) {IgnoreInjected = false},
            new KeyOrCombinationHandler(
                new KeyCombinationHandler(VirtualKeyCode.Shift, VirtualKeyCode.KeyA) { IgnoreInjected = false },
                new KeyCombinationHandler(VirtualKeyCode.Shift, VirtualKeyCode.KeyB) { IgnoreInjected = false })
        )
        {
            // Timeout for test
            Timeout = TimeSpan.FromMilliseconds(200)
        };

        using (KeyboardHook.KeyboardEvents.Where(sequenceHandler).Subscribe(keyboardHookEventArgs => pressCount++))
        using (var counter = new InjectedKeyCounter())
        {
            KeyboardInputGenerator.KeyCombinationPress(VirtualKeyCode.Print);
            await counter.WaitForAsync(2);
            Assert.Equal(0, pressCount);
            KeyboardInputGenerator.KeyCombinationPress(VirtualKeyCode.Shift, VirtualKeyCode.KeyB);
            await counter.WaitForAsync(6);
            Assert.Equal(1, pressCount);
            KeyboardInputGenerator.KeyCombinationPress(VirtualKeyCode.Print);
            await counter.WaitForAsync(8);
            KeyboardInputGenerator.KeyCombinationPress(VirtualKeyCode.Shift, VirtualKeyCode.KeyA);
            await counter.WaitForAsync(12);
            Assert.Equal(2, pressCount);

            // Test with timeout, waiting too long between the keys of the sequence (this delay is part of the test)
            KeyboardInputGenerator.KeyCombinationPress(VirtualKeyCode.Print);
            await counter.WaitForAsync(14);
            await Task.Delay(400, TestContext.Current.CancellationToken);
            KeyboardInputGenerator.KeyCombinationPress(VirtualKeyCode.Shift, VirtualKeyCode.KeyA);
            await counter.WaitForAsync(18);
            Assert.Equal(2, pressCount);
        }
    }

    /// <summary>
    /// A handler keeps state, using the same instance in two subscriptions at the same time must fail instead of silently not working
    /// </summary>
    [Fact]
    public void TestKeyHandler_SameInstanceTwice_Fails()
    {
        var keyHandler = new KeyCombinationHandler(VirtualKeyCode.Back, VirtualKeyCode.RightShift) { IgnoreInjected = false };
        Exception error = null;
        using (KeyboardHook.KeyboardEvents.Where(keyHandler).Subscribe(_ => { }))
        using (KeyboardHook.KeyboardEvents.Where(keyHandler).Subscribe(_ => { }, exception => error = exception))
        {
            Assert.IsType<InvalidOperationException>(error);
        }
        // After disposing, the handler can be used again
        error = null;
        using (KeyboardHook.KeyboardEvents.Where(keyHandler).Subscribe(_ => { }, exception => error = exception))
        {
            Assert.Null(error);
        }
    }

    /// <summary>
    /// The factory overload creates a handler for every subscription
    /// </summary>
    [StaFact]
    public async Task TestKeyHandler_Factory_PerSubscription()
    {
        int pressCount1 = 0;
        int pressCount2 = 0;
        var observable = KeyboardHook.KeyboardEvents.Where(() => new KeyCombinationHandler(VirtualKeyCode.Back, VirtualKeyCode.RightShift) { IgnoreInjected = false });
        using (observable.Subscribe(_ => pressCount1++))
        using (observable.Subscribe(_ => pressCount2++))
        using (var counter = new InjectedKeyCounter())
        {
            KeyboardInputGenerator.KeyCombinationPress(VirtualKeyCode.Back, VirtualKeyCode.RightShift);
            await counter.WaitForAsync(4);
        }
        Assert.Equal(1, pressCount1);
        Assert.Equal(1, pressCount2);
    }
}