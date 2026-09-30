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

`ClipboardNative.Access()` opens the clipboard on the calling thread; use and dispose the token on that thread and
keep it short. When the clipboard is busy `CanAccess` is `false`, and the `Get...` / `Set...` methods throw a
`ClipboardAccessDeniedException`.

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

Call `ClearContents()` before you write, it makes you the owner of the clipboard:

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

## Delayed rendering

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

Files, images, streams, `AccessAsync` and error handling: see the [documentation](https://www.dapplo.net/Dapplo.Windows/articles/clipboard-usage.html).
