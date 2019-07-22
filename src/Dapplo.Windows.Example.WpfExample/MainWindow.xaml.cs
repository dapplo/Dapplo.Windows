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
using Dapplo.Windows.Dpi.Wpf;
using Dapplo.Windows.Messages;
using Dapplo.Windows.Messages.Enums;
using Dapplo.Windows.Messages.Structs;
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

            this.DeviceNotifications()
                .Subscribe(deviceChangeEvent =>
                {
                    Debug.WriteLine("Device change: {0}", deviceChangeEvent.EventType);
                    switch (deviceChangeEvent.EventType)
                    {
                        case DeviceChangeEvent.DeviceArrival:
                        case DeviceChangeEvent.DeviceRemoveComplete:
                            if (deviceChangeEvent.TryGetDeviceBroadcastDeviceType(out DevBroadcastDeviceInterface devBroadcastDeviceInterface))
                            {
                                Debug.WriteLine("{0}: {1}", devBroadcastDeviceInterface, devBroadcastDeviceInterface.FriendlyDeviceName());

                                // A small example to lock the PC when a yubikey is removed
                                if (devBroadcastDeviceInterface.Name.Contains("Yubi") && deviceChangeEvent.EventType == DeviceChangeEvent.DeviceRemoveComplete)
                                {
                                    User32Api.LockWorkStation();
                                }
                            }
                            break;
                    }
                });
        }
    }
}
