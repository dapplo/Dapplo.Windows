// Copyright (c) Dapplo and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.ComponentModel;
using System.Reactive;
using System.Reactive.Concurrency;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Threading;
using Dapplo.Windows.Advapi32.Enums;
using Microsoft.Win32;

namespace Dapplo.Windows.Advapi32;

/// <summary>
/// Monitors registry keys for changes, using RegNotifyChangeKeyValue
/// </summary>
public static class RegistryMonitor
{
    private const int ErrorInvalidParameter = 87;

    /// <summary>
    /// Create an observable to monitor for registry changes
    /// </summary>
    /// <param name="hive">RegistryHive</param>
    /// <param name="subKey">string</param>
    /// <param name="registrationScheduler">IScheduler which is used to subscribe (open the key and register the first notification)</param>
    /// <param name="filter">RegistryNotifyFilter, <see cref="RegistryNotifyFilter.ThreadAgnostic"/> is always added (Windows 8 and later)</param>
    /// <returns>IObservable which produces a Unit for every change, on a thread pool thread</returns>
    public static IObservable<Unit> ObserveChanges(RegistryHive hive, string subKey, IScheduler registrationScheduler = null, RegistryNotifyFilter filter = RegistryNotifyFilter.ChangeLastSet)
    {
        return ObserveChanges(new IntPtr((int) hive), subKey, registrationScheduler, filter);
    }

    /// <summary>
    /// Create an observable to monitor for registry changes
    /// </summary>
    /// <param name="hKey">IntPtr</param>
    /// <param name="subKey">string</param>
    /// <param name="registrationScheduler">IScheduler which is used to subscribe (open the key and register the first notification)</param>
    /// <param name="filter">RegistryNotifyFilter, <see cref="RegistryNotifyFilter.ThreadAgnostic"/> is always added (Windows 8 and later)</param>
    /// <returns>IObservable which produces a Unit for every change, on a thread pool thread</returns>
    public static IObservable<Unit> ObserveChanges(IntPtr hKey, string subKey, IScheduler registrationScheduler = null, RegistryNotifyFilter filter = RegistryNotifyFilter.ChangeLastSet)
    {
        var observable = Observable.Create<Unit>(
            observer =>
            {
                // RegOpenKeyEx returns the error code, it doesn't set the last error
                var result = Advapi32Api.RegOpenKeyEx(hKey, subKey, RegistryOpenOptions.None, RegistryKeySecurityAccessRights.Read, out var registryKey);
                if (result != 0)
                {
                    observer.OnError(new Win32Exception(result));
                    return Disposable.Empty;
                }

                var keyWatcher = new KeyWatcher(registryKey, filter, observer);
                keyWatcher.Start();
                return Disposable.Create(() =>
                {
                    // First stop the watcher, so no callback can use the key anymore, then close the key
                    keyWatcher.Dispose();
                    Advapi32Api.RegCloseKey(registryKey);
                });
            });
        return registrationScheduler == null ? observable : observable.SubscribeOn(registrationScheduler);
    }

    /// <summary>
    /// Watches an opened registry key, re-arming the notification after every change.
    /// </summary>
    private sealed class KeyWatcher : IDisposable
    {
        private readonly object _lock = new object();
        private readonly object _onNextLock = new object();
        private readonly IntPtr _key;
        private readonly IObserver<Unit> _observer;
        private readonly AutoResetEvent _changedEvent = new AutoResetEvent(false);
        private RegistryNotifyFilter _filter;
        private RegisteredWaitHandle _registeredWait;
        private bool _isStopped;

        public KeyWatcher(IntPtr key, RegistryNotifyFilter filter, IObserver<Unit> observer)
        {
            _key = key;
            // Without REG_NOTIFY_THREAD_AGNOSTIC the registration ends when the (thread pool) thread which made it exits, which causes phantom notifications
            _filter = filter | RegistryNotifyFilter.ThreadAgnostic;
            _observer = observer;
        }

        public void Start()
        {
            lock (_lock)
            {
                Arm();
            }
        }

        /// <summary>
        /// Register for the next change, must be called with the lock held
        /// </summary>
        private bool Arm()
        {
            if (_isStopped)
            {
                return false;
            }
            var eventHandle = _changedEvent.SafeWaitHandle.DangerousGetHandle();
            var result = Advapi32Api.RegNotifyChangeKeyValue(_key, true, _filter, eventHandle, true);
            if (result == ErrorInvalidParameter && (_filter & RegistryNotifyFilter.ThreadAgnostic) != 0)
            {
                // Before Windows 8 REG_NOTIFY_THREAD_AGNOSTIC is not supported
                _filter &= ~RegistryNotifyFilter.ThreadAgnostic;
                result = Advapi32Api.RegNotifyChangeKeyValue(_key, true, _filter, eventHandle, true);
            }
            if (result != 0)
            {
                Stop();
                // RegNotifyChangeKeyValue returns the error code, it doesn't set the last error
                _observer.OnError(new Win32Exception(result));
                return false;
            }
            _registeredWait = ThreadPool.RegisterWaitForSingleObject(_changedEvent, OnChanged, null, Timeout.Infinite, true);
            return true;
        }

        /// <summary>
        /// Called on a thread pool thread when the event is signalled
        /// </summary>
        private void OnChanged(object state, bool timedOut)
        {
            lock (_lock)
            {
                // The wait was registered to execute only once, release it
                _registeredWait?.Unregister(null);
                _registeredWait = null;
                // Re-arm first, so changes which happen while the observer processes this change are not lost
                if (!Arm())
                {
                    return;
                }
            }
            // Serialize the OnNext calls, without holding the lock which Dispose needs
            lock (_onNextLock)
            {
                if (!_isStopped)
                {
                    _observer.OnNext(Unit.Default);
                }
            }
        }

        /// <summary>
        /// Stop watching, must be called with the lock held
        /// </summary>
        private void Stop()
        {
            if (_isStopped)
            {
                return;
            }
            _isStopped = true;
            _registeredWait?.Unregister(null);
            _registeredWait = null;
            _changedEvent.Dispose();
        }

        public void Dispose()
        {
            lock (_lock)
            {
                Stop();
            }
        }
    }
}
