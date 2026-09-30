# More packages

Smaller packages, each for one area of Windows.

## Windows version (Dapplo.Windows.Common)

`WindowsVersion` reads the real version with `RtlGetVersion`, also when the application has no manifest which declares
the newer Windows versions. `IsWindows10` means exactly Windows 10; use the `...OrLater` properties for "this or newer".

<!-- sample: MorePackagesSamples.WindowsVersionCheck -->
```csharp
// The real version, also without an application manifest
Console.WriteLine($"Windows {WindowsVersion.WinVersion}");
if (WindowsVersion.IsWindows11OrLater)
{
    Console.WriteLine("Rounded corners are available");
}
```

Dapplo.Windows.Common also has the geometry structs (`NativeRect`, `NativePoint`, `NativeSize`, the `Float` variants)
with extension methods such as `Contains`, `Intersect`, `Union`, `Offset`, `Inflate`, `IsDockedToLeftOf`. Right and
Bottom are exclusive, as in a Win32 `RECT`. `HResult` has `Succeeded()`, `Failed()` and `ThrowOnFailure()`, and
`Win32.GetLastErrorCode()` / `Win32.GetMessage()` help with Win32 errors.

## Citrix (Dapplo.Windows.Citrix)

Detects a Citrix session through `WFAPI.DLL`; on a machine without Citrix `IsAvailable` is simply `false`.

<!-- sample: MorePackagesSamples.Citrix -->
```csharp
if (WinFrame.IsAvailable)
{
    Console.WriteLine($"Citrix session, client {WinFrame.GetClientName()} ({WinFrame.GetClientIpAddress()})");
}
```

## Desktop Window Manager (Dapplo.Windows.DesktopWindowsManager)

<!-- sample: MorePackagesSamples.Dwm -->
```csharp
// The window bounds as the user sees them, without the invisible resize borders
if (DwmApi.GetExtendedFrameBounds(hWnd, out var frameBounds))
{
    Console.WriteLine($"Visible frame: {frameBounds}");
}

// Windows which are "there" but not visible, e.g. on another virtual desktop or a suspended store app
bool isCloaked = DwmApi.IsWindowCloaked(hWnd);

// The accent color of the user
System.Drawing.Color accent = DwmApi.ColorizationSystemDrawingColor;
```

`DwmApi` also sets the window corner preference on Windows 11 (`SetWindowCornerPreference`), and
`DwmSetWindowAttribute` gives access to the other attributes (dark title bar, backdrop type, ...).

## Devices

**Dapplo.Windows.Devices** reports `WM_DEVICECHANGE` of the SharedMessageWindow: devices and volumes which arrive or
are removed. The events arrive on the SharedMessageWindow thread and are copies, so they can be used after `ObserveOn`.

<!-- sample: MorePackagesSamples.Devices -->
```csharp
// WM_DEVICECHANGE, on the SharedMessageWindow thread
var arrivals = DeviceNotification.OnDeviceArrival()
    .Subscribe(info => Console.WriteLine($"Connected: {info.Device.FriendlyDeviceName} (USB: {info.Device.IsUsb})"));

var removals = DeviceNotification.OnDeviceRemoved()
    .Subscribe(info => Console.WriteLine($"Removed: {info.Device.FriendlyDeviceName}"));

// Drives: USB sticks, network drives, CDs
var volumes = DeviceNotification.OnVolumeAdded()
    .Subscribe(info => Console.WriteLine($"New drive(s): {info.Volume.Drives}"));
```

`DeviceNotification.CreateDeviceNotificationObservable(DeviceInterfaceClass)` limits the notifications to one device
interface class.

## Registry changes (Dapplo.Windows.Advapi32)

<!-- sample: MorePackagesSamples.Registry -->
```csharp
// Produces a value (Unit) on a thread-pool thread for every change of the key, e.g. the theme setting
var subscription = RegistryMonitor
    .ObserveChanges(RegistryHive.CurrentUser, @"Software\Microsoft\Windows\CurrentVersion\Themes\Personalize")
    .Throttle(TimeSpan.FromMilliseconds(200))
    .Subscribe(_ => Console.WriteLine("The theme settings changed"));
```

`Advapi32Api.CurrentLogonSid` returns the logon SID of the current session.

## Kernel32 (Dapplo.Windows.Kernel32)

<!-- sample: MorePackagesSamples.Kernel32 -->
```csharp
// Call first thing in Main: load DLLs only from System32 (and the application directory when you allow it)
Kernel32Api.PreventDllHijacking(searchApplicationDirectory: true);

// The path of another process, also for elevated processes where Process.MainModule fails
using var explorer = Process.GetProcessesByName("explorer")[0];
string path = explorer.GetProcessPath();

// Running as a packaged (MSIX / Store) application?
bool isPackaged = PackageInfo.HasPackageIdentity;
```

## Shell32 (Dapplo.Windows.Shell32)

<!-- sample: MorePackagesSamples.Shell32 -->
```csharp
if (Shell32Api.TryGetTaskbarPosition(out var appBarData))
{
    Console.WriteLine($"The taskbar is at {appBarData.AppBarEdge}, bounds {appBarData.Bounds}, state {Shell32Api.GetTaskbarState()}");
}
```

## Sounds (Dapplo.Windows.Multimedia)

<!-- sample: MorePackagesSamples.Sounds -->
```csharp
// One of the sounds of the Windows sound scheme
WinMm.PlaySystemSound(SystemSounds.SystemAsterisk);

// A WAV file, asynchronous. The data is copied, it plays until it's done or StopPlaying is called.
WinMm.Play(System.IO.File.ReadAllBytes(@"C:\Windows\Media\chimes.wav"));

// A WAVE resource of the executable
WinMm.Play("NotificationSound");
```

## Embedded browser (Dapplo.Windows.EmbeddedBrowser)

The Windows Forms `WebBrowser` control renders pages like Internet Explorer 7 unless the process is registered for a
newer mode (`FEATURE_BROWSER_EMULATION`). `ExtendedWebBrowser` is a `WebBrowser` which doesn't show the script error dialog and keeps
the scripts of the page running after an error. This package references Windows Forms.

<!-- sample: MorePackagesSamples.EmbeddedBrowser -->
```csharp
// Call before the first WebBrowser is created: without this the WebBrowser control renders like IE 7
InternetExplorerVersion.ChangeEmbeddedVersion();
Console.WriteLine($"Installed Internet Explorer version: {InternetExplorerVersion.Version}");
```

## Low-level API packages

**Dapplo.Windows.User32**, **Dapplo.Windows.Gdi32**, **Dapplo.Windows.Kernel32**, **Dapplo.Windows.Shell32** and
**Dapplo.Windows.Com** contain the P/Invoke declarations, structs, enums and safe handles which the other packages use.
They are documented in the [API reference](../api/index.md). A few notes:

- User32 imports that handle text use the Unicode (W) entry points; WPARAM, LPARAM and LRESULT are pointer-sized.
- GDI handles are wrapped in `SafeHandle` types (`SafeHBitmapHandle`, `SafeWindowDcHandle`, ...), dispose them.
- `User32Api.TrySendMessage` sends with a timeout, so a hung window doesn't block you.
