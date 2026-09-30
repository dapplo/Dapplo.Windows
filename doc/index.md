# Dapplo.Windows

Dapplo.Windows gives .NET applications on Windows access to the parts of the operating system which .NET doesn't
cover: windows and their events, keyboard and mouse hooks, the clipboard, DPI awareness, power management, the Restart
Manager, file dialogs and more. It was built for [Greenshot](https://getgreenshot.org/) and is split into small NuGet
packages for .NET Framework 4.8 and .NET 10.

- [Getting started](articles/intro.md): requirements, the packages and how the library works
- [API reference](api/index.md)
- [Migrating to 3.0](articles/migration-3.0.md) and the
  [changelog](https://github.com/dapplo/Dapplo.Windows/blob/master/CHANGELOG.md)
- [Source code and issues on GitHub](https://github.com/dapplo/Dapplo.Windows)

## What you can do

| Topic | Package |
|---|---|
| [Find, inspect, move and capture windows, react to window events](articles/window-management.md) | Dapplo.Windows |
| [Receive window messages without a window of your own](articles/window-messages.md) | Dapplo.Windows.Messages |
| [Global keyboard and mouse hooks, hotkeys, send input](articles/input-handling.md) | Dapplo.Windows.Input |
| [Monitor, read and write the clipboard](articles/clipboard-usage.md) | Dapplo.Windows.Clipboard |
| [Follow the DPI of each monitor](articles/dpi-awareness.md) | Dapplo.Windows.Dpi, .Forms, .Wpf |
| [Keep the PC awake, sleep, shut down, wake timers, power events](articles/system-state.md) | Dapplo.Windows.SystemState |
| [Survive updates: restart registration and installer support](articles/restart-manager.md) | Dapplo.Windows.AppRestartManager, .InstallerManager |
| [Extract icons, write ICO / CUR files, capture the cursor](articles/icons.md) | Dapplo.Windows.Icons |
| [File and folder dialogs without WinForms or WPF](articles/dialogs.md) | Dapplo.Windows.Dialogs |
| [Windows Forms and WPF integration](articles/forms-and-wpf.md) | Dapplo.Windows.Forms, .Wpf |
| [Citrix, DWM, devices, registry, sounds and more](articles/more-packages.md) | several |
| [Recipes](articles/common-scenarios.md) | several |

## A first example

<!-- sample: GettingStartedSamples.FirstKeyboardHook -->
```csharp
// using Dapplo.Windows.Input.Enums; using Dapplo.Windows.Input.Keyboard; using System.Reactive.Linq;
// Ctrl+Shift+S anywhere in Windows. The handler runs on the hook thread, ObserveOn moves the work to the UI thread.
IDisposable subscription = KeyboardHook.KeyboardEvents
    .Where(new KeyCombinationHandler(VirtualKeyCode.Control, VirtualKeyCode.Shift, VirtualKeyCode.KeyS))
    .ObserveOn(SynchronizationContext.Current)
    .Subscribe(_ => Console.WriteLine("Ctrl+Shift+S pressed"));
```

## License

MIT, see [LICENSE](https://github.com/dapplo/Dapplo.Windows/blob/master/LICENSE).
