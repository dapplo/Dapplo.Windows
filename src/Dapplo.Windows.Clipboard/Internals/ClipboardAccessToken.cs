// Copyright (c) Dapplo and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
using System;

namespace Dapplo.Windows.Clipboard.Internals;

/// <summary>
/// This is the clipboard access token
/// </summary>
internal class ClipboardAccessToken : IClipboardAccessToken
{
    private readonly Action _disposeAction;

    public ClipboardAccessToken(Action disposeAction = null)
    {
        _disposeAction = disposeAction;
    }

    /// <inheritdoc />
    public void Dispose()
    {
        CanAccess = false;
        _disposeAction?.Invoke();
    }

    /// <inheritdoc />
    public bool CanAccess { get; internal set; } = true;

    /// <inheritdoc />
    public bool IsOpenTimeout { get; internal set; }

    /// <inheritdoc />
    public bool IsLockTimeout { get; internal set; }

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
        throw new ClipboardAccessDeniedException("The clipboard is no longer locked, please check your disposing code.");
    }
}