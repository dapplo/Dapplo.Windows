// Copyright (c) Dapplo and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
using System;

namespace Dapplo.Windows.Clipboard;

/// <summary>
/// The clipboard couldn't be accessed: another application keeps it open (<see cref="IsOpenTimeout"/>, see <see cref="BlockingWindow"/>,
/// <see cref="BlockingProcessId"/> and <see cref="BlockingProcessName"/>), or another thread of this application uses it (<see cref="IsLockTimeout"/>).
/// </summary>
public class ClipboardAccessDeniedException : Exception
{
    /// <summary>
    /// The window which had the clipboard open when opening it failed, IntPtr.Zero when unknown.
    /// Use it to tell the user which application blocks the clipboard.
    /// </summary>
    public IntPtr BlockingWindow { get; internal set; }

    /// <summary>
    /// The process ID of <see cref="BlockingWindow"/>, 0 when unknown
    /// </summary>
    public int BlockingProcessId { get; internal set; }

    /// <summary>
    /// The application which kept the clipboard open, to show to the user: the file name of its executable (e.g. "notepad.exe"),
    /// else the process name, else the title of <see cref="BlockingWindow"/>; null when unknown.
    /// It was determined when opening failed, together with the message.
    /// </summary>
    public string BlockingProcessName { get; internal set; }

    /// <summary>
    /// True when the clipboard couldn't be opened because another application (or window) kept it open
    /// </summary>
    public bool IsOpenTimeout { get; internal set; }

    /// <summary>
    /// True when another thread or task of this application held the in-process clipboard lock too long
    /// </summary>
    public bool IsLockTimeout { get; internal set; }

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