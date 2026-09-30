// Copyright (c) Dapplo and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Diagnostics;
using System.Reactive.Linq;
using Dapplo.Windows.Advapi32;
using Dapplo.Windows.Citrix;
using Dapplo.Windows.Common;
using Dapplo.Windows.DesktopWindowsManager;
using Dapplo.Windows.Devices;
using Dapplo.Windows.EmbeddedBrowser;
using Dapplo.Windows.Kernel32;
using Dapplo.Windows.Multimedia;
using Dapplo.Windows.Multimedia.Enums;
using Dapplo.Windows.Shell32;
using Microsoft.Win32;

namespace Dapplo.Windows.Example.DocSamples;

/// <summary>
/// Samples for doc/articles/more-packages.md
/// </summary>
public static class MorePackagesSamples
{
    public static void WindowsVersionCheck()
    {
        #region WindowsVersionCheck
        // The real version, also without an application manifest
        Console.WriteLine($"Windows {WindowsVersion.WinVersion}");
        if (WindowsVersion.IsWindows11OrLater)
        {
            Console.WriteLine("Rounded corners are available");
        }
        #endregion
    }

    public static void Citrix()
    {
        #region Citrix
        if (WinFrame.IsAvailable)
        {
            Console.WriteLine($"Citrix session, client {WinFrame.GetClientName()} ({WinFrame.GetClientIpAddress()})");
        }
        #endregion
    }

    public static void Dwm(IntPtr hWnd)
    {
        #region Dwm
        // The window bounds as the user sees them, without the invisible resize borders
        if (DwmApi.GetExtendedFrameBounds(hWnd, out var frameBounds))
        {
            Console.WriteLine($"Visible frame: {frameBounds}");
        }

        // Windows which are "there" but not visible, e.g. on another virtual desktop or a suspended store app
        bool isCloaked = DwmApi.IsWindowCloaked(hWnd);

        // The accent color of the user
        System.Drawing.Color accent = DwmApi.ColorizationSystemDrawingColor;
        #endregion
    }

    public static void Devices()
    {
        #region Devices
        // WM_DEVICECHANGE, on the SharedMessageWindow thread
        var arrivals = DeviceNotification.OnDeviceArrival()
            .Subscribe(info => Console.WriteLine($"Connected: {info.Device.FriendlyDeviceName} (USB: {info.Device.IsUsb})"));

        var removals = DeviceNotification.OnDeviceRemoved()
            .Subscribe(info => Console.WriteLine($"Removed: {info.Device.FriendlyDeviceName}"));

        // Drives: USB sticks, network drives, CDs
        var volumes = DeviceNotification.OnVolumeAdded()
            .Subscribe(info => Console.WriteLine($"New drive(s): {info.Volume.Drives}"));
        #endregion
    }

    public static void Registry()
    {
        #region Registry
        // Produces a value (Unit) on a thread-pool thread for every change of the key, e.g. the theme setting
        var subscription = RegistryMonitor
            .ObserveChanges(RegistryHive.CurrentUser, @"Software\Microsoft\Windows\CurrentVersion\Themes\Personalize")
            .Throttle(TimeSpan.FromMilliseconds(200))
            .Subscribe(_ => Console.WriteLine("The theme settings changed"));
        #endregion
    }

    public static void Kernel32()
    {
        #region Kernel32
        // Call first thing in Main: load DLLs only from System32 (and the application directory when you allow it)
        Kernel32Api.PreventDllHijacking(searchApplicationDirectory: true);

        // The path of another process, also for elevated processes where Process.MainModule fails
        using var explorer = Process.GetProcessesByName("explorer")[0];
        string path = explorer.GetProcessPath();

        // Running as a packaged (MSIX / Store) application?
        bool isPackaged = PackageInfo.HasPackageIdentity;
        #endregion
    }

    public static void Shell32()
    {
        #region Shell32
        if (Shell32Api.TryGetTaskbarPosition(out var appBarData))
        {
            Console.WriteLine($"The taskbar is at {appBarData.AppBarEdge}, bounds {appBarData.Bounds}, state {Shell32Api.GetTaskbarState()}");
        }
        #endregion
    }

    public static void Sounds()
    {
        #region Sounds
        // One of the sounds of the Windows sound scheme
        WinMm.PlaySystemSound(SystemSounds.SystemAsterisk);

        // A WAV file, asynchronous. The data is copied, it plays until it's done or StopPlaying is called.
        WinMm.Play(System.IO.File.ReadAllBytes(@"C:\Windows\Media\chimes.wav"));

        // A WAVE resource of the executable
        WinMm.Play("NotificationSound");
        #endregion
    }

    public static void EmbeddedBrowser()
    {
        #region EmbeddedBrowser
        // Call before the first WebBrowser is created: without this the WebBrowser control renders like IE 7
        InternetExplorerVersion.ChangeEmbeddedVersion();
        Console.WriteLine($"Installed Internet Explorer version: {InternetExplorerVersion.Version}");
        #endregion
    }
}
