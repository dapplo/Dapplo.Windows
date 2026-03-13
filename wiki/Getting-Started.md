# Getting Started

This guide will help you install Dapplo.Windows and start using its packages.

## Prerequisites

- Windows operating system
- .NET Framework 4.6.2+ **or** .NET Core 3.1+ / .NET 5+
- Visual Studio 2019+ or any compatible IDE

## Installation

Dapplo.Windows is distributed as multiple NuGet packages. Install only the packages you need:

```powershell
# Window management and WinEvent hooks
Install-Package Dapplo.Windows

# Reactive clipboard monitoring and manipulation
Install-Package Dapplo.Windows.Clipboard

# DPI awareness and scaling
Install-Package Dapplo.Windows.Dpi

# Low-level keyboard and mouse hooks, input generation
Install-Package Dapplo.Windows.Input
```

Or with the .NET CLI:

```bash
dotnet add package Dapplo.Windows
dotnet add package Dapplo.Windows.Clipboard
dotnet add package Dapplo.Windows.Dpi
dotnet add package Dapplo.Windows.Input
```

## Your First Window Query

Get information about the currently active window:

```csharp
using Dapplo.Windows.Desktop;
using Dapplo.Windows.User32;

// Get the foreground window
var handle = User32Api.GetForegroundWindow();
var window = InteropWindow.FromHandle(handle);
window.Fill();

Console.WriteLine($"Title:   {window.Caption}");
Console.WriteLine($"Class:   {window.Classname}");
Console.WriteLine($"Bounds:  {window.Bounds}");
Console.WriteLine($"Visible: {window.IsVisible()}");
```

## Your First Clipboard Monitor

Subscribe to clipboard changes reactively:

```csharp
using Dapplo.Windows.Clipboard;
using System.Reactive.Linq;

var subscription = ClipboardNative.OnUpdate
    .Where(info => info.Formats.Contains("Text"))
    .Subscribe(info =>
    {
        using var clipboard = ClipboardNative.Access();
        Console.WriteLine($"New clipboard text: {clipboard.GetAsString()}");
    });

// Keep the application alive (e.g., Console.ReadLine()) then clean up:
subscription.Dispose();
```

## Your First Keyboard Hook

React to keyboard input system-wide:

```csharp
using Dapplo.Windows.Input.Keyboard;
using System.Reactive.Linq;

using var keyboardHook = KeyboardHook.Create();

var subscription = keyboardHook.KeyboardEvents
    .Where(e => e.Key == VirtualKeyCode.Snapshot && e.IsDown)
    .Subscribe(_ => Console.WriteLine("Print Screen pressed!"));

Console.ReadLine();
```

## Resource Management

Always dispose subscriptions and hooks when you are done:

```csharp
// IDisposable-based hooks and subscriptions
using var keyboardHook = KeyboardHook.Create();
using var subscription = keyboardHook.KeyboardEvents.Subscribe(...);

// Clipboard access uses a lock — always release it
using (var clipboard = ClipboardNative.Access())
{
    var text = clipboard.GetAsString();
}
```

## Threading

Many Windows APIs must be called from the correct thread. Use `ObserveOn` to marshal events to the UI thread when needed:

```csharp
using System.Reactive.Concurrency;

ClipboardNative.OnUpdate
    .ObserveOn(SynchronizationContext.Current)   // switch to UI thread
    .Subscribe(info => labelStatus.Text = "Clipboard changed");
```

## Next Steps

| Topic | What you will learn |
|-------|---------------------|
| [[Window-Management]] | Enumerate, query, and manipulate windows |
| [[Clipboard]] | Full clipboard API — read, write, formats, Cloud Clipboard |
| [[Input-Handling]] | Keyboard/mouse hooks and input generation |
| [[DPI-Awareness]] | Build crisp applications on high-DPI displays |
| [[Restart-Manager]] | Register for automatic restart with Windows |
| [[Icon-Creation]] | Extract and convert icons |
| [[Common-Scenarios]] | Real-world recipes using multiple packages |
