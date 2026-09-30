// Copyright (c) Dapplo and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.ComponentModel;
using System.Runtime.InteropServices;
using Microsoft.Win32.SafeHandles;

namespace Dapplo.Windows.SystemState;

/// <summary>
/// Keeps the system (and optionally the display) awake while this object is alive, using a power request
/// (PowerCreateRequest / PowerSetRequest). Dispose it to allow the system to sleep again.
/// </summary>
/// <remarks>
/// Unlike SetThreadExecutionState, a power request is not bound to the calling thread: it can be created and disposed on any thread,
/// e.g. before and after an await. Several blockers can be active at the same time, the system can sleep when all of them are disposed
/// (or when the process exits). The reason is shown by <c>powercfg /requests</c>.
/// See <a href="https://learn.microsoft.com/en-us/windows/win32/api/winbase/nf-winbase-powercreaterequest">PowerCreateRequest function</a>
/// </remarks>
public sealed class SleepBlocker : IDisposable
{
    private const uint PowerRequestContextVersion = 0;
    private const uint PowerRequestContextSimpleString = 0x1;

    private enum PowerRequestType
    {
        PowerRequestDisplayRequired = 0,
        PowerRequestSystemRequired = 1,
    }

    /// <summary>
    /// REASON_CONTEXT with POWER_REQUEST_CONTEXT_SIMPLE_STRING, padded to (at least) the size of the native union
    /// </summary>
    [StructLayout(LayoutKind.Sequential)]
    private struct ReasonContext
    {
        public uint Version;
        public uint Flags;
        public IntPtr SimpleReasonString;
        private readonly IntPtr _padding1;
        private readonly IntPtr _padding2;
        private readonly IntPtr _padding3;
    }

    private sealed class PowerRequestHandle : SafeHandleZeroOrMinusOneIsInvalid
    {
        private PowerRequestHandle() : base(true)
        {
        }

        protected override bool ReleaseHandle()
        {
            // Closing the handle also clears all requests which were set with it
            return CloseHandle(handle);
        }
    }

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern PowerRequestHandle PowerCreateRequest(ref ReasonContext context);

    [DllImport("kernel32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool PowerSetRequest(PowerRequestHandle powerRequest, PowerRequestType requestType);

    [DllImport("kernel32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool CloseHandle(IntPtr hObject);

    private readonly PowerRequestHandle _powerRequestHandle;

    /// <summary>
    /// Create a SleepBlocker, the system does not go to sleep due to idleness until this is disposed.
    /// </summary>
    /// <param name="keepDisplayOn">true to also keep the display on</param>
    /// <param name="reason">The reason, shown by <c>powercfg /requests</c>, if null a default text is used</param>
    /// <exception cref="Win32Exception">When the power request could not be created or set</exception>
    public SleepBlocker(bool keepDisplayOn = false, string reason = null)
    {
        KeepsDisplayOn = keepDisplayOn;
        Reason = string.IsNullOrEmpty(reason) ? "The application is busy" : reason;

        var reasonString = Marshal.StringToHGlobalUni(Reason);
        int createError;
        try
        {
            var reasonContext = new ReasonContext
            {
                Version = PowerRequestContextVersion,
                Flags = PowerRequestContextSimpleString,
                SimpleReasonString = reasonString
            };
            _powerRequestHandle = PowerCreateRequest(ref reasonContext);
            createError = Marshal.GetLastWin32Error();
        }
        finally
        {
            Marshal.FreeHGlobal(reasonString);
        }

        if (_powerRequestHandle.IsInvalid)
        {
            throw new Win32Exception(createError, "PowerCreateRequest failed");
        }

        if (!PowerSetRequest(_powerRequestHandle, PowerRequestType.PowerRequestSystemRequired)
            || (keepDisplayOn && !PowerSetRequest(_powerRequestHandle, PowerRequestType.PowerRequestDisplayRequired)))
        {
            var error = Marshal.GetLastWin32Error();
            _powerRequestHandle.Dispose();
            throw new Win32Exception(error, "PowerSetRequest failed");
        }
    }

    /// <summary>
    /// True when this blocker also keeps the display on
    /// </summary>
    public bool KeepsDisplayOn { get; }

    /// <summary>
    /// The reason which was passed to Windows
    /// </summary>
    public string Reason { get; }

    /// <summary>
    /// True until the blocker is disposed
    /// </summary>
    public bool IsActive => !_powerRequestHandle.IsClosed;

    /// <summary>
    /// Allow the system to sleep again (as far as this blocker is concerned), can be called multiple times and from any thread.
    /// </summary>
    public void Dispose()
    {
        _powerRequestHandle.Dispose();
    }
}
