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
| `TriggerOnKeyUp` | `false` | `true` fires on key-up instead of key-down, see below |
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

### TriggerOnKeyUp

With `TriggerOnKeyUp = true` the handler fires when the **first** key of the combination is released, if all keys were
down together and no other key was pressed. At that moment the other keys of the combination can still be held down.
In this mode the key events are never marked as handled: the key-downs already reached the active application, and
swallowing only the key-up would leave a stuck key. `IsPassThrough` has no effect.

If you send input in reaction to the combination, wait until the user released the modifier keys, otherwise your input
is combined with them (Ctrl+Shift+Home instead of Home):

<!-- sample: InputSamples.WaitForReleasePInvoke -->
```csharp
[DllImport("user32")]
private static extern short GetAsyncKeyState(VirtualKeyCode key);

private static void WaitUntilReleased(params VirtualKeyCode[] keys)
{
    // The high bit is set while the key is down
    while (keys.Any(key => (GetAsyncKeyState(key) & 0x8000) != 0))
    {
        Thread.Sleep(20);
    }
}
```

<!-- sample: InputSamples.TriggerOnKeyUp -->
```csharp
var handler = new KeyCombinationHandler(VirtualKeyCode.Control, VirtualKeyCode.Shift, VirtualKeyCode.KeyD)
{
    TriggerOnKeyUp = true
};

var subscription = KeyboardHook.KeyboardEvents
    .Where(handler)
    // Leave the hook thread, waiting there would block the keyboard
    .ObserveOn(TaskPoolScheduler.Default)
    .Subscribe(_ =>
    {
        // The other keys of the combination can still be down, wait for them before sending input
        WaitUntilReleased(VirtualKeyCode.LeftControl, VirtualKeyCode.RightControl, VirtualKeyCode.LeftShift, VirtualKeyCode.RightShift);
        KeyboardInputGenerator.KeyPresses(VirtualKeyCode.Home);
    });
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

There is no method to type text: virtual keys depend on the keyboard layout. To insert text, put it on the clipboard
and send Ctrl+V (see [Common scenarios](common-scenarios.md#insert-text-with-a-hotkey)).

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
