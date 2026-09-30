# SharedMessageWindow

Package **Dapplo.Windows.Messages**. Full version: [Window messages](https://www.dapplo.net/Dapplo.Windows/articles/window-messages.html).

Many notifications arrive as window messages: clipboard changes, power, session and device changes, hotkeys, raw
input. The `SharedMessageWindow` is one hidden window, shared by all of Dapplo.Windows, which receives them:

- It's created on first use on its own STA thread with a message loop, and lives until the process exits.
  `Handle` never returns 0.
- It's a hidden top-level window (not message-only), so it also gets broadcasts like `WM_QUERYENDSESSION`,
  `WM_POWERBROADCAST` and `WM_SETTINGCHANGE`.
- `Messages` is called synchronously in the window procedure, on the window thread. Keep `OnNext` short.
- Set `Handled` and `Result` synchronously in `OnNext` to answer a message.
- An exception in a subscriber ends only that subscription; it's published on `SubscriberErrors`.

<!-- sample: MessagesSamples.SimpleFilter -->
```csharp
// Called on the SharedMessageWindow thread: keep it short, and never block it
IDisposable subscription = SharedMessageWindow.Messages
    .Where(m => m.Msg == WindowsMessages.WM_DISPLAYCHANGE)
    .Subscribe(m => Console.WriteLine($"Display changed, new resolution {(int)m.LParam & 0xFFFF}x{((int)m.LParam >> 16) & 0xFFFF}"));

// Stop listening, the window itself stays alive
subscription.Dispose();
```

<!-- sample: MessagesSamples.HandleMessage -->
```csharp
// Handled and Result must be set synchronously in OnNext, before any ObserveOn
var subscription = SharedMessageWindow.Messages
    .Where(m => m.Msg == WindowsMessages.WM_QUERYENDSESSION)
    .Subscribe(m =>
    {
        m.Result = 1;    // TRUE: the session may end
        m.Handled = true; // don't call DefWindowProc
    });
```

## Registrations: Listen

`Listen(onSetup, onTeardown)` runs both callbacks on the window thread, once per subscription, for APIs that register
a window, like `RegisterHotKey`:

<!-- sample: MessagesSamples.ListenHotkeyPInvoke -->
```csharp
[DllImport("user32", SetLastError = true)]
private static extern bool RegisterHotKey(IntPtr hWnd, int id, uint modifiers, uint virtualKey);

[DllImport("user32", SetLastError = true)]
private static extern bool UnregisterHotKey(IntPtr hWnd, int id);
```

<!-- sample: MessagesSamples.ListenHotkey -->
```csharp
const int hotkeyId = 1;
const uint modControl = 0x0002, modShift = 0x0004, modNoRepeat = 0x4000;

// RegisterHotKey must be called on the thread of the window which gets WM_HOTKEY:
// Listen runs onSetup and onTeardown on the SharedMessageWindow thread
var subscription = SharedMessageWindow.Listen(
        onSetup: hwnd =>
        {
            if (!RegisterHotKey(hwnd, hotkeyId, modControl | modShift | modNoRepeat, (uint)'P'))
            {
                // The exception goes to OnError of this subscription
                throw new System.ComponentModel.Win32Exception();
            }
        },
        onTeardown: hwnd => UnregisterHotKey(hwnd, hotkeyId))
    .Where(m => m.Msg == WindowsMessages.WM_HOTKEY && m.WParam == hotkeyId)
    .Subscribe(
        m => Console.WriteLine("Ctrl+Shift+P pressed"),
        ex => Console.WriteLine($"Registering the hotkey failed: {ex.Message}"));

// Disposing the subscription runs onTeardown (UnregisterHotKey) on the window thread
subscription.Dispose();
```

`Invoke(action)` runs any code on the window thread:

<!-- sample: MessagesSamples.Invoke -->
```csharp
// Run code on the window thread and wait for it, exceptions are rethrown here
bool isWindowThread = false;
SharedMessageWindow.Invoke(hwnd => isWindowThread = SharedMessageWindow.IsWindowThread);

// The handle exists as soon as it's requested, and stays valid for the life of the process
IntPtr handle = SharedMessageWindow.Handle;
```

## Who uses it

| Feature | Message |
|---|---|
| `ClipboardNative.OnUpdate`, delayed rendering | `WM_CLIPBOARDUPDATE`, `WM_RENDERFORMAT`, `WM_RENDERALLFORMATS` |
| `WinEventHook` | WinEvent callbacks |
| `RawInputMonitor`, `RawInputDeviceMonitor` | `WM_INPUT`, `WM_INPUT_DEVICE_CHANGE` |
| `WindowsSessionListener` | `WM_WTSSESSION_CHANGE` |
| `EnvironmentMonitor` | `WM_SETTINGCHANGE` |
| `DisplayInfo` | `WM_DISPLAYCHANGE`, `WM_SETTINGCHANGE`, `WM_DPICHANGED` |
| `PowerBroadcastListener` | `WM_POWERBROADCAST` |
| `ApplicationRestartManager.ListenForEndSession` | `WM_QUERYENDSESSION`, `WM_ENDSESSION` |
| `DeviceNotification` | `WM_DEVICECHANGE` |

## Session changes

<!-- sample: MessagesSamples.SessionListener -->
```csharp
var sessionListener = new WindowsSessionListener();

// The events are raised on the SharedMessageWindow thread
sessionListener.SessionLockChange += (sender, args) =>
{
    if (args.EventType == WtsSessionChangeEvents.WTS_SESSION_LOCK)
    {
        Console.WriteLine($"Session {args.SessionId} locked");
    }
    else if (args.EventType == WtsSessionChangeEvents.WTS_SESSION_UNLOCK)
    {
        Console.WriteLine($"Session {args.SessionId} unlocked");
    }
};

sessionListener.SessionLogonChange += (sender, args) =>
    Console.WriteLine(args.EventType == WtsSessionChangeEvents.WTS_SESSION_LOGON ? "Logged on" : "Logged off");

// Early at logon the registration can fail, it's retried for about 2 minutes before this is raised
sessionListener.RegistrationFailed += (sender, args) =>
    Console.WriteLine($"No session notifications: {args.GetException().Message}");

sessionListener.Start();

// Ignore events for a while, without unregistering
sessionListener.Pause();
sessionListener.Resume();

// Stop listening and unregister
sessionListener.Dispose();
```

## Your own windows

For the messages of your own forms or WPF windows use `WinProcFormsMessages()` (Dapplo.Windows.Forms) or
`WinProcMessages()` (Dapplo.Windows.Wpf):

<!-- sample: MessagesSamples.FormsMessages -->
```csharp
// Subclasses the form's window. Runs on the UI thread, you may set Handled / Result.
// The sequence follows handle re-creation and completes when the form is disposed.
var subscription = form.WinProcFormsMessages()
    .Where(m => m.Message == WindowsMessages.WM_NCHITTEST)
    .Subscribe(m =>
    {
        // HTCAPTION: the whole window can be dragged like its title bar
        m.Result = new IntPtr(2);
        m.Handled = true;
    });
```
