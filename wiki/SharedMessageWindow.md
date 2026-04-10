# SharedMessageWindow

`SharedMessageWindow` is one of the most important low-level building blocks in the Dapplo.Windows library. Almost every feature that requires receiving Windows messages — clipboard monitoring, raw keyboard/mouse input, device change notifications, session events, and more — is powered by it.

**Package:** `Dapplo.Windows.Messages`

```powershell
Install-Package Dapplo.Windows.Messages
```

## What Is It?

Many Windows APIs deliver asynchronous notifications by posting a message to a **window handle (HWND)**. To receive those messages from non-UI (background/console) applications, you need a dedicated message-processing window and a thread that runs its message loop.

`SharedMessageWindow` manages all of that transparently:

1. It creates a hidden, invisible window on a **dedicated STA background thread**.
2. It exposes every received message as an **`IObservable<WindowMessage>`** stream (`Messages` property).
3. It uses `Publish().RefCount()` to **share the window and its thread across all subscribers** — the window is created on the first subscription and destroyed automatically when the last subscriber disposes.
4. The `Listen()` overload lets consumers register and unregister Windows APIs that require an HWND, tied to the exact lifetime of the window.

## Key API Surface

```csharp
public static class SharedMessageWindow
{
    // The HWND of the currently active message window (0 if not yet started).
    public static nint Handle { get; }

    // Shared observable of all messages received by the window.
    // The window exists only while there is at least one subscriber.
    public static IObservable<WindowMessage> Messages { get; }

    // Like Messages, but also invokes callbacks when the window is created (onSetup)
    // or destroyed / subscription disposed (onTeardown).
    // Use this when you need to call a Windows API that requires an HWND.
    public static IObservable<WindowMessage> Listen(
        Action<nint> onSetup   = null,
        Action<nint> onTeardown = null);
}
```

`WindowMessage` exposes:

| Property | Type | Description |
|----------|------|-------------|
| `Hwnd` | `nint` | Window handle that received the message |
| `Msg` | `WindowsMessages` | The Windows message identifier (e.g. `WM_CLIPBOARDUPDATE`) |
| `WParam` | `nint` | First message parameter |
| `LParam` | `nint` | Second message parameter |
| `Handled` | `bool` | Set to `true` to prevent `DefWindowProc` from processing the message |
| `Result` | `nuint` | The return value forwarded to the OS when `Handled == true` |

## The `Listen()` Pattern

Many Windows notification APIs (clipboard, device changes, session events, raw input) follow the same pattern:

1. Call a **register** function with your HWND when the window is ready.
2. Process incoming messages.
3. Call an **unregister** function with the same HWND when you are done.

`Listen(onSetup, onTeardown)` automates steps 1 and 3:

```csharp
SharedMessageWindow.Listen(
    onSetup:    hwnd => RegisterSomeApi(hwnd),   // called when HWND is ready
    onTeardown: hwnd => UnregisterSomeApi(hwnd)) // called on dispose or shutdown
.Where(m => m.Msg == WindowsMessages.WM_SOME_MESSAGE)
.Subscribe(m => HandleMessage(m));
```

## Who Uses It?

Every major Dapplo.Windows feature that receives system notifications is built on `SharedMessageWindow`.

### Clipboard — `WM_CLIPBOARDUPDATE`

`ClipboardNative.OnUpdate` registers the message window as a clipboard format listener and filters `WM_CLIPBOARDUPDATE` messages:

```csharp
// Inside Dapplo.Windows.Clipboard
SharedMessageWindow.Listen(
    onSetup:    hwnd => NativeMethods.AddClipboardFormatListener(hwnd),
    onTeardown: hwnd => NativeMethods.RemoveClipboardFormatListener(hwnd))
.Where(m => m.Msg == WindowsMessages.WM_CLIPBOARDUPDATE)
.Subscribe(m => NotifyClipboardChanged(m));
```

**User-facing API:** [[Clipboard]]

### Raw Input — `WM_INPUT`

`RawInputMonitor.Listen()` registers devices for raw input (e.g., high-frequency mouse/keyboard hardware data) and filters `WM_INPUT`:

```csharp
// Inside Dapplo.Windows.Input
RawInputApi.RegisterRawInput(
    SharedMessageWindow.Handle,
    RawInputDeviceFlags.InputSink | RawInputDeviceFlags.DeviceNotify,
    devices);

SharedMessageWindow.Messages
    .Where(m => m.Msg == WindowsMessages.WM_INPUT)
    .Subscribe(m =>
    {
        m.Handled = true;
        var rawInput = RawInputApi.GetRawInputData(m.LParam);
        observer.OnNext(rawInput);
    });
```

**User-facing API:** [[Input-Handling]]

### Device Change Notifications — `WM_DEVICECHANGE`

`DeviceNotification.OnNotification` registers the window with `RegisterDeviceNotification` and filters `WM_DEVICECHANGE`:

```csharp
// Inside Dapplo.Windows.Devices
SharedMessageWindow.Listen(
    onSetup:    hwnd => RegisterDeviceNotification(hwnd, filter, flags),
    onTeardown: hwnd => UnregisterDeviceNotification(handle))
.Where(m => m.Msg == WindowsMessages.WM_DEVICECHANGE && m.LParam != 0)
.Subscribe(m => observer.OnNext(new DeviceNotificationEvent(m.WParam, m.LParam)));
```

### Session Changes — `WM_WTSSESSION_CHANGE`

`WindowsSessionListener` registers for Terminal Services session events (lock/unlock, logon/logoff):

```csharp
// Inside Dapplo.Windows.Messages
SharedMessageWindow.Listen(
    onSetup:    hwnd => WTSRegisterSessionNotification(hwnd, NOTIFY_FOR_THIS_SESSION),
    onTeardown: hwnd => WTSUnRegisterSessionNotification(hwnd))
.Where(m => m.Msg == WindowsMessages.WM_WTSSESSION_CHANGE)
.Subscribe(m => DispatchSessionEvent(m));
```

### Application End Session — `WM_QUERYENDSESSION` / `WM_ENDSESSION`

`ApplicationRestartManager.ListenForEndSession()` intercepts system shutdown and Restart Manager close requests:

```csharp
// Inside Dapplo.Windows.AppRestartManager
SharedMessageWindow.Messages
    .Where(m => m.Msg.IsIn(WindowsMessages.WM_QUERYENDSESSION, WindowsMessages.WM_ENDSESSION))
    .Subscribe(m =>
    {
        var reason = (EndSessionReasons)m.LParam;
        // optionally set m.Result and m.Handled to control the OS response
        observer.OnNext(new EndSessionMessage(m.Msg, reason));
    });
```

**User-facing API:** [[Restart-Manager]]

### Power State Changes — `WM_POWERBROADCAST`

`PowerBroadcastListener` in `Dapplo.Windows.SystemState` exposes power state changes (suspend, resume, battery status) as `IObservable<PowerBroadcastEvent>` streams:

```csharp
// Inside Dapplo.Windows.SystemState
SharedMessageWindow.Messages
    .Where(m => m.Msg == WindowsMessages.WM_POWERBROADCAST)
    .Select(m => (PowerBroadcastEvent)(uint)m.WParam)
    .Publish().RefCount();
```

**User-facing API:** [[System-State]]



`EnvironmentMonitor.EnvironmentUpdateEvents` detects system-wide setting changes (theme, fonts, locale, etc.):

```csharp
// Inside Dapplo.Windows
SharedMessageWindow.Messages
    .Where(m => m.Msg == WindowsMessages.WM_SETTINGCHANGE)
    .Select(m => EnvironmentChangedEventArgs.Create(
        (SystemParametersInfoActions)(int)m.WParam,
        Marshal.PtrToStringAuto((IntPtr)m.LParam)))
    .Publish().RefCount();
```

### Display Changes — `WM_DISPLAYCHANGE`

`DisplayInfo` detects monitor configuration changes (resolution, DPI, connection/disconnection):

```csharp
// Inside Dapplo.Windows.User32
SharedMessageWindow.Listen()
    .Where(m => m.Msg == WindowsMessages.WM_DISPLAYCHANGE)
    .Subscribe(m => RefreshDisplayInfo());
```

## Using `SharedMessageWindow` Directly

If you need to react to a Windows message not already wrapped by a Dapplo package, you can subscribe to `Messages` or `Listen()` yourself:

### Simple Message Filter

```csharp
using Dapplo.Windows.Messages;
using Dapplo.Windows.Messages.Enumerations;
using System.Reactive.Linq;

// React to WM_POWERBROADCAST (power state changes)
var sub = SharedMessageWindow.Messages
    .Where(m => m.Msg == WindowsMessages.WM_POWERBROADCAST)
    .Subscribe(m =>
    {
        Console.WriteLine($"Power event: wParam={m.WParam}");
    });

Console.ReadLine();
sub.Dispose(); // disposes the window if this was the last subscriber
```

### Registration-Required APIs

Use `Listen()` when you need to pass the HWND to a Windows API:

```csharp
using Dapplo.Windows.Messages;
using System.Runtime.InteropServices;

// Hypothetical: register for some custom notification
[DllImport("someapi.dll")] static extern void RegisterForNotification(nint hwnd);
[DllImport("someapi.dll")] static extern void UnregisterNotification(nint hwnd);

var sub = SharedMessageWindow.Listen(
        onSetup:    hwnd => RegisterForNotification(hwnd),
        onTeardown: hwnd => UnregisterNotification(hwnd))
    .Where(m => m.Msg == (WindowsMessages)0xC001) // custom message
    .Subscribe(m => Console.WriteLine("Custom notification received"));
```

### Responding to a Message

Set `Handled = true` and assign `Result` to control what the OS sees as the return value:

```csharp
SharedMessageWindow.Messages
    .Where(m => m.Msg == WindowsMessages.WM_QUERYENDSESSION)
    .Subscribe(m =>
    {
        bool canShutdown = AskUser();
        m.Result  = canShutdown ? 1u : 0u;
        m.Handled = true;  // prevents DefWindowProc from being called
    });
```

## Design Notes

| Detail | Explanation |
|--------|-------------|
| **STA thread** | The window runs on a Single-Threaded Apartment thread, as required by COM and many Windows APIs |
| **Real HWND, not HWND_MESSAGE** | Uses a real but invisible window (`WS_POPUP + WS_EX_TOOLWINDOW`) rather than `HWND_MESSAGE`, which ensures compatibility with broadcast messages and some notification APIs that don't work with message-only windows |
| **Hidden from Alt+Tab** | `WS_EX_TOOLWINDOW` hides the window from the taskbar and Alt+Tab switcher |
| **Shared / ref-counted** | `Publish().RefCount()` means all consumers share one thread and one window; the window is only alive while something is subscribed |
| **Thread-safe handle access** | `Handle` is a `BehaviorSubject<nint>` under the hood; `Listen()` handles the race between window creation and caller setup |

## See Also

- [[Clipboard]] — built on `WM_CLIPBOARDUPDATE`
- [[Input-Handling]] — raw input via `WM_INPUT`
- [[Restart-Manager]] — session/shutdown via `WM_QUERYENDSESSION` / `WM_ENDSESSION`
- [[System-State]] — power events via `WM_POWERBROADCAST`
- [[Getting-Started]]
