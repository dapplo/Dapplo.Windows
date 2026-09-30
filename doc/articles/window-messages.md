# Window messages and the SharedMessageWindow

Many Windows notifications arrive as messages for a window: clipboard changes, power events, session changes, device
changes, hotkeys, raw input. **Dapplo.Windows.Messages** provides one hidden window for all of them, the
`SharedMessageWindow`, and exposes its messages as an observable.

```powershell
dotnet add package Dapplo.Windows.Messages
```

Namespaces used on this page: `Dapplo.Windows.Messages`, `Dapplo.Windows.Messages.Enumerations`,
`System.Reactive.Linq`; for the integrations `Dapplo.Windows.Forms.Messages`, `Dapplo.Windows.Wpf.Messages` and
`Dapplo.Windows.Desktop`.

## The SharedMessageWindow

- It is created on first use (the first access to `Handle`, `Messages`, `Listen` or `Invoke`) on its own background
  STA thread with a message loop, and then lives until the process exits. Subscribing or disposing never creates or
  destroys it. If something destroys the window, it's created again on the next use.
- When the process exits (`AppDomain.ProcessExit`) it is destroyed on its own thread, see [Shutdown](#shutdown), so
  delayed rendered clipboard content is rendered and survives the process.
- It is a hidden top-level window (`WS_POPUP` with `WS_EX_TOOLWINDOW`), not a message-only window: only top-level
  windows receive broadcasts such as `WM_QUERYENDSESSION`, `WM_POWERBROADCAST`, `WM_DISPLAYCHANGE` and
  `WM_SETTINGCHANGE`. It doesn't show up in the taskbar or in Alt+Tab.
- `SharedMessageWindow.Handle` returns the handle; it waits until the window exists and never returns 0.
- All of Dapplo.Windows uses this one window, so every package that needs messages shares one thread.

### Receiving messages

`SharedMessageWindow.Messages` is a hot observable of every message the window receives. `OnNext` is called
synchronously inside the window procedure, on the window thread. Keep it short: while your code runs, the window can't
process other messages.

<!-- sample: MessagesSamples.SimpleFilter -->
```csharp
// Called on the SharedMessageWindow thread: keep it short, and never block it
IDisposable subscription = SharedMessageWindow.Messages
    .Where(m => m.Msg == WindowsMessages.WM_DISPLAYCHANGE)
    .Subscribe(m => Console.WriteLine($"Display changed, new resolution {(int)m.LParam & 0xFFFF}x{((int)m.LParam >> 16) & 0xFFFF}"));

// Stop listening, the window itself stays alive
subscription.Dispose();
```

Use `ObserveOn` to continue somewhere else, for example on the UI thread:

<!-- sample: MessagesSamples.ObserveOnUi -->
```csharp
// Filter on the window thread, then continue on the UI thread (call this on the UI thread)
var subscription = SharedMessageWindow.Messages
    .Where(m => m.Msg == WindowsMessages.WM_SETTINGCHANGE)
    .ObserveOn(SynchronizationContext.Current)
    .Subscribe(m => statusLabel.Text = "Settings changed");
```

### Answering a message

Each message is a `WindowMessage` object which all subscribers share. Set `Handled = true` and a `Result` to answer
Windows yourself; when nobody sets `Handled`, the default window procedure processes the message. This only works
synchronously inside `OnNext`: after `ObserveOn`, `Throttle`, `Delay` or an `await` the answer was already sent.

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

### Registrations: Listen

Many APIs register a window for notifications and must be called on the thread of that window (`RegisterHotKey`,
`AddClipboardFormatListener`, `WTSRegisterSessionNotification`, `RegisterRawInputDevices`, ...). `Listen(onSetup,
onTeardown)` runs `onSetup` on the window thread when you subscribe, and `onTeardown` on the window thread when you
dispose, exactly once per subscription. When `onSetup` throws, the exception goes to `OnError` of the subscriber and
`onTeardown` is not called.

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

### Running code on the window thread: Invoke

`Invoke(action)` runs the action synchronously on the window thread and rethrows its exceptions. On the window thread
itself it calls the action directly. Don't call it from a thread the window thread is waiting for, that would
deadlock.

<!-- sample: MessagesSamples.Invoke -->
```csharp
// Run code on the window thread and wait for it, exceptions are rethrown here
bool isWindowThread = false;
SharedMessageWindow.Invoke(hwnd => isWindowThread = SharedMessageWindow.IsWindowThread);

// The handle exists as soon as it's requested, and stays valid until the window is shut down (at process exit)
IntPtr handle = SharedMessageWindow.Handle;
```

### Shutdown

`SharedMessageWindow.Shutdown(timeout)` destroys the window on its own thread and waits until its message loop ended.
Destroying it makes Windows send `WM_RENDERALLFORMATS` (when the window owns the clipboard) and `WM_DESTROY`: the
[delayed clipboard renderers](clipboard-usage.md#delayed-rendering) run synchronously on the window thread before
`Shutdown` returns. It returns `false` when the timeout elapsed.

- It's called automatically on `AppDomain.ProcessExit`, with `ProcessExitShutdownTimeout` (default 1.5 seconds;
  on .NET Framework all ProcessExit handlers together get about 2 seconds). ProcessExit runs on another thread, the
  window thread is a background thread which is still running then. After that `IsProcessExiting` is `true` and the
  window is not created again: using it throws an `ObjectDisposedException`.
- Call it yourself at the end of `Main` when you want to choose the moment or need more time.
- After an explicit `Shutdown` (not at process exit) the next use creates a new window, but the registrations of the
  old window (`Listen`, `Invoke`: clipboard listener, hotkeys, session notifications) are gone and their `onTeardown`
  is not called. It's meant for the end of the application.
- Called on the window thread, it destroys the window directly and returns `true`; the loop ends right after.

<!-- sample: MessagesSamples.Shutdown -->
```csharp
// Happens automatically on AppDomain.ProcessExit, with this timeout (default 1.5 seconds)
SharedMessageWindow.ProcessExitShutdownTimeout = TimeSpan.FromSeconds(1);

// Or at the end of Main, to control the moment and the timeout yourself:
// the window is destroyed on its own thread, delayed rendered clipboard formats are rendered (WM_RENDERALLFORMATS)
bool isShutDown = SharedMessageWindow.Shutdown(TimeSpan.FromSeconds(5));
if (!isShutDown)
{
    Console.WriteLine("The window thread didn't end in time, e.g. a delayed renderer is still busy");
}
```

### Errors in subscribers

An exception in a subscriber doesn't stop the message loop or the other subscribers. The failing subscription ends,
and the exception is published on `SubscriberErrors` and written to `System.Diagnostics.Trace`.

<!-- sample: MessagesSamples.SubscriberErrors -->
```csharp
// An exception in a subscriber ends only that subscription, it is published here (and written to Trace)
var errorSubscription = SharedMessageWindow.SubscriberErrors
    .Subscribe(ex => Console.Error.WriteLine($"A message subscriber failed: {ex}"));
```

### Your own messages

`WindowsMessage.RegisterWindowsMessage` registers a message which is unique on the desktop, for example so a second
instance of your application can talk to the first one (see
[Common scenarios](common-scenarios.md#single-instance)).

<!-- sample: MessagesSamples.CustomMessage -->
```csharp
// A message which is unique for the whole desktop, e.g. to let a second instance talk to the first
uint showMeMessage = WindowsMessage.RegisterWindowsMessage("MyApp.ShowMe");

var subscription = SharedMessageWindow.Messages
    .Where(m => (uint)m.Msg == showMeMessage)
    .Subscribe(m => Console.WriteLine("Another instance asked me to show myself"));
```

## What uses the SharedMessageWindow

| Feature | Message | Page |
|---|---|---|
| `ClipboardNative.OnUpdate`, delayed rendering | `WM_CLIPBOARDUPDATE`, `WM_RENDERFORMAT`, `WM_RENDERALLFORMATS`, `WM_DESTROYCLIPBOARD` | [Clipboard](clipboard-usage.md) |
| `WinEventHook` | WinEvent callbacks (the hooks are installed on the window thread) | [Window management](window-management.md) |
| `RawInputMonitor`, `RawInputDeviceMonitor` | `WM_INPUT`, `WM_INPUT_DEVICE_CHANGE` | [Keyboard and mouse](input-handling.md) |
| `WindowsSessionListener` | `WM_WTSSESSION_CHANGE` | below |
| `EnvironmentMonitor` | `WM_SETTINGCHANGE` | below |
| `DisplayInfo` | `WM_DISPLAYCHANGE`, `WM_SETTINGCHANGE`, `WM_DPICHANGED` | [Window management](window-management.md#displays) |
| `PowerBroadcastListener` | `WM_POWERBROADCAST` | [Power and system state](system-state.md) |
| `ApplicationRestartManager.ListenForEndSession` | `WM_QUERYENDSESSION`, `WM_ENDSESSION` | [Restart Manager](restart-manager.md) |
| `DeviceNotification` | `WM_DEVICECHANGE` | [More packages](more-packages.md#devices) |

## Session changes: lock, unlock, logon, logoff

`WindowsSessionListener` reports when the workstation is locked or unlocked, and logon / logoff of the session. The
events are raised on the SharedMessageWindow thread. Early at logon the registration can fail because the Remote
Desktop Services are not running yet; the listener retries every 2 seconds for about 2 minutes and then raises
`RegistrationFailed`.

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

## Environment changes

`EnvironmentMonitor.EnvironmentUpdateEvents` reports `WM_SETTINGCHANGE`: changed system parameters, and named areas
such as `"ImmersiveColorSet"` (light / dark mode) or `"Environment"` (environment variables).

<!-- sample: MessagesSamples.EnvironmentChanges -->
```csharp
// WM_SETTINGCHANGE, e.g. when the user switches between light and dark mode ("ImmersiveColorSet")
var subscription = EnvironmentMonitor.EnvironmentUpdateEvents
    .Where(e => e.Area == "ImmersiveColorSet")
    .Subscribe(e => Console.WriteLine("The color theme changed"));
```

## Messages of your own windows

The SharedMessageWindow only sees its own messages. For the messages of your forms and WPF windows use the
integration packages. Both run on the UI thread, so `Handled` and `Result` can be set in `OnNext`.

Windows Forms (**Dapplo.Windows.Forms**): `WinProcFormsMessages()` subclasses the control, follows handle re-creation
and completes when the control is disposed.

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

WPF (**Dapplo.Windows.Wpf**): `WinProcMessages()` hooks the `HwndSource` of the window; subscribing before the window
is shown works, the hook is added when the source is created. Disposing the subscription never closes the window.

<!-- sample: MessagesSamples.WpfMessages -->
```csharp
// Works before the window is shown, the hook is added when the HwndSource is created
var subscription = window.WinProcMessages()
    .Where(m => m.Message == WindowsMessages.WM_DPICHANGED)
    .Subscribe(m => Console.WriteLine("DPI changed"));
```

## Names of the message types

- `WindowsMessages` is the enum of message identifiers (`WM_...`).
- `WindowMessage` is a message of the SharedMessageWindow (`Hwnd`, `Msg`, `WParam`, `LParam`, `Handled`, `Result`).
- `WindowMessageInfo` is a message of your own form or WPF window (`Handle`, `Message`, `WordParam`, `LongParam`,
  `Handled`, `Result`).
- `WindowsMessage` is a helper to register and name custom messages.

## See also

- [Getting started: how the library works](intro.md#how-the-library-works)
- [Keyboard and mouse](input-handling.md)
