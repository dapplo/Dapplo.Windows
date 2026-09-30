// Copyright (c) Dapplo and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
using System;
using System.Runtime.InteropServices;
using Dapplo.Log;
using Dapplo.Windows.User32.Enums;
using Dapplo.Windows.User32.SafeHandles;

namespace Dapplo.Windows.User32;

/// <summary>
///     Switches the calling thread to the current input desktop, and restores the previous desktop of the thread when disposed.
///     This must be created and disposed on the same thread. Note: SetThreadDesktop fails when the thread already owns a window or a hook,
///     which is typical for a UI thread, so use a dedicated thread. Check <see cref="IsSwitched"/> to see if the switch worked.
/// </summary>
public sealed class ThreadDesktopScope : IDisposable
{
    private static readonly LogSource Log = new LogSource();
    private readonly SafeDesktopHandle _desktopHandle;
    private readonly IntPtr _previousDesktop;
    private readonly int _managedThreadId;
    private bool _disposed;

    private ThreadDesktopScope(SafeDesktopHandle desktopHandle, IntPtr previousDesktop, bool isSwitched)
    {
        _desktopHandle = desktopHandle;
        _previousDesktop = previousDesktop;
        _managedThreadId = Environment.CurrentManagedThreadId;
        IsSwitched = isSwitched;
    }

    /// <summary>
    ///     True if the thread was switched to the input desktop
    /// </summary>
    public bool IsSwitched { get; }

    /// <summary>
    ///     Switch the calling thread to the current input desktop
    /// </summary>
    /// <param name="desiredAccess">DesktopAccessRight for opening the input desktop, default is <see cref="SafeDesktopHandle.DefaultInputDesktopAccess"/></param>
    /// <returns>ThreadDesktopScope, dispose it on the same thread to switch back</returns>
    public static ThreadDesktopScope SwitchToInputDesktop(DesktopAccessRight desiredAccess = SafeDesktopHandle.DefaultInputDesktopAccess)
    {
        // The handle from GetThreadDesktop doesn't need to be closed
        var previousDesktop = User32Api.GetThreadDesktop(GetCurrentThreadId());
        var desktopHandle = SafeDesktopHandle.OpenInputDesktop(desiredAccess);
        var isSwitched = false;
        if (!desktopHandle.IsInvalid)
        {
            isSwitched = User32Api.SetThreadDesktop(desktopHandle);
            if (!isSwitched)
            {
                Log.Warn().WriteLine(User32Api.CreateWin32Exception("SetThreadDesktop"), "Couldn't switch to the input desktop.");
            }
        }
        return new ThreadDesktopScope(desktopHandle, previousDesktop, isSwitched);
    }

    /// <summary>
    ///     Restore the previous desktop of the thread, and close the input desktop handle
    /// </summary>
    /// <exception cref="InvalidOperationException">when called from another thread than the one which created the scope</exception>
    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }
        if (IsSwitched)
        {
            if (Environment.CurrentManagedThreadId != _managedThreadId)
            {
                throw new InvalidOperationException("A ThreadDesktopScope must be disposed on the thread which created it.");
            }
            if (!User32Api.SetThreadDesktop(_previousDesktop))
            {
                Log.Warn().WriteLine(User32Api.CreateWin32Exception("SetThreadDesktop"), "Couldn't restore the previous desktop.");
            }
        }
        _disposed = true;
        // Only now the desktop is no longer in use by this thread, and CloseDesktop can succeed
        _desktopHandle.Dispose();
    }

    [DllImport("kernel32", ExactSpelling = true)]
    private static extern int GetCurrentThreadId();
}
