// Copyright (c) Dapplo and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
using System;

namespace Dapplo.Windows.Clipboard;

/// <inheritdoc />
public class ClipboardAccessDeniedException : Exception
{
    /// <inheritdoc />
    public ClipboardAccessDeniedException()
    {
    }

    /// <inheritdoc />
    public ClipboardAccessDeniedException(string message) : base(message)
    {
    }

    /// <inheritdoc />
    public ClipboardAccessDeniedException(string message, Exception inner) : base(message, inner)
    {
    }
}