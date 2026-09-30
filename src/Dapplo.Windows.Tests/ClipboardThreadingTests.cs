// Copyright (c) Dapplo and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using Dapplo.Windows.Clipboard;
using Xunit;

namespace Dapplo.Windows.Tests;

/// <summary>
/// Thread affinity of the clipboard access: Windows ties an opened clipboard to the thread which opened it.
/// These tests force the retry path, where the waiting for the clipboard is asynchronous.
/// </summary>
/// <remarks>Interactive: these tests change the clipboard. They are excluded by default, run them with --filter Category=Interactive.</remarks>
[Trait("Category", "Interactive")]
public class ClipboardThreadingTests
{
    [DllImport("user32", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool OpenClipboard(IntPtr hWndNewOwner);

    [DllImport("user32", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool CloseClipboard();

    /// <summary>
    /// Keeps the clipboard open on a separate thread, bypassing the in-process lock of Dapplo.Windows.Clipboard,
    /// like another application would. Returns when the clipboard is open, the returned task completes when it's closed again.
    /// </summary>
    internal static Task BlockClipboard(TimeSpan duration, IntPtr owner = default)
    {
        var opened = new ManualResetEventSlim();
        var closed = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        var blocker = new Thread(() =>
        {
            try
            {
                var deadline = DateTime.UtcNow.AddSeconds(5);
                while (!OpenClipboard(owner))
                {
                    if (DateTime.UtcNow > deadline)
                    {
                        throw new InvalidOperationException("The test couldn't open the clipboard to block it.");
                    }
                    Thread.Sleep(10);
                }
                opened.Set();
                Thread.Sleep(duration);
                CloseClipboard();
                closed.SetResult(true);
            }
            catch (Exception ex)
            {
                closed.TrySetException(ex);
                opened.Set();
            }
        })
        {
            IsBackground = true,
            Name = "ClipboardBlocker"
        };
        blocker.Start();
        opened.Wait(TimeSpan.FromSeconds(10));
        if (closed.Task.IsFaulted)
        {
            closed.Task.GetAwaiter().GetResult();
        }
        return closed.Task;
    }

    /// <summary>
    /// AccessAsync from a UI context which has to retry: the token must be usable after the await, and disposing it must close the clipboard.
    /// </summary>
    [WpfFact]
    public async Task AccessAsync_RetryPath_UiContext_TokenIsUsableAndClosesTheClipboard()
    {
        const string text = "Dapplo.Windows.Tests.ClipboardThreadingTests.AccessAsync.Ui";
        Assert.NotNull(SynchronizationContext.Current);
        var blocked = BlockClipboard(TimeSpan.FromMilliseconds(300));

        using (var clipboardAccessToken = await ClipboardNative.AccessAsync(retries: 20))
        {
            Assert.True(blocked.IsCompleted, "The clipboard should only be opened after the blocker closed it (retry path).");
            Assert.True(clipboardAccessToken.CanAccess);
            clipboardAccessToken.ClearContents();
            clipboardAccessToken.SetAsUnicodeString(text);
            Assert.Equal(text, clipboardAccessToken.GetAsUnicodeString());
        }
        await blocked;

        // The clipboard must be closed: another thread can open it
        Assert.True(await Task.Run(() => OpenAndCloseOnThisThread()));
    }

    /// <summary>
    /// AccessAsync without a SynchronizationContext, the caller uses ConfigureAwait(false)
    /// </summary>
    [Fact]
    public async Task AccessAsync_RetryPath_NoContext_TokenIsUsableAndClosesTheClipboard()
    {
        const string text = "Dapplo.Windows.Tests.ClipboardThreadingTests.AccessAsync.NoContext";
        await Task.Run(async () =>
        {
            var blocked = BlockClipboard(TimeSpan.FromMilliseconds(300));
            using (var clipboardAccessToken = await ClipboardNative.AccessAsync(retries: 20).ConfigureAwait(false))
            {
                Assert.True(blocked.IsCompleted, "The clipboard should only be opened after the blocker closed it (retry path).");
                Assert.True(clipboardAccessToken.CanAccess);
                clipboardAccessToken.ClearContents();
                clipboardAccessToken.SetAsUnicodeString(text);
                Assert.Equal(text, clipboardAccessToken.GetAsUnicodeString());
            }
            await blocked.ConfigureAwait(false);
        });
        Assert.True(await Task.Run(() => OpenAndCloseOnThisThread()));
    }

    /// <summary>
    /// UseAsync from a UI context which has to retry: the work runs on the UI thread, and the clipboard is closed afterwards
    /// </summary>
    [WpfFact]
    public async Task UseAsync_RetryPath_UiContext_WorkRunsOnCallerThread()
    {
        const string text = "Dapplo.Windows.Tests.ClipboardThreadingTests.UseAsync.Ui";
        var uiThreadId = Environment.CurrentManagedThreadId;
        var blocked = BlockClipboard(TimeSpan.FromMilliseconds(300));

        var workThreadId = await ClipboardNative.UseAsync(clipboard =>
        {
            Assert.True(blocked.IsCompleted, "The clipboard should only be opened after the blocker closed it (retry path).");
            clipboard.ClearContents();
            clipboard.SetAsUnicodeString(text);
            return Environment.CurrentManagedThreadId;
        }, new ClipboardAccessOptions { Retries = 20 });
        await blocked;

        Assert.Equal(uiThreadId, workThreadId);
        var read = await ClipboardNative.UseAsync(clipboard => clipboard.GetAsUnicodeString());
        Assert.Equal(text, read);
        Assert.True(await Task.Run(() => OpenAndCloseOnThisThread()));
    }

    /// <summary>
    /// UseAsync without a SynchronizationContext, the continuation can end up on any thread but open, work and close happen on one
    /// </summary>
    [Fact]
    public async Task UseAsync_RetryPath_NoContext_Succeeds()
    {
        const string text = "Dapplo.Windows.Tests.ClipboardThreadingTests.UseAsync.NoContext";
        await Task.Run(async () =>
        {
            var blocked = BlockClipboard(TimeSpan.FromMilliseconds(300));
            await ClipboardNative.UseAsync(clipboard =>
            {
                clipboard.ClearContents();
                clipboard.SetAsUnicodeString(text);
            }, new ClipboardAccessOptions { Retries = 20 }).ConfigureAwait(false);
            await blocked.ConfigureAwait(false);
            var read = await ClipboardNative.UseAsync(clipboard => clipboard.GetAsUnicodeString()).ConfigureAwait(false);
            Assert.Equal(text, read);
        });
        Assert.True(await Task.Run(() => OpenAndCloseOnThisThread()));
    }

    /// <summary>
    /// UseAsync throws a ClipboardAccessDeniedException when the clipboard stays blocked, and releases the in-process lock
    /// </summary>
    [WpfFact]
    public async Task UseAsync_StaysBlocked_Throws()
    {
        var blocked = BlockClipboard(TimeSpan.FromMilliseconds(500));
        await Assert.ThrowsAsync<ClipboardAccessDeniedException>(() => ClipboardNative.UseAsync(_ => { }, new ClipboardAccessOptions { Retries = 1, RetryInterval = TimeSpan.FromMilliseconds(20) }));
        await blocked;
        // The in-process lock is released again
        Assert.True(await ClipboardNative.UseAsync(clipboard => clipboard.CanAccess));
    }

    /// <summary>
    /// UseAsync can be cancelled while waiting
    /// </summary>
    [WpfFact]
    public async Task UseAsync_Cancelled_WhileWaiting()
    {
        var blocked = BlockClipboard(TimeSpan.FromMilliseconds(500));
        using var cts = new CancellationTokenSource(TimeSpan.FromMilliseconds(100));
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => ClipboardNative.UseAsync(_ => { }, new ClipboardAccessOptions { Retries = 50 }, cts.Token));
        await blocked;
        Assert.True(await ClipboardNative.UseAsync(clipboard => clipboard.CanAccess));
    }

    /// <summary>
    /// An exception in the work is passed on, and the clipboard is closed
    /// </summary>
    [WpfFact]
    public async Task UseAsync_WorkThrows_ClipboardIsClosed()
    {
        Action<IClipboardAccessToken> work = _ => throw new FormatException();
        await Assert.ThrowsAsync<FormatException>(() => ClipboardNative.UseAsync(work));
        Assert.True(await Task.Run(() => OpenAndCloseOnThisThread()));
    }

    /// <summary>
    /// Work which returns a Task (it would await while the clipboard is open) is rejected at runtime, e.g. when it comes in as a Func&lt;object&gt;
    /// </summary>
    [WpfFact]
    public async Task UseAsync_WorkReturnsTask_Throws()
    {
        Func<IClipboardAccessToken, object> work = _ => Task.CompletedTask;
        await Assert.ThrowsAsync<InvalidOperationException>(() => ClipboardNative.UseAsync(work));
        Assert.True(await Task.Run(() => OpenAndCloseOnThisThread()));
    }

    /// <summary>
    /// Using the token on another thread throws an InvalidOperationException
    /// </summary>
    [WpfFact]
    public async Task Token_UsedOnOtherThread_ThrowsInvalidOperationException()
    {
        using var clipboardAccessToken = ClipboardNative.Access();
        Assert.True(clipboardAccessToken.CanAccess);
        var exception = await Task.Run(() => Record.Exception(() => clipboardAccessToken.GetAsUnicodeString()));
        Assert.IsType<InvalidOperationException>(exception);
        Assert.True(clipboardAccessToken.CanAccess);
    }

    /// <summary>
    /// Disposing the token on another thread throws and leaves the token usable, it can then be disposed on the owner thread
    /// </summary>
    [WpfFact]
    public async Task Token_DisposedOnOtherThread_ThrowsAndStaysOpen()
    {
        var clipboardAccessToken = ClipboardNative.Access();
        Assert.True(clipboardAccessToken.CanAccess);
        var exception = await Task.Run(() => Record.Exception(() => clipboardAccessToken.Dispose()));
        Assert.IsType<InvalidOperationException>(exception);
        Assert.True(clipboardAccessToken.CanAccess);

        clipboardAccessToken.Dispose();
        Assert.False(clipboardAccessToken.CanAccess);
        Assert.True(await Task.Run(() => OpenAndCloseOnThisThread()));
    }

    /// <summary>
    /// Open and close the clipboard via the Win32 API, bypassing the in-process lock
    /// </summary>
    internal static bool OpenAndCloseOnThisThread()
    {
        for (var attempt = 0; attempt < 20; attempt++)
        {
            if (OpenClipboard(IntPtr.Zero))
            {
                return CloseClipboard();
            }
            Thread.Sleep(25);
        }
        return false;
    }
}
