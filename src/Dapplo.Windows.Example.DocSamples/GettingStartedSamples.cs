// Copyright (c) Dapplo and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Linq;
using System.Reactive.Linq;
using System.Threading;
using Dapplo.Windows.Clipboard;
using Dapplo.Windows.Desktop;
using Dapplo.Windows.Input.Enums;
using Dapplo.Windows.Input.Keyboard;

namespace Dapplo.Windows.Example.DocSamples;

/// <summary>
/// Samples for README.md, doc/articles/intro.md, doc/index.md, wiki/Getting-Started.md
/// </summary>
public static class GettingStartedSamples
{
    public static void FirstWindowQuery()
    {
        #region FirstWindowQuery
        // using Dapplo.Windows.Desktop;
        foreach (var window in InteropWindowQuery.GetVisibleApplicationWindows())
        {
            Console.WriteLine($"{window.GetCaption()} - {window.GetClassname()} at {window.GetInfo().Bounds}");
        }
        #endregion
    }

    public static void WindowTitles()
    {
        #region WindowTitles
        // using Dapplo.Windows.Desktop; using System.Reactive.Linq;
        IDisposable subscription = WinEventHook.WindowTitleChangeObservable()
            .Subscribe(info =>
            {
                var window = InteropWindowFactory.CreateFor(info.Handle);
                Console.WriteLine($"Title changed: {window.GetCaption(forceUpdate: true)}");
            });
        #endregion
    }

    public static void FirstClipboardMonitor()
    {
        #region FirstClipboardMonitor
        // using Dapplo.Windows.Clipboard; using System.Reactive.Linq;
        IDisposable subscription = ClipboardNative.OnUpdate
            .Where(info => info.FormatIds.Contains((uint)StandardClipboardFormats.UnicodeText))
            .Throttle(TimeSpan.FromMilliseconds(100))
            .Subscribe(info =>
            {
                using var clipboard = ClipboardNative.Access();
                Console.WriteLine($"Copied: {clipboard.GetAsUnicodeString()}");
            });
        #endregion
    }

    public static void FirstKeyboardHook()
    {
        #region FirstKeyboardHook
        // using Dapplo.Windows.Input.Enums; using Dapplo.Windows.Input.Keyboard; using System.Reactive.Linq;
        // Ctrl+Shift+S anywhere in Windows. The handler runs on the hook thread, ObserveOn moves the work to the UI thread.
        IDisposable subscription = KeyboardHook.KeyboardEvents
            .Where(new KeyCombinationHandler(VirtualKeyCode.Control, VirtualKeyCode.Shift, VirtualKeyCode.KeyS))
            .ObserveOn(SynchronizationContext.Current)
            .Subscribe(_ => Console.WriteLine("Ctrl+Shift+S pressed"));
        #endregion
    }

    public static void Dispose()
    {
        #region Dispose
        // Hooks and registrations are made when you subscribe, and removed when you dispose the subscription
        IDisposable subscription = ClipboardNative.OnUpdate.Subscribe(info => { });
        // ...
        subscription.Dispose();

        // Clipboard access is a lock for all applications: hold it briefly and always dispose it
        using (var clipboard = ClipboardNative.Access())
        {
            // ...
        }
        #endregion
    }
}
