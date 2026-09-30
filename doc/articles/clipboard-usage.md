# Clipboard

**Dapplo.Windows.Clipboard** monitors the clipboard with an observable and reads and writes its content in any
format, with support for delayed rendering and the Windows clipboard history / cloud clipboard options.

```powershell
dotnet add package Dapplo.Windows.Clipboard
```

Namespaces used on this page: `Dapplo.Windows.Clipboard`, `System.Reactive.Linq`.

## Monitoring changes

`ClipboardNative.OnUpdate` reports every clipboard change. A subscriber first gets the current state, then one
`ClipboardUpdateInformation` per change. The information (`Id`, `OwnerHandle`, `Formats`, `FormatIds`, `Timestamp`) is
collected *without* opening the clipboard, so it never blocks and never fails because another application has the
clipboard open.

<!-- sample: ClipboardSamples.Monitor -->
```csharp
// Every subscriber first gets the current state, then one update per clipboard change.
// The information is collected without opening the clipboard, on the SharedMessageWindow thread.
IDisposable subscription = ClipboardNative.OnUpdate.Subscribe(info =>
{
    Console.WriteLine($"Clipboard #{info.Id} from window {info.OwnerHandle}: {string.Join(", ", info.Formats)}");
});

// Stop monitoring
subscription.Dispose();
```

Standard formats are named like the Win32 constants (`"CF_UNICODETEXT"`, `"CF_HDROP"`), registered formats by their
registered name (`"PNG"`, `"HTML Format"`). Compare IDs for standard formats, or use `StandardClipboardFormats.X.AsString()`:

<!-- sample: ClipboardSamples.FilterByFormat -->
```csharp
// Standard formats have names like "CF_UNICODETEXT", compare the IDs or use AsString()
var textChanges = ClipboardNative.OnUpdate
    .Where(info => info.FormatIds.Contains((uint)StandardClipboardFormats.UnicodeText))
    .Subscribe(info => Console.WriteLine("Text was copied"));

var fileChanges = ClipboardNative.OnUpdate
    .Where(info => info.Formats.Contains(StandardClipboardFormats.Drop.AsString()))
    .Subscribe(info => Console.WriteLine("Files were copied"));

// Registered formats are compared by name, e.g. "PNG" or "HTML Format"
var imageChanges = ClipboardNative.OnUpdate
    .Where(info => info.Formats.Contains("PNG"))
    .Subscribe(info => Console.WriteLine("A PNG was copied"));
```

### Threading

`OnUpdate` publishes on the SharedMessageWindow thread. Don't open the clipboard there and don't do slow work there:
move to another thread first. `Throttle` does that and also gives the copying application time to finish:

<!-- sample: ClipboardSamples.ReadOnChange -->
```csharp
var subscription = ClipboardNative.OnUpdate
    .Where(info => info.FormatIds.Contains((uint)StandardClipboardFormats.UnicodeText))
    // Don't open the clipboard on the SharedMessageWindow thread, and wait until the copying application is done
    .Throttle(TimeSpan.FromMilliseconds(200))
    .Subscribe(info =>
    {
        using var clipboard = ClipboardNative.Access();
        if (clipboard.CanAccess)
        {
            Console.WriteLine($"Copied: {clipboard.GetAsUnicodeString()}");
        }
    });
```

In a UI application, `ObserveOn` the UI thread:

<!-- sample: ClipboardSamples.MonitorOnUi -->
```csharp
// Call on the UI thread: the handler then runs on the UI thread, where the clipboard can be opened and the UI updated
var subscription = ClipboardNative.OnUpdate
    .ObserveOn(SynchronizationContext.Current)
    .Subscribe(info => UpdatePasteButton(ClipboardNative.HasFormat(StandardClipboardFormats.UnicodeText)));
```

## Access: the clipboard lock

The clipboard is one resource for all applications. To read or write it, open it with `ClipboardNative.Access()`, do
your work, and dispose the returned `IClipboardAccessToken`. While you hold it, no other application can use the
clipboard, so keep it short.

- Windows ties the opened clipboard to the thread which opened it. Use and dispose the token on that thread; on another
  thread `CanAccess` is `false`.
- `Access()` doesn't throw when the clipboard is busy. It retries opening it (5 times, 100 ms apart by default) and
  returns a token with `CanAccess == false` and `IsOpenTimeout` (another application has it) or `IsLockTimeout`
  (another thread of your process has it).
- The `Get...` / `Set...` extension methods throw a `ClipboardAccessDeniedException` when the token has no access.

<!-- sample: ClipboardSamples.ReadText -->
```csharp
// Access opens the clipboard on this thread, dispose the token on the same thread
using (var clipboard = ClipboardNative.Access())
{
    if (!clipboard.CanAccess)
    {
        // Another application kept the clipboard open (IsOpenTimeout), or another thread of this process has it (IsLockTimeout)
        return;
    }
    Console.WriteLine($"Formats: {string.Join(", ", clipboard.AvailableFormats())}");
    if (ClipboardNative.HasFormat(StandardClipboardFormats.UnicodeText))
    {
        string text = clipboard.GetAsUnicodeString();
        Console.WriteLine(text);
    }
}
```

`AccessAsync()` waits asynchronously for the in-process lock, but opens the clipboard on the thread which continues
after the `await`. Don't `await` anything else while you hold the token. Cancelling throws an
`OperationCanceledException`.

<!-- sample: ClipboardSamples.ReadTextAsync -->
```csharp
// Only the waiting for the clipboard is asynchronous, the clipboard is opened on the thread which continues after the await.
// Don't await again while holding the token.
using var clipboard = await ClipboardNative.AccessAsync();
string text = clipboard.CanAccess ? clipboard.GetAsUnicodeString() : null;
```

## Reading

| Content | Method |
|---|---|
| Text | `GetAsUnicodeString()` (CF_UNICODETEXT), or `GetAsUnicodeString(format)` for other text formats |
| Files | `GetFileNames()` (CF_HDROP) |
| Any format as bytes | `GetAsBytes(format)` |
| Any format as stream | `GetAsStream(format)` / `TryGetAsStream(format, out stream)`; the stream is a copy and stays valid after the token is disposed |
| Which formats | `AvailableFormats()`, `AvailableFormatIds()`, or `ClipboardNative.HasFormat(format)` without opening the clipboard |

<!-- sample: ClipboardSamples.ReadFiles -->
```csharp
using var clipboard = ClipboardNative.Access();
foreach (var fileName in clipboard.GetFileNames())
{
    Console.WriteLine(fileName);
}
```

<!-- sample: ClipboardSamples.ReadImage -->
```csharp
using var clipboard = ClipboardNative.Access();
// Most applications also place a PNG, which keeps the transparency
if (clipboard.TryGetAsStream("PNG", out var pngStream))
{
    // The stream is a copy, it stays valid after the token is disposed
    using (pngStream)
    using (var bitmap = new Bitmap(pngStream))
    {
        Console.WriteLine($"Image of {bitmap.Width}x{bitmap.Height}");
    }
}
```

<!-- sample: ClipboardSamples.ReadCustomFormat -->
```csharp
using var clipboard = ClipboardNative.Access();
if (clipboard.AvailableFormats().Contains("MyApp.Settings"))
{
    byte[] data = clipboard.GetAsBytes("MyApp.Settings");
    Console.WriteLine($"{data.Length} bytes");
}
```

## Writing

Call `ClearContents()` first. It removes the previous content and makes the window of the token (by default the
SharedMessageWindow) the clipboard owner; without it your formats are added to the content of another application.
Then place one or more formats:

<!-- sample: ClipboardSamples.WriteText -->
```csharp
using var clipboard = ClipboardNative.Access();
// Always clear first: this removes the previous content and makes the window of the token the clipboard owner
clipboard.ClearContents();
clipboard.SetAsUnicodeString("Hello, World!");
```

<!-- sample: ClipboardSamples.WriteFiles -->
```csharp
using var clipboard = ClipboardNative.Access();
clipboard.ClearContents();
// Explorer can paste these, the paths must be fully qualified
clipboard.SetFileNames(new[] { @"C:\Temp\report.pdf", @"C:\Temp\image.png" });
```

<!-- sample: ClipboardSamples.WriteStream -->
```csharp
using var pngStream = new MemoryStream();
bitmap.Save(pngStream, System.Drawing.Imaging.ImageFormat.Png);
pngStream.Position = 0;

using var clipboard = ClipboardNative.Access();
clipboard.ClearContents();
clipboard.SetAsStream("PNG", pngStream);
```

Offer the same content in several formats, the application which pastes picks the best one it understands:

<!-- sample: ClipboardSamples.WriteMultipleFormats -->
```csharp
using var clipboard = ClipboardNative.Access();
clipboard.ClearContents();
// Place several formats of the same content, the application which pastes picks the best one
clipboard.SetAsUnicodeString("Hello, World!");
clipboard.SetAsBytes(Encoding.ASCII.GetBytes(@"{\rtf1\ansi Hello, {\b World}!}"), "Rich Text Format");
// Your own format, it's registered on first use
clipboard.SetAsBytes(Encoding.UTF8.GetBytes("{\"greeting\":\"Hello\"}"), "MyApp.Settings");
```

"HTML Format" is not plain HTML: it needs a header with byte offsets, see
[HTML Clipboard Format](https://learn.microsoft.com/en-us/windows/win32/dataxchg/html-clipboard-format).

To empty the clipboard:

<!-- sample: ClipboardSamples.Clear -->
```csharp
using var clipboard = ClipboardNative.Access();
clipboard.ClearContents();
```

## Delayed rendering

With delayed rendering you announce a format and create the data only when an application pastes it. Register a
renderer for the format first, then announce the format with `SetDelayedRenderedContent`.

<!-- sample: ClipboardSamples.DelayedRendering -->
```csharp
// 1. Register the renderer, keep the registration as long as the content can be requested
IDisposable registration = ClipboardNative.RegisterDelayedRenderer("MyApp.LargeData", request =>
{
    // Called on the SharedMessageWindow thread when an application pastes the format.
    // Render right here, with the token of the request: don't open the clipboard, await or switch threads.
    byte[] data = CreateLargeData();
    request.AccessToken.SetAsBytes(data, request.RequestedFormatId);
});

// 2. Announce the format, the data is created only when somebody pastes it
using (var clipboard = ClipboardNative.Access())
{
    // ClearContents makes the SharedMessageWindow the owner, which gets the render requests
    clipboard.ClearContents();
    clipboard.SetDelayedRenderedContent("MyApp.LargeData");
}

// 3. Later, when the data can't be provided anymore
registration.Dispose();
```

- The renderer is called synchronously on the SharedMessageWindow thread when a format is requested (WM_RENDERFORMAT),
  and for all formats when the owner window is destroyed (WM_RENDERALLFORMATS, `request.RenderAllFormats` is `true`).
- The SharedMessageWindow is not destroyed when the process exits, so it gets no WM_RENDERALLFORMATS then: formats
  which nobody requested are gone from the clipboard when your process ends. Place content that must survive your
  application directly.
- Use `request.AccessToken`; it's only valid while the renderer runs. Don't call `Access()`, don't `await`, don't
  switch threads.
- Only one renderer per format can be registered at a time. `SetDelayedRenderedContent` throws an
  `InvalidOperationException` when no renderer is registered or when the token's window doesn't own the clipboard
  (call `ClearContents()` first).
- Exceptions in a renderer are written to `System.Diagnostics.Trace`.

## Clipboard history and cloud clipboard

Windows 10 1809 and later keep a clipboard history (Win+V) and can sync the clipboard to other devices. Applications
control this per content with special formats:

| Format | Effect |
|---|---|
| `ExcludeClipboardContentFromMonitorProcessing` | not in the history, not synced, and clipboard monitors should ignore it |
| `CanIncludeInClipboardHistory` (DWORD 0 / 1) | 0 keeps it out of the history, 1 allows it |
| `CanUploadToCloudClipboard` (DWORD 0 / 1) | 0 keeps it from being synced, 1 allows it |

Place them after the content, with the same token. For passwords and other secrets use `ExcludeFromMonitorProcessing()`:

<!-- sample: ClipboardSamples.CloudOptionsSensitive -->
```csharp
using var clipboard = ClipboardNative.Access();
clipboard.ClearContents();
clipboard.SetAsUnicodeString("MyPassword123!");
// Not in the clipboard history (Win+V), not synced to other devices, ignored by clipboard monitors
clipboard.ExcludeFromMonitorProcessing();
```

`SetCloudClipboardOptions` places only the options you pass; without arguments it places nothing and the user's
settings apply:

<!-- sample: ClipboardSamples.CloudOptions -->
```csharp
using var clipboard = ClipboardNative.Access();
clipboard.ClearContents();
clipboard.SetAsUnicodeString("Temporary value");
// Keep it out of the history and the cloud, but let clipboard managers see it.
// Options which are not passed (null) are not placed, then the user's settings apply.
clipboard.SetCloudClipboardOptions(canIncludeInHistory: false, canUploadToCloud: false);
```

<!-- sample: ClipboardSamples.CloudOptionsSingle -->
```csharp
using var clipboard = ClipboardNative.Access();
clipboard.ClearContents();
clipboard.SetAsUnicodeString("Shared snippet");
clipboard.SetCanIncludeInClipboardHistory(true);
clipboard.SetCanUploadToCloudClipboard(false);
```

If you write a clipboard monitor yourself, respect `ExcludeClipboardContentFromMonitorProcessing`
(the constant is `ClipboardCloudExtensions.ExcludeClipboardContentFromMonitorProcessingFormat`).

## Errors

<!-- sample: ClipboardSamples.AccessDenied -->
```csharp
try
{
    // Wait up to 1 second for another thread of this process, and try to open the clipboard 10 times, 50ms apart
    using var clipboard = ClipboardNative.Access(retries: 10, retryInterval: TimeSpan.FromMilliseconds(50), timeout: TimeSpan.FromSeconds(1));
    // The Get / Set extension methods throw a ClipboardAccessDeniedException when the token has no access
    var text = clipboard.GetAsUnicodeString();
}
catch (ClipboardAccessDeniedException ex)
{
    Console.WriteLine($"The clipboard is in use: {ex.Message}");
}
```

## Formats

| `StandardClipboardFormats` | Name | Content |
|---|---|---|
| `Text` | `CF_TEXT` | ANSI text |
| `UnicodeText` | `CF_UNICODETEXT` | UTF-16 text |
| `Bitmap` | `CF_BITMAP` | GDI bitmap handle |
| `DeviceIndependentBitmap` | `CF_DIB` | BITMAPINFO followed by the pixels |
| `DeviceIndependentBitmapV5` | `CF_DIBV5` | BITMAPV5HEADER followed by the pixels |
| `Drop` | `CF_HDROP` | list of files |
| `Locale` | `CF_LOCALE` | the locale of CF_TEXT |
| registered | `PNG` | PNG file |
| registered | `HTML Format` | HTML with a header |
| registered | `Rich Text Format` | RTF |

## See also

- [Window messages and the SharedMessageWindow](window-messages.md)
- [Common scenarios](common-scenarios.md): clipboard history, saving copied images, inserting text with a hotkey
