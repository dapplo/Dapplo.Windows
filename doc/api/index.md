# API reference

The reference is generated from the XML documentation of the packages. Pick a namespace in the tree on the left; this
page tells you which package contains which namespaces and where to start. For explanations and samples, read the
[documentation](../articles/intro.md). The reference shows the `net10.0-windows` build; a few types exist only in the
.NET Framework build.

## Feature packages

| Package | Namespaces | Start with |
|---|---|---|
| Dapplo.Windows | `Dapplo.Windows.Desktop`, `.App`, `.Enums`, `.Icons`, `.Software`, `.Structs` | `InteropWindowFactory`, `InteropWindowQuery`, `InteropWindowExtensions`, `WinEventHook`, `WindowScroller`, `InstallationInformation` |
| Dapplo.Windows.Clipboard | `Dapplo.Windows.Clipboard` | `ClipboardNative`, `IClipboardAccessToken` and its extension classes |
| Dapplo.Windows.Input | `Dapplo.Windows.Input`, `.Enums`, `.Keyboard`, `.Mouse`, `.Structs` | `KeyboardHook`, `KeyCombinationHandler`, `KeyboardInputGenerator`, `MouseHook`, `MouseInputGenerator`, `RawInputMonitor` |
| Dapplo.Windows.Dpi | `Dapplo.Windows.Dpi`, `.Enums` | `DpiHandler`, `DpiCalculator`, `BitmapScaleHandler`, `NativeDpiMethods` |
| Dapplo.Windows.Forms | `Dapplo.Windows.Forms`, `.Dpi`, `.Messages` | `DpiAwareForm`, `FormsDpiExtensions`, `WinProcFormsExtensions` |
| Dapplo.Windows.Wpf | `Dapplo.Windows.Wpf`, `.Dpi`, `.Messages` | `WindowDpiExtensions`, `WinProcWindowsExtensions`, `NativeStructWpfExtensions`, `BitmapSourceExtensions` |
| Dapplo.Windows.Messages | `Dapplo.Windows.Messages`, `.Enums`, `.Structs`, `.Native` | `SharedMessageWindow`, `WindowMessage`, `RegisteredWindowMessages`, `WindowsSessionListener`, `WindowsMessages` |
| Dapplo.Windows.SystemState | `Dapplo.Windows.SystemState`, `.Enums` | `SystemStateApi`, `SleepBlocker`, `PowerManagementApi`, `WaitableTimer`, `PowerBroadcastListener` |
| Dapplo.Windows.AppRestartManager | `Dapplo.Windows.AppRestartManager`, `.Enums` | `ApplicationRestartManager`, `EndSessionMessage` |
| Dapplo.Windows.InstallerManager | `Dapplo.Windows.InstallerManager`, `.Enums`, `.Structs` | `InstallerRestartManager`, `RestartManagerApi` |
| Dapplo.Windows.Dialogs | `Dapplo.Windows.Dialogs` | `FileDialog`, `FileOpenDialogBuilder`, `FileSaveDialogBuilder`, `FolderPickerBuilder` |
| Dapplo.Windows.Icons | `Dapplo.Windows.Icons`, `.Enums`, `.SafeHandles`, `.Structs` | `IconHelper`, `IconFileWriter`, `CursorHelper` |
| Dapplo.Windows.Devices | `Dapplo.Windows.Devices`, `.Enums`, `.Structs` | `DeviceNotification` |
| Dapplo.Windows.DesktopWindowsManager | `Dapplo.Windows.DesktopWindowsManager`, `.Enums`, `.Structs` | `DwmApi` |
| Dapplo.Windows.Citrix | `Dapplo.Windows.Citrix`, `.Enums`, `.Structs` | `WinFrame` |
| Dapplo.Windows.EmbeddedBrowser | `Dapplo.Windows.EmbeddedBrowser` | `InternetExplorerVersion`, `ExtendedWebBrowser` |
| Dapplo.Windows.Multimedia | `Dapplo.Windows.Multimedia`, `.Enums` | `WinMm` |
| Dapplo.Windows.Advapi32 | `Dapplo.Windows.Advapi32`, `.Enums`, `.Structs` | `RegistryMonitor`, `Advapi32Api` |

## Native API packages

| Package | Namespaces | Start with |
|---|---|---|
| Dapplo.Windows.User32 | `Dapplo.Windows.User32`, `.Enums`, `.SafeHandles`, `.Structs`, `.TypeConverters` | `User32Api`, `DisplayInfo` |
| Dapplo.Windows.Gdi32 | `Dapplo.Windows.Gdi32`, `.Enums`, `.SafeHandles`, `.Structs` | `Gdi32Api`, the safe handles |
| Dapplo.Windows.Kernel32 | `Dapplo.Windows.Kernel32`, `.Enums`, `.Structs` | `Kernel32Api`, `PackageInfo` |
| Dapplo.Windows.Shell32 | `Dapplo.Windows.Shell32`, `.Enums`, `.Structs` | `Shell32Api` |
| Dapplo.Windows.Com | `Dapplo.Windows.Com` | `DisposableCom`, `Ole32Api` (`ComWrapper` is only in the .NET Framework build) |
| Dapplo.Windows.Common | `Dapplo.Windows.Common`, `.Enums`, `.Extensions`, `.Structs`, `.Structs.PixelFormats`, `.TypeConverters` | `NativeRect`, `NativePoint`, `NativeSize`, `HResult`, `Win32`, `WindowsVersion` |

Some packages add types to a namespace of another package: the window icon extensions of Dapplo.Windows are in
`Dapplo.Windows.Icons`.
