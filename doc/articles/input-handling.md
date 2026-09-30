# Keyboard and mouse

**Dapplo.Windows.Input** has global (low-level) keyboard and mouse hooks, handlers for key combinations and sequences,
input generation with `SendInput`, and raw input.

```powershell
dotnet add package Dapplo.Windows.Input
```

Namespaces used on this page: `Dapplo.Windows.Input`, `Dapplo.Windows.Input.Enums`, `Dapplo.Windows.Input.Keyboard`,
`Dapplo.Windows.Input.Mouse`, `Dapplo.Windows.Messages.Enumerations`, `Dapplo.Windows.Common.Structs`,
`System.Reactive.Linq`, `System.Reactive.Concurrency`.

## Keyboard hook

`KeyboardHook.KeyboardEvents` reports every key-down and key-up of the system. The hook is installed with the first
subscription and removed when the last one is disposed.

<!-- sample: InputSamples.KeyboardHookBasics -->
```csharp
// The hook is installed with the first subscription and removed with the last
IDisposable subscription = KeyboardHook.KeyboardEvents
    .Where(args => args.IsKeyDown)
    .Subscribe(args => Console.WriteLine($"{args.Key} down, Ctrl: {args.IsControl}, Shift: {args.IsShift}"));

// ...
subscription.Dispose();
```

`KeyboardHookEventArgs` tells you the `Key` (`VirtualKeyCode`), `IsKeyDown`, the modifier state (`IsControl`,
`IsShift`, `IsAlt`, `IsWindows` and the left / right variants), the lock keys (`IsCapsLockActive`, ...), `ScanCode`,
`IsExtended` and whether the event was injected by a program (`IsInjectedByProcess`).

Text which is sent as Unicode characters (by `KeyboardInputGenerator.TypeText`, an IME, an on-screen keyboard or a
remote desktop client) arrives as `VirtualKeyCode.Packet` (VK_PACKET) events: `IsPacket` is true and
`PacketCharacter` is the UTF-16 code unit. A character outside the Basic Multilingual Plane, like most emoji, arrives
as two packets (the surrogates). The key combination and sequence handlers ignore packets.

### The hook thread

`KeyboardHook` and `MouseHook` run on their own background thread with a message loop. It doesn't matter which thread
subscribes, and your `OnNext` is called on the hook thread.

While your `OnNext` runs, **the keyboard of the whole system waits**. When a low-level hook takes too long (the
`LowLevelHooksTimeout`, at most about a second), Windows removes it silently and no events arrive anymore. So:

- In `KeyboardEvents` only decide `Handled`, quickly. Then use `ObserveOn` for the real work:

<!-- sample: InputSamples.ObserveOnUi -->
```csharp
// Decide Handled on the hook thread (quick!), then do the real work on the UI thread.
// Call this on the UI thread, so SynchronizationContext.Current is the one of the UI.
var subscription = KeyboardHook.KeyboardEvents
    .Where(args => args.Key == VirtualKeyCode.PrintScreen)
    // Swallow the key-down and the key-up, so Windows doesn't take its own screenshot
    .Do(args => args.Handled = true)
    .Where(args => args.IsKeyDown)
    .ObserveOn(SynchronizationContext.Current)
    .Subscribe(args => TakeScreenshot());
```

- If you never set `Handled`, use `KeyboardEventsNonBlocking`. The events are delivered in order on a separate thread,
  so a slow subscriber never delays the keyboard:

<!-- sample: InputSamples.NonBlocking -->
```csharp
// For listeners which never set Handled: the events are delivered in order on another thread,
// so a slow subscriber never delays the keyboard of the whole system
var subscription = KeyboardHook.KeyboardEventsNonBlocking
    .Where(args => args.IsKeyDown)
    .Subscribe(args => LogKey(args.Key));
```

- An exception in a subscriber never reaches the hook. It ends that subscription and is published on
  `SubscriberErrors`:

<!-- sample: InputSamples.SubscriberErrors -->
```csharp
// An exception in a subscriber never reaches the hook, it is published here and ends only that subscription
var errors = KeyboardHook.SubscriberErrors
    .Subscribe(ex => Console.Error.WriteLine($"Keyboard subscriber failed: {ex}"));
```

If the hook can't be installed, the subscriber gets `OnError` with a `Win32Exception`.

### Swallowing keys

Set `Handled = true` in `OnNext` (on the hook thread, before any `ObserveOn`) and other applications don't see the key.
Swallow the key-down and the key-up, otherwise other applications see half a key press.

<!-- sample: InputSamples.SuppressKey -->
```csharp
// Swallow the Windows keys, e.g. while a game or a kiosk application is active.
// Swallow both the key-down and the key-up, otherwise other applications see half a key press.
var subscription = KeyboardHook.KeyboardEvents
    .Subscribe(args =>
    {
        if (args.Key == VirtualKeyCode.LeftWin || args.Key == VirtualKeyCode.RightWin)
        {
            args.Handled = true;
        }
    });
```

## Key combinations

A `KeyCombinationHandler` keeps track of the pressed keys and reports when exactly the keys of the combination are
down, nothing else. `Control`, `Shift`, `Menu` (Alt) and `Win` match both the left and the right key. Use the
`Where(handler)` extension on the keyboard observable:

<!-- sample: InputSamples.KeyCombination -->
```csharp
// Ctrl+Shift+S, Control and Shift match both the left and the right key.
// The handler marks the key events as handled, so other applications don't see the combination.
var subscription = KeyboardHook.KeyboardEvents
    .Where(new KeyCombinationHandler(VirtualKeyCode.Control, VirtualKeyCode.Shift, VirtualKeyCode.KeyS))
    .ObserveOn(SynchronizationContext.Current)
    .Subscribe(args => SaveAll());
```

By default the handler marks the key events of the combination as handled, so the active application doesn't get
them. The options:

<!-- sample: InputSamples.KeyCombinationOptions -->
```csharp
var handler = new KeyCombinationHandler(VirtualKeyCode.Win, VirtualKeyCode.Shift, VirtualKeyCode.KeyA)
{
    // Let other applications see the keys too (default false: they are swallowed)
    IsPassThrough = true,
    // Fire again while the keys are held and auto-repeat (default false)
    CanRepeat = false,
    // Also react to keys sent with SendInput by other programs (default true: ignore them)
    IgnoreInjected = true
};
var subscription = KeyboardHook.KeyboardEvents.Where(handler).Subscribe(_ => Console.WriteLine("Win+Shift+A"));
```

| Property | Default | Meaning |
|---|---|---|
| `IsPassThrough` | `false` | `true` lets other applications see the keys too |
| `CanRepeat` | `false` | `true` fires again for auto-repeat while the keys are held |
| `IgnoreInjected` | `true` | `false` also reacts to keys sent by programs (`SendInput`) |
| `TriggerMode` | `KeyDown` | `FirstKeyUp` or `AllKeysUp` fire on the release of the combination, see [Trigger mode](#trigger-mode) |
| `KeyStateVerifier` | physical key state | checks on every key-down that the keys it considers pressed are still down, so a missed key-up (Win+L, UAC, Ctrl+Alt+Del) doesn't block the combination |

Combinations can be parsed from text, for example from a settings file:

<!-- sample: InputSamples.KeyCombinationFromString -->
```csharp
// "ctrl", "alt", "shift" and "win" match the left and the right key, single characters become KeyA .. KeyZ, Key0 .. Key9
var keys = KeyHelper.VirtualKeyCodesFromString("Ctrl + Alt + P").ToArray();
var subscription = KeyboardHook.KeyboardEvents
    .Where(new KeyCombinationHandler(keys))
    .Subscribe(_ => Console.WriteLine("Ctrl+Alt+P"));

// A readable, localized name for a key, e.g. for a settings dialog
string text = KeyHelper.VirtualCodeToLocaleDisplayText(VirtualKeyCode.PrintScreen);
```

### One handler per subscription

A handler has state (which keys are down), so one instance may only be used by one active subscription. Using it in a
second subscription gives that subscription an `InvalidOperationException`. When you subscribe to the same observable
more than once, pass a factory, then every subscription gets its own handler:

<!-- sample: InputSamples.HandlerFactory -->
```csharp
// A handler keeps track of the pressed keys, so it may be used by one subscription only.
// When an observable is subscribed more than once, give Where a factory: every subscription gets its own handler.
IObservable<KeyboardHookEventArgs> saveHotkey = KeyboardHook.KeyboardEvents
    .Where(() => new KeyCombinationHandler(VirtualKeyCode.Control, VirtualKeyCode.KeyS));

var first = saveHotkey.Subscribe(_ => Console.WriteLine("Save (1)"));
var second = saveHotkey.Subscribe(_ => Console.WriteLine("Save (2)"));
```

### Either of several combinations, or a sequence

<!-- sample: InputSamples.KeyOrCombination -->
```csharp
// PrintScreen, or Ctrl+Shift+4
var handler = new KeyOrCombinationHandler(
    new KeyCombinationHandler(VirtualKeyCode.PrintScreen),
    new KeyCombinationHandler(VirtualKeyCode.Control, VirtualKeyCode.Shift, VirtualKeyCode.Key4));

var subscription = KeyboardHook.KeyboardEvents.Where(handler).Subscribe(_ => Console.WriteLine("Capture"));
```

A `KeySequenceHandler` fires when the combinations are pressed one after the other, each within `Timeout` (default 1
second) of the previous one:

<!-- sample: InputSamples.KeySequence -->
```csharp
// Ctrl+K followed by Ctrl+C (like Visual Studio), at most 2 seconds apart
var handler = new KeySequenceHandler(
    new KeyCombinationHandler(VirtualKeyCode.Control, VirtualKeyCode.KeyK),
    new KeyCombinationHandler(VirtualKeyCode.Control, VirtualKeyCode.KeyC))
{
    Timeout = TimeSpan.FromSeconds(2)
};

var subscription = KeyboardHook.KeyboardEvents.Where(handler).Subscribe(_ => Console.WriteLine("Comment selection"));
```

### Trigger mode

`TriggerMode` sets when the handler fires:

| `TriggerMode` | Fires | Key events |
|---|---|---|
| `KeyDown` (default) | on the key-down which completes the combination | swallowed, unless `IsPassThrough` |
| `FirstKeyUp` | when the first key of the combination is released; the other keys can still be down | passed on |
| `AllKeysUp` | once, when the last key of the combination is released | passed on |

In both key-up modes the key events are never marked as handled: the key-downs already reached the active
application, and swallowing only the key-up would leave a stuck key. `IsPassThrough` has no effect. Choose a
combination which doesn't mean anything in the applications of your users.

A key-up mode only fires when all keys of the combination were down together, without another key. With `AllKeysUp`,
another key pressed while a key of the combination is still down (Ctrl+A, then C while Ctrl is held) cancels the
trigger; pressing the complete combination again arms it again (auto-repeat doesn't count).

`AllKeysUp` is the mode for hotkeys which send input: when it fires, the user doesn't hold any key of the combination
anymore, so your input isn't combined with it.

<!-- sample: InputSamples.AllKeysUp -->
```csharp
// Ctrl+Alt+D types the date into the active application, when the user released all keys of the combination
var handler = new KeyCombinationHandler(VirtualKeyCode.Control, VirtualKeyCode.Menu, VirtualKeyCode.KeyD)
{
    TriggerMode = TriggerMode.AllKeysUp
};

var subscription = KeyboardHook.KeyboardEvents
    .Where(handler)
    // Leave the hook thread, sending input from there would block the keyboard
    .ObserveOn(TaskPoolScheduler.Default)
    // No key of the combination is down anymore, so the text isn't combined with Ctrl or Alt
    .Subscribe(_ => KeyboardInputGenerator.TypeText(DateTime.Now.ToString("yyyy-MM-dd")));
```

With `FirstKeyUp` the other keys can still be down when the handler fires. If you send input in reaction, wait until
the user released them, otherwise your input is combined with them (Ctrl+Shift+Home instead of Home):

<!-- sample: InputSamples.WaitForRelease -->
```csharp
private static void WaitUntilReleased(params VirtualKeyCode[] keys)
{
    // The asynchronous (current, system wide) key state, Control and Shift match the left and the right key
    while (KeyboardState.IsAnyDown(keys))
    {
        Thread.Sleep(20);
    }
}
```

<!-- sample: InputSamples.FirstKeyUp -->
```csharp
var handler = new KeyCombinationHandler(VirtualKeyCode.Control, VirtualKeyCode.Shift, VirtualKeyCode.KeyH)
{
    TriggerMode = TriggerMode.FirstKeyUp
};

var subscription = KeyboardHook.KeyboardEvents
    .Where(handler)
    // Leave the hook thread, waiting there would block the keyboard
    .ObserveOn(TaskPoolScheduler.Default)
    .Subscribe(_ =>
    {
        // The other keys of the combination can still be down, wait for them before sending input
        WaitUntilReleased(VirtualKeyCode.Control, VirtualKeyCode.Shift);
        KeyboardInputGenerator.KeyPresses(VirtualKeyCode.Home);
    });
```

## Key state

`KeyboardState` reads the state of a key. `Shift`, `Control`, `Menu` and `Win` check the left and the right key.
Windows keeps two states:

- **Asynchronous** (`IsDown`, `IsAnyDown`, GetAsyncKeyState): the state right now, system wide, including injected
  input. Use it in background threads and hooks, e.g. to wait until the user released some keys. In a low-level hook
  the key of the current event isn't updated yet (use `IsKeyDown` of the event), and while another desktop (UAC,
  Win+L) is active all keys read as up.
- **Per thread** (`IsDownForCurrentThread`, `IsToggled`, GetKeyState): the state at the time of the keyboard message
  which the calling thread is processing. Use it in a key event handler of your UI. It lags behind when the thread
  doesn't process keyboard messages. The toggle state (CapsLock, NumLock, ScrollLock) only exists here.

<!-- sample: InputSamples.KeyStateQuery -->
```csharp
// Right now, system wide (GetAsyncKeyState): use this in background threads and hooks
bool shiftDown = KeyboardState.IsDown(VirtualKeyCode.Shift);
bool anyModifier = KeyboardState.IsAnyDown(VirtualKeyCode.Shift, VirtualKeyCode.Control, VirtualKeyCode.Menu, VirtualKeyCode.Win);

// At the time of the keyboard message the UI thread is processing (GetKeyState): use this in a key event handler
bool ctrlWithThisKey = KeyboardState.IsDownForCurrentThread(VirtualKeyCode.Control);

// Toggle keys
bool capsLock = KeyboardState.IsToggled(VirtualKeyCode.Capital);
```

## Generating keyboard input

`KeyboardInputGenerator` sends keys with `SendInput`. Arrow, navigation, right Ctrl / Alt, Windows and media keys are
sent as extended keys. `VirtualKeyCode.Win` is sent as the left Windows key.

<!-- sample: InputSamples.GenerateKeys -->
```csharp
// Press and release keys one after the other: types "hi" (with the current keyboard layout)
KeyboardInputGenerator.KeyPresses(VirtualKeyCode.KeyH, VirtualKeyCode.KeyI);

// Ctrl+C: all keys down, then all keys up in reverse order
KeyboardInputGenerator.KeyCombinationPress(VirtualKeyCode.Control, VirtualKeyCode.KeyC);

// Hold Shift while pressing some keys
KeyboardInputGenerator.KeyDown(VirtualKeyCode.Shift);
KeyboardInputGenerator.KeyPresses(VirtualKeyCode.Right, VirtualKeyCode.Right);
KeyboardInputGenerator.KeyUp(VirtualKeyCode.Shift);

// Win+D shows the desktop, Win is sent as the left Windows key
KeyboardInputGenerator.KeyCombinationPress(VirtualKeyCode.Win, VirtualKeyCode.KeyD);
```

### Typing text

Virtual keys depend on the keyboard layout (`KeyPresses(VirtualKeyCode.KeyY)` types a "z" with a German layout).
`TypeText` sends the text as Unicode characters instead (`SendInput` with `KEYEVENTF_UNICODE`), so every character
arrives as it is, including accents and emoji:

<!-- sample: InputSamples.TypeText -->
```csharp
// Types the text as Unicode characters, independent of the keyboard layout.
// Line breaks become Enter, \t becomes Tab, other control characters are skipped.
var text = "Grüße aus Köln 👋\r\nPrice:\t42 €";
uint inserted = KeyboardInputGenerator.TypeText(text);

// Two events (down and up) per UTF-16 code unit, Enter and Tab, fewer means the input was blocked
if (inserted < KeyboardInput.ForText(text).Length)
{
    Console.Error.WriteLine("The input was blocked, e.g. by an elevated application");
}
```

- Every UTF-16 code unit is a key-down and a key-up; a surrogate pair is sent as two code units, the high one first.
- `"\r\n"`, `"\n"` and `"\r"` are sent as the Enter key, `"\t"` as the Tab key. All other control characters are
  skipped, applications treat them as commands (Backspace, Escape, Ctrl+C in a console), not as text.
- The text is sent in batches; the result is the number of inserted events. When the input is blocked (an elevated
  foreground application, the secure desktop), the rest of the text isn't sent and the result is lower than
  `KeyboardInput.ForText(text).Length`.
- Keys the user holds are combined with the input: send text from a hotkey with `TriggerMode.AllKeysUp`, or wait until
  the keys are released.
- Low-level hooks see the characters as `VirtualKeyCode.Packet`. Some applications ignore such input: games, remote
  desktop and virtual machine windows, and applications which read keys with raw input or GetAsyncKeyState. For those
  use `KeyPresses` or paste via the clipboard.

Input goes to the foreground window. Windows blocks input into windows of processes with a higher integrity level
(e.g. an elevated application) unless your process is elevated too.

## Mouse hook

`MouseHook.MouseEvents` works like the keyboard hook, with the same threading rules, `MouseEventsNonBlocking` and
`SubscriberErrors`. `WindowsMessage` says what happened (`WM_MOUSEMOVE`, `WM_LBUTTONDOWN`, `WM_MOUSEWHEEL`, ...),
`Point` is the position in screen coordinates.

<!-- sample: InputSamples.MouseHookBasics -->
```csharp
// WindowsMessage tells what happened: WM_MOUSEMOVE, WM_LBUTTONDOWN, WM_MOUSEWHEEL, ...
var clicks = MouseHook.MouseEvents
    .Where(args => args.WindowsMessage == WindowsMessages.WM_LBUTTONDOWN)
    .Subscribe(args => Console.WriteLine($"Left click at {args.Point}"));

// Wheel events have a WheelDelta, 120 is one notch (positive is away from the user)
var wheel = MouseHook.MouseEventsNonBlocking
    .Where(args => args.WindowsMessage == WindowsMessages.WM_MOUSEWHEEL)
    .Subscribe(args => Console.WriteLine($"Wheel {args.WheelDelta / 120} notches"));

// Mouse moves are frequent: sample them, and never do slow work in the hook
var moves = MouseHook.MouseEventsNonBlocking
    .Where(args => args.WindowsMessage == WindowsMessages.WM_MOUSEMOVE)
    .Sample(TimeSpan.FromMilliseconds(100))
    .Subscribe(args => Console.WriteLine($"Mouse at {args.Point}"));
```

<!-- sample: InputSamples.SuppressMouse -->
```csharp
// Swallow right clicks (down and up), e.g. while your application shows its own context menu
var subscription = MouseHook.MouseEvents
    .Where(args => args.WindowsMessage == WindowsMessages.WM_RBUTTONDOWN || args.WindowsMessage == WindowsMessages.WM_RBUTTONUP)
    .Subscribe(args => args.Handled = true);
```

## Generating mouse input

<!-- sample: InputSamples.GenerateMouse -->
```csharp
// Coordinates are screen pixels, also on monitors left of or above the primary one
MouseInputGenerator.MoveMouse(new NativePoint(100, 200));

// Click at the current position, or at a location
MouseInputGenerator.MouseClick(MouseButtons.Left);
MouseInputGenerator.MouseClick(MouseButtons.Right, new NativePoint(300, 400));

// Drag: press, move, release
MouseInputGenerator.MouseDown(MouseButtons.Left, new NativePoint(100, 100));
MouseInputGenerator.MoveMouse(new NativePoint(400, 100));
MouseInputGenerator.MouseUp(MouseButtons.Left, new NativePoint(400, 100));

// Scroll one notch down
MouseInputGenerator.MoveMouseWheel(-120);
```

## Idle time

<!-- sample: InputSamples.Idle -->
```csharp
// How long ago did the user touch the keyboard or mouse?
TimeSpan idle = NativeInput.LastInputTimeSpan;
if (idle > TimeSpan.FromMinutes(5))
{
    Console.WriteLine("The user is away");
}
```

## Raw input

Raw input tells you which device (which keyboard, which mouse) the input came from, and it also works while your
application is in the background. `RawInputMonitor.Listen` registers the device types on the SharedMessageWindow when
you subscribe, and unregisters them when you dispose; several monitors can be active at the same time. The events
arrive on the SharedMessageWindow thread. HID reports of other devices are in `RawInputEventArgs.HidData`.

<!-- sample: InputSamples.RawInput -->
```csharp
// Raw input tells which device the input came from, and works while the application is in the background.
// The events arrive on the SharedMessageWindow thread.
var subscription = RawInputMonitor.Listen(RawInputDevices.Keyboard)
    .Subscribe(args =>
    {
        var keyboard = args.RawInput.Device.Keyboard;
        Console.WriteLine($"Device {args.RawInput.Header.DeviceHandle}: {keyboard.VirtualKey} ({keyboard.Flags})");
    });
```

<!-- sample: InputSamples.RawInputDevices -->
```csharp
// All currently connected keyboards and mice
foreach (var device in RawInputApi.GetAllDevices())
{
    Console.WriteLine($"{device.DisplayName}: {device.DeviceName}");
}

// Get told when a keyboard is connected or removed
var subscription = RawInputDeviceMonitor.Listen(RawInputDevices.Keyboard)
    .Subscribe(change => Console.WriteLine($"{(change.Added ? "Added" : "Removed")}: {change.DeviceInformation.DisplayName}"));
```

## Tips

- Dispose hook subscriptions when you don't need them anymore, every hook costs every key press of the system a little
  time.
- Never block in `KeyboardEvents` / `MouseEvents`: no I/O, no UI, no locks, no `Thread.Sleep`.
- Security software may treat global hooks and injected input as suspicious; tell your users why your application
  needs them.

## See also

- [Window messages and the SharedMessageWindow](window-messages.md) for `RegisterHotKey`
- [Common scenarios](common-scenarios.md)
