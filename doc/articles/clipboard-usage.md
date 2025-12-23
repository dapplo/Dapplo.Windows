# Clipboard Usage Guide

This guide covers clipboard monitoring and manipulation using the `Dapplo.Windows.Clipboard` package.

## Overview

The Clipboard package provides a reactive API for monitoring and manipulating the Windows clipboard. It uses [Reactive Extensions](https://github.com/dotnet/reactive) (System.Reactive) for event handling.

## Installation

```powershell
Install-Package Dapplo.Windows.Clipboard
```

## Monitoring Clipboard Changes

### Basic Monitoring

```csharp
using Dapplo.Windows.Clipboard;
using System;

// Subscribe to all clipboard changes
var subscription = ClipboardNative.OnUpdate.Subscribe(info =>
{
    Console.WriteLine($"Clipboard changed!");
    Console.WriteLine($"Available formats: {string.Join(", ", info.Formats)}");
    Console.WriteLine($"Sequence ID: {info.Id}");
});

// Dispose when done
subscription.Dispose();
```

### Filter by Format

```csharp
using Dapplo.Windows.Clipboard;
using System.Reactive.Linq;

// Only react to text changes
var textSubscription = ClipboardNative.OnUpdate
    .Where(info => info.Formats.Contains("Text") || info.Formats.Contains("UnicodeText"))
    .Subscribe(info =>
    {
        Console.WriteLine("Text copied to clipboard");
    });

// Only react to image changes
var imageSubscription = ClipboardNative.OnUpdate
    .Where(info => info.Formats.Contains("PNG") || info.Formats.Contains("Bitmap"))
    .Subscribe(info =>
    {
        Console.WriteLine("Image copied to clipboard");
    });

// Only react to files
var fileSubscription = ClipboardNative.OnUpdate
    .Where(info => info.Formats.Contains("FileDrop"))
    .Subscribe(info =>
    {
        Console.WriteLine("Files copied to clipboard");
    });
```

### Thread Synchronization

Ensure clipboard operations run on the correct thread:

```csharp
using Dapplo.Windows.Clipboard;
using System.Reactive.Linq;
using System.Reactive.Concurrency;

// In a WPF or Windows Forms application
var subscription = ClipboardNative.OnUpdate
    .ObserveOn(SynchronizationContext.Current) // Run on UI thread
    .Subscribe(info =>
    {
        // Safe to update UI here
        UpdateClipboardStatus(info.Formats);
    });
```

## Reading Clipboard Content

### Access Clipboard

Always use the `Access()` method to safely access the clipboard:

```csharp
using Dapplo.Windows.Clipboard;

using (var clipboard = ClipboardNative.Access())
{
    // Clipboard is now locked for your exclusive use
    var formats = clipboard.AvailableFormats();
    Console.WriteLine($"Available formats: {string.Join(", ", formats)}");
}
// Clipboard is automatically released
```

### Read Text

```csharp
using Dapplo.Windows.Clipboard;

using (var clipboard = ClipboardNative.Access())
{
    if (clipboard.AvailableFormats().Contains("Text"))
    {
        string text = clipboard.GetAsString();
        Console.WriteLine($"Clipboard text: {text}");
    }
}
```

### Read Files

```csharp
using Dapplo.Windows.Clipboard;
using System.Collections.Generic;

using (var clipboard = ClipboardNative.Access())
{
    if (clipboard.AvailableFormats().Contains("FileDrop"))
    {
        IEnumerable<string> files = clipboard.GetAsFileNames();
        foreach (var file in files)
        {
            Console.WriteLine($"File: {file}");
        }
    }
}
```

### Read Images

```csharp
using Dapplo.Windows.Clipboard;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;

using (var clipboard = ClipboardNative.Access())
{
    // Read as stream (PNG format)
    if (clipboard.AvailableFormats().Contains("PNG"))
    {
        using var stream = clipboard.GetAsStream("PNG");
        using var fileStream = File.Create("clipboard_image.png");
        stream.CopyTo(fileStream);
    }

    // Read as bitmap
    if (clipboard.AvailableFormats().Contains("Bitmap"))
    {
        // Note: Direct bitmap reading may require additional code
        using var stream = clipboard.GetAsStream("Bitmap");
        // Process bitmap stream
    }
}
```

### Read Custom Formats

```csharp
using Dapplo.Windows.Clipboard;
using System.IO;

using (var clipboard = ClipboardNative.Access())
{
    string customFormat = "MyApplication.CustomFormat";
    
    if (clipboard.AvailableFormats().Contains(customFormat))
    {
        // Read as stream
        using var stream = clipboard.GetAsStream(customFormat);
        
        // Read as bytes
        byte[] data = clipboard.GetAsBytes(customFormat);
        
        Console.WriteLine($"Custom data size: {data.Length} bytes");
    }
}
```

## Writing to Clipboard

### Set Text

```csharp
using Dapplo.Windows.Clipboard;

using (var clipboard = ClipboardNative.Access())
{
    clipboard.SetAsUnicodeString("Hello, World!");
}
```

### Set Files

```csharp
using Dapplo.Windows.Clipboard;
using System.Collections.Generic;

var files = new List<string>
{
    @"C:\path\to\file1.txt",
    @"C:\path\to\file2.txt"
};

using (var clipboard = ClipboardNative.Access())
{
    clipboard.SetAsFileNames(files);
}
```

### Set Custom Format

```csharp
using Dapplo.Windows.Clipboard;
using System.Text;

using (var clipboard = ClipboardNative.Access())
{
    string customFormat = "MyApplication.CustomFormat";
    byte[] data = Encoding.UTF8.GetBytes("Custom clipboard data");
    
    clipboard.SetAsBytes(customFormat, data);
}
```

### Set Multiple Formats

You can set multiple formats at once:

```csharp
using Dapplo.Windows.Clipboard;
using System.Text;

using (var clipboard = ClipboardNative.Access())
{
    string text = "Hello, World!";
    
    // Set as both Unicode text and HTML
    clipboard.SetAsUnicodeString(text);
    
    string html = $"<html><body>{text}</body></html>";
    clipboard.SetAsBytes("HTML Format", Encoding.UTF8.GetBytes(html));
}
```

## Advanced Scenarios

### Delayed Rendering

Delayed rendering allows you to provide clipboard data only when it's actually requested:

```csharp
using Dapplo.Windows.Clipboard;
using System;

// Subscribe to render requests
var renderSubscription = ClipboardNative.OnRenderFormat.Subscribe(request =>
{
    if (request.IsDestroyClipboard)
    {
        // Clean up any delayed rendering resources
        return;
    }

    if (request.RenderAllFormats)
    {
        // Render all delayed formats
        return;
    }

    // Render specific format on demand
    switch (request.RequestedFormat)
    {
        case "MyFormat":
            using (var clipboard = ClipboardNative.Access())
            {
                byte[] data = GenerateLargeData(); // Only generate when needed
                clipboard.SetAsBytes("MyFormat", data);
            }
            break;
    }
});

// Set clipboard with delayed rendering
using (var clipboard = ClipboardNative.Access())
{
    clipboard.SetAsDelayedRenderer("MyFormat");
}
```

### Clear Clipboard

```csharp
using Dapplo.Windows.Clipboard;

using (var clipboard = ClipboardNative.Access())
{
    clipboard.Clear();
}
```

### Monitor Clipboard in Reactive Pipeline

```csharp
using Dapplo.Windows.Clipboard;
using System;
using System.Reactive.Linq;

var subscription = ClipboardNative.OnUpdate
    .Where(info => info.Formats.Contains("Text"))
    .Throttle(TimeSpan.FromMilliseconds(500)) // Avoid rapid updates
    .Subscribe(info =>
    {
        using var clipboard = ClipboardNative.Access();
        var text = clipboard.GetAsString();
        ProcessClipboardText(text);
    });
```

### Get Clipboard Owner

```csharp
using Dapplo.Windows.Clipboard;

var subscription = ClipboardNative.OnUpdate.Subscribe(info =>
{
    Console.WriteLine($"Clipboard owner handle: {info.OwnerHandle}");
});
```

## Error Handling

### Handle Access Denied

```csharp
using Dapplo.Windows.Clipboard;

try
{
    using var clipboard = ClipboardNative.Access();
    var text = clipboard.GetAsString();
}
catch (ClipboardAccessDeniedException ex)
{
    Console.WriteLine($"Clipboard access denied: {ex.Message}");
    // Another application is currently using the clipboard
}
```

### Retry Logic

```csharp
using Dapplo.Windows.Clipboard;
using System;
using System.Threading;

int maxRetries = 3;
int retryDelay = 100; // milliseconds

for (int i = 0; i < maxRetries; i++)
{
    try
    {
        using var clipboard = ClipboardNative.Access();
        clipboard.SetAsUnicodeString("Hello, World!");
        break; // Success
    }
    catch (ClipboardAccessDeniedException)
    {
        if (i == maxRetries - 1)
        {
            throw; // Give up after max retries
        }
        Thread.Sleep(retryDelay);
    }
}
```

## Common Clipboard Formats

Standard Windows clipboard formats:

- **Text** - ANSI text
- **UnicodeText** - Unicode text
- **Bitmap** - Bitmap image
- **PNG** - PNG image format
- **FileDrop** - List of file paths
- **HTML Format** - HTML content
- **Rich Text Format** - RTF content
- **Csv** - Comma-separated values

You can also check available formats:

```csharp
using Dapplo.Windows.Clipboard;

using (var clipboard = ClipboardNative.Access())
{
    var formats = clipboard.AvailableFormats();
    foreach (var format in formats)
    {
        Console.WriteLine($"Format: {format}");
    }
}
```

## Best Practices

### 1. Always Use `using` Statements

This ensures the clipboard is properly released:

```csharp
using (var clipboard = ClipboardNative.Access())
{
    // Use clipboard
} // Automatically released
```

### 2. Dispose Subscriptions

Clean up event subscriptions when done:

```csharp
var subscription = ClipboardNative.OnUpdate.Subscribe(...);

// Later
subscription.Dispose();
```

### 3. Handle Concurrency

The clipboard is a shared resource. Use appropriate error handling:

```csharp
try
{
    using var clipboard = ClipboardNative.Access();
    // Use clipboard
}
catch (ClipboardAccessDeniedException)
{
    // Handle gracefully
}
```

### 4. Use Reactive Operators

Leverage Rx operators for better control:

```csharp
var subscription = ClipboardNative.OnUpdate
    .Throttle(TimeSpan.FromMilliseconds(500))  // Debounce rapid changes
    .DistinctUntilChanged(info => info.Id)      // Skip duplicates
    .ObserveOn(SynchronizationContext.Current)  // UI thread
    .Subscribe(info => { /* Handle */ });
```

## See Also

- [API Reference](../api/index.md)
- [Getting Started](intro.md)
- [Common Scenarios](common-scenarios.md)
- [Reactive Extensions Documentation](http://reactivex.io/)
