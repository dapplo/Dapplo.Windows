// Copyright (c) Dapplo and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
using System;
using System.ComponentModel;
using System.Runtime.InteropServices;
using Dapplo.Windows.Kernel32;

namespace Dapplo.Windows.Clipboard.Internals;

/// <summary>
/// This class contains native information to handle the clipboard contents.
/// For writing: fill the memory, call <see cref="Commit"/> to place it on the clipboard, and dispose.
/// If <see cref="Commit"/> is not called (e.g. because writing the payload failed), the memory is freed and nothing is placed on the clipboard.
/// </summary>
internal sealed class ClipboardNativeInfo : IDisposable
{
    private bool _isLocked = true;
    private bool _isCommitted;
    private bool _isDisposed;

    internal IntPtr GlobalHandle { get; set; }
    internal bool NeedsWrite { get; set; }

    /// <summary>
    /// The format id which is processed
    /// </summary>
    internal uint FormatId { get; set; }
    internal IntPtr MemoryPtr { get; set; }

    /// <summary>
    /// Returns the size of the clipboard area, this is the size of the allocation which can be larger than the actual data.
    /// </summary>
    internal long Size => (long)Kernel32Api.GlobalSize(GlobalHandle).ToUInt64();

    /// <summary>
    /// Place the written memory on the clipboard, after this the memory is owned by the system.
    /// </summary>
    /// <exception cref="Win32Exception">When SetClipboardData failed, the memory is freed in that case</exception>
    internal void Commit()
    {
        if (!NeedsWrite)
        {
            throw new InvalidOperationException("Only clipboard write information can be committed.");
        }
        if (_isCommitted || _isDisposed)
        {
            throw new InvalidOperationException("The clipboard write information was already committed or disposed.");
        }
        // Unlock before handing the memory over to the system
        Unlock();
        if (NativeMethods.SetClipboardData(FormatId, GlobalHandle) == IntPtr.Zero)
        {
            var error = Marshal.GetLastWin32Error();
            FreeMemory();
            throw new Win32Exception(error, $"Placing clipboard format {FormatId} on the clipboard failed: {new Win32Exception(error).Message}");
        }
        _isCommitted = true;
    }

    /// <summary>
    /// Cleanup this native info by unlocking the global handle, uncommitted write memory is freed.
    /// </summary>
    public void Dispose()
    {
        if (_isDisposed)
        {
            return;
        }
        _isDisposed = true;
        Unlock();
        if (NeedsWrite && !_isCommitted)
        {
            FreeMemory();
        }
    }

    private void Unlock()
    {
        if (!_isLocked)
        {
            return;
        }
        _isLocked = false;
        Kernel32Api.GlobalUnlock(GlobalHandle);
    }

    private void FreeMemory()
    {
        if (GlobalHandle == IntPtr.Zero)
        {
            return;
        }
        NativeMethods.GlobalFree(GlobalHandle);
        GlobalHandle = IntPtr.Zero;
        MemoryPtr = IntPtr.Zero;
    }
}
