// Copyright (c) Dapplo and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
using System;
using System.Diagnostics;
using System.Reactive.Linq;
using System.Windows;
using Dapplo.Windows.Devices;
using Dapplo.Windows.Dpi.Wpf;
using Dapplo.Windows.Messages;
using Dapplo.Windows.Messages.Enumerations;
using Dapplo.Windows.User32;

namespace Dapplo.Windows.Example.WpfExample
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow
    {
        public MainWindow()
        {
            InitializeComponent();
            this.AttachDpiHandler();

            this.WinProcMessages()
                .Where(m => m.Message == WindowsMessages.WM_DESTROY)
                .Subscribe(m => { MessageBox.Show($"{m.Message}"); });

            DeviceNotification.OnVolumeAdded().Subscribe(volumeInfo => Debug.WriteLine($"Drives {volumeInfo.Volume.Drives} were added"));
            DeviceNotification.OnVolumeRemoved().Subscribe(volumeInfo => Debug.WriteLine($"Drives {volumeInfo.Volume.Drives} were removed"));

            DeviceNotification
                .OnDeviceArrival()
                .Subscribe(deviceInterfaceChangeInfo => Debug.WriteLine("Device added: {0}, for more information goto {1}", deviceInterfaceChangeInfo.Device.FriendlyDeviceName, deviceInterfaceChangeInfo.Device.UsbDeviceInfoUri));

            // A small example to lock the PC when a YubiKey is removed
            DeviceNotification.OnDeviceRemoved()
                .Where(deviceInterfaceChangeInfo => deviceInterfaceChangeInfo.Device.Name.Contains("Yubi"))
                .Subscribe(deviceInterfaceChangeInfo => User32Api.LockWorkStation());
        }
    }
}
