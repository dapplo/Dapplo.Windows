//  Dapplo - building blocks for desktop applications
//  Copyright (C) 2017-2019  Dapplo
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
