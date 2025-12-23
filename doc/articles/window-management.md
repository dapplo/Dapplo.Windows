# Window Management Guide

This guide covers working with windows using the `Dapplo.Windows` package.

## Overview

The `Dapplo.Windows` package provides the `InteropWindow` class, which represents a Windows window and provides methods for querying and manipulating it.

## Getting Window Information

### From Window Handle

```csharp
using Dapplo.Windows.Desktop;
using Dapplo.Windows.User32;

// Get the foreground window
IntPtr handle = User32Api.GetForegroundWindow();
IInteropWindow window = InteropWindow.FromHandle(handle);

// Populate all window properties
window.Fill();

// Access properties
Console.WriteLine($"Title: {window.Caption}");
Console.WriteLine($"Class: {window.Classname}");
Console.WriteLine($"Process ID: {window.ProcessId}");
Console.WriteLine($"Bounds: {window.Bounds}");
```

### Selective Property Loading

For better performance, load only the properties you need:

```csharp
using Dapplo.Windows.Desktop;
using Dapplo.Windows.Enums;

var window = InteropWindow.FromHandle(handle);

// Load only caption and bounds
window.Fill(InteropWindowRetrieveSettings.Caption | 
            InteropWindowRetrieveSettings.Info);

Console.WriteLine($"{window.Caption} at {window.Bounds}");
```

## Enumerating Windows

### Get All Top-Level Windows

```csharp
using Dapplo.Windows.Desktop;
using System.Linq;

var windows = InteropWindowQuery.GetTopLevelWindows();

foreach (var window in windows)
{
    Console.WriteLine($"{window.Caption} ({window.Classname})");
}
```

### Filter Windows

```csharp
using Dapplo.Windows.Desktop;

// Get all visible windows
var visibleWindows = InteropWindowQuery.GetTopLevelWindows()
    .Where(w => w.IsVisible())
    .ToList();

// Get windows by process name
var notepadWindows = InteropWindowQuery.GetTopLevelWindows()
    .Where(w => w.GetProcessPath()?.Contains("notepad.exe") ?? false)
    .ToList();

// Get windows by class name
var explorerWindows = InteropWindowQuery.GetTopLevelWindows()
    .Where(w => w.GetClassname() == "CabinetWClass")
    .ToList();
```

### Get Child Windows

```csharp
using Dapplo.Windows.Desktop;

var parentWindow = InteropWindow.FromHandle(handle);
parentWindow.Fill(InteropWindowRetrieveSettings.Children);

foreach (var child in parentWindow.Children)
{
    Console.WriteLine($"Child: {child.Caption} ({child.Classname})");
}
```

## Window Manipulation

### Show/Hide Windows

```csharp
using Dapplo.Windows.Desktop;
using Dapplo.Windows.User32.Enums;

var window = InteropWindow.FromHandle(handle);

// Show the window
window.Show();

// Hide the window
window.Hide();

// Minimize
window.Minimize();

// Maximize
window.Maximize();

// Restore (unmaximize/unminimize)
window.Restore();
```

### Move and Resize

```csharp
using Dapplo.Windows.Desktop;
using Dapplo.Windows.Common.Structs;

var window = InteropWindow.FromHandle(handle);

// Move window
window.MoveTo(new NativePoint(100, 100));

// Resize window
window.Resize(new NativeSize(800, 600));

// Move and resize together
window.SetPlacement(new NativeRect(100, 100, 900, 700));
```

### Bring to Front

```csharp
using Dapplo.Windows.Desktop;

var window = InteropWindow.FromHandle(handle);

// Bring window to foreground
window.ToForeground();

// Flash window in taskbar
window.Flash();
```

## Monitoring Window Events

### Window Creation and Destruction

```csharp
using Dapplo.Windows.Desktop;
using System;

// Monitor window creation
var creationSubscription = WinEventHook.Create(WinEvents.EVENT_OBJECT_CREATE)
    .Subscribe(winEvent =>
    {
        var window = InteropWindow.FromHandle(winEvent.Handle);
        Console.WriteLine($"Window created: {window.GetCaption()}");
    });

// Monitor window destruction
var destructionSubscription = WinEventHook.Create(WinEvents.EVENT_OBJECT_DESTROY)
    .Subscribe(winEvent =>
    {
        Console.WriteLine($"Window destroyed: {winEvent.Handle}");
    });

// Clean up when done
creationSubscription.Dispose();
destructionSubscription.Dispose();
```

### Window Title Changes

```csharp
using Dapplo.Windows.Desktop;

var subscription = WinEventHook.Create(WinEvents.EVENT_OBJECT_NAMECHANGE)
    .Subscribe(winEvent =>
    {
        var window = InteropWindow.FromHandle(winEvent.Handle);
        Console.WriteLine($"Title changed: {window.GetCaption()}");
    });
```

### Foreground Window Changes

```csharp
using Dapplo.Windows.Desktop;

var subscription = WinEventHook.Create(WinEvents.EVENT_SYSTEM_FOREGROUND)
    .Subscribe(winEvent =>
    {
        var window = InteropWindow.FromHandle(winEvent.Handle);
        Console.WriteLine($"Foreground window: {window.GetCaption()}");
    });
```

### Window Location/Size Changes

```csharp
using Dapplo.Windows.Desktop;

var subscription = WinEventHook.Create(WinEvents.EVENT_OBJECT_LOCATIONCHANGE)
    .Subscribe(winEvent =>
    {
        var window = InteropWindow.FromHandle(winEvent.Handle);
        window.Fill(InteropWindowRetrieveSettings.Info);
        Console.WriteLine($"Window moved/resized: {window.Caption} at {window.Bounds}");
    });
```

### Filter Events by Process or Window

```csharp
using Dapplo.Windows.Desktop;
using System.Reactive.Linq;

// Only monitor specific window
var targetHandle = /* your window handle */;
var subscription = WinEventHook.Create(WinEvents.EVENT_OBJECT_NAMECHANGE)
    .Where(winEvent => winEvent.Handle == targetHandle)
    .Subscribe(winEvent =>
    {
        Console.WriteLine("Target window title changed");
    });

// Only monitor windows from specific process
var targetProcessId = /* your process ID */;
var processSubscription = WinEventHook.Create(WinEvents.EVENT_OBJECT_CREATE)
    .Where(winEvent => 
    {
        var window = InteropWindow.FromHandle(winEvent.Handle);
        return window.GetProcessId() == targetProcessId;
    })
    .Subscribe(winEvent =>
    {
        Console.WriteLine("Window created in target process");
    });
```

## Advanced Scenarios

### Find Window by Title

```csharp
using Dapplo.Windows.Desktop;
using System.Linq;

string targetTitle = "Notepad";
var window = InteropWindowQuery.GetTopLevelWindows()
    .FirstOrDefault(w => w.GetCaption()?.Contains(targetTitle) ?? false);

if (window != null)
{
    Console.WriteLine($"Found window: {window.Caption}");
}
```

### Get Windows Stacking Order (Z-Order)

```csharp
using Dapplo.Windows.Desktop;

var desktop = InteropWindow.GetDesktopWindow();
desktop.Fill(InteropWindowRetrieveSettings.ZOrderedChildren);

Console.WriteLine("Windows in Z-order (top to bottom):");
foreach (var window in desktop.ZOrderedChildren.Where(w => w.IsVisible()))
{
    Console.WriteLine($"  {window.Caption}");
}
```

### Window Screenshots

```csharp
using Dapplo.Windows.Desktop;
using Dapplo.Windows.Gdi32;
using System.Drawing;
using System.Drawing.Imaging;

var window = InteropWindow.FromHandle(handle);
window.Fill(InteropWindowRetrieveSettings.Info);

// Capture window content
using (var bitmap = window.Capture())
{
    bitmap.Save("window_capture.png", ImageFormat.Png);
}
```

### Check Window State

```csharp
using Dapplo.Windows.Desktop;

var window = InteropWindow.FromHandle(handle);

if (window.IsVisible())
{
    Console.WriteLine("Window is visible");
}

if (window.IsMinimized())
{
    Console.WriteLine("Window is minimized");
}

if (window.IsMaximized())
{
    Console.WriteLine("Window is maximized");
}

if (window.IsAppWindow())
{
    Console.WriteLine("Window is an application window");
}
```

## Best Practices

### 1. Check Window Validity

Always check if a window still exists before using it:

```csharp
var window = InteropWindow.FromHandle(handle);

if (window.Exists())
{
    // Safe to use the window
    Console.WriteLine(window.GetCaption());
}
```

### 2. Dispose Subscriptions

Always dispose of event subscriptions when done:

```csharp
var subscription = WinEventHook.Create(WinEvents.EVENT_OBJECT_CREATE)
    .Subscribe(HandleWindowCreated);

// Later, when cleaning up
subscription.Dispose();
```

### 3. Handle Exceptions

Windows can be destroyed or become invalid:

```csharp
try
{
    var caption = window.GetCaption();
}
catch (Win32Exception ex)
{
    Console.WriteLine($"Error accessing window: {ex.Message}");
}
```

### 4. Cache Window Information

If you're repeatedly accessing window properties, cache them:

```csharp
var window = InteropWindow.FromHandle(handle);
window.Fill(); // Load all properties at once

// Now properties are cached
var caption = window.Caption;
var bounds = window.Bounds;
var processId = window.ProcessId;
```

## See Also

- [API Reference](../api/index.md)
- [Getting Started](intro.md)
- [Common Scenarios](common-scenarios.md)
