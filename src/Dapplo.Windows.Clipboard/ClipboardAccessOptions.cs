// Copyright (c) Dapplo and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
using System;

namespace Dapplo.Windows.Clipboard;

/// <summary>
/// Options for opening the clipboard with <see cref="ClipboardNative.UseAsync{T}(Func{IClipboardAccessToken, T}, ClipboardAccessOptions, System.Threading.CancellationToken)"/>.
/// </summary>
public sealed class ClipboardAccessOptions
{
    /// <summary>
    /// The window which becomes the clipboard owner when the contents are cleared.
    /// <see cref="IntPtr.Zero"/> (default) uses the SharedMessageWindow, which also handles delayed rendering.
    /// </summary>
    public IntPtr Owner { get; set; }

    /// <summary>
    /// How often opening the clipboard is retried when another application has it open, default 5.
    /// </summary>
    public int Retries { get; set; } = 5;

    /// <summary>
    /// The time between two attempts to open the clipboard, default 100ms. The waiting is asynchronous.
    /// </summary>
    public TimeSpan RetryInterval { get; set; } = TimeSpan.FromMilliseconds(100);

    /// <summary>
    /// How long to wait for the in-process lock, while another thread or task of this application uses the clipboard, default 200ms.
    /// </summary>
    public TimeSpan LockTimeout { get; set; } = TimeSpan.FromMilliseconds(200);

    /// <summary>
    /// Throws when a value is out of range
    /// </summary>
    internal void Validate()
    {
        if (Retries < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(Retries), Retries, "Retries must not be negative.");
        }
        if (RetryInterval < TimeSpan.Zero)
        {
            throw new ArgumentOutOfRangeException(nameof(RetryInterval), RetryInterval, "RetryInterval must not be negative.");
        }
        if (LockTimeout < TimeSpan.Zero && LockTimeout != System.Threading.Timeout.InfiniteTimeSpan)
        {
            throw new ArgumentOutOfRangeException(nameof(LockTimeout), LockTimeout, "LockTimeout must not be negative, except Timeout.InfiniteTimeSpan.");
        }
    }
}
