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

Every subscriber first receives the current state, then one update per clipboard change. The information (`Id`, `OwnerHandle`, `Formats`/`FormatIds`) is collected without opening the clipboard (GetClipboardSequenceNumber, GetClipboardOwner, GetUpdatedClipboardFormats), so it never blocks or fails when another application holds the clipboard.

### Filter by Format

```csharp
using Dapplo.Windows.Clipboard;
using System.Reactive.Linq;

// Only react to text changes
var textSubscription = ClipboardNative.OnUpdate
    .Where(info => info.Formats.Contains(StandardClipboardFormats.Text.AsString()) || info.Formats.Contains(StandardClipboardFormats.UnicodeText.AsString()))
    .Subscribe(info =>
    {
        Console.WriteLine("Text copied to clipboard");
    });

// Only react to image changes
var imageSubscription = ClipboardNative.OnUpdate
    .Where(info => info.Formats.Contains("PNG") || info.Formats.Contains(StandardClipboardFormats.Bitmap.AsString()))
    .Subscribe(info =>
    {
        Console.WriteLine("Image copied to clipboard");
    });

// Only react to files
var fileSubscription = ClipboardNative.OnUpdate
    .Where(info => info.Formats.Contains(StandardClipboardFormats.Drop.AsString()))
    .Subscribe(info =>
    {
        Console.WriteLine("Files copied to clipboard");
    });
```

### Thread Synchronization

`OnUpdate` publishes on the thread of the SharedMessageWindow. Keep handlers short, and use `ObserveOn` before doing real work, especially before opening the clipboard with `ClipboardNative.Access()`:

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

Always use the `Access()` method (or `await AccessAsync()`) to safely access the clipboard.
Windows ties an opened clipboard to the thread which opened it: use and dispose the token on that thread, and don't `await` while holding it
(`AccessAsync()` only waits asynchronously, the clipboard is opened on the thread which continues after the `await`).
On another thread `CanAccess` is `false` and the extension methods throw a `ClipboardAccessDeniedException`.

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
    if (ClipboardNative.HasFormat(StandardClipboardFormats.UnicodeText))
    {
        string text = clipboard.GetAsUnicodeString();
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
    if (ClipboardNative.HasFormat(StandardClipboardFormats.Drop))
    {
        IEnumerable<string> files = clipboard.GetFileNames();
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
        // The stream reads the clipboard memory directly, use it inside the using block of the access token
        using var stream = clipboard.GetAsStream("PNG");
        using var fileStream = File.Create("clipboard_image.png");
        stream.CopyTo(fileStream);
    }

    // Read as bitmap
    if (ClipboardNative.HasFormat(StandardClipboardFormats.Bitmap))
    {
        // Note: Direct bitmap reading may require additional code
        using var stream = clipboard.GetAsStream(StandardClipboardFormats.Bitmap);
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
        
        // Read as bytes, note: this is the complete memory block, which can be larger than the data which was placed
        byte[] data = clipboard.GetAsBytes(customFormat);
        
        Console.WriteLine($"Custom data size: {data.Length} bytes");
    }
}
```

## Writing to Clipboard

Call `ClearContents()` before placing new content: this removes the formats of the previous owner (otherwise they are mixed with yours)
and makes the window of the token (by default the SharedMessageWindow) the clipboard owner.

### Set Text

```csharp
using Dapplo.Windows.Clipboard;

using (var clipboard = ClipboardNative.Access())
{
    clipboard.ClearContents();
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
    clipboard.ClearContents();
    clipboard.SetFileNames(files);
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

    clipboard.ClearContents();
    clipboard.SetAsBytes(data, customFormat);
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

    clipboard.ClearContents();
    // Set as both Unicode text and RTF
    clipboard.SetAsUnicodeString(text);
    clipboard.SetAsBytes(Encoding.ASCII.GetBytes(@"{\rtf1\ansi Hello, World!}"), "Rich Text Format");
}
```

Note: "HTML Format" (CF_HTML) is not plain HTML, it needs a header with `Version`, `StartHTML`, `EndHTML`, `StartFragment` and `EndFragment` byte offsets,
see [HTML Clipboard Format](https://learn.microsoft.com/en-us/windows/win32/dataxchg/html-clipboard-format).

## Advanced Scenarios

### Delayed Rendering

Delayed rendering allows you to provide clipboard data only when it's actually requested.
Register a renderer for the format first, then advertise the format:

```csharp
using Dapplo.Windows.Clipboard;
using System;

// Keep the registration as long as the content can be requested, dispose it afterwards
IDisposable renderer = ClipboardNative.RegisterDelayedRenderer("MyFormat", request =>
{
    // Called synchronously on the SharedMessageWindow thread, Windows needs the data before this returns.
    // Use the AccessToken of the request, it's only valid during this call:
    // do NOT call ClipboardNative.Access(), await or ObserveOn here.
    // request.RenderAllFormats is true when the owner window is destroyed and all not yet requested formats must be rendered.
    byte[] data = GenerateLargeData(); // Only generate when needed
    request.AccessToken.SetAsBytes(data, request.RequestedFormatId);
});

// Set clipboard with delayed rendering, ClearContents makes the SharedMessageWindow the owner which receives the requests
using (var clipboard = ClipboardNative.Access())
{
    clipboard.ClearContents();
    clipboard.SetDelayedRenderedContent("MyFormat");
}
```

`SetDelayedRenderedContent` throws an `InvalidOperationException` if no renderer is registered for the format, or if the clipboard isn't owned by the window of the token (call `ClearContents()` first).
The library answers WM_RENDERFORMAT without opening the clipboard, and opens/closes the clipboard itself for WM_RENDERALLFORMATS.
Exceptions thrown by a renderer are written to `System.Diagnostics.Trace`.

### Clear Clipboard

```csharp
using Dapplo.Windows.Clipboard;

using (var clipboard = ClipboardNative.Access())
{
    clipboard.ClearContents();
}
```

### Monitor Clipboard in Reactive Pipeline

```csharp
using Dapplo.Windows.Clipboard;
using System;
using System.Reactive.Linq;

var subscription = ClipboardNative.OnUpdate
    .Where(info => info.Formats.Contains(StandardClipboardFormats.UnicodeText.AsString()))
    .Throttle(TimeSpan.FromMilliseconds(500)) // Avoid rapid updates
    .Subscribe(info =>
    {
        using var clipboard = ClipboardNative.Access();
        var text = clipboard.GetAsUnicodeString();
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
    var text = clipboard.GetAsUnicodeString();
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

## Cloud Clipboard and Clipboard History

Windows 10 and later versions support cloud clipboard synchronization and clipboard history. You can control how your clipboard content interacts with these features using the cloud clipboard extensions.

### What are Cloud Clipboard Options?

The cloud clipboard formats allow you to control three aspects of clipboard behavior:

1. **CanIncludeInClipboardHistory** - Controls whether the content appears in Windows clipboard history (Win+V)
2. **CanUploadToCloudClipboard** - Controls whether the content syncs across devices via cloud
3. **ExcludeClipboardContentFromMonitorProcessing** - When present (whatever the value), the content is excluded from history, cloud sync and clipboard monitoring apps

`SetCloudClipboardOptions` only places the formats for the options which are specified: `canIncludeInHistory` and `canUploadToCloud` are `bool?` (null = don't place the format, true = DWORD 1, false = DWORD 0),
and the exclusion format is only placed for `excludeFromMonitoring: true`. Without options nothing is placed and the Windows defaults apply.

### Setting Cloud Clipboard Options

The simplest way to control cloud clipboard behavior is using the `SetCloudClipboardOptions` method:

```csharp
using Dapplo.Windows.Clipboard;

using (var clipboard = ClipboardNative.Access())
{
    // Set your clipboard content
    clipboard.ClearContents();
    clipboard.SetAsUnicodeString("Sensitive information");
    
    // Prevent from being stored in history or synced to cloud
    clipboard.SetCloudClipboardOptions(
        canIncludeInHistory: false,
        canUploadToCloud: false,
        excludeFromMonitoring: true
    );
}
```

### Use Cases

#### Private/Sensitive Content

Prevent sensitive data from being stored in history or synced:

```csharp
using Dapplo.Windows.Clipboard;

using (var clipboard = ClipboardNative.Access())
{
    clipboard.ClearContents();
    clipboard.SetAsUnicodeString("MyPassword123!");
    
    // Disable history and cloud sync for sensitive data
    clipboard.SetCloudClipboardOptions(
        canIncludeInHistory: false,
        canUploadToCloud: false
    );
}
```

#### Temporary Content

For temporary clipboard content that shouldn't clutter history:

```csharp
using Dapplo.Windows.Clipboard;

using (var clipboard = ClipboardNative.Access())
{
    clipboard.ClearContents();
    clipboard.SetAsUnicodeString("Temporary data");
    
    // Don't include in history, but allow cloud sync
    clipboard.SetCloudClipboardOptions(
        canIncludeInHistory: false,
        canUploadToCloud: true
    );
}
```

#### Normal Content (Default)

For normal content nothing needs to be placed, the Windows defaults (history and cloud sync as configured by the user) apply.
To explicitly allow both:

```csharp
using Dapplo.Windows.Clipboard;

using (var clipboard = ClipboardNative.Access())
{
    clipboard.ClearContents();
    clipboard.SetAsUnicodeString("Normal clipboard content");

    // Explicitly allow history and cloud sync, this doesn't place the exclusion format
    clipboard.SetCloudClipboardOptions(canIncludeInHistory: true, canUploadToCloud: true);
}
```

### Individual Option Methods

You can also set each option individually:

```csharp
using Dapplo.Windows.Clipboard;

using (var clipboard = ClipboardNative.Access())
{
    clipboard.ClearContents();
    clipboard.SetAsUnicodeString("My content");

    // Set options individually
    clipboard.SetCanIncludeInClipboardHistory(false);
    clipboard.SetCanUploadToCloudClipboard(true);
    // Or exclude the content from history, cloud sync and monitoring applications completely
    clipboard.ExcludeFromMonitorProcessing();
}
```

### Checking Cloud Clipboard Formats

You can check if cloud clipboard formats are present:

```csharp
using Dapplo.Windows.Clipboard;
using System.Linq;

using (var clipboard = ClipboardNative.Access())
{
    var formats = clipboard.AvailableFormats().ToList();
    
    bool hasHistoryFormat = formats.Contains(
        ClipboardCloudExtensions.CanIncludeInClipboardHistoryFormat);
    bool hasCloudFormat = formats.Contains(
        ClipboardCloudExtensions.CanUploadToCloudClipboardFormat);
    bool hasMonitorFormat = formats.Contains(
        ClipboardCloudExtensions.ExcludeClipboardContentFromMonitorProcessingFormat);
}
```

### Best Practices for Cloud Clipboard

1. **Always set cloud options after setting content** - The cloud formats should be set in the same clipboard access session as your content
2. **Default to the Windows defaults** - Unless you have a specific reason, don't place any of these formats
3. **Be consistent** - If you disable history, you probably want to disable cloud sync too
4. **Consider user privacy** - For passwords and sensitive data, always disable history and cloud sync

## Common Clipboard Formats

Standard Windows clipboard formats. Use the `StandardClipboardFormats` enum with format-specific methods:

| Enum value | String name | Description |
|---|---|---|
| `StandardClipboardFormats.Text` | `CF_TEXT` | ANSI text |
| `StandardClipboardFormats.UnicodeText` | `CF_UNICODETEXT` | Unicode text |
| `StandardClipboardFormats.Bitmap` | `CF_BITMAP` | Bitmap image |
| `StandardClipboardFormats.Drop` | `CF_HDROP` | List of file paths |
| *(registered by apps)* | `PNG` | PNG image format |
| *(registered by apps)* | `HTML Format` | HTML content |
| *(registered by apps)* | `Rich Text Format` | RTF content |

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

### 1. Always Use `using` Statements, Short and on One Thread

This ensures the clipboard is properly released. Don't `await` or switch threads while holding the token, and keep the block short: no other application can use the clipboard meanwhile.

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
