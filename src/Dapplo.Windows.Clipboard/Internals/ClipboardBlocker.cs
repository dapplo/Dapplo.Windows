// Copyright (c) Dapplo and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
using System;
using System.Diagnostics;

namespace Dapplo.Windows.Clipboard.Internals;

/// <summary>
/// Information on the window which keeps the clipboard open, taken when opening the clipboard failed
/// </summary>
internal readonly struct ClipboardBlocker
{
    private ClipboardBlocker(IntPtr window, int processId)
    {
        Window = window;
        ProcessId = processId;
    }

    /// <summary>
    /// The window which has the clipboard open, IntPtr.Zero when unknown
    /// </summary>
    public IntPtr Window { get; }

    /// <summary>
    /// The process ID of that window, 0 when unknown
    /// </summary>
    public int ProcessId { get; }

    /// <summary>
    /// Find out who has the clipboard open right now
    /// </summary>
    public static ClipboardBlocker Detect()
    {
        var window = NativeMethods.GetOpenClipboardWindow();
        if (window == IntPtr.Zero)
        {
            return default;
        }
        NativeMethods.GetWindowThreadProcessId(window, out var processId);
        return new ClipboardBlocker(window, unchecked((int)processId));
    }

    /// <summary>
    /// A description for the exception message, e.g. " It is in use by window 0x1234 of process 42 (notepad)."
    /// </summary>
    public string Describe()
    {
        if (Window == IntPtr.Zero)
        {
            return " The window which has it open is unknown (it was opened without a window, or was closed again).";
        }
        string processName = null;
        if (ProcessId != 0)
        {
            try
            {
                using var process = Process.GetProcessById(ProcessId);
                processName = process.ProcessName;
            }
            catch (Exception)
            {
                // The process exited, or we may not query it
            }
        }
        return processName == null
            ? $" It is in use by window 0x{Window.ToInt64():X} of process {ProcessId}."
            : $" It is in use by window 0x{Window.ToInt64():X} of process {ProcessId} ({processName}).";
    }
}
