﻿// Copyright (c) Dapplo and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.ComponentModel;
using System.Reactive;
using System.Reactive.Concurrency;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using Dapplo.Windows.Advapi32.Enums;
using Microsoft.Win32;

namespace Dapplo.Windows.Advapi32;

/// <summary>
/// 
/// </summary>
public static class RegistryMonitor
{
    /// <summary>
    /// Create an observable to monitor for registry changes
    /// </summary>
    /// <param name="hive">RegistryHive</param>
    /// <param name="subKey">string</param>
    /// <param name="registrationScheduler">IScheduler</param>
    /// <param name="filter">RegistryNotifyFilter</param>
    /// <returns>IObservable</returns>
    public static IObservable<Unit> ObserveChanges(RegistryHive hive, string subKey, IScheduler registrationScheduler = null, RegistryNotifyFilter filter = RegistryNotifyFilter.ChangeLastSet)
    {
        return ObserveChanges(new IntPtr((int) hive), subKey, registrationScheduler, filter);
    }

    /// <summary>
    /// Create an observable to monitor for registry changes
    /// </summary>
    /// <param name="hKey">IntPtr</param>
    /// <param name="subKey">string</param>
    /// <param name="registrationScheduler">IScheduler</param>
    /// <param name="filter">RegistryNotifyFilter</param>
    /// <returns>IObservable</returns>
    public static IObservable<Unit> ObserveChanges(IntPtr hKey, string subKey, IScheduler registrationScheduler = null, RegistryNotifyFilter filter = RegistryNotifyFilter.ChangeLastSet)
    {
        return Observable.Create<Unit>(
            obs =>
            {
                try
                {
                    var result = Advapi32Api.RegOpenKeyEx(hKey, subKey, RegistryOpenOptions.None, RegistryKeySecurityAccessRights.Read, out var registryKey);
                    if (result != 0)
                    {
                        throw new Win32Exception(Marshal.GetLastWin32Error());
                    }
                    return new CompositeDisposable(
                        CreateKeyValuesChangedObservable(registryKey, filter).SubscribeOn(registrationScheduler ?? Scheduler.CurrentThread).Subscribe(obs),
                        Disposable.Create(() => Advapi32Api.RegCloseKey(registryKey)));
                }
                catch (Win32Exception e)
                {
                    obs.OnError(e);
                    return Disposable.Empty;
                }
            });
    }

    /// <summary>
    /// Helper method to call the action when the WaitHandle is signaled.
    /// </summary>
    /// <param name="waitObject">WaitHandle</param>
    /// <param name="action">Action</param>
    /// <returns>IDisposable</returns>
    private static IDisposable SetCallbackWhenSignalled(WaitHandle waitObject, Action action)
    {
        var registeredWait = ThreadPool.RegisterWaitForSingleObject(waitObject, (s, t) => action(), null, -1, true);
        return Disposable.Create(() => registeredWait.Unregister(null));
    }

    /// <summary>
    /// Internal method to create the value changed observable
    /// </summary>
    /// <param name="key"></param>
    /// <param name="filter"></param>
    /// <returns></returns>
    private static IObservable<Unit> CreateKeyValuesChangedObservable(IntPtr key, RegistryNotifyFilter filter)
    {
        return Observable.Create<Unit>(
            obs =>
            {
                var eventNotify = new AutoResetEvent(false);
                var result = Advapi32Api.RegNotifyChangeKeyValue(key, true, filter, eventNotify.SafeWaitHandle.DangerousGetHandle(), true);
                if (result != 0)
                {
                    obs.OnError(new Win32Exception(Marshal.GetLastWin32Error()));
                }

                return new CompositeDisposable(
                    eventNotify,
                    SetCallbackWhenSignalled(eventNotify,
                        () => {
                            obs.OnNext(Unit.Default);
                            obs.OnCompleted();
                        }));
            }).Repeat();
    }
}