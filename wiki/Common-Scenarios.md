# Common scenarios

Recipes which combine several packages. More in the [documentation](https://www.dapplo.net/Dapplo.Windows/articles/common-scenarios.html).

## Screenshot of the active window with a hotkey

<!-- sample: CommonScenariosSamples.ScreenshotHotkey -->
```csharp
// Alt+PrintScreen replacement: capture the active window as PNG onto the clipboard
var subscription = KeyboardHook.KeyboardEvents
    .Where(new KeyCombinationHandler(VirtualKeyCode.Menu, VirtualKeyCode.PrintScreen))
    // Leave the hook thread before doing the work
    .ObserveOn(TaskPoolScheduler.Default)
    .Subscribe(_ =>
    {
        var window = InteropWindowQuery.GetForegroundWindow();
        using var bitmap = window.PrintWindow();
        if (bitmap == null)
        {
            return;
        }
        using var png = new MemoryStream();
        bitmap.Save(png, ImageFormat.Png);
        png.Position = 0;

        using var clipboard = ClipboardNative.Access();
        clipboard.ClearContents();
        clipboard.SetAsStream("PNG", png);
    });
```

## Time tracking

<!-- sample: CommonScenariosSamples.ActiveWindowTracking -->
```csharp
// How long is each application in the foreground?
var usage = new Dictionary<string, TimeSpan>();
string currentProcess = null;
var since = DateTimeOffset.Now;

var subscription = WinEventHook.Create(WinEvents.EVENT_SYSTEM_FOREGROUND)
    // Don't query the process on the SharedMessageWindow thread
    .ObserveOn(TaskPoolScheduler.Default)
    .Subscribe(info =>
    {
        var now = DateTimeOffset.Now;
        if (currentProcess != null)
        {
            usage[currentProcess] = (usage.TryGetValue(currentProcess, out var total) ? total : TimeSpan.Zero) + (now - since);
        }
        using var process = Process.GetProcessById(InteropWindowFactory.CreateFor(info.Handle).GetProcessId());
        currentProcess = process.ProcessName;
        since = now;
    });
```

## Clipboard history

<!-- sample: CommonScenariosSamples.ClipboardHistory -->
```csharp
var history = new List<string>();
var subscription = ClipboardNative.OnUpdate
    // Skip the current content, only log changes
    .Skip(1)
    .Where(info => info.FormatIds.Contains((uint)StandardClipboardFormats.UnicodeText))
    // Respect passwords and other excluded content
    .Where(info => !info.Formats.Contains(ClipboardCloudExtensions.ExcludeClipboardContentFromMonitorProcessingFormat))
    .ObserveOn(TaskPoolScheduler.Default)
    .Subscribe(info =>
    {
        using var clipboard = ClipboardNative.Access();
        if (clipboard.CanAccess)
        {
            history.Add(clipboard.GetAsUnicodeString());
        }
    });
```

## Insert text with a hotkey

`TypeText` types the text independent of the keyboard layout, `TriggerMode.AllKeysUp` waits until the hotkey is released:

<!-- sample: CommonScenariosSamples.InsertTimestamp -->
```csharp
// Ctrl+Alt+D types the current date into the active application.
// AllKeysUp fires when the user released all keys of the combination, so the text isn't combined with Ctrl or Alt,
// and the keys are passed on, so no key gets stuck.
var subscription = KeyboardHook.KeyboardEvents
    .Where(new KeyCombinationHandler(VirtualKeyCode.Control, VirtualKeyCode.Menu, VirtualKeyCode.KeyD) { TriggerMode = TriggerMode.AllKeysUp })
    .ObserveOn(TaskPoolScheduler.Default)
    .Subscribe(_ => KeyboardInputGenerator.TypeText(DateTime.Now.ToString("yyyy-MM-dd")));
```

## Single instance

<!-- sample: CommonScenariosSamples.SingleInstancePInvoke -->
```csharp
[System.Runtime.InteropServices.DllImport("user32", SetLastError = true)]
private static extern bool PostMessage(IntPtr hWnd, uint msg, IntPtr wParam, IntPtr lParam);
```

<!-- sample: CommonScenariosSamples.SingleInstance -->
```csharp
// The first instance listens, a second instance broadcasts and exits
uint showMessage = WindowsMessage.RegisterWindowsMessage("MyApp.ShowMainWindow");
using var mutex = new Mutex(true, "MyApp.SingleInstance", out var isFirstInstance);
if (!isFirstInstance)
{
    // HWND_BROADCAST: every top-level window gets it, also the SharedMessageWindow of the first instance
    PostMessage(new IntPtr(0xFFFF), showMessage, IntPtr.Zero, IntPtr.Zero);
    return;
}
var subscription = SharedMessageWindow.Messages
    .Where(m => (uint)m.Msg == showMessage)
    .ObserveOn(SynchronizationContext.Current)
    .Subscribe(_ => showMainWindow());
```

## Handling many events

<!-- sample: CommonScenariosSamples.Debounce -->
```csharp
// Location changes arrive by the hundred while a window is dragged: wait until it is quiet for 250ms
var subscription = WinEventHook.Create(WinEvents.EVENT_OBJECT_LOCATIONCHANGE)
    .Where(info => info.ObjectIdentifier == ObjectIdentifiers.Window)
    .Throttle(TimeSpan.FromMilliseconds(250))
    .Subscribe(info => Console.WriteLine("A window stopped moving"));

// Or collect them and process them in batches
var batches = ClipboardNative.OnUpdate
    .Buffer(TimeSpan.FromSeconds(1))
    .Where(batch => batch.Count > 0)
    .Subscribe(batch => Console.WriteLine($"{batch.Count} clipboard changes in the last second"));
```
