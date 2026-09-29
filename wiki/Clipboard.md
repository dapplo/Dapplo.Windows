# Clipboard

The `Dapplo.Windows.Clipboard` package provides a reactive API for monitoring and manipulating the Windows clipboard. It is built on [Reactive Extensions (Rx.NET)](https://github.com/dotnet/reactive).

Access from several threads is serialized by an in-process lock, but Windows ties an opened clipboard to the thread which opened it: an access token from `ClipboardNative.Access()` / `AccessAsync()` must be used and disposed on the thread which got it (see [Threading](#threading)).

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

Every subscriber first receives the current state, then one update per clipboard change. The information (`Id`, `OwnerHandle`, `Formats`/`FormatIds`) is collected without opening the clipboard, so it never blocks or fails when another application holds the clipboard. The updates are published on the SharedMessageWindow thread: keep the handler short and use `ObserveOn` before doing real work, especially before reading the content with `ClipboardNative.Access()`.

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

### Read the Content on a Change

```csharp
ClipboardNative.OnUpdate
    .Where(info => info.Formats.Contains(StandardClipboardFormats.UnicodeText.AsString()))
    .ObserveOn(SynchronizationContext.Current)   // don't open the clipboard on the SharedMessageWindow thread
    .Subscribe(info =>
    {
        using var clipboard = ClipboardNative.Access();
        if (clipboard.CanAccess)
        {
            labelText.Text = clipboard.GetAsUnicodeString();
        }
    });
```

## Reading Clipboard Content

Always access the clipboard through `ClipboardNative.Access()` (or `await ClipboardNative.AccessAsync()`), which acquires the clipboard lock and releases it automatically when the `using` block exits. Keep the block short and synchronous, see [Threading](#threading).

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
        // Use the stream (e.g., Image.FromStream(stream)) inside the using block:
        // the stream reads the clipboard memory directly, it's invalid once the clipboard is closed.
    }
}
```

`GetAsBytes` returns the complete clipboard memory block, which can be larger than the actual data (the allocation is often rounded up). `GetAsUnicodeString` stops at the first NUL character.

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

Call `ClearContents()` first: this removes the content of the previous owner (otherwise your formats are mixed with e.g. an old image) and makes the window of the token (by default the SharedMessageWindow) the clipboard owner.

### Text

```csharp
using (var clipboard = ClipboardNative.Access())
{
    clipboard.ClearContents();
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
    clipboard.ClearContents();
    byte[] data = System.Text.Encoding.UTF8.GetBytes("custom data");
    clipboard.SetAsBytes(data, "MyApp.CustomFormat");
}
```

### Multiple Formats at Once

```csharp
using (var clipboard = ClipboardNative.Access())
{
    clipboard.ClearContents();
    clipboard.SetAsUnicodeString("Hello");
    clipboard.SetAsBytes(
        System.Text.Encoding.ASCII.GetBytes(@"{\rtf1\ansi Hello}"),
        "Rich Text Format");
}
```

Note: "HTML Format" (CF_HTML) is not plain HTML, it needs a header with `Version`, `StartHTML`, `EndHTML`, `StartFragment` and `EndFragment` byte offsets, see [HTML Clipboard Format](https://learn.microsoft.com/en-us/windows/win32/dataxchg/html-clipboard-format).

### Clear the Clipboard

```csharp
using (var clipboard = ClipboardNative.Access())
{
    clipboard.ClearContents();
}
```

## Delayed Rendering

Delayed rendering lets you advertise clipboard formats without computing the actual data until something requests it — useful for large or expensive payloads.

1. Register a renderer for the format with `ClipboardNative.RegisterDelayedRenderer`, keep the registration as long as your content can be requested.
2. Advertise the format: `ClearContents()` (makes the SharedMessageWindow the owner) and `SetDelayedRenderedContent(format)`.

```csharp
// Provide the data when requested, dispose the registration when it's no longer needed
var renderer = ClipboardNative.RegisterDelayedRenderer("MyApp.HeavyFormat", request =>
{
    // Called synchronously on the SharedMessageWindow thread.
    // Do NOT call ClipboardNative.Access(), await or ObserveOn here: Windows requires the data before this returns,
    // and for WM_RENDERFORMAT the clipboard must not be opened. Use request.AccessToken, it's only valid during this call.
    byte[] data = GenerateLargeData();
    request.AccessToken.SetAsBytes(data, request.RequestedFormatId);
});

// Advertise the format with delayed rendering
using (var clipboard = ClipboardNative.Access())
{
    clipboard.ClearContents();
    clipboard.SetDelayedRenderedContent("MyApp.HeavyFormat");
}
```

`SetDelayedRenderedContent` throws an `InvalidOperationException` when no renderer is registered for the format, or when the clipboard is not owned by the window of the token (call `ClearContents()` first).
The renderer is also called with `request.RenderAllFormats == true` when the formats which were not requested yet must be rendered because the owner window is destroyed; the library opens and closes the clipboard for that.
When another application takes over the clipboard, nothing will be requested anymore. Exceptions thrown by a renderer are written to `System.Diagnostics.Trace`.

## Cloud Clipboard and Clipboard History

Windows 10+ supports clipboard history (Win+V) and cross-device cloud sync. You can control whether your content participates in these features.

`SetCloudClipboardOptions` only places the formats for the options you specify, without options the Windows defaults apply. `CanIncludeInClipboardHistory` and `CanUploadToCloudClipboard` are placed with the DWORD 1 (allow) or 0 (prevent). `ExcludeClipboardContentFromMonitorProcessing` works by its mere presence: it's only placed for `excludeFromMonitoring: true` (or `ExcludeFromMonitorProcessing()`), and then excludes the content from history, cloud sync and clipboard monitors.

### Protect Sensitive Data

```csharp
using (var clipboard = ClipboardNative.Access())
{
    clipboard.ClearContents();
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
    clipboard.ClearContents();
    clipboard.SetAsUnicodeString("Temporary value");
    clipboard.SetCloudClipboardOptions(canIncludeInHistory: false, canUploadToCloud: true);
}
```

### Set Options Individually

```csharp
using (var clipboard = ClipboardNative.Access())
{
    clipboard.ClearContents();
    clipboard.SetAsUnicodeString("My content");
    clipboard.SetCanIncludeInClipboardHistory(false);
    clipboard.SetCanUploadToCloudClipboard(true);
    // or exclude it from history, cloud sync and monitoring applications completely:
    // clipboard.ExcludeFromMonitorProcessing();
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

## Threading

- The clipboard is opened per thread: use and dispose an access token on the thread which got it. On another thread `CanAccess` is `false` and the extension methods throw a `ClipboardAccessDeniedException`.
- `AccessAsync()` only waits asynchronously, the clipboard is opened on the thread which continues after the `await` (the `SynchronizationContext` of the caller, e.g. the UI thread). Don't `await` while holding the token.
- `OnUpdate` publishes on the SharedMessageWindow thread, delayed renderers are called on that thread. Don't block it.
- While your application holds the clipboard open, no other application can use it: keep the `using` blocks short.

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
- **Keep clipboard sessions short and on one thread** — don't hold the lock while doing heavy work, and don't `await` while holding it.
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
