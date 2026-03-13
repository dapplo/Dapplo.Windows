# Input Handling

The `Dapplo.Windows.Input` package provides system-wide keyboard and mouse hooks built on [Reactive Extensions (Rx.NET)](https://github.com/dotnet/reactive), plus utilities for programmatically generating input events.

## Installation

```powershell
Install-Package Dapplo.Windows.Input
```

## Keyboard Hooks

### Create a Keyboard Hook

```csharp
using Dapplo.Windows.Input.Keyboard;

using var keyboardHook = KeyboardHook.Create();

var subscription = keyboardHook.KeyboardEvents.Subscribe(e =>
{
    Console.WriteLine($"Key: {e.Key}  Down: {e.IsDown}");
});

Console.ReadLine(); // keep alive
```

### Filter Events

```csharp
using System.Reactive.Linq;

// Key presses only
keyboardHook.KeyboardEvents
    .Where(e => e.IsDown)
    .Subscribe(e => Console.WriteLine($"Pressed: {e.Key}"));

// A specific key
keyboardHook.KeyboardEvents
    .Where(e => e.Key == VirtualKeyCode.Escape && e.IsDown)
    .Subscribe(_ => Console.WriteLine("Escape!"));
```

### Detect Key Combinations

Check modifier key state directly on the event:

```csharp
// Ctrl+C
keyboardHook.KeyboardEvents
    .Where(e => e.IsDown && e.Key == VirtualKeyCode.C && e.IsControlPressed)
    .Subscribe(_ => Console.WriteLine("Ctrl+C"));

// Ctrl+Shift+A
keyboardHook.KeyboardEvents
    .Where(e => e.IsDown && e.Key == VirtualKeyCode.A && e.IsControlPressed && e.IsShiftPressed)
    .Subscribe(_ => Console.WriteLine("Ctrl+Shift+A"));
```

### `KeyCombinationHandler` — Structured Combinations

`KeyCombinationHandler` makes complex hotkey detection clean and explicit:

```csharp
var handler = new KeyCombinationHandler(
    VirtualKeyCode.Control,
    VirtualKeyCode.Menu,   // Alt
    VirtualKeyCode.T);

keyboardHook.KeyboardEvents
    .Where(handler)
    .Subscribe(e =>
    {
        Console.WriteLine("Ctrl+Alt+T");
        e.Handled = true;  // prevent other apps from seeing it
    });
```

### `TriggerOnKeyUp` — Inject Input After Release

When you want to inject keystrokes as a result of a hotkey, use `TriggerOnKeyUp = true`. This fires *after* the modifier keys are released so they do not contaminate the injected input.

```csharp
var handler = new KeyCombinationHandler(
    VirtualKeyCode.Control,
    VirtualKeyCode.Menu,
    VirtualKeyCode.LeftWin,
    VirtualKeyCode.T)
{
    TriggerOnKeyUp = true
};

keyboardHook.KeyboardEvents
    .Where(handler)
    .Subscribe(e =>
    {
        KeyboardInputGenerator.TypeText(DateTime.Now.ToString("yyyy-MM-dd--HH-mm-ss"));
        e.Handled = true;
    });
```

This is useful for text-expansion / AutoHotKey-style scripts.

### Detect Key Sequences

```csharp
using System.Collections.Generic;

var sequence = new List<VirtualKeyCode>();

keyboardHook.KeyboardEvents
    .Where(e => e.IsDown)
    .Subscribe(e =>
    {
        sequence.Add(e.Key);
        if (sequence.Count > 3) sequence.RemoveAt(0);

        if (sequence is [VirtualKeyCode.G, VirtualKeyCode.G, VirtualKeyCode.O])
        {
            Console.WriteLine("GGO sequence detected!");
            sequence.Clear();
        }
    });
```

## Generating Keyboard Input

`KeyboardInputGenerator` injects input events into the system's input stream.

### Type Text

```csharp
using Dapplo.Windows.Input.Keyboard;

KeyboardInputGenerator.TypeText("Hello, World!");
```

### Press a Key Combination

```csharp
// Copy (Ctrl+C)
KeyboardInputGenerator.KeyCombinationPress(VirtualKeyCode.Control, VirtualKeyCode.C);

// Paste (Ctrl+V)
KeyboardInputGenerator.KeyCombinationPress(VirtualKeyCode.Control, VirtualKeyCode.V);
```

### Press and Release Individual Keys

```csharp
KeyboardInputGenerator.KeyDown(VirtualKeyCode.Shift);
KeyboardInputGenerator.KeyPress(VirtualKeyCode.A);   // types "A"
KeyboardInputGenerator.KeyUp(VirtualKeyCode.Shift);
```

## Mouse Hooks

### Create a Mouse Hook

```csharp
using Dapplo.Windows.Input.Mouse;

using var mouseHook = MouseHook.Create();

var subscription = mouseHook.MouseEvents.Subscribe(e =>
{
    Console.WriteLine($"Button: {e.Button}  Down: {e.IsButtonDown}  At: {e.Point}");
});
```

### Filter Mouse Events

```csharp
using System.Reactive.Linq;

// Left-button clicks
mouseHook.MouseEvents
    .Where(e => e.Button == MouseButtons.Left && e.IsButtonDown)
    .Subscribe(e => Console.WriteLine($"Left click at {e.Point}"));

// Mouse movement
mouseHook.MouseEvents
    .Where(e => e.IsMouseMoveEvent)
    .Subscribe(e => Console.WriteLine($"Mouse at {e.Point}"));

// Scroll wheel
mouseHook.MouseEvents
    .Where(e => e.IsScrollEvent)
    .Subscribe(e => Console.WriteLine($"Scroll delta: {e.ScrollDelta}"));
```

## Generating Mouse Input

```csharp
using Dapplo.Windows.Input.Mouse;
using Dapplo.Windows.Common.Structs;

// Move to absolute screen coordinate
MouseInputGenerator.MoveTo(new NativePoint(500, 300));

// Click left button
MouseInputGenerator.LeftButtonClick();

// Double-click
MouseInputGenerator.LeftButtonDoubleClick();

// Right-click
MouseInputGenerator.RightButtonClick();

// Scroll down
MouseInputGenerator.MoveMouseWheel(-120);
```

## Suppressing Input

Set `e.Handled = true` inside a hook subscription to prevent the key or mouse event from being passed to other applications:

```csharp
keyboardHook.KeyboardEvents
    .Where(e => e.IsDown && e.Key == VirtualKeyCode.F12)
    .Subscribe(e =>
    {
        Console.WriteLine("F12 intercepted");
        e.Handled = true;  // other apps will not see F12
    });
```

> **Note:** Suppression works only while the hook is active. Dispose the hook to restore normal behavior.

## Best Practices

1. **Dispose hooks and subscriptions** when they are no longer needed to free the system hook slot.
2. **Keep hook handlers fast** — low-level input hooks are synchronous and will delay system input if they block.
3. **Use `TriggerOnKeyUp`** when injecting input as part of a hotkey response.
4. **Test on all target DPI settings** — mouse coordinates are in physical pixels.

## See Also

- [[Getting-Started]]
- [[Common-Scenarios]]
- [Reactive Extensions](http://reactivex.io/)
