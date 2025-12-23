# Common Scenarios

This guide provides examples of common scenarios and use cases for Dapplo.Windows.

## Application Monitoring

### Monitor When a Specific Application Starts

```csharp
using Dapplo.Windows.Desktop;
using System.Reactive.Linq;

// Monitor for Notepad windows
var subscription = WinEventHook.Create(WinEvents.EVENT_OBJECT_CREATE)
    .Where(e => 
    {
        var window = InteropWindow.FromHandle(e.Handle);
        var processPath = window.GetProcessPath();
        return processPath?.Contains("notepad.exe") ?? false;
    })
    .Subscribe(e =>
    {
        Console.WriteLine("Notepad started!");
    });
```

### Track Active Window

```csharp
using Dapplo.Windows.Desktop;

string currentApp = "";

var subscription = WinEventHook.Create(WinEvents.EVENT_SYSTEM_FOREGROUND)
    .Subscribe(e =>
    {
        var window = InteropWindow.FromHandle(e.Handle);
        string appName = window.GetCaption();
        
        if (appName != currentApp)
        {
            currentApp = appName;
            Console.WriteLine($"Switched to: {currentApp}");
            
            // Track time spent in each app
            LogAppUsage(currentApp);
        }
    });
```

### Detect When Screen Locks

```csharp
using Dapplo.Windows.Desktop;
using Dapplo.Windows.Messages;
using System;

WinProcHandler.Instance.Subscribe(new WinProcHandlerHook((hwnd, msg, wparam, lparam, ref handled) =>
{
    if ((uint)msg == WindowsMessages.WM_WTSSESSION_CHANGE)
    {
        const int WTS_SESSION_LOCK = 0x7;
        const int WTS_SESSION_UNLOCK = 0x8;
        
        if (wparam.ToInt32() == WTS_SESSION_LOCK)
        {
            Console.WriteLine("Screen locked");
        }
        else if (wparam.ToInt32() == WTS_SESSION_UNLOCK)
        {
            Console.WriteLine("Screen unlocked");
        }
    }
    
    return IntPtr.Zero;
}));
```

## Clipboard Automation

### Auto-Save Clipboard History

```csharp
using Dapplo.Windows.Clipboard;
using System.Collections.Generic;
using System.Reactive.Linq;

var clipboardHistory = new List<string>();

var subscription = ClipboardNative.OnUpdate
    .Where(info => info.Formats.Contains("UnicodeText"))
    .Subscribe(info =>
    {
        using var clipboard = ClipboardNative.Access();
        var text = clipboard.GetAsString();
        
        if (!string.IsNullOrEmpty(text) && !clipboardHistory.Contains(text))
        {
            clipboardHistory.Add(text);
            Console.WriteLine($"Saved to history: {text.Substring(0, Math.Min(50, text.Length))}...");
            
            // Keep only last 100 items
            if (clipboardHistory.Count > 100)
            {
                clipboardHistory.RemoveAt(0);
            }
        }
    });
```

### Auto-Format Clipboard Text

```csharp
using Dapplo.Windows.Clipboard;
using System.Reactive.Linq;
using System.Text.RegularExpressions;

// Automatically clean up clipboard text
var subscription = ClipboardNative.OnUpdate
    .Where(info => info.Formats.Contains("UnicodeText"))
    .Throttle(TimeSpan.FromMilliseconds(100)) // Avoid rapid changes
    .Subscribe(info =>
    {
        using var clipboard = ClipboardNative.Access();
        var text = clipboard.GetAsString();
        
        // Remove multiple spaces, clean up line breaks
        var cleaned = Regex.Replace(text, @"\s+", " ").Trim();
        
        if (cleaned != text)
        {
            clipboard.SetAsUnicodeString(cleaned);
            Console.WriteLine("Cleaned clipboard text");
        }
    });
```

### Save Clipboard Images Automatically

```csharp
using Dapplo.Windows.Clipboard;
using System;
using System.IO;
using System.Reactive.Linq;

var subscription = ClipboardNative.OnUpdate
    .Where(info => info.Formats.Contains("PNG") || info.Formats.Contains("Bitmap"))
    .Subscribe(info =>
    {
        using var clipboard = ClipboardNative.Access();
        
        if (clipboard.AvailableFormats().Contains("PNG"))
        {
            var filename = $"clipboard_{DateTime.Now:yyyyMMdd_HHmmss}.png";
            using var stream = clipboard.GetAsStream("PNG");
            using var fileStream = File.Create(filename);
            stream.CopyTo(fileStream);
            
            Console.WriteLine($"Saved clipboard image: {filename}");
        }
    });
```

## Window Management Automation

### Auto-Tile Windows

```csharp
using Dapplo.Windows.Desktop;
using Dapplo.Windows.Common.Structs;
using Dapplo.Windows.User32;
using System.Linq;

void TileWindows()
{
    var windows = InteropWindowQuery.GetTopLevelWindows()
        .Where(w => w.IsVisible() && w.IsAppWindow())
        .ToList();
    
    if (windows.Count == 0) return;
    
    var screen = User32Api.GetWorkArea();
    int windowsPerRow = (int)Math.Ceiling(Math.Sqrt(windows.Count));
    int windowWidth = screen.Width / windowsPerRow;
    int windowHeight = screen.Height / ((windows.Count + windowsPerRow - 1) / windowsPerRow);
    
    for (int i = 0; i < windows.Count; i++)
    {
        int row = i / windowsPerRow;
        int col = i % windowsPerRow;
        
        var bounds = new NativeRect(
            col * windowWidth,
            row * windowHeight,
            (col + 1) * windowWidth,
            (row + 1) * windowHeight
        );
        
        windows[i].SetPlacement(bounds);
    }
}
```

### Minimize All Except Active

```csharp
using Dapplo.Windows.Desktop;
using Dapplo.Windows.User32;
using System.Linq;

void MinimizeAllExceptActive()
{
    var activeWindow = User32Api.GetForegroundWindow();
    
    var windows = InteropWindowQuery.GetTopLevelWindows()
        .Where(w => w.Handle != activeWindow && w.IsVisible())
        .ToList();
    
    foreach (var window in windows)
    {
        window.Minimize();
    }
}
```

### Always On Top Toggle

```csharp
using Dapplo.Windows.Desktop;
using Dapplo.Windows.User32;
using Dapplo.Windows.User32.Enums;

void ToggleAlwaysOnTop(IntPtr windowHandle)
{
    var window = InteropWindow.FromHandle(windowHandle);
    window.Fill(InteropWindowRetrieveSettings.ExtendedWindowStyle);
    
    bool isTopMost = (window.ExtendedWindowStyleFlags & ExtendedWindowStyleFlags.TopMost) != 0;
    
    User32Api.SetWindowPos(
        windowHandle,
        isTopMost ? User32Api.HWND_NOTOPMOST : User32Api.HWND_TOPMOST,
        0, 0, 0, 0,
        WindowPos.SWP_NOMOVE | WindowPos.SWP_NOSIZE
    );
    
    Console.WriteLine($"Always on top: {!isTopMost}");
}
```

## DPI-Aware Screenshot Tool

```csharp
using Dapplo.Windows.Desktop;
using Dapplo.Windows.Dpi;
using Dapplo.Windows.Gdi32;
using System.Drawing;
using System.Drawing.Imaging;

Bitmap TakeDpiAwareScreenshot()
{
    var window = User32Api.GetForegroundWindow();
    var interopWindow = InteropWindow.FromHandle(window);
    interopWindow.Fill(InteropWindowRetrieveSettings.Info);
    
    // Get window DPI
    var dpi = DpiHandler.GetDpiForWindow(window);
    
    // Capture window
    var bitmap = interopWindow.Capture();
    
    // Set DPI metadata
    bitmap.SetResolution(dpi, dpi);
    
    return bitmap;
}
```

## Input Automation

### Auto-Fill Form

```csharp
using Dapplo.Windows.Input.Keyboard;
using System.Threading.Tasks;

async Task FillForm(string name, string email, string message)
{
    // Focus first field (assuming it's already focused)
    KeyboardInputGenerator.TypeText(name);
    
    // Move to next field
    KeyboardInputGenerator.KeyPress(VirtualKeyCode.Tab);
    await Task.Delay(100);
    
    // Fill email
    KeyboardInputGenerator.TypeText(email);
    
    // Move to next field
    KeyboardInputGenerator.KeyPress(VirtualKeyCode.Tab);
    await Task.Delay(100);
    
    // Fill message
    KeyboardInputGenerator.TypeText(message);
    
    // Submit
    KeyboardInputGenerator.KeyPress(VirtualKeyCode.Return);
}
```

### Screenshot Hotkey

```csharp
using Dapplo.Windows.Input.Keyboard;
using Dapplo.Windows.Desktop;
using System.Reactive.Linq;
using System.Drawing.Imaging;

var keyboardHook = KeyboardHook.Create();

var subscription = keyboardHook.KeyboardEvents
    .Where(e => e.IsDown && e.Key == VirtualKeyCode.PrintScreen)
    .Subscribe(e =>
    {
        var window = User32Api.GetForegroundWindow();
        var interopWindow = InteropWindow.FromHandle(window);
        
        using var bitmap = interopWindow.Capture();
        var filename = $"screenshot_{DateTime.Now:yyyyMMdd_HHmmss}.png";
        bitmap.Save(filename, ImageFormat.Png);
        
        Console.WriteLine($"Screenshot saved: {filename}");
        
        // Suppress the default print screen behavior
        e.Handled = true;
    });
```

### Text Expansion

```csharp
using Dapplo.Windows.Input.Keyboard;
using System.Collections.Generic;
using System.Reactive.Linq;

var keyboardHook = KeyboardHook.Create();
var typedText = "";

var expansions = new Dictionary<string, string>
{
    ["btw"] = "by the way",
    ["brb"] = "be right back",
    ["thx"] = "thanks"
};

var subscription = keyboardHook.KeyboardEvents
    .Where(e => e.IsDown)
    .Subscribe(e =>
    {
        if (e.Key >= VirtualKeyCode.A && e.Key <= VirtualKeyCode.Z)
        {
            typedText += ((char)('a' + (e.Key - VirtualKeyCode.A))).ToString();
        }
        else if (e.Key == VirtualKeyCode.Space)
        {
            if (expansions.ContainsKey(typedText.ToLower()))
            {
                // Delete typed text
                for (int i = 0; i < typedText.Length; i++)
                {
                    KeyboardInputGenerator.KeyPress(VirtualKeyCode.Back);
                }
                
                // Type expansion
                KeyboardInputGenerator.TypeText(expansions[typedText.ToLower()]);
            }
            
            typedText = "";
        }
        else if (e.Key == VirtualKeyCode.Back && typedText.Length > 0)
        {
            typedText = typedText.Substring(0, typedText.Length - 1);
        }
    });
```

## System Integration

### Detect Citrix Session

```csharp
using Dapplo.Windows.Citrix;

void CheckCitrixEnvironment()
{
    if (WinFrame.IsAvailable)
    {
        var clientName = WinFrame.QuerySessionInformation(InfoClasses.ClientName);
        var clientAddress = WinFrame.QuerySessionInformation(InfoClasses.ClientAddress);
        
        Console.WriteLine($"Running in Citrix");
        Console.WriteLine($"Client: {clientName}");
        Console.WriteLine($"Address: {clientAddress}");
        
        // Adjust behavior for Citrix
        EnableCitrixOptimizations();
    }
    else
    {
        Console.WriteLine("Not running in Citrix");
    }
}
```

### List Installed Software

```csharp
using Dapplo.Windows.Shell32;
using System;

void ListInstalledSoftware()
{
    var software = SoftwareInventory.GetInstalledSoftware();
    
    foreach (var app in software.OrderBy(s => s.DisplayName))
    {
        Console.WriteLine($"{app.DisplayName} - {app.DisplayVersion}");
    }
}
```

### Monitor Display Changes

```csharp
using Dapplo.Windows.Messages;
using System;

WinProcHandler.Instance.Subscribe(new WinProcHandlerHook((hwnd, msg, wparam, lparam, ref handled) =>
{
    if ((uint)msg == WindowsMessages.WM_DISPLAYCHANGE)
    {
        Console.WriteLine("Display configuration changed");
        
        // Reposition windows or adjust layouts
        HandleDisplayChange();
    }
    
    return IntPtr.Zero;
}));
```

## Performance Optimization

### Debounce High-Frequency Events

```csharp
using Dapplo.Windows.Desktop;
using System.Reactive.Linq;

// Debounce window location changes
var subscription = WinEventHook.Create(WinEvents.EVENT_OBJECT_LOCATIONCHANGE)
    .Throttle(TimeSpan.FromMilliseconds(100))
    .Subscribe(e =>
    {
        // Only process after 100ms of no changes
        UpdateWindowLayout(e.Handle);
    });
```

### Batch Processing

```csharp
using Dapplo.Windows.Clipboard;
using System.Reactive.Linq;

// Batch clipboard changes
var subscription = ClipboardNative.OnUpdate
    .Buffer(TimeSpan.FromSeconds(5))
    .Where(batch => batch.Count > 0)
    .Subscribe(batch =>
    {
        Console.WriteLine($"Processed {batch.Count} clipboard changes");
        // Save batch to database, etc.
    });
```

## Error Handling Patterns

### Retry Logic

```csharp
using Dapplo.Windows.Clipboard;
using System;
using System.Threading;

T RetryOperation<T>(Func<T> operation, int maxRetries = 3, int delayMs = 100)
{
    for (int i = 0; i < maxRetries; i++)
    {
        try
        {
            return operation();
        }
        catch (ClipboardAccessDeniedException) when (i < maxRetries - 1)
        {
            Thread.Sleep(delayMs);
        }
    }
    
    throw new InvalidOperationException("Operation failed after retries");
}

// Usage
var text = RetryOperation(() =>
{
    using var clipboard = ClipboardNative.Access();
    return clipboard.GetAsString();
});
```

### Graceful Degradation

```csharp
using Dapplo.Windows.Desktop;
using System;

void SafeWindowOperation(IntPtr handle)
{
    try
    {
        var window = InteropWindow.FromHandle(handle);
        
        if (!window.Exists())
        {
            Console.WriteLine("Window no longer exists");
            return;
        }
        
        window.Fill();
        Console.WriteLine(window.Caption);
    }
    catch (Win32Exception ex)
    {
        Console.WriteLine($"Window operation failed: {ex.Message}");
        // Continue without crashing
    }
}
```

## See Also

- [Window Management Guide](window-management.md)
- [Clipboard Usage Guide](clipboard-usage.md)
- [DPI Awareness Guide](dpi-awareness.md)
- [Input Handling Guide](input-handling.md)
- [API Reference](../api/index.md)
