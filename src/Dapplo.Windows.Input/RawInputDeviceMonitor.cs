// Copyright (c) Dapplo and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Reactive.Linq;
using Dapplo.Windows.Input.Enums;
using Dapplo.Windows.Messages;
using Dapplo.Windows.Messages.Enumerations;
using Dapplo.Windows.Messages.Structs;

namespace Dapplo.Windows.Input;

/// <summary>
/// Reactive access to RawInput device information and changes.
/// This class provides an observable stream of device change events, and a cache of currently known devices.
/// </summary>
public static class RawInputDeviceMonitor
{
    private const long DeviceArrival = 1; // GIDC_ARRIVAL, GIDC_REMOVAL is 2
    private const RawInputDeviceFlags ListenFlags = RawInputDeviceFlags.DeviceNotify;
    private static readonly ConcurrentDictionary<IntPtr, RawInputDeviceInformation> DeviceCache = new();

    // Only used on the window thread of the SharedMessageWindow: the event args of the last WM_INPUT_DEVICE_CHANGE,
    // so the cache is updated once and every subscriber gets the same instance.
    private static WindowMessage _lastMessage;
    private static RawInputDeviceChangeEventArgs _lastEventArgs;
    private static bool _lastIsKnown;

    /// <summary>
    /// The raw-input devices which arrived while a listener was active (Windows reports all present devices when the registration is made).
    /// This is thread safe to read.
    /// </summary>
    public static IReadOnlyDictionary<IntPtr, RawInputDeviceInformation> Devices { get; } = new ReadOnlyDictionary<IntPtr, RawInputDeviceInformation>(DeviceCache);

    /// <summary>
    /// Listen to arrival and removal (WM_INPUT_DEVICE_CHANGE) of raw input devices of the specified types.
    /// </summary>
    /// <remarks>
    /// The devices are registered, on the <see cref="SharedMessageWindow"/>, when subscribing and unregistered when the last subscription which needs them is disposed.
    /// Multiple listeners (also <see cref="RawInputMonitor"/>) can be active at the same time, their registrations are combined.
    /// OnNext is called on the thread of the SharedMessageWindow.
    /// When the information of a device cannot be retrieved (e.g. it was removed very quickly, or it's unknown at removal), the <see cref="RawInputDeviceInformation"/> only has the handle.
    /// </remarks>
    /// <param name="devices">RawInputDevices to listen to, at least one</param>
    /// <returns>IObservable with RawInputDeviceChangeEventArgs</returns>
    public static IObservable<RawInputDeviceChangeEventArgs> Listen(params RawInputDevices[] devices)
    {
        if (devices == null || devices.Length == 0)
        {
            throw new ArgumentException("At least one device is needed.", nameof(devices));
        }
        var registeredDevices = devices.Distinct().ToArray();
        var deviceTypes = registeredDevices.Select(RawInputApi.GetDeviceType).Distinct().ToArray();

        return SharedMessageWindow.Listen(
                hWnd => RawInputRegistrations.Add(hWnd, ListenFlags, registeredDevices),
                hWnd => RawInputRegistrations.Remove(hWnd, ListenFlags, registeredDevices))
            .Where(windowMessage => windowMessage.Msg == WindowsMessages.WM_INPUT_DEVICE_CHANGE)
            .Where(windowMessage =>
            {
                var eventArgs = GetEventArgs(windowMessage);
                // Devices with unknown information are always passed on, as the type is not known
                return !_lastIsKnown || deviceTypes.Contains(eventArgs.DeviceInformation.DeviceInfo.Type);
            })
            .Select(GetEventArgs);
    }

    /// <summary>
    /// Create the RawInputDeviceChangeEventArgs for the WM_INPUT_DEVICE_CHANGE and update the cache, once per message
    /// </summary>
    /// <param name="windowMessage">WindowMessage</param>
    /// <returns>RawInputDeviceChangeEventArgs</returns>
    private static RawInputDeviceChangeEventArgs GetEventArgs(WindowMessage windowMessage)
    {
        if (ReferenceEquals(windowMessage, _lastMessage))
        {
            return _lastEventArgs;
        }

        var isNew = (long)windowMessage.WParam == DeviceArrival;
        var deviceHandle = windowMessage.LParam;
        RawInputDeviceInformation deviceInformation = null;
        if (isNew)
        {
            try
            {
                deviceInformation = RawInputApi.GetDeviceInformation(deviceHandle);
                DeviceCache[deviceHandle] = deviceInformation;
            }
            catch (Exception ex)
            {
                // The device might already be gone again
                Trace.TraceWarning("Dapplo.Windows.Input.RawInputDeviceMonitor: couldn't get the information for raw input device {0}: {1}", deviceHandle, ex.Message);
            }
        }
        else
        {
            DeviceCache.TryRemove(deviceHandle, out deviceInformation);
        }

        _lastIsKnown = deviceInformation != null;
        _lastEventArgs = new RawInputDeviceChangeEventArgs
        {
            Added = isNew,
            DeviceInformation = deviceInformation ?? new RawInputDeviceInformation { Handle = deviceHandle }
        };
        _lastMessage = windowMessage;
        return _lastEventArgs;
    }
}
