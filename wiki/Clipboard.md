# Clipboard

The `Dapplo.Windows.Clipboard` package provides a reactive, thread-safe API for monitoring and manipulating the Windows clipboard. It is built on [Reactive Extensions (Rx.NET)](https://github.com/dotnet/reactive).

## Installation

```powershell
Install-Package Dapplo.Windows.Clipboard
```

## Monitoring Clipboard Changes

### Subscribe to All Changes

```csharp
using Dapplo.Windows.Clipboard;

var subscription = ClipboardNative.OnUpdate.Subscribe(info =>
{
    Console.WriteLine($"Clipboard changed — formats: {string.Join(", ", info.Formats)}");
});

subscription.Dispose(); // clean up when done
```

### Filter by Format

```csharp
using Dapplo.Windows.Clipboard;
using System.Reactive.Linq;

// Text only
ClipboardNative.OnUpdate
    .Where(info => info.Formats.Contains(StandardClipboardFormats.UnicodeText.AsString()))
    .Subscribe(info => Console.WriteLine("Text copied"));

// Image only
ClipboardNative.OnUpdate
    .Where(info => info.Formats.Contains("PNG") || info.Formats.Contains(StandardClipboardFormats.Bitmap.AsString()))
    .Subscribe(info => Console.WriteLine("Image copied"));

// Files only
ClipboardNative.OnUpdate
    .Where(info => info.Formats.Contains(StandardClipboardFormats.Drop.AsString()))
    .Subscribe(info => Console.WriteLine("Files copied"));
```

### Marshal to the UI Thread

```csharp
using System.Reactive.Linq;

ClipboardNative.OnUpdate
    .ObserveOn(SynchronizationContext.Current)   // switch to UI thread
    .Subscribe(info => labelStatus.Text = $"Formats: {string.Join(", ", info.Formats)}");
```

## Reading Clipboard Content

Always access the clipboard through `ClipboardNative.Access()`, which acquires the clipboard lock and releases it automatically when the `using` block exits.

### Text

```csharp
using (var clipboard = ClipboardNative.Access())
{
    if (ClipboardNative.HasFormat(StandardClipboardFormats.UnicodeText))
    {
        string text = clipboard.GetAsUnicodeString();
        Console.WriteLine(text);
    }
}
```

### Files

```csharp
using (var clipboard = ClipboardNative.Access())
{
    if (ClipboardNative.HasFormat(StandardClipboardFormats.Drop))
    {
        foreach (var file in clipboard.GetFileNames())
            Console.WriteLine(file);
    }
}
```

### Images (as stream)

```csharp
using (var clipboard = ClipboardNative.Access())
{
    if (clipboard.AvailableFormats().Contains("PNG"))
    {
        using var stream = clipboard.GetAsStream("PNG");
        // Use the stream (e.g., Image.FromStream(stream))
    }
}
```

### Custom Formats

```csharp
using (var clipboard = ClipboardNative.Access())
{
    const string myFormat = "MyApp.CustomFormat";
    if (clipboard.AvailableFormats().Contains(myFormat))
    {
        byte[] data = clipboard.GetAsBytes(myFormat);
    }
}
```

### List Available Formats

```csharp
using (var clipboard = ClipboardNative.Access())
{
    foreach (var format in clipboard.AvailableFormats())
        Console.WriteLine(format);
}
```

## Writing to the Clipboard

### Text

```csharp
using (var clipboard = ClipboardNative.Access())
{
    clipboard.SetAsUnicodeString("Hello, World!");
}
```

### Files

```csharp
using (var clipboard = ClipboardNative.Access())
{
    clipboard.ClearContents();
    clipboard.SetFileNames(new[] { @"C:\file1.txt", @"C:\file2.txt" });
}
```

### Custom Format

```csharp
using (var clipboard = ClipboardNative.Access())
{
    byte[] data = System.Text.Encoding.UTF8.GetBytes("custom data");
    clipboard.SetAsBytes(data, "MyApp.CustomFormat");
}
```

### Multiple Formats at Once

```csharp
using (var clipboard = ClipboardNative.Access())
{
    clipboard.SetAsUnicodeString("Hello");
    clipboard.SetAsBytes(
        System.Text.Encoding.UTF8.GetBytes("<html><body>Hello</body></html>"),
        "HTML Format");
}
```

### Clear the Clipboard

```csharp
using (var clipboard = ClipboardNative.Access())
{
    clipboard.ClearContents();
}
```

## Delayed Rendering

Delayed rendering lets you advertise clipboard formats without computing the actual data until something requests it — useful for large or expensive payloads.

```csharp
// Advertise the format with delayed rendering
using (var clipboard = ClipboardNative.Access())
{
    clipboard.ClearContents();
    clipboard.SetDelayedRenderedContent("MyApp.HeavyFormat");
}

// Provide the data when requested
var sub = ClipboardNative.OnRenderFormat.Subscribe(request =>
{
    if (request.IsDestroyClipboard || request.RenderAllFormats)
        return;   // clean up or pre-render all

    if (request.RequestedFormat == "MyApp.HeavyFormat")
    {
        // Do NOT call ClipboardNative.Access() here — the clipboard is already open.
        // Use request.AccessToken directly.
        byte[] data = GenerateLargeData();
        request.AccessToken.SetAsBytes(data, "MyApp.HeavyFormat");
    }
});
```

## Cloud Clipboard and Clipboard History

Windows 10+ supports clipboard history (Win+V) and cross-device cloud sync. You can control whether your content participates in these features.

### Protect Sensitive Data

```csharp
using (var clipboard = ClipboardNative.Access())
{
    clipboard.SetAsUnicodeString("Pa$$w0rd!");

    clipboard.SetCloudClipboardOptions(
        canIncludeInHistory: false,
        canUploadToCloud:    false,
        excludeFromMonitoring: true);
}
```

### Temporary Content (No History)

```csharp
using (var clipboard = ClipboardNative.Access())
{
    clipboard.SetAsUnicodeString("Temporary value");
    clipboard.SetCloudClipboardOptions(canIncludeInHistory: false, canUploadToCloud: true);
}
```

### Set Options Individually

```csharp
using (var clipboard = ClipboardNative.Access())
{
    clipboard.SetAsUnicodeString("My content");
    clipboard.SetCanIncludeInClipboardHistory(false);
    clipboard.SetCanUploadToCloudClipboard(true);
}
```

## Error Handling

### Access Denied

Another application may be holding the clipboard open:

```csharp
try
{
    using var clipboard = ClipboardNative.Access();
    var text = clipboard.GetAsUnicodeString();
}
catch (ClipboardAccessDeniedException ex)
{
    Console.WriteLine($"Clipboard busy: {ex.Message}");
}
```

### Retry Logic

```csharp
for (int attempt = 0; attempt < 3; attempt++)
{
    try
    {
        using var clipboard = ClipboardNative.Access();
        clipboard.SetAsUnicodeString("Hello");
        break;
    }
    catch (ClipboardAccessDeniedException)
    {
        if (attempt == 2) throw;
        System.Threading.Thread.Sleep(100);
    }
}
```

## Common Format Reference

| Format name | `StandardClipboardFormats` value | Description |
|-------------|----------------------------------|-------------|
| `CF_UNICODETEXT` | `UnicodeText` | Unicode text |
| `CF_TEXT` | `Text` | ANSI text |
| `CF_BITMAP` | `Bitmap` | Device-dependent bitmap |
| `CF_HDROP` | `Drop` | List of file paths |
| `PNG` | *(registered)* | PNG image |
| `HTML Format` | *(registered)* | HTML fragment |
| `Rich Text Format` | *(registered)* | RTF content |

## Best Practices

- **Always use `using`** for `ClipboardNative.Access()` — the clipboard is a system-wide lock.
- **Keep clipboard sessions short** — don't hold the lock while doing heavy work.
- **Dispose subscriptions** when your component is torn down.
- **Use Rx throttle/debounce** to avoid reacting to rapid clipboard changes:

  ```csharp
  ClipboardNative.OnUpdate
      .Throttle(TimeSpan.FromMilliseconds(300))
      .DistinctUntilChanged(info => info.Id)
      .Subscribe(info => ProcessClipboard(info));
  ```

## See Also

- [[Getting-Started]]
- [[Common-Scenarios]]
- [Reactive Extensions](http://reactivex.io/)
