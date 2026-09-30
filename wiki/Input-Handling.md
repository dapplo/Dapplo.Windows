# Input handling

Package **Dapplo.Windows.Input**. Full version: [Keyboard and mouse](https://www.dapplo.net/Dapplo.Windows/articles/input-handling.html).

## Keyboard hook

`KeyboardHook` and `MouseHook` are static and run on their own thread. Your `OnNext` runs on that thread while the
keyboard of the whole system waits, and Windows removes a hook that is too slow. So only decide `Handled` there, and
`ObserveOn` for the real work. Listeners that never set `Handled` should use `KeyboardEventsNonBlocking`.

<!-- sample: InputSamples.KeyboardHookBasics -->
```csharp
// The hook is installed with the first subscription and removed with the last
IDisposable subscription = KeyboardHook.KeyboardEvents
    .Where(args => args.IsKeyDown)
    .Subscribe(args => Console.WriteLine($"{args.Key} down, Ctrl: {args.IsControl}, Shift: {args.IsShift}"));

// ...
subscription.Dispose();
```

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

## Key combinations

`KeyCombinationHandler` fires when exactly the keys of the combination are down, and swallows them (set
`IsPassThrough = true` to let them through). `Control`, `Shift`, `Menu` (Alt) and `Win` match the left and right key.

<!-- sample: InputSamples.KeyCombination -->
```csharp
// Ctrl+Shift+S, Control and Shift match both the left and the right key.
// The handler marks the key events as handled, so other applications don't see the combination.
var subscription = KeyboardHook.KeyboardEvents
    .Where(new KeyCombinationHandler(VirtualKeyCode.Control, VirtualKeyCode.Shift, VirtualKeyCode.KeyS))
    .ObserveOn(SynchronizationContext.Current)
    .Subscribe(args => SaveAll());
```

A handler has state: use one instance per subscription, or the factory overload of `Where`:

<!-- sample: InputSamples.HandlerFactory -->
```csharp
// A handler keeps track of the pressed keys, so it may be used by one subscription only.
// When an observable is subscribed more than once, give Where a factory: every subscription gets its own handler.
IObservable<KeyboardHookEventArgs> saveHotkey = KeyboardHook.KeyboardEvents
    .Where(() => new KeyCombinationHandler(VirtualKeyCode.Control, VirtualKeyCode.KeyS));

var first = saveHotkey.Subscribe(_ => Console.WriteLine("Save (1)"));
var second = saveHotkey.Subscribe(_ => Console.WriteLine("Save (2)"));
```

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

`TriggerOnKeyUp = true` fires when the first key of the combination is released; the other keys can still be down, and
the keys are passed on to the active application. Wait for the modifiers to be released before you send input, see the
[documentation](https://www.dapplo.net/Dapplo.Windows/articles/input-handling.html#triggeronkeyup).

## Generating input

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

There is no method to type text; put the text on the clipboard and send Ctrl+V (see [[Common-Scenarios]]).

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

## Mouse hook

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

Raw input, idle time and suppressing mouse events: see the [documentation](https://www.dapplo.net/Dapplo.Windows/articles/input-handling.html).
