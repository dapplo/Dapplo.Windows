# Input Handling Guide

This guide covers keyboard and mouse input monitoring and generation using the `Dapplo.Windows.Input` package.

## Overview

The Input package provides:
- Low-level keyboard and mouse hooks
- Reactive Extensions (Rx) based event handling
- Input generation (simulating keyboard and mouse events)
- Key combination and sequence handlers

## Installation

```powershell
Install-Package Dapplo.Windows.Input
```

## Keyboard Input

### Keyboard Hook

Create a keyboard hook to monitor keyboard events:

```csharp
using Dapplo.Windows.Input.Keyboard;
using System;

// Create keyboard hook
var keyboardHook = KeyboardHook.Create();

// Subscribe to keyboard events
var subscription = keyboardHook.KeyboardEvents.Subscribe(keyEvent =>
{
    Console.WriteLine($"Key: {keyEvent.Key}, IsDown: {keyEvent.IsDown}");
});

// Clean up when done
subscription.Dispose();
keyboardHook.Dispose();
```

### Filter Keyboard Events

```csharp
using Dapplo.Windows.Input.Keyboard;
using System.Reactive.Linq;

var keyboardHook = KeyboardHook.Create();

// Only react to key presses (not releases)
var subscription = keyboardHook.KeyboardEvents
    .Where(e => e.IsDown)
    .Subscribe(e =>
    {
        Console.WriteLine($"Key pressed: {e.Key}");
    });

// Monitor specific key
var escSubscription = keyboardHook.KeyboardEvents
    .Where(e => e.Key == VirtualKeyCode.Escape && e.IsDown)
    .Subscribe(e =>
    {
        Console.WriteLine("ESC pressed");
    });
```

### Key Combinations

Detect key combinations like Ctrl+C:

```csharp
using Dapplo.Windows.Input.Keyboard;
using System.Reactive.Linq;

var keyboardHook = KeyboardHook.Create();

// Detect Ctrl+C
var ctrlCSubscription = keyboardHook.KeyboardEvents
    .Where(e => e.IsDown && e.Key == VirtualKeyCode.C && e.IsControlPressed)
    .Subscribe(e =>
    {
        Console.WriteLine("Ctrl+C pressed");
    });

// Detect Ctrl+Shift+A
var complexComboSubscription = keyboardHook.KeyboardEvents
    .Where(e => e.IsDown && 
                e.Key == VirtualKeyCode.A && 
                e.IsControlPressed && 
                e.IsShiftPressed)
    .Subscribe(e =>
    {
        Console.WriteLine("Ctrl+Shift+A pressed");
    });
```

### Key Sequence Detection

Detect a sequence of keys (like vim commands):

```csharp
using Dapplo.Windows.Input.Keyboard;
using System;
using System.Reactive.Linq;
using System.Collections.Generic;

var keyboardHook = KeyboardHook.Create();
var keySequence = new List<VirtualKeyCode>();

var subscription = keyboardHook.KeyboardEvents
    .Where(e => e.IsDown)
    .Subscribe(e =>
    {
        keySequence.Add(e.Key);
        
        // Keep only last 3 keys
        if (keySequence.Count > 3)
        {
            keySequence.RemoveAt(0);
        }
        
        // Check for sequence: G, G, O
        if (keySequence.Count == 3 &&
            keySequence[0] == VirtualKeyCode.G &&
            keySequence[1] == VirtualKeyCode.G &&
            keySequence[2] == VirtualKeyCode.O)
        {
            Console.WriteLine("GGO sequence detected!");
            keySequence.Clear();
        }
    });
```

### Suppress Key Events

Prevent key events from reaching other applications:

```csharp
using Dapplo.Windows.Input.Keyboard;

var keyboardHook = KeyboardHook.Create();

var subscription = keyboardHook.KeyboardEvents
    .Subscribe(e =>
    {
        if (e.Key == VirtualKeyCode.PrintScreen && e.IsDown)
        {
            // Handle screenshot yourself
            TakeScreenshot();
            
            // Suppress the key so system doesn't handle it
            e.Handled = true;
        }
    });
```

## Generating Keyboard Input

### Type Text

```csharp
using Dapplo.Windows.Input.Keyboard;

// Type text
KeyboardInputGenerator.TypeText("Hello, World!");

// Type with delay between characters
KeyboardInputGenerator.TypeText("Slow typing", 100); // 100ms delay
```

### Press Keys

```csharp
using Dapplo.Windows.Input.Keyboard;

// Press and release a single key
KeyboardInputGenerator.KeyPress(VirtualKeyCode.Return);

// Press multiple keys
KeyboardInputGenerator.KeyPress(VirtualKeyCode.A);
KeyboardInputGenerator.KeyPress(VirtualKeyCode.B);
KeyboardInputGenerator.KeyPress(VirtualKeyCode.C);
```

### Key Combinations

```csharp
using Dapplo.Windows.Input.Keyboard;

// Press Ctrl+C
KeyboardInputGenerator.KeyCombinationPress(
    VirtualKeyCode.Control, 
    VirtualKeyCode.C
);

// Press Ctrl+Shift+S
KeyboardInputGenerator.KeyCombinationPress(
    VirtualKeyCode.Control,
    VirtualKeyCode.Shift,
    VirtualKeyCode.S
);

// Alt+Tab
KeyboardInputGenerator.KeyCombinationPress(
    VirtualKeyCode.Alt,
    VirtualKeyCode.Tab
);
```

### Advanced Key Control

```csharp
using Dapplo.Windows.Input.Keyboard;

// Key down
KeyboardInputGenerator.KeyDown(VirtualKeyCode.Shift);

// Type while shift is held
KeyboardInputGenerator.KeyPress(VirtualKeyCode.A);
KeyboardInputGenerator.KeyPress(VirtualKeyCode.B);

// Key up
KeyboardInputGenerator.KeyUp(VirtualKeyCode.Shift);
```

## Mouse Input

### Mouse Hook

Monitor mouse events:

```csharp
using Dapplo.Windows.Input.Mouse;
using System;

// Create mouse hook
var mouseHook = MouseHook.Create();

// Subscribe to mouse events
var subscription = mouseHook.MouseEvents.Subscribe(mouseEvent =>
{
    Console.WriteLine($"Mouse: {mouseEvent.Button} at ({mouseEvent.Point.X}, {mouseEvent.Point.Y})");
});

// Clean up
subscription.Dispose();
mouseHook.Dispose();
```

### Filter Mouse Events

```csharp
using Dapplo.Windows.Input.Mouse;
using System.Reactive.Linq;

var mouseHook = MouseHook.Create();

// Only left clicks
var leftClickSubscription = mouseHook.MouseEvents
    .Where(e => e.Button == MouseButtons.Left && e.IsButtonDown)
    .Subscribe(e =>
    {
        Console.WriteLine($"Left click at ({e.Point.X}, {e.Point.Y})");
    });

// Only right clicks
var rightClickSubscription = mouseHook.MouseEvents
    .Where(e => e.Button == MouseButtons.Right && e.IsButtonDown)
    .Subscribe(e =>
    {
        Console.WriteLine($"Right click at ({e.Point.X}, {e.Point.Y})");
    });

// Mouse wheel
var wheelSubscription = mouseHook.MouseEvents
    .Where(e => e.Delta != 0)
    .Subscribe(e =>
    {
        Console.WriteLine($"Mouse wheel: {e.Delta}");
    });
```

### Mouse Movement

```csharp
using Dapplo.Windows.Input.Mouse;
using System.Reactive.Linq;
using System;

var mouseHook = MouseHook.Create();

// Track mouse movement
var moveSubscription = mouseHook.MouseEvents
    .Where(e => e.IsMouseMove)
    .Throttle(TimeSpan.FromMilliseconds(100)) // Limit update rate
    .Subscribe(e =>
    {
        Console.WriteLine($"Mouse at ({e.Point.X}, {e.Point.Y})");
    });
```

### Detect Click Patterns

```csharp
using Dapplo.Windows.Input.Mouse;
using System.Reactive.Linq;
using System;

var mouseHook = MouseHook.Create();

// Double-click detection
var doubleClickSubscription = mouseHook.MouseEvents
    .Where(e => e.Button == MouseButtons.Left && e.IsButtonDown)
    .Buffer(TimeSpan.FromMilliseconds(500), 2) // Two clicks within 500ms
    .Where(clicks => clicks.Count == 2)
    .Subscribe(clicks =>
    {
        Console.WriteLine("Double-click detected!");
    });
```

## Generating Mouse Input

### Move Mouse

```csharp
using Dapplo.Windows.Input.Mouse;
using Dapplo.Windows.Common.Structs;

// Move to absolute position
MouseInputGenerator.MoveMouse(new NativePoint(100, 100));

// Move relative to current position
var currentPos = MouseInputGenerator.GetMousePosition();
MouseInputGenerator.MoveMouse(new NativePoint(currentPos.X + 10, currentPos.Y + 10));
```

### Click Mouse

```csharp
using Dapplo.Windows.Input.Mouse;

// Left click
MouseInputGenerator.Click();

// Right click
MouseInputGenerator.RightClick();

// Middle click
MouseInputGenerator.MiddleClick();

// Double click
MouseInputGenerator.DoubleClick();
```

### Advanced Mouse Control

```csharp
using Dapplo.Windows.Input.Mouse;
using Dapplo.Windows.Common.Structs;

// Press button down
MouseInputGenerator.MouseDown(MouseButtons.Left);

// Move while button is down (drag)
MouseInputGenerator.MoveMouse(new NativePoint(200, 200));

// Release button
MouseInputGenerator.MouseUp(MouseButtons.Left);
```

### Mouse Wheel

```csharp
using Dapplo.Windows.Input.Mouse;

// Scroll up (positive value)
MouseInputGenerator.MouseWheel(120);

// Scroll down (negative value)
MouseInputGenerator.MouseWheel(-120);
```

## Raw Input Monitoring

Monitor raw input devices (keyboards, mice, HID devices):

```csharp
using Dapplo.Windows.Input;
using System;

// Create raw input monitor
var rawInputMonitor = RawInputMonitor.Create();

// Subscribe to raw input events
var subscription = rawInputMonitor.RawInputEvents.Subscribe(rawEvent =>
{
    Console.WriteLine($"Raw input from device: {rawEvent.DeviceHandle}");
    
    if (rawEvent.Device == RawInputDeviceType.Keyboard)
    {
        Console.WriteLine($"Keyboard: {rawEvent.VirtualKey}");
    }
    else if (rawEvent.Device == RawInputDeviceType.Mouse)
    {
        Console.WriteLine($"Mouse: {rawEvent.Mouse}");
    }
});

// Clean up
subscription.Dispose();
rawInputMonitor.Dispose();
```

## Common Patterns

### Global Hotkey

```csharp
using Dapplo.Windows.Input.Keyboard;
using System.Reactive.Linq;

var keyboardHook = KeyboardHook.Create();

// Ctrl+Alt+H hotkey
var hotkeySubscription = keyboardHook.KeyboardEvents
    .Where(e => e.IsDown && 
                e.Key == VirtualKeyCode.H && 
                e.IsControlPressed && 
                e.IsAltPressed)
    .Subscribe(e =>
    {
        Console.WriteLine("Global hotkey activated!");
        e.Handled = true; // Prevent other apps from seeing it
    });
```

### Auto-Clicker

```csharp
using Dapplo.Windows.Input.Mouse;
using System.Threading;
using System.Threading.Tasks;

async Task AutoClick(int intervalMs, int count)
{
    for (int i = 0; i < count; i++)
    {
        MouseInputGenerator.Click();
        await Task.Delay(intervalMs);
    }
}

// Click 10 times with 1 second interval
await AutoClick(1000, 10);
```

### Keyboard Macro

```csharp
using Dapplo.Windows.Input.Keyboard;
using System.Threading.Tasks;

async Task PlayMacro()
{
    // Type username
    KeyboardInputGenerator.TypeText("myusername");
    
    // Press Tab
    KeyboardInputGenerator.KeyPress(VirtualKeyCode.Tab);
    
    await Task.Delay(100);
    
    // Type password
    KeyboardInputGenerator.TypeText("mypassword");
    
    // Press Enter
    KeyboardInputGenerator.KeyPress(VirtualKeyCode.Return);
}
```

## Best Practices

### 1. Dispose Hooks

Always dispose hooks when done:

```csharp
using (var keyboardHook = KeyboardHook.Create())
{
    // Use hook
} // Automatically disposed
```

### 2. Handle Errors

Input operations can fail:

```csharp
try
{
    KeyboardInputGenerator.KeyPress(VirtualKeyCode.A);
}
catch (Win32Exception ex)
{
    Console.WriteLine($"Input failed: {ex.Message}");
}
```

### 3. Throttle Events

Limit high-frequency events:

```csharp
var subscription = mouseHook.MouseEvents
    .Where(e => e.IsMouseMove)
    .Throttle(TimeSpan.FromMilliseconds(50))
    .Subscribe(e => UpdateUI(e.Point));
```

### 4. Clean Up Subscriptions

```csharp
var subscription = keyboardHook.KeyboardEvents.Subscribe(...);

// Later
subscription.Dispose();
```

### 5. Test Input Generation

Always test generated input:
- Verify correct keys are sent
- Check timing between inputs
- Test with target applications
- Handle focus issues

## Security Considerations

### Administrator Privileges

Low-level hooks may require administrator privileges:
- Keyboard/mouse hooks work in standard user mode
- Some input generation may need elevation
- Consider UAC implications

### Input Injection

Be careful with input generation:
- Never inject sensitive data (passwords) programmatically in production
- Validate and sanitize any text being typed
- Consider security implications of automated input

## See Also

- [API Reference](../api/index.md)
- [Getting Started](intro.md)
- [Common Scenarios](common-scenarios.md)
