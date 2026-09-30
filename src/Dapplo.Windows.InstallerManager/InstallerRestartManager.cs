// Copyright (c) Dapplo and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading;
using Dapplo.Windows.InstallerManager.Enums;
using Dapplo.Windows.InstallerManager.Structs;

namespace Dapplo.Windows.InstallerManager;

/// <summary>
///     High-level helper class for using the Windows Restart Manager.
///     The Restart Manager helps minimize application downtime during software installations and updates
///     by identifying which applications are using locked files and then shutting them down and restarting them.
/// </summary>
/// <example>
///     Example usage to find processes locking a file:
///     <code>
///     using (var session = InstallerRestartManager.CreateSession())
///     {
///         session.RegisterFile(@"C:\path\to\locked\file.dll");
///         var processes = session.GetProcessesUsingResources();
///         
///         foreach (var process in processes)
///         {
///             Console.WriteLine($"Process {process.AppName} (PID: {process.Process.ProcessId}) is using the file");
///         }
///     }
///     </code>
/// </example>
public sealed class InstallerRestartManager : IDisposable
{
    private const int ErrorMoreData = 234;
    private const int MaxGetListAttempts = 5;

    private int _sessionHandle = RestartManagerApi.RmInvalidSession;
    private readonly string _sessionKey;
    private bool _disposed;

    private InstallerRestartManager(int sessionHandle, string sessionKey)
    {
        _sessionHandle = sessionHandle;
        _sessionKey = sessionKey;
    }

    /// <summary>
    ///     Creates a new Restart Manager session.
    /// </summary>
    /// <returns>A new InstallerRestartManager instance.</returns>
    /// <exception cref="Win32Exception">Thrown when the session could not be started.</exception>
    public static InstallerRestartManager CreateSession()
    {
        var sessionKey = new StringBuilder(RestartManagerApi.RmSessionKeyLen + 1);
        var result = RestartManagerApi.RmStartSession(out var sessionHandle, 0, sessionKey);
        
        if (result != 0)
        {
            throw new Win32Exception(result, "Failed to start Restart Manager session");
        }

        return new InstallerRestartManager(sessionHandle, sessionKey.ToString());
    }

    /// <summary>
    ///     Gets the session key for this Restart Manager session.
    /// </summary>
    public string SessionKey => _sessionKey;

    /// <summary>
    /// Gets the unique identifier for the current session.
    /// </summary>
    public int SessionHandle => _sessionHandle;

    /// <summary>
    ///     Registers one or more files with the Restart Manager session.
    /// </summary>
    /// <param name="filenames">Full paths to the files to register.</param>
    /// <exception cref="ObjectDisposedException">Thrown when the session has been disposed.</exception>
    /// <exception cref="Win32Exception">Thrown when the resources could not be registered.</exception>
    public void RegisterFiles(params string[] filenames)
    {
        ThrowIfDisposed();
        
        if (filenames == null || filenames.Length == 0)
        {
            return;
        }

        var result = RestartManagerApi.RmRegisterResources(
            _sessionHandle,
            (uint)filenames.Length,
            filenames,
            0,
            null,
            0,
            null);

        if (result != 0)
        {
            throw new Win32Exception(result, "Failed to register files with Restart Manager");
        }
    }

    /// <summary>
    ///     Registers a single file with the Restart Manager session.
    /// </summary>
    /// <param name="filename">Full path to the file to register.</param>
    /// <exception cref="ObjectDisposedException">Thrown when the session has been disposed.</exception>
    /// <exception cref="Win32Exception">Thrown when the resource could not be registered.</exception>
    public void RegisterFile(string filename)
    {
        RegisterFiles(filename);
    }

    /// <summary>
    ///     Registers one or more processes with the Restart Manager session.
    /// </summary>
    /// <param name="processes">Array of RmUniqueProcess structures identifying the processes.</param>
    /// <exception cref="ObjectDisposedException">Thrown when the session has been disposed.</exception>
    /// <exception cref="Win32Exception">Thrown when the resources could not be registered.</exception>
    public void RegisterProcesses(params RmUniqueProcess[] processes)
    {
        ThrowIfDisposed();
        
        if (processes == null || processes.Length == 0)
        {
            return;
        }

        var result = RestartManagerApi.RmRegisterResources(
            _sessionHandle,
            0,
            null,
            (uint)processes.Length,
            processes,
            0,
            null);

        if (result != 0)
        {
            throw new Win32Exception(result, "Failed to register processes with Restart Manager");
        }
    }

    /// <summary>
    ///     Registers one or more Windows services with the Restart Manager session.
    /// </summary>
    /// <param name="serviceNames">Short names of the services to register.</param>
    /// <exception cref="ObjectDisposedException">Thrown when the session has been disposed.</exception>
    /// <exception cref="Win32Exception">Thrown when the resources could not be registered.</exception>
    public void RegisterServices(params string[] serviceNames)
    {
        ThrowIfDisposed();
        
        if (serviceNames == null || serviceNames.Length == 0)
        {
            return;
        }

        var result = RestartManagerApi.RmRegisterResources(
            _sessionHandle,
            0,
            null,
            0,
            null,
            (uint)serviceNames.Length,
            serviceNames);

        if (result != 0)
        {
            throw new Win32Exception(result, "Failed to register services with Restart Manager");
        }
    }

    /// <summary>
    ///     Gets a list of all applications and services using the registered resources.
    /// </summary>
    /// <returns>A list of RmProcessInfo structures describing the affected applications.</returns>
    /// <exception cref="ObjectDisposedException">Thrown when the session has been disposed.</exception>
    /// <exception cref="Win32Exception">Thrown when the list could not be retrieved.</exception>
    public IReadOnlyList<RmProcessInfo> GetProcessesUsingResources() => GetProcessesUsingResources(out _);

    /// <summary>
    ///     Gets a list of all applications and services using the registered resources, along with the reboot reason.
    /// </summary>
    /// <param name="rebootReason">Receives flags indicating why a reboot might be necessary.</param>
    /// <returns>A list of RmProcessInfo structures describing the affected applications.</returns>
    /// <exception cref="ObjectDisposedException">Thrown when the session has been disposed.</exception>
    /// <exception cref="Win32Exception">Thrown when the list could not be retrieved.</exception>
    public IReadOnlyList<RmProcessInfo> GetProcessesUsingResources(out RmRebootReason rebootReason)
    {
        ThrowIfDisposed();

        uint pnProcInfo = 0;
        var result = RestartManagerApi.RmGetList(_sessionHandle, out var pnProcInfoNeeded, ref pnProcInfo, null, out rebootReason);

        // The list can grow between the size query and the actual call (a process starts using a resource),
        // in that case RmGetList returns ERROR_MORE_DATA again and the call is retried with the new size.
        for (var attempt = 0; result == ErrorMoreData && attempt < MaxGetListAttempts; attempt++)
        {
            if (pnProcInfoNeeded == 0)
            {
                break;
            }
            var processInfo = new RmProcessInfo[pnProcInfoNeeded];
            pnProcInfo = pnProcInfoNeeded;
            result = RestartManagerApi.RmGetList(_sessionHandle, out pnProcInfoNeeded, ref pnProcInfo, processInfo, out rebootReason);
            if (result == 0)
            {
                return processInfo.Take((int)pnProcInfo).ToList();
            }
        }

        if (result != 0)
        {
            throw new Win32Exception(result, "Failed to get process list from Restart Manager");
        }

        // Success with an empty buffer means nothing is using the registered resources
        return Array.Empty<RmProcessInfo>();
    }

    /// <summary>
    ///     Shuts down the applications and services using the registered resources.
    /// </summary>
    /// <param name="shutdownType">
    ///     Flags controlling the shutdown behavior. The default, <see cref="RmShutdownType.Graceful"/>, asks the applications to close
    ///     and fails (ERROR_FAIL_SHUTDOWN) when one of them refuses, so no unsaved data is lost.
    ///     Pass <see cref="RmShutdownType.RmForceShutdown"/> to explicitly opt in to killing unresponsive applications.
    /// </param>
    /// <param name="statusCallback">Optional callback to receive progress updates (0-100).</param>
    /// <exception cref="ObjectDisposedException">Thrown when the session has been disposed.</exception>
    /// <exception cref="Win32Exception">Thrown when the shutdown failed, e.g. because an application refused to close.</exception>
    public void Shutdown(RmShutdownType shutdownType = RmShutdownType.Graceful, Action<uint> statusCallback = null)
    {
        ThrowIfDisposed();

        RestartManagerApi.RmStatusCallback callback = null;
        if (statusCallback != null)
        {
            callback = statusCallback.Invoke;
        }

        var result = RestartManagerApi.RmShutdown(_sessionHandle, shutdownType, callback);
        GC.KeepAlive(callback);

        if (result != 0)
        {
            throw new Win32Exception(result, "Failed to shutdown applications via Restart Manager");
        }
    }

    /// <summary>
    ///     Restarts applications and services that were shut down by the Shutdown method.
    /// </summary>
    /// <param name="statusCallback">Optional callback to receive progress updates (0-100).</param>
    /// <exception cref="ObjectDisposedException">Thrown when the session has been disposed.</exception>
    /// <exception cref="Win32Exception">Thrown when the restart failed.</exception>
    public void Restart(Action<uint> statusCallback = null)
    {
        ThrowIfDisposed();

        RestartManagerApi.RmStatusCallback callback = null;
        if (statusCallback != null)
        {
            callback = statusCallback.Invoke;
        }

        var result = RestartManagerApi.RmRestart(_sessionHandle, 0, callback);
        GC.KeepAlive(callback);

        if (result != 0)
        {
            throw new Win32Exception(result, "Failed to restart applications via Restart Manager");
        }
    }

    /// <summary>
    ///     Checks if a reboot would be required to complete the operation.
    ///     This queries the Restart Manager every time (the state can change), when you also need the process list
    ///     use <see cref="GetProcessesUsingResources(out RmRebootReason)"/> instead to avoid a second query.
    /// </summary>
    /// <returns>True if a reboot is required, false otherwise.</returns>
    /// <exception cref="ObjectDisposedException">Thrown when the session has been disposed.</exception>
    /// <exception cref="Win32Exception">Thrown when the check failed.</exception>
    public bool IsRebootRequired()
    {
        GetProcessesUsingResources(out var rebootReason);
        return rebootReason != RmRebootReason.RmRebootReasonNone;
    }

    /// <summary>
    ///     Gets the reason why a reboot would be required.
    ///     This queries the Restart Manager every time (the state can change), when you also need the process list
    ///     use <see cref="GetProcessesUsingResources(out RmRebootReason)"/> instead to avoid a second query.
    /// </summary>
    /// <returns>Flags indicating the reboot reason.</returns>
    /// <exception cref="ObjectDisposedException">Thrown when the session has been disposed.</exception>
    /// <exception cref="Win32Exception">Thrown when the check failed.</exception>
    public RmRebootReason GetRebootReason()
    {
        GetProcessesUsingResources(out var rebootReason);
        return rebootReason;
    }

    private void ThrowIfDisposed()
    {
        if (_disposed)
        {
            throw new ObjectDisposedException(nameof(InstallerRestartManager));
        }
    }

    /// <summary>
    ///     Ends the Restart Manager session and releases resources.
    /// </summary>
    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }
        _disposed = true;
        EndSession();
        GC.SuppressFinalize(this);
    }

    /// <summary>
    ///     Ends the Restart Manager session if the instance was not disposed, a leaked session would otherwise stay open until the process exits.
    /// </summary>
    ~InstallerRestartManager()
    {
        EndSession();
    }

    private void EndSession()
    {
        var sessionHandle = Interlocked.Exchange(ref _sessionHandle, RestartManagerApi.RmInvalidSession);
        if (sessionHandle != RestartManagerApi.RmInvalidSession)
        {
            RestartManagerApi.RmEndSession(sessionHandle);
        }
    }
}
