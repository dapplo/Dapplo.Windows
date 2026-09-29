// Copyright (c) Dapplo and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Dapplo.Windows.Input.Enums;
using Dapplo.Windows.Messages;
using Dapplo.Windows.Messages.Enumerations;
using Dapplo.Windows.Messages.Structs;
using System;
using System.Linq;
using System.Reactive.Linq;

namespace Dapplo.Windows.Input;

/// <summary>
/// Reactive access to RawInput
/// </summary>
public static class RawInputMonitor
{
    private const RawInputDeviceFlags ListenFlags = RawInputDeviceFlags.InputSink;

    // Only used on the window thread of the SharedMessageWindow: the event args of the last WM_INPUT,
    // so the data is read once and every subscriber gets the same instance.
    private static WindowMessage _lastMessage;
    private static RawInputEventArgs _lastEventArgs;

    /// <summary>
    /// Listen to the raw input (WM_INPUT) of the specified devices, also when the application is not in the foreground (RIDEV_INPUTSINK).
    /// </summary>
    /// <remarks>
    /// The devices are registered, on the <see cref="SharedMessageWindow"/>, when subscribing and unregistered when the last subscription which needs them is disposed.
    /// Multiple listeners (also <see cref="RawInputDeviceMonitor"/>) can be active at the same time, their registrations are combined.
    /// OnNext is called on the thread of the SharedMessageWindow, keep it short and use ObserveOn for other work.
    /// Only input from devices of the requested type (mouse, keyboard or HID) is passed on.
    /// </remarks>
    /// <param name="devices">RawInputDevices to listen to, at least one</param>
    /// <returns>IObservable with RawInputEventArgs</returns>
    public static IObservable<RawInputEventArgs> Listen(params RawInputDevices[] devices)
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
            // Don't set Handled for WM_INPUT, DefWindowProc must run to clean up
            .Where(windowMessage => windowMessage.Msg == WindowsMessages.WM_INPUT)
            .Select(GetEventArgs)
            .Where(eventArgs => eventArgs != null && deviceTypes.Contains(eventArgs.RawInput.Header.Type));
    }

    /// <summary>
    /// Create the RawInputEventArgs for the WM_INPUT, once per message
    /// </summary>
    /// <param name="windowMessage">WindowMessage</param>
    /// <returns>RawInputEventArgs or null if the data couldn't be read</returns>
    private static RawInputEventArgs GetEventArgs(WindowMessage windowMessage)
    {
        if (ReferenceEquals(windowMessage, _lastMessage))
        {
            return _lastEventArgs;
        }

        RawInputEventArgs eventArgs = null;
        if (RawInputApi.TryGetRawInputData(windowMessage.LParam, out var rawInput, out var hidData))
        {
            eventArgs = new RawInputEventArgs
            {
                // GET_RAWINPUT_CODE_WPARAM: RIM_INPUT (0) foreground, RIM_INPUTSINK (1) background
                IsForeground = ((long)windowMessage.WParam & 0xFF) == 0,
                RawInput = rawInput,
                HidData = hidData
            };
        }
        _lastMessage = windowMessage;
        _lastEventArgs = eventArgs;
        return eventArgs;
    }
}
