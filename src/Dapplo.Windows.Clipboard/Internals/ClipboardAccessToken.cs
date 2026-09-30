// Copyright (c) Dapplo and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
using System;
using System.Threading;

namespace Dapplo.Windows.Clipboard.Internals;

/// <summary>
/// This is the clipboard access token
/// </summary>
internal sealed class ClipboardAccessToken : IClipboardAccessToken
{
    private Action _disposeAction;
    private volatile bool _canAccess = true;

    public ClipboardAccessToken(Action disposeAction = null)
    {
        _disposeAction = disposeAction;
        OwnerThreadId = Environment.CurrentManagedThreadId;
    }

    /// <summary>
    /// The managed thread ID of the thread which opened the clipboard, the clipboard can only be used from this thread.
    /// </summary>
    internal int OwnerThreadId { get; }

    /// <summary>
    /// The window handle which was passed to OpenClipboard, this window becomes the clipboard owner when the contents are cleared.
    /// </summary>
    internal IntPtr OwnerHandle { get; set; }

    /// <inheritdoc />
    /// <exception cref="InvalidOperationException">When the token of an open clipboard is disposed on another thread than the one which opened it, the clipboard stays open.</exception>
    public void Dispose()
    {
        // Win32 can only close the clipboard on the thread which opened it: closing it elsewhere fails and would leave it open for good.
        // Throw instead, the token stays valid so it can still be disposed on the owner thread.
        if (Volatile.Read(ref _disposeAction) != null && !IsOnOwnerThread)
        {
            throw new InvalidOperationException($"The clipboard was opened on thread {OwnerThreadId} and can only be closed there, but the access token was disposed on thread {Environment.CurrentManagedThreadId}. The clipboard stays open until the token is disposed on thread {OwnerThreadId}. Don't await or switch threads while holding the token, use ClipboardNative.UseAsync.");
        }
        _canAccess = false;
        // Take the action out atomically, so a second (or concurrent) Dispose never closes the clipboard or releases the lock again
        Interlocked.Exchange(ref _disposeAction, null)?.Invoke();
    }

    /// <inheritdoc />
    public bool CanAccess
    {
        get => _canAccess && IsOnOwnerThread;
        internal set => _canAccess = value;
    }

    /// <inheritdoc />
    public bool IsOpenTimeout { get; internal set; }

    /// <inheritdoc />
    public bool IsLockTimeout { get; internal set; }

    /// <inheritdoc />
    public IntPtr BlockingWindow { get; private set; }

    /// <inheritdoc />
    public int BlockingProcessId { get; private set; }

    /// <summary>
    /// When <see cref="IsOpenTimeout"/>: the name of the blocking application, determined once when opening failed,
    /// see <see cref="ClipboardAccessTokenExtensions.GetBlockingProcessName"/>
    /// </summary>
    internal string BlockingProcessName { get; private set; }

    /// <summary>
    /// The description of the blocker for the exception message, determined together with <see cref="BlockingProcessName"/>
    /// </summary>
    private string _blockerDescription;

    /// <summary>
    /// Create a token for a failed open, with the window which blocks the clipboard.
    /// The blocker is described right away (the process may be gone later), and only once.
    /// </summary>
    internal static ClipboardAccessToken OpenTimeout(ClipboardBlocker blocker)
    {
        var description = blocker.Describe(out var processName);
        return new ClipboardAccessToken
        {
            CanAccess = false,
            IsOpenTimeout = true,
            BlockingWindow = blocker.Window,
            BlockingProcessId = blocker.ProcessId,
            BlockingProcessName = processName,
            _blockerDescription = description
        };
    }

    /// <summary>
    /// Create the exception for a failed open, the blocker is described once for the message and <see cref="ClipboardAccessDeniedException.BlockingProcessName"/>
    /// </summary>
    internal static ClipboardAccessDeniedException CreateOpenTimeoutException(ClipboardBlocker blocker)
    {
        var description = blocker.Describe(out var processName);
        return CreateOpenTimeoutException(blocker.Window, blocker.ProcessId, processName, description);
    }

    /// <summary>
    /// Create the exception for a failed open
    /// </summary>
    internal static ClipboardAccessDeniedException CreateOpenTimeoutException(IntPtr blockingWindow, int blockingProcessId, string blockingProcessName, string description) =>
        new("The clipboard couldn't be opened for usage, it's probably locked by another process." + description)
        {
            IsOpenTimeout = true,
            BlockingWindow = blockingWindow,
            BlockingProcessId = blockingProcessId,
            BlockingProcessName = blockingProcessName
        };

    /// <summary>
    /// Create the exception for a timeout of the in-process lock
    /// </summary>
    internal static ClipboardAccessDeniedException CreateLockTimeoutException() =>
        new("The clipboard was already locked by another thread or task in your application, a timeout occured.")
        {
            IsLockTimeout = true
        };

    /// <summary>
    /// True if the current thread is the thread which opened the clipboard
    /// </summary>
    private bool IsOnOwnerThread => OwnerThreadId == Environment.CurrentManagedThreadId;

    /// <inheritdoc />
    public void ThrowWhenNoAccess()
    {
        if (CanAccess)
        {
            return;
        }

        if (IsLockTimeout)
        {
            throw CreateLockTimeoutException();
        }
        if (IsOpenTimeout)
        {
            // The blocker was described when opening failed, don't query the process again
            throw CreateOpenTimeoutException(BlockingWindow, BlockingProcessId, BlockingProcessName, _blockerDescription ?? "");
        }
        if (_canAccess)
        {
            throw new InvalidOperationException($"The clipboard was opened on thread {OwnerThreadId} but is used on thread {Environment.CurrentManagedThreadId}. The clipboard can only be used on the thread which opened it, don't await, ObserveOn or otherwise switch threads while holding the access token; use ClipboardNative.UseAsync.");
        }
        throw new ClipboardAccessDeniedException("The clipboard is no longer locked, please check your disposing code.");
    }
}
