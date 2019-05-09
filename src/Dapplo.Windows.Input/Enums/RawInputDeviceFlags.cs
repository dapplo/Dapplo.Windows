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

namespace Dapplo.Windows.Input.Enums
{
    /// <summary>
    ///     Mode flag that specifies how to interpret the information provided by usUsagePage and usUsage.
    ///    It can be zero (the default) or one of the following values. By default, the operating system sends raw input from devices with the specified top level collection (TLC) to the registered application as long as it has the window focus.
    ///     See <a href="https://msdn.microsoft.com/en-us/library/windows/desktop/ms645565.aspx">RAWINPUTDEVICE structure</a>
    /// </summary>
    [Flags]
    public enum RawInputDeviceFlags
    {

        /// <summary>
        ///     RIDEV_REMOVE: If set, this removes the top level collection from the inclusion list.
        ///     This tells the operating system to stop reading from a device which matches the top level collection.
        /// </summary>
        Remove = 0x00000001,

        /// <summary>
        ///     RIDEV_EXCLUDE: If set, this specifies the top level collections to exclude when reading a complete usage page.
        ///     This flag only affects a TLC whose usage page is already specified with RIDEV_PAGEONLY.
        /// </summary>
        Exclude = 0x00000010,

        /// <summary>
        ///     RIDEV_PAGEONLY: If set, this specifies all devices whose top level collection is from the specified usUsagePage.
        ///     Note that usUsage must be zero.
        ///     To exclude a particular top level collection, use RIDEV_EXCLUDE.
        /// </summary>
        PageOnly = 0x00000020,

        /// <summary>
        ///     RIDEV_NOLEGACY: If set, this prevents any devices specified by usUsagePage or usUsage from generating legacy messages.
        ///     This is only for the mouse and keyboard. See Remarks.
        /// </summary>
        NoLegacy = 0x00000030,

        /// <summary>
        ///     RIDEV_INPUTSINK: If set, this enables the caller to receive the input even when the caller is not in the foreground.
        ///     Note that hwndTarget must be specified.
        /// </summary>
        InputSink = 0x00000100,

        /// <summary>
        ///     RIDEV_NOHOTKEYS: If set, the application-defined keyboard device hotkeys are not handled.
        ///     However, the system hotkeys; for example, ALT+TAB and CTRL+ALT+DEL, are still handled.
        ///     By default, all keyboard hotkeys are handled.
        ///     RIDEV_NOHOTKEYS can be specified even if RIDEV_NOLEGACY is not specified and hwndTarget is NULL.
        /// </summary>
        NoHotkeys = 0x00000200,

        /// <summary>
        ///     RIDEV_CAPTUREMOUSE: If set, the mouse button click does not activate the other window.
        /// </summary>
        CaptureMouse = 0x00000200,

        /// <summary>
        ///     RIDEV_APPKEYS: If set, the application command keys are handled.
        ///     RIDEV_APPKEYS can be specified only if RIDEV_NOLEGACY is specified for a keyboard device.
        /// </summary>
        AppKeys = 0x00000400,
        
        /// <summary>
        ///     RIDEV_EXINPUTSINK: If set, this enables the caller to receive input in the background only if the foreground application does not process it.
        ///     In other words, if the foreground application is not registered for raw input, then the background application that is registered will receive the input.
        ///     Windows XP:  This flag is not supported until Windows Vista
        /// </summary>
        ExInputSink = 0x00001000,

        /// <summary>
        ///     RIDEV_DEVNOTIFY: If set, this enables the caller to receive WM_INPUT_DEVICE_CHANGE notifications for device arrival and device removal.
        ///     Windows XP:  This flag is not supported until Windows Vista
        /// </summary>
        DeviceNotify = 0x00002000
    }
}