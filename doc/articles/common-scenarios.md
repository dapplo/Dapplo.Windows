# Common scenarios

Recipes which combine several packages. They are short on purpose: add error handling and disposal as your
application needs it.

Namespaces used on this page: `Dapplo.Windows.Clipboard`, `Dapplo.Windows.Desktop`, `Dapplo.Windows.Enums`,
`Dapplo.Windows.Input.Enums`, `Dapplo.Windows.Input.Keyboard`, `Dapplo.Windows.Messages`,
`Dapplo.Windows.Messages.Enumerations`, `Dapplo.Windows.User32`, `Dapplo.Windows.User32.Enums`,
`Dapplo.Windows.Common.Extensions`, `System.Reactive.Linq`, `System.Reactive.Concurrency`.

## Screenshot of the active window with a hotkey

Alt+PrintScreen, but as PNG with transparency on the clipboard. The work is moved off the hook thread with `ObserveOn`.

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

## Time tracking: which application is active

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

## Notice when an application starts

<!-- sample: CommonScenariosSamples.ApplicationStarts -->
```csharp
// Get told when Notepad opens a window
var subscription = WinEventHook.WindowCreateDestroyObservable()
    .Where(info => info.WinEvent == WinEvents.EVENT_OBJECT_CREATE)
    .Select(info => InteropWindowFactory.CreateFor(info.Handle))
    .Where(window => window.IsTopLevel() && window.GetClassname() == "Notepad")
    .Subscribe(window => Console.WriteLine($"Notepad opened a window: {window.Handle}"));
```

For the lock screen and logon / logoff see `WindowsSessionListener` in
[Window messages](window-messages.md#session-changes-lock-unlock-logon-logoff).

## Clipboard history

A simple history of copied text. It skips content which asked to be excluded from monitoring (passwords).

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

## Save copied images

<!-- sample: CommonScenariosSamples.SaveClipboardImages -->
```csharp
var folder = Environment.GetFolderPath(Environment.SpecialFolder.MyPictures);
var subscription = ClipboardNative.OnUpdate
    .Skip(1)
    .Where(info => info.Formats.Contains("PNG"))
    .ObserveOn(TaskPoolScheduler.Default)
    .Subscribe(info =>
    {
        using var clipboard = ClipboardNative.Access();
        if (clipboard.CanAccess && clipboard.TryGetAsStream("PNG", out var png))
        {
            using (png)
            using (var file = File.Create(Path.Combine(folder, $"clipboard-{info.Timestamp:yyyyMMdd-HHmmss}.png")))
            {
                png.CopyTo(file);
            }
        }
    });
```

## Insert text with a hotkey

There is no API to type text, keys depend on the keyboard layout. Put the text on the clipboard and send Ctrl+V.
`TriggerOnKeyUp` passes the hotkey on, and the short wait lets the user release Ctrl and Alt before Ctrl+V is sent.

<!-- sample: CommonScenariosSamples.InsertTimestamp -->
```csharp
// Ctrl+Alt+D types the current date into the active application: via the clipboard and Ctrl+V,
// as there is no API to "type" text. TriggerOnKeyUp lets the keys through, so the user can't get stuck keys.
var subscription = KeyboardHook.KeyboardEvents
    .Where(new KeyCombinationHandler(VirtualKeyCode.Control, VirtualKeyCode.Menu, VirtualKeyCode.KeyD) { TriggerOnKeyUp = true })
    .ObserveOn(TaskPoolScheduler.Default)
    .Subscribe(_ =>
    {
        // Give the user a moment to release Ctrl and Alt, otherwise Ctrl+Alt+V is sent
        Thread.Sleep(300);
        using (var clipboard = ClipboardNative.Access())
        {
            clipboard.ClearContents();
            clipboard.SetAsUnicodeString(DateTime.Now.ToString("yyyy-MM-dd"));
            clipboard.ExcludeFromMonitorProcessing();
        }
        KeyboardInputGenerator.KeyCombinationPress(VirtualKeyCode.Control, VirtualKeyCode.KeyV);
    });
```

This replaces the previous clipboard content. To be nice, read the old content first and put it back after the paste.

## Tile windows

<!-- sample: CommonScenariosSamples.TileWindows -->
```csharp
// Place the visible windows of the primary display next to each other
var primary = DisplayInfo.AllDisplayInfos.First(display => display.IsPrimary);
var windows = InteropWindowQuery.GetTopLevelWindows()
    .Where(window => window.IsVisible() && !window.IsMinimized() && !string.IsNullOrEmpty(window.GetCaption()))
    .Where(window => primary.Bounds.Contains(window.GetInfo().Bounds.Location))
    .ToList();
if (windows.Count == 0)
{
    return;
}
var width = primary.WorkingArea.Width / windows.Count;
for (var i = 0; i < windows.Count; i++)
{
    windows[i].Restore();
    User32Api.SetWindowPos(windows[i].Handle, IntPtr.Zero,
        primary.WorkingArea.Left + i * width, primary.WorkingArea.Top, width, primary.WorkingArea.Height,
        WindowPos.SWP_NOZORDER | WindowPos.SWP_NOACTIVATE);
}
```

## Minimize all other windows

<!-- sample: CommonScenariosSamples.MinimizeOthers -->
```csharp
var active = InteropWindowQuery.GetForegroundWindow();
foreach (var window in InteropWindowQuery.GetTopLevelWindows().Where(w => w.IsVisible() && w.Handle != active.Handle))
{
    window.Minimize();
}
```

## Single instance

A second instance tells the first one to show itself, with a registered message which the SharedMessageWindow of the
first instance receives.

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

WinEvents, mouse moves and clipboard changes can arrive in bursts. `Throttle` waits for a quiet moment, `Sample` takes
the latest value every interval, `Buffer` collects them.

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

## More

- A DPI-aware form: [DPI awareness](dpi-awareness.md#dpiawareform)
- Update files that are in use: [Restart Manager](restart-manager.md#installer-side)
- Keep the PC awake, or wake it up: [Power and system state](system-state.md)
- A global hotkey with `RegisterHotKey`: [Window messages](window-messages.md#registrations-listen)
