# Getting started

Full version: [Getting started](https://www.dapplo.net/Dapplo.Windows/articles/intro.html) in the documentation.

## Requirements

- Windows
- .NET Framework 4.8 or .NET 10 (`net480` and `net10.0-windows`)

## Installation

Install the package for the feature you need, the [[Home]] page lists them with their dependencies.

```powershell
dotnet add package Dapplo.Windows            # windows and window events
dotnet add package Dapplo.Windows.Clipboard  # clipboard
dotnet add package Dapplo.Windows.Input      # keyboard and mouse hooks
dotnet add package Dapplo.Windows.Dpi        # DPI awareness (plus .Forms or .Wpf)
```

## First steps

List the visible application windows:

<!-- sample: GettingStartedSamples.FirstWindowQuery -->
```csharp
// using Dapplo.Windows.Desktop;
foreach (var window in InteropWindowQuery.GetTopLevelWindows())
{
    Console.WriteLine($"{window.GetCaption()} - {window.GetClassname()} at {window.GetInfo().Bounds}");
}
```

Get told when a window title changes:

<!-- sample: GettingStartedSamples.WindowTitles -->
```csharp
// using Dapplo.Windows.Desktop; using System.Reactive.Linq;
IDisposable subscription = WinEventHook.WindowTitleChangeObservable()
    .Subscribe(info =>
    {
        var window = InteropWindowFactory.CreateFor(info.Handle);
        Console.WriteLine($"Title changed: {window.GetCaption(forceUpdate: true)}");
    });
```

Log the text that is copied to the clipboard:

<!-- sample: GettingStartedSamples.FirstClipboardMonitor -->
```csharp
// using Dapplo.Windows.Clipboard; using System.Reactive.Linq;
IDisposable subscription = ClipboardNative.OnUpdate
    .Where(info => info.FormatIds.Contains((uint)StandardClipboardFormats.UnicodeText))
    .Throttle(TimeSpan.FromMilliseconds(100))
    .Subscribe(info =>
    {
        using var clipboard = ClipboardNative.Access();
        Console.WriteLine($"Copied: {clipboard.GetAsUnicodeString()}");
    });
```

A global hotkey:

<!-- sample: GettingStartedSamples.FirstKeyboardHook -->
```csharp
// using Dapplo.Windows.Input.Enums; using Dapplo.Windows.Input.Keyboard; using System.Reactive.Linq;
// Ctrl+Shift+S anywhere in Windows. The handler runs on the hook thread, ObserveOn moves the work to the UI thread.
IDisposable subscription = KeyboardHook.KeyboardEvents
    .Where(new KeyCombinationHandler(VirtualKeyCode.Control, VirtualKeyCode.Shift, VirtualKeyCode.KeyS))
    .ObserveOn(SynchronizationContext.Current)
    .Subscribe(_ => Console.WriteLine("Ctrl+Shift+S pressed"));
```

## Rules that apply everywhere

- **Subscribing installs, disposing removes.** Hooks and registrations exist while you're subscribed. Keep the
  `IDisposable` and dispose it.
- **Events arrive on background threads.** Window messages (clipboard, power, session, devices, WinEvents, raw input)
  arrive on the thread of the [[SharedMessageWindow]], keyboard and mouse events on the hook thread. Keep `OnNext`
  short and use `ObserveOn` for UI or slow work.
- **Answer synchronously.** Set `Handled` / `Result` inside `OnNext`, before any `ObserveOn`, `Throttle` or `await`.
- **A failing subscriber doesn't break the others.** Its exception ends only its own subscription and is published on
  `SubscriberErrors`.

<!-- sample: GettingStartedSamples.Dispose -->
```csharp
// Hooks and registrations are made when you subscribe, and removed when you dispose the subscription
IDisposable subscription = ClipboardNative.OnUpdate.Subscribe(info => { });
// ...
subscription.Dispose();

// Clipboard access is a lock for all applications: hold it briefly and always dispose it
using (var clipboard = ClipboardNative.Access())
{
    // ...
}
```

## Next

- [[Window-Management]], [[Input-Handling]], [[Clipboard]], [[DPI-Awareness]]
- [Migrating to 3.0](https://www.dapplo.net/Dapplo.Windows/articles/migration-3.0.html)
