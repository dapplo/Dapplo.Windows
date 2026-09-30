# Clipboard

Package **Dapplo.Windows.Clipboard**. Full version: [Clipboard](https://www.dapplo.net/Dapplo.Windows/articles/clipboard-usage.html).

## Monitoring

`ClipboardNative.OnUpdate` first gives the current state, then one update per change. The information is collected
without opening the clipboard, on the thread of the [[SharedMessageWindow]]: move to another thread before you open
the clipboard.

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

Standard formats are named like `"CF_UNICODETEXT"`; compare the IDs or use `StandardClipboardFormats.X.AsString()`.

## Reading and writing

Threading rules:

- The clipboard works on **any thread**, no STA thread is needed.
- Windows ties the opened clipboard to the thread which opened it: use and dispose the token on that thread.
  On another thread the `Get...` / `Set...` methods and `Dispose` throw an `InvalidOperationException`.
- **Never `await` while the clipboard is open.** From async code, prefer `UseAsync`.

`ClipboardNative.UseAsync(work)` waits asynchronously until the clipboard can be opened, then opens it, runs `work` and
closes it again on one thread. `work` must not be async; it throws a `ClipboardAccessDeniedException` when the
clipboard stays busy. `ClipboardAccessOptions` sets the owner window, retries and timeouts.

<!-- sample: ClipboardSamples.UseAsync -->
```csharp
// Waits asynchronously until the clipboard can be opened, then opens it, runs the work and closes it again:
// all on one thread, so the token can't end up on another thread. The work must not await.
string text = await ClipboardNative.UseAsync(clipboard => clipboard.GetAsUnicodeString());

// Write: prepare the content first, only place it inside the work
var contents = new ClipboardContents().AddUnicodeString("Hello, World!");
await ClipboardNative.UseAsync(clipboard => clipboard.ReplaceContents(contents));
```

`ClipboardNative.Access()` opens the clipboard on the calling thread; keep it short. When the clipboard is busy
`CanAccess` is `false`, and the `Get...` / `Set...` methods throw a `ClipboardAccessDeniedException`.

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

Write with `ClipboardNative.ReplaceContents`: it clears the clipboard and places all formats of a `ClipboardContents`
in one short operation. Formats can only be added to the current content with `AddToCurrentContents`, and only while
you own it.

<!-- sample: ClipboardSamples.ReplaceContents -->
```csharp
// Prepare everything before the clipboard is opened
using var pngStream = new MemoryStream();
bitmap.Save(pngStream, System.Drawing.Imaging.ImageFormat.Png);
pngStream.Position = 0;

var contents = new ClipboardContents()
    // The richest format first, the application which pastes picks the first one it understands
    .AddStream("PNG", pngStream)
    .AddUnicodeString("A screenshot")
    .AddFileNames(new[] { @"C:\Temp\screenshot.png" })
    // Optional: clipboard history (Win+V) and cloud clipboard
    .WithCloudClipboardOptions(canUploadToCloud: false);

// Opens the clipboard, clears it, places all formats and closes it again.
// Throws a ClipboardAccessDeniedException when the clipboard can't be opened.
ClipboardNative.ReplaceContents(contents);
```

Low level: call `ClearContents()` before the `Set...` methods, it makes you the owner of the clipboard. On content of
another window the `Set...` methods throw an `InvalidOperationException`.

<!-- sample: ClipboardSamples.WriteText -->
```csharp
using var clipboard = ClipboardNative.Access();
// Always clear first: this removes the previous content and makes the window of the token the clipboard owner
clipboard.ClearContents();
clipboard.SetAsUnicodeString("Hello, World!");
```

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

## Snapshots

`ClipboardNative.ReadSnapshotAsync(formats)` copies formats into memory in one short session; decode or upload them
afterwards. `ClipboardSnapshot` and `clipboard.AsDataSource()` both implement `IClipboardDataSource`, so the same
`GetAsUnicodeString` / `GetAsBytes` / `GetFileNames` code reads both.

<!-- sample: ClipboardSamples.Snapshot -->
```csharp
// Copy the formats you need in one short clipboard session...
ClipboardSnapshot snapshot = await ClipboardNative.ReadSnapshotAsync(new[] { "PNG", StandardClipboardFormats.UnicodeText.AsString() });

// ...then decode, save or upload while other applications can use the clipboard again
if (snapshot.TryGetStream("PNG", out var pngStream))
{
    using (pngStream)
    using (var file = File.Create(@"C:\Temp\pasted.png"))
    {
        await pngStream.CopyToAsync(file);
    }
}
string text = snapshot.GetAsUnicodeString();

// Has the clipboard changed since?
bool changed = snapshot.SequenceNumber != ClipboardNative.SequenceNumber;
```

When the clipboard stays busy, `ClipboardAccessDeniedException.BlockingWindow` / `BlockingProcessId` (also on the token)
tell which application keeps it open.

## HTML, bitmaps and metafiles

Without System.Drawing: `AddHtml` / `SetAsHtml` and `TryGetAsHtml` handle CF_HTML with its byte-offset header;
`AddDib` / `SetAsDib` and `TryGetAsDib` read and write CF_DIB / CF_DIBV5 as raw BGRA32 pixels (`DibImage`);
`TryGetEnhancedMetafileBits` reads CF_ENHMETAFILE as EMF bytes.

<!-- sample: ClipboardSamples.Html -->
```csharp
// Write: the header with the UTF-8 byte offsets is created for you
await ClipboardNative.UseAsync(clipboard => clipboard.ReplaceContents(new ClipboardContents()
    .AddHtml("<p>Hello <b>World</b></p>", new Uri("https://example.com/"))
    .AddUnicodeString("Hello World")));

// Read: what a browser or Word copied
var snapshot = await ClipboardNative.ReadSnapshotAsync(new[] { ClipboardHtml.FormatName });
if (snapshot.TryGetAsHtml(out ClipboardHtml html))
{
    Console.WriteLine($"Copied from {html.SourceUrl}: {html.Fragment}");
}
```

## Drag and drop and virtual files

`DataObjectReader` reads an OLE data object (a drop, or `ClipboardNative.GetOleDataObject()`): formats with an index,
`IStream` data and virtual files (`GetVirtualFiles()`, e.g. Outlook attachments). OLE needs an STA UI thread.

## Delayed rendering

The renderer runs when an application pastes the format, and at process exit for every format nobody requested yet:
the SharedMessageWindow is destroyed then (`SharedMessageWindow.Shutdown`), so the content survives your application.

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

// 2. Announce the format, the data is created only when somebody pastes it.
// ReplaceContents clears the clipboard, this makes the SharedMessageWindow the owner which gets the render requests.
ClipboardNative.ReplaceContents(new ClipboardContents().AddDelayedRendered("MyApp.LargeData"));

// 3. Keep the registration until the process exits: then the SharedMessageWindow is destroyed,
// and the renderer is called for every format which nobody requested yet (WM_RENDERALLFORMATS), so the content survives your process.
// Disposing it earlier means these formats can't be rendered anymore.
registration.Dispose();
```

For content-specific data, `SetDelayedRenderedContent(format, () => stream)` needs no registration: the renderer is
dropped when the content is replaced. Clipboard history and clipboard managers usually request formats right away.

## Clipboard history and cloud clipboard

<!-- sample: ClipboardSamples.CloudOptionsSensitive -->
```csharp
using var clipboard = ClipboardNative.Access();
clipboard.ClearContents();
clipboard.SetAsUnicodeString("MyPassword123!");
// Not in the clipboard history (Win+V), not synced to other devices, ignored by clipboard monitors
clipboard.ExcludeFromMonitorProcessing();
```

<!-- sample: ClipboardSamples.CloudOptions -->
```csharp
using var clipboard = ClipboardNative.Access();
clipboard.ClearContents();
clipboard.SetAsUnicodeString("Temporary value");
// Keep it out of the history and the cloud, but let clipboard managers see it.
// Options which are not passed (null) are not placed, then the user's settings apply.
clipboard.SetCloudClipboardOptions(canIncludeInHistory: false, canUploadToCloud: false);
```

Files, images, streams, `AccessAsync`, `ClipboardAccessOptions` and error handling: see the [documentation](https://www.dapplo.net/Dapplo.Windows/articles/clipboard-usage.html).
