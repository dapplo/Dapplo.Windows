// Copyright (c) Dapplo and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Linq;
using System.Reactive.Concurrency;
using System.Reactive.Linq;
using System.Threading;
using Dapplo.Windows.Common.Structs;
using Dapplo.Windows.Input;
using Dapplo.Windows.Input.Enums;
using Dapplo.Windows.Input.Keyboard;
using Dapplo.Windows.Input.Mouse;
using Dapplo.Windows.Input.Structs;
using Dapplo.Windows.Messages.Enumerations;

namespace Dapplo.Windows.Example.DocSamples;

/// <summary>
/// Samples for doc/articles/input-handling.md, wiki/Input-Handling.md
/// </summary>
public static class InputSamples
{
    public static void KeyboardHookBasics()
    {
        #region KeyboardHookBasics
        // The hook is installed with the first subscription and removed with the last
        IDisposable subscription = KeyboardHook.KeyboardEvents
            .Where(args => args.IsKeyDown)
            .Subscribe(args => Console.WriteLine($"{args.Key} down, Ctrl: {args.IsControl}, Shift: {args.IsShift}"));

        // ...
        subscription.Dispose();
        #endregion
    }

    public static void NonBlocking()
    {
        #region NonBlocking
        // For listeners which never set Handled: the events are delivered in order on another thread,
        // so a slow subscriber never delays the keyboard of the whole system
        var subscription = KeyboardHook.KeyboardEventsNonBlocking
            .Where(args => args.IsKeyDown)
            .Subscribe(args => LogKey(args.Key));
        #endregion
    }

    public static void ObserveOnUi()
    {
        #region ObserveOnUi
        // Decide Handled on the hook thread (quick!), then do the real work on the UI thread.
        // Call this on the UI thread, so SynchronizationContext.Current is the one of the UI.
        var subscription = KeyboardHook.KeyboardEvents
            .Where(args => args.Key == VirtualKeyCode.PrintScreen)
            // Swallow the key-down and the key-up, so Windows doesn't take its own screenshot
            .Do(args => args.Handled = true)
            .Where(args => args.IsKeyDown)
            .ObserveOn(SynchronizationContext.Current)
            .Subscribe(args => TakeScreenshot());
        #endregion
    }

    public static void SubscriberErrors()
    {
        #region SubscriberErrors
        // An exception in a subscriber never reaches the hook, it is published here and ends only that subscription
        var errors = KeyboardHook.SubscriberErrors
            .Subscribe(ex => Console.Error.WriteLine($"Keyboard subscriber failed: {ex}"));
        #endregion
    }

    public static void KeyCombination()
    {
        #region KeyCombination
        // Ctrl+Shift+S, Control and Shift match both the left and the right key.
        // The handler marks the key events as handled, so other applications don't see the combination.
        var subscription = KeyboardHook.KeyboardEvents
            .Where(new KeyCombinationHandler(VirtualKeyCode.Control, VirtualKeyCode.Shift, VirtualKeyCode.KeyS))
            .ObserveOn(SynchronizationContext.Current)
            .Subscribe(args => SaveAll());
        #endregion
    }

    public static void KeyCombinationOptions()
    {
        #region KeyCombinationOptions
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
        #endregion
    }

    public static void KeyCombinationFromString()
    {
        #region KeyCombinationFromString
        // "ctrl", "alt", "shift" and "win" match the left and the right key, single characters become KeyA .. KeyZ, Key0 .. Key9
        var keys = KeyHelper.VirtualKeyCodesFromString("Ctrl + Alt + P").ToArray();
        var subscription = KeyboardHook.KeyboardEvents
            .Where(new KeyCombinationHandler(keys))
            .Subscribe(_ => Console.WriteLine("Ctrl+Alt+P"));

        // A readable, localized name for a key, e.g. for a settings dialog
        string text = KeyHelper.VirtualCodeToLocaleDisplayText(VirtualKeyCode.PrintScreen);
        #endregion
    }

    public static void HandlerFactory()
    {
        #region HandlerFactory
        // A handler keeps track of the pressed keys, so it may be used by one subscription only.
        // When an observable is subscribed more than once, give Where a factory: every subscription gets its own handler.
        IObservable<KeyboardHookEventArgs> saveHotkey = KeyboardHook.KeyboardEvents
            .Where(() => new KeyCombinationHandler(VirtualKeyCode.Control, VirtualKeyCode.KeyS));

        var first = saveHotkey.Subscribe(_ => Console.WriteLine("Save (1)"));
        var second = saveHotkey.Subscribe(_ => Console.WriteLine("Save (2)"));
        #endregion
    }

    public static void KeyOrCombination()
    {
        #region KeyOrCombination
        // PrintScreen, or Ctrl+Shift+4
        var handler = new KeyOrCombinationHandler(
            new KeyCombinationHandler(VirtualKeyCode.PrintScreen),
            new KeyCombinationHandler(VirtualKeyCode.Control, VirtualKeyCode.Shift, VirtualKeyCode.Key4));

        var subscription = KeyboardHook.KeyboardEvents.Where(handler).Subscribe(_ => Console.WriteLine("Capture"));
        #endregion
    }

    public static void KeySequence()
    {
        #region KeySequence
        // Ctrl+K followed by Ctrl+C (like Visual Studio), at most 2 seconds apart
        var handler = new KeySequenceHandler(
            new KeyCombinationHandler(VirtualKeyCode.Control, VirtualKeyCode.KeyK),
            new KeyCombinationHandler(VirtualKeyCode.Control, VirtualKeyCode.KeyC))
        {
            Timeout = TimeSpan.FromSeconds(2)
        };

        var subscription = KeyboardHook.KeyboardEvents.Where(handler).Subscribe(_ => Console.WriteLine("Comment selection"));
        #endregion
    }

    public static void AllKeysUp()
    {
        #region AllKeysUp
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
        #endregion
    }

    #region WaitForRelease
    private static void WaitUntilReleased(params VirtualKeyCode[] keys)
    {
        // The asynchronous (current, system wide) key state, Control and Shift match the left and the right key
        while (KeyboardState.IsAnyDown(keys))
        {
            Thread.Sleep(20);
        }
    }
    #endregion

    public static void FirstKeyUp()
    {
        #region FirstKeyUp
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
        #endregion
    }

    public static void KeyStateQuery()
    {
        #region KeyStateQuery
        // Right now, system wide (GetAsyncKeyState): use this in background threads and hooks
        bool shiftDown = KeyboardState.IsDown(VirtualKeyCode.Shift);
        bool anyModifier = KeyboardState.IsAnyDown(VirtualKeyCode.Shift, VirtualKeyCode.Control, VirtualKeyCode.Menu, VirtualKeyCode.Win);

        // At the time of the keyboard message the UI thread is processing (GetKeyState): use this in a key event handler
        bool ctrlWithThisKey = KeyboardState.IsDownForCurrentThread(VirtualKeyCode.Control);

        // Toggle keys
        bool capsLock = KeyboardState.IsToggled(VirtualKeyCode.Capital);
        #endregion
    }

    public static void SuppressKey()
    {
        #region SuppressKey
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
        #endregion
    }

    public static void GenerateKeys()
    {
        #region GenerateKeys
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
        #endregion
    }

    public static void TypeText()
    {
        #region TypeText
        // Types the text as Unicode characters, independent of the keyboard layout.
        // Line breaks become Enter, \t becomes Tab, other control characters are skipped.
        var text = "Grüße aus Köln 👋\r\nPrice:\t42 €";
        uint inserted = KeyboardInputGenerator.TypeText(text);

        // Two events (down and up) per UTF-16 code unit, Enter and Tab, fewer means the input was blocked
        if (inserted < KeyboardInput.ForText(text).Length)
        {
            Console.Error.WriteLine("The input was blocked, e.g. by an elevated application");
        }
        #endregion
    }

    public static void MouseHookBasics()
    {
        #region MouseHookBasics
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
        #endregion
    }

    public static void SuppressMouse()
    {
        #region SuppressMouse
        // Swallow right clicks (down and up), e.g. while your application shows its own context menu
        var subscription = MouseHook.MouseEvents
            .Where(args => args.WindowsMessage == WindowsMessages.WM_RBUTTONDOWN || args.WindowsMessage == WindowsMessages.WM_RBUTTONUP)
            .Subscribe(args => args.Handled = true);
        #endregion
    }

    public static void GenerateMouse()
    {
        #region GenerateMouse
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
        #endregion
    }

    public static void Idle()
    {
        #region Idle
        // How long ago did the user touch the keyboard or mouse?
        TimeSpan idle = NativeInput.LastInputTimeSpan;
        if (idle > TimeSpan.FromMinutes(5))
        {
            Console.WriteLine("The user is away");
        }
        #endregion
    }

    public static void RawInput()
    {
        #region RawInput
        // Raw input tells which device the input came from, and works while the application is in the background.
        // The events arrive on the SharedMessageWindow thread.
        var subscription = RawInputMonitor.Listen(RawInputDevices.Keyboard)
            .Subscribe(args =>
            {
                var keyboard = args.RawInput.Device.Keyboard;
                Console.WriteLine($"Device {args.RawInput.Header.DeviceHandle}: {keyboard.VirtualKey} ({keyboard.Flags})");
            });
        #endregion
    }

    public static void RawInputDevicesSample()
    {
        #region RawInputDevices
        // All currently connected keyboards and mice
        foreach (var device in RawInputApi.GetAllDevices())
        {
            Console.WriteLine($"{device.DisplayName}: {device.DeviceName}");
        }

        // Get told when a keyboard is connected or removed
        var subscription = RawInputDeviceMonitor.Listen(RawInputDevices.Keyboard)
            .Subscribe(change => Console.WriteLine($"{(change.Added ? "Added" : "Removed")}: {change.DeviceInformation.DisplayName}"));
        #endregion
    }

    private static void LogKey(VirtualKeyCode key) { }
    private static void TakeScreenshot() { }
    private static void SaveAll() { }
}
