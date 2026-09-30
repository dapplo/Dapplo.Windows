// Copyright (c) Dapplo and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
using System;
using System.Diagnostics;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Windows;
using Dapplo.Windows.Devices;
using Dapplo.Windows.Input.Enums;
using Dapplo.Windows.Input.Keyboard;
using Dapplo.Windows.Messages;
using Dapplo.Windows.Messages.Enumerations;
using Dapplo.Windows.User32;
using Dapplo.Windows.Wpf.Dpi;
using Dapplo.Windows.Wpf.Messages;

namespace Dapplo.Windows.Example.WpfExample;

/// <summary>
/// Interaction logic for MainWindow.xaml
/// </summary>
public partial class MainWindow
{
    private readonly CompositeDisposable _subscriptions = new();
    private WindowsSessionListener _sessionListener;
    // Written on the UI thread, read on the keyboard hook thread (WPF properties like IsActive can't be read there)
    private volatile bool _isActive;

    public MainWindow()
    {
        InitializeComponent();
        this.AttachDpiHandler();

        Activated += (sender, args) => _isActive = true;
        Deactivated += (sender, args) => _isActive = false;

        _subscriptions.Add(this.WinProcMessages()
            .Where(m => m.Message == WindowsMessages.WM_DESTROY)
            .Subscribe(m => Debug.WriteLine($"{m.Message}")));

        // Handled must be set synchronously on the hook thread, this swallows the Print Screen key only while this window is active
        _subscriptions.Add(KeyboardHook.KeyboardEvents.Subscribe(HandleKeyboardEvent));
        _subscriptions.Add(DeviceNotification.OnVolumeAdded().Subscribe(volumeInfo => Debug.WriteLine($"Drives {volumeInfo.Volume.Drives} were added")));
        _subscriptions.Add(DeviceNotification.OnVolumeRemoved().Subscribe(volumeInfo => Debug.WriteLine($"Drives {volumeInfo.Volume.Drives} were removed")));

        _subscriptions.Add(DeviceNotification
            .OnDeviceArrival()
            .Subscribe(deviceInterfaceChangeInfo => Debug.WriteLine("Device added: {0}, for more information goto {1}", deviceInterfaceChangeInfo.Device.FriendlyDeviceName, deviceInterfaceChangeInfo.Device.UsbDeviceInfoUri)));

        // A small example to lock the PC when a YubiKey is removed
        _subscriptions.Add(DeviceNotification.OnDeviceRemoved()
            .Where(deviceInterfaceChangeInfo => deviceInterfaceChangeInfo.Device.Name.Contains("Yubi"))
            .Subscribe(deviceInterfaceChangeInfo => User32Api.LockWorkStation()));

        // Example of using WindowsSessionListener to handle session changes
        _sessionListener = new WindowsSessionListener();
        _sessionListener.SessionLockChange += (sender, args) =>
        {
            Debug.WriteLine($"Session lock/unlock event: {args.EventType}, Session ID: {args.SessionId}");
        };
        _sessionListener.SessionLogonChange += (sender, args) =>
        {
            Debug.WriteLine($"Session logon/logoff event: {args.EventType}, Session ID: {args.SessionId}");
        };
        _sessionListener.Start();

        // Make sure to dispose the listener and the subscriptions when the window closes
        Closed += (sender, args) =>
        {
            _sessionListener?.Dispose();
            _subscriptions.Dispose();
        };
    }

    private void HandleKeyboardEvent(KeyboardHookEventArgs args)
    {
        if (_isActive && args.IsKeyDown && args.Key == VirtualKeyCode.PrintScreen)
        {
            args.Handled = true; // Prevent the Print Screen key from being processed by the system
        }
    }
}