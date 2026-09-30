// Copyright (c) Dapplo and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
using System;

namespace Dapplo.Windows.Clipboard;

/// <summary>
/// This interface is returned by ClipboardNative.Access() and ClipboardNative.AccessAsync().
/// When you got a IClipboardAccessToken, you can access the clipboard, until it's disposed. Don't forget to dispose this!!!
/// </summary>
/// <remarks>
/// The clipboard can be used from any thread, no STA thread is needed. But Windows ties an opened clipboard to the thread which opened it:
/// the token can only be used on the thread which opened the clipboard, and must be disposed on that thread (disposing it elsewhere throws an
/// InvalidOperationException and leaves the clipboard open). Never await, ObserveOn or otherwise switch threads while holding the token,
/// keep the usage short: while the token is held, no other application can access the clipboard.
/// Prefer <see cref="ClipboardNative.UseAsync{T}(System.Func{IClipboardAccessToken, T}, ClipboardAccessOptions, System.Threading.CancellationToken)"/>,
/// which guarantees this.
/// </remarks>
public interface IClipboardAccessToken : IDisposable
{
    /// <summary>
    /// Check if the clipboard can be accessed, this is false after disposing and when called from another thread than the one which opened the clipboard
    /// </summary>
    bool CanAccess { get; }

    /// <summary>
    /// The clipboard access was denied due to a timeout
    /// </summary>
    bool IsLockTimeout { get; }

    /// <summary>
    /// The clipboard couldn't be opened
    /// </summary>
    bool IsOpenTimeout { get; }

    /// <summary>
    /// This throws a <see cref="ClipboardAccessDeniedException"/> when the clipboard couldn't be opened or is no longer open,
    /// and an <see cref="System.InvalidOperationException"/> when it's used on another thread than the one which opened it.
    /// </summary>
    void ThrowWhenNoAccess();
}