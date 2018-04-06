#region Copyright (C) 2016-2018 Dapplo
//  Dapplo - building blocks for desktop applications
//  Copyright (C) 2016-2018 Dapplo
// 
//  For more information see: http://dapplo.net/
//  Dapplo repositories are hosted on GitHub: https://github.com/dapplo
// 
//  This file is part of Dapplo.Windows
// 
//  Dapplo.Windows is free software: you can redistribute it and/or modify
//  it under the terms of the GNU Lesser General Public License as published by
//  the Free Software Foundation, either version 3 of the License, or
//  (at your option) any later version.
// 
//  Dapplo.Windows is distributed in the hope that it will be useful,
//  but WITHOUT ANY WARRANTY; without even the implied warranty of
//  MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
//  GNU Lesser General Public License for more details.
// 
//  You should have a copy of the GNU Lesser General Public License
//  along with Dapplo.Windows. If not, see <http://www.gnu.org/licenses/lgpl.txt>.
#endregion

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

namespace Dapplo.Windows.Advapi32
{
    /// <summary>
    /// 
    /// </summary>
    public static class Registry
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
                        var result = RegOpenKeyEx(hKey, subKey, RegistryOpenOptions.None, RegistryKeySecurityAccessRights.Read, out var registryKey);
                        if (result != 0)
                        {
                            throw new Win32Exception(Marshal.GetLastWin32Error());
                        }
                        return new CompositeDisposable(
                            CreateKeyValuesChangedObservable(registryKey, filter).SubscribeOn(registrationScheduler ?? Scheduler.CurrentThread).Subscribe(obs),
                            Disposable.Create(() => RegCloseKey(registryKey)));
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
                    var result = RegNotifyChangeKeyValue(key, true, filter, eventNotify.SafeWaitHandle.DangerousGetHandle(), true);
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

        /// <summary>
        /// Notifies the caller about changes to the attributes or contents of a specified registry key.
        /// </summary>
        /// <param name="hKey">IntPtr A handle to an open registry key.</param>
        /// <param name="watchSubtree">If this parameter is TRUE, the function reports changes in the specified key and its subkeys. If the parameter is FALSE, the function reports changes only in the specified key.</param>
        /// <param name="notifyFilter">RegChangeNotifyFilter A value that indicates the changes that should be reported. </param>
        /// <param name="hEvent"></param>
        /// <param name="asynchronous">If this parameter is TRUE, the function returns immediately and reports changes by signaling the specified event. If this parameter is FALSE, the function does not return until a change has occurred.</param>
        /// <returns></returns>
        [DllImport("advapi32.dll", SetLastError = true)]
        private static extern int RegNotifyChangeKeyValue(IntPtr hKey, bool watchSubtree, RegistryNotifyFilter notifyFilter, IntPtr hEvent, bool asynchronous);

        /// <summary>
        /// Opens the specified registry key. Note that key names are not case sensitive.
        /// To perform transacted registry operations on a key, call the RegOpenKeyTransacted function.
        /// </summary>
        /// <param name="hive">RegistryHive</param>
        /// <param name="subKey">string</param>
        /// <param name="ulOptions">RegistryOpenOptions</param>
        /// <param name="samDesired">RegistryKeySecurityAccessRights</param>
        /// <param name="hOpenedKey">UIntPtr a handle to the registry key</param>
        /// <returns></returns>
        [DllImport("advapi32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern int RegOpenKeyEx(RegistryHive hive, string subKey, RegistryOpenOptions ulOptions, RegistryKeySecurityAccessRights samDesired, out IntPtr hOpenedKey);


        /// <summary>
        /// Opens the specified registry key. Note that key names are not case sensitive.
        /// To perform transacted registry operations on a key, call the RegOpenKeyTransacted function.
        /// </summary>
        /// <param name="hKey">IntPtr A handle to an open registry key.</param>
        /// <param name="subKey">
        /// The name of the registry subkey to be opened.
        /// Key names are not case sensitive.
        /// The lpSubKey parameter can be a pointer to an empty string. If lpSubKey is a pointer to an empty string and hKey is HKEY_CLASSES_ROOT, phkResult receives the same hKey handle passed into the function. Otherwise, phkResult receives a new handle to the key specified by hKey.
        /// The lpSubKey parameter can be NULL only if hKey is one of the predefined keys. If lpSubKey is NULL and hKey is HKEY_CLASSES_ROOT, phkResult receives a new handle to the key specified by hKey. Otherwise, phkResult receives the same hKey handle passed in to the function.
        /// </param>
        /// <param name="ulOptions">RegistryOpenOptions</param>
        /// <param name="samDesired">RegistryKeySecurityAccessRights</param>
        /// <param name="hOpenedKey">UIntPtr a handle to the registry key</param>
        /// <returns></returns>
        [DllImport("advapi32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern int RegOpenKeyEx(IntPtr hKey, string subKey, RegistryOpenOptions ulOptions, RegistryKeySecurityAccessRights samDesired, out IntPtr hOpenedKey);


        /// <summary>
        /// Closes a handle to the specified registry key.
        /// </summary>
        /// <param name="hKey">UIntPtr a handle to the registry key</param>
        /// <returns></returns>
        [DllImport("advapi32.dll", SetLastError = true)]
        private static extern int RegCloseKey(IntPtr hKey);
    }
}
