// Copyright (c) Dapplo and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
using System;
using System.Diagnostics;
using System.Reactive.Linq;
using System.Threading;
using System.Threading.Tasks;
using Dapplo.Windows.Input.Keyboard;
using Xunit;

namespace Dapplo.Windows.Tests;

/// <summary>
/// Waits for a condition instead of using fixed delays, so the tests don't depend on the speed of the machine
/// </summary>
internal static class TestWait
{
    /// <summary>
    /// The default time to wait for a condition
    /// </summary>
    public static readonly TimeSpan DefaultTimeout = TimeSpan.FromSeconds(5);

    /// <summary>
    /// Poll the condition until it is true, fail the test when this takes longer than the timeout
    /// </summary>
    /// <param name="condition">Func which returns true when the wait is over</param>
    /// <param name="message">string for the failure</param>
    /// <param name="timeout">TimeSpan, default is <see cref="DefaultTimeout"/></param>
    public static async Task UntilAsync(Func<bool> condition, string message, TimeSpan? timeout = null)
    {
        var maxWait = timeout ?? DefaultTimeout;
        var stopwatch = Stopwatch.StartNew();
        while (!condition())
        {
            if (stopwatch.Elapsed > maxWait)
            {
                Assert.Fail($"{message} (waited {maxWait.TotalMilliseconds} ms)");
            }
            await Task.Delay(10, TestContext.Current.CancellationToken);
        }
    }

    /// <summary>
    /// Wait for the task, fail the test when this takes longer than the timeout
    /// </summary>
    /// <typeparam name="T">Type of the result</typeparam>
    /// <param name="task">Task to wait for</param>
    /// <param name="message">string for the failure</param>
    /// <param name="timeout">TimeSpan, default is <see cref="DefaultTimeout"/></param>
    /// <returns>T</returns>
    public static async Task<T> ForAsync<T>(Task<T> task, string message, TimeSpan? timeout = null)
    {
        var maxWait = timeout ?? DefaultTimeout;
        var completed = await Task.WhenAny(task, Task.Delay(maxWait, TestContext.Current.CancellationToken));
        if (completed != task)
        {
            Assert.Fail($"{message} (waited {maxWait.TotalMilliseconds} ms)");
        }
        return await task;
    }
}

/// <summary>
/// Counts the injected keyboard events which the low-level keyboard hook has processed.
/// The hook calls its observers in the order they subscribed, create this after the subscription which is tested:
/// when the count is reached, that subscription has seen all these events too.
/// </summary>
internal sealed class InjectedKeyCounter : IDisposable
{
    private readonly IDisposable _subscription;
    private int _count;

    public InjectedKeyCounter()
    {
        _subscription = KeyboardHook.KeyboardEvents
            .Where(keyboardHookEventArgs => keyboardHookEventArgs.IsInjectedByProcess)
            .Subscribe(_ => Interlocked.Increment(ref _count));
    }

    /// <summary>
    /// The number of injected keyboard events so far
    /// </summary>
    public int Count => Volatile.Read(ref _count);

    /// <summary>
    /// Wait until the hook has processed the specified number of injected keyboard events
    /// </summary>
    /// <param name="expectedCount">int, a key press is two events (down and up)</param>
    public Task WaitForAsync(int expectedCount) => TestWait.UntilAsync(() => Count >= expectedCount, $"Expected {expectedCount} injected keyboard events, the hook processed {Count}");

    public void Dispose() => _subscription.Dispose();
}
