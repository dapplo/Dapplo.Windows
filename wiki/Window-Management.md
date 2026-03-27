# Window Management

The `Dapplo.Windows` package provides the `InteropWindow` class for querying, enumerating, and manipulating windows through native Windows APIs.

## Installation

```powershell
Install-Package Dapplo.Windows
```

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

Console.WriteLine($"Title:    {window.Caption}");
Console.WriteLine($"Class:    {window.Classname}");
Console.WriteLine($"PID:      {window.ProcessId}");
Console.WriteLine($"Bounds:   {window.Bounds}");
```

### Selective Property Loading

For better performance, load only the properties you need:

```csharp
using Dapplo.Windows.Desktop;

var window = InteropWindow.FromHandle(handle);
window.Fill(InteropWindowRetrieveSettings.Caption |
            InteropWindowRetrieveSettings.Info);

Console.WriteLine($"{window.Caption} at {window.Bounds}");
```

## Enumerating Windows

### All Top-Level Windows

```csharp
using Dapplo.Windows.Desktop;

var windows = InteropWindowQuery.GetTopLevelWindows();
foreach (var window in windows)
{
    Console.WriteLine($"{window.Caption} ({window.Classname})");
}
```

### Filtered Windows

```csharp
using Dapplo.Windows.Desktop;
using System.Linq;

// Visible windows only
var visible = InteropWindowQuery.GetTopLevelWindows()
    .Where(w => w.IsVisible())
    .ToList();

// By process executable name
var notepadWindows = InteropWindowQuery.GetTopLevelWindows()
    .Where(w => w.GetProcessPath()?.Contains("notepad.exe") ?? false)
    .ToList();

// By window class
var explorerWindows = InteropWindowQuery.GetTopLevelWindows()
    .Where(w => w.GetClassname() == "CabinetWClass")
    .ToList();
```

### Child Windows

```csharp
using Dapplo.Windows.Desktop;

var parent = InteropWindow.FromHandle(handle);
parent.Fill(InteropWindowRetrieveSettings.Children);

foreach (var child in parent.Children)
{
    Console.WriteLine($"  {child.Caption} ({child.Classname})");
}
```

## Window Manipulation

### Show, Hide, and State Changes

```csharp
using Dapplo.Windows.Desktop;

var window = InteropWindow.FromHandle(handle);

window.Show();
window.Hide();
window.Minimize();
window.Maximize();
window.Restore();
```

### Move and Resize

```csharp
using Dapplo.Windows.Desktop;
using Dapplo.Windows.Common.Structs;

var window = InteropWindow.FromHandle(handle);

window.MoveTo(new NativePoint(100, 100));
window.Resize(new NativeSize(800, 600));
window.SetPlacement(new NativeRect(100, 100, 900, 700));
```

### Bring to Front

```csharp
var window = InteropWindow.FromHandle(handle);
window.ToForeground();
window.Flash();    // Flash in the taskbar
```

## Monitoring Window Events

`WinEventHook` provides a reactive stream of WinEvents. Dispose the subscription when you no longer need it.

### Window Creation and Destruction

```csharp
using Dapplo.Windows.Desktop;

var createSub = WinEventHook.Create(WinEvents.EVENT_OBJECT_CREATE)
    .Subscribe(e =>
    {
        var w = InteropWindow.FromHandle(e.Handle);
        Console.WriteLine($"Created: {w.GetCaption()}");
    });

var destroySub = WinEventHook.Create(WinEvents.EVENT_OBJECT_DESTROY)
    .Subscribe(e => Console.WriteLine($"Destroyed: {e.Handle}"));

// Clean up
createSub.Dispose();
destroySub.Dispose();
```

### Title Changes

```csharp
WinEventHook.Create(WinEvents.EVENT_OBJECT_NAMECHANGE)
    .Subscribe(e =>
    {
        var w = InteropWindow.FromHandle(e.Handle);
        Console.WriteLine($"Title changed: {w.GetCaption()}");
    });
```

### Foreground Window Changes

```csharp
WinEventHook.Create(WinEvents.EVENT_SYSTEM_FOREGROUND)
    .Subscribe(e =>
    {
        var w = InteropWindow.FromHandle(e.Handle);
        Console.WriteLine($"Active window: {w.GetCaption()}");
    });
```

### Location / Size Changes

```csharp
WinEventHook.Create(WinEvents.EVENT_OBJECT_LOCATIONCHANGE)
    .Subscribe(e =>
    {
        var w = InteropWindow.FromHandle(e.Handle);
        w.Fill(InteropWindowRetrieveSettings.Info);
        Console.WriteLine($"Moved/resized: {w.Caption} → {w.Bounds}");
    });
```

### Filtering Events

Use Rx operators to narrow the stream:

```csharp
using System.Reactive.Linq;

// Only events for a specific window
WinEventHook.Create(WinEvents.EVENT_OBJECT_NAMECHANGE)
    .Where(e => e.Handle == targetHandle)
    .Subscribe(e => Console.WriteLine("Target title changed"));

// Only events for a specific process
WinEventHook.Create(WinEvents.EVENT_OBJECT_CREATE)
    .Where(e => InteropWindow.FromHandle(e.Handle).GetProcessId() == myPid)
    .Subscribe(e => Console.WriteLine("Window created in my process"));
```

## Advanced Scenarios

### Find a Window by Title

```csharp
var window = InteropWindowQuery.GetTopLevelWindows()
    .FirstOrDefault(w => w.GetCaption()?.Contains("Notepad") ?? false);
```

### Z-Order (Stacking Order)

```csharp
var desktop = InteropWindow.GetDesktopWindow();
desktop.Fill(InteropWindowRetrieveSettings.ZOrderedChildren);

foreach (var w in desktop.ZOrderedChildren.Where(w => w.IsVisible()))
{
    Console.WriteLine(w.Caption);
}
```

### Check Window State

```csharp
var window = InteropWindow.FromHandle(handle);

Console.WriteLine($"Visible:    {window.IsVisible()}");
Console.WriteLine($"Minimized:  {window.IsMinimized()}");
Console.WriteLine($"Maximized:  {window.IsMaximized()}");
Console.WriteLine($"App window: {window.IsAppWindow()}");
```

## Best Practices

1. **Always check validity** before using a window handle — windows can be destroyed at any time:

   ```csharp
   if (window.Exists())
       Console.WriteLine(window.GetCaption());
   ```

2. **Dispose subscriptions** to avoid memory leaks:

   ```csharp
   using var sub = WinEventHook.Create(WinEvents.EVENT_OBJECT_CREATE).Subscribe(...);
   ```

3. **Cache properties** — call `Fill()` once and use the cached values rather than making repeated API calls.

4. **Handle `Win32Exception`** — windows may become invalid between your check and use.

## See Also

- [[Getting-Started]]
- [[Common-Scenarios]]
- [GitHub Repository](https://github.com/dapplo/Dapplo.Windows)
