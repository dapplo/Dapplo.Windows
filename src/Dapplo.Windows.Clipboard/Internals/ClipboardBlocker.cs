// Copyright (c) Dapplo and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
using System;
using System.Diagnostics;
using System.IO;
using Dapplo.Windows.Kernel32;

namespace Dapplo.Windows.Clipboard.Internals;

/// <summary>
/// Information on the window which keeps the clipboard open, taken when opening the clipboard failed
/// </summary>
internal readonly struct ClipboardBlocker
{
    internal ClipboardBlocker(IntPtr window, int processId)
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
    /// A description for the exception message, e.g. " It is in use by window 0x1234 of process 42 (notepad.exe).", and the name of the
    /// blocking application. The process is queried once for both.
    /// </summary>
    /// <param name="processName">the file name of the executable, else the process name, else the window title; null when unknown</param>
    /// <returns>string which starts with a space, to append to a message</returns>
    public string Describe(out string processName)
    {
        processName = GetProcessName();
        if (Window == IntPtr.Zero)
        {
            return " The window which has it open is unknown (it was opened without a window, or was closed again).";
        }
        return processName == null
            ? $" It is in use by window 0x{Window.ToInt64():X} of process {ProcessId}."
            : $" It is in use by window 0x{Window.ToInt64():X} of process {ProcessId} ({processName}).";
    }

    /// <summary>
    /// The name of the blocking application: the file name of its executable (QueryFullProcessImageName, which also works for elevated
    /// processes and other bitness), else the process name, else the title of the window; null when none of these is known.
    /// </summary>
    private string GetProcessName()
    {
        if (ProcessId != 0)
        {
            try
            {
                var path = Kernel32Api.GetProcessPath(ProcessId);
                if (!string.IsNullOrEmpty(path))
                {
                    var fileName = Path.GetFileName(path);
                    if (!string.IsNullOrEmpty(fileName))
                    {
                        return fileName;
                    }
                }
            }
            catch (Exception)
            {
                // Try the process name
            }
            try
            {
                using var process = Process.GetProcessById(ProcessId);
                var processName = process.ProcessName;
                if (!string.IsNullOrEmpty(processName))
                {
                    return processName;
                }
            }
            catch (Exception)
            {
                // The process exited, or we may not query it
            }
        }
        if (Window == IntPtr.Zero)
        {
            return null;
        }
        var title = NativeMethods.GetWindowTitle(Window);
        return string.IsNullOrEmpty(title) ? null : title;
    }
}
