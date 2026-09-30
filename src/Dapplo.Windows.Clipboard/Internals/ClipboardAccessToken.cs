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
            throw new ClipboardAccessDeniedException("The clipboard was already locked by another thread or task in your application, a timeout occured.");
        }
        if (IsOpenTimeout)
        {
            throw new ClipboardAccessDeniedException("The clipboard couldn't be opened for usage, it's probably locked by another process");
        }
        if (_canAccess)
        {
            throw new InvalidOperationException($"The clipboard was opened on thread {OwnerThreadId} but is used on thread {Environment.CurrentManagedThreadId}. The clipboard can only be used on the thread which opened it, don't await, ObserveOn or otherwise switch threads while holding the access token; use ClipboardNative.UseAsync.");
        }
        throw new ClipboardAccessDeniedException("The clipboard is no longer locked, please check your disposing code.");
    }
}
