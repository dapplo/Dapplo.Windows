// Copyright (c) Dapplo and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Reactive.Concurrency;
using System.Reactive.Linq;
using System.Threading;
using System.Threading.Tasks;
using Dapplo.Windows.Clipboard;
using Dapplo.Windows.Common.Extensions;
using Dapplo.Windows.Common.Structs;
using Dapplo.Windows.Desktop;
using Dapplo.Windows.Dpi;
using Dapplo.Windows.Enums;
using Dapplo.Windows.Input.Enums;
using Dapplo.Windows.Input.Keyboard;
using Dapplo.Windows.Messages;
using Dapplo.Windows.Messages.Enums;
using Dapplo.Windows.User32;
using Dapplo.Windows.User32.Enums;

namespace Dapplo.Windows.Example.DocSamples;

/// <summary>
/// Samples for doc/articles/common-scenarios.md, wiki/Common-Scenarios.md
/// </summary>
public static class CommonScenariosSamples
{
    public static void ScreenshotHotkey()
    {
        #region ScreenshotHotkey
        // Alt+PrintScreen replacement: capture the active window as PNG onto the clipboard
        var subscription = KeyboardHook.KeyboardEvents
            .Where(new KeyCombinationHandler(VirtualKeyCode.Menu, VirtualKeyCode.PrintScreen))
            // Leave the hook thread before doing the work
            .ObserveOn(TaskPoolScheduler.Default)
            .Subscribe(_ =>
            {
                var window = InteropWindowQuery.GetForegroundWindow();
                using var bitmap = window.PrintWindow();
                if (bitmap == null)
                {
                    return;
                }
                using var png = new MemoryStream();
                bitmap.Save(png, ImageFormat.Png);
                png.Position = 0;

                using var clipboard = ClipboardNative.Access();
                clipboard.ClearContents();
                clipboard.SetAsStream("PNG", png);
            });
        #endregion
    }

    public static void ActiveWindowTracking()
    {
        #region ActiveWindowTracking
        // How long is each application in the foreground?
        var usage = new Dictionary<string, TimeSpan>();
        string currentProcess = null;
        var since = DateTimeOffset.Now;

        var subscription = WinEventHook.Create(WinEvents.EVENT_SYSTEM_FOREGROUND)
            // Don't query the process on the SharedMessageWindow thread
            .ObserveOn(TaskPoolScheduler.Default)
            .Subscribe(info =>
            {
                var now = DateTimeOffset.Now;
                if (currentProcess != null)
                {
                    usage[currentProcess] = (usage.TryGetValue(currentProcess, out var total) ? total : TimeSpan.Zero) + (now - since);
                }
                using var process = Process.GetProcessById(InteropWindowFactory.CreateFor(info.Handle).GetProcessId());
                currentProcess = process.ProcessName;
                since = now;
            });
        #endregion
    }

    public static void ApplicationStarts()
    {
        #region ApplicationStarts
        // Get told when Notepad opens a window
        var subscription = WinEventHook.WindowCreateDestroyObservable()
            .Where(info => info.WinEvent == WinEvents.EVENT_OBJECT_CREATE)
            .Select(info => InteropWindowFactory.CreateFor(info.Handle))
            .Where(window => window.IsVisibleApplicationWindow() && window.GetClassname() == "Notepad")
            .Subscribe(window => Console.WriteLine($"Notepad opened a window: {window.Handle}"));
        #endregion
    }

    public static void ClipboardHistory()
    {
        #region ClipboardHistory
        var history = new List<string>();
        var subscription = ClipboardNative.OnUpdate
            // Skip the current content, only log changes
            .Skip(1)
            .Where(info => info.FormatIds.Contains((uint)StandardClipboardFormats.UnicodeText))
            // Respect passwords and other excluded content
            .Where(info => !info.Formats.Contains(ClipboardCloudExtensions.ExcludeClipboardContentFromMonitorProcessingFormat))
            .ObserveOn(TaskPoolScheduler.Default)
            .Subscribe(info =>
            {
                using var clipboard = ClipboardNative.Access();
                if (clipboard.CanAccess)
                {
                    history.Add(clipboard.GetAsUnicodeString());
                }
            });
        #endregion
    }

    public static void SaveClipboardImages()
    {
        #region SaveClipboardImages
        var folder = Environment.GetFolderPath(Environment.SpecialFolder.MyPictures);
        var subscription = ClipboardNative.OnUpdate
            .Skip(1)
            .Where(info => info.Formats.Contains("PNG"))
            .ObserveOn(TaskPoolScheduler.Default)
            .Subscribe(info =>
            {
                using var clipboard = ClipboardNative.Access();
                if (clipboard.CanAccess && clipboard.TryGetAsStream("PNG", out var png))
                {
                    using (png)
                    using (var file = File.Create(Path.Combine(folder, $"clipboard-{info.Timestamp:yyyyMMdd-HHmmss}.png")))
                    {
                        png.CopyTo(file);
                    }
                }
            });
        #endregion
    }

    public static void InsertTimestamp()
    {
        #region InsertTimestamp
        // Ctrl+Alt+D types the current date into the active application.
        // AllKeysUp fires when the user released all keys of the combination, so the text isn't combined with Ctrl or Alt,
        // and the keys are passed on, so no key gets stuck.
        var subscription = KeyboardHook.KeyboardEvents
            .Where(new KeyCombinationHandler(VirtualKeyCode.Control, VirtualKeyCode.Menu, VirtualKeyCode.KeyD) { TriggerMode = TriggerMode.AllKeysUp })
            .ObserveOn(TaskPoolScheduler.Default)
            .Subscribe(_ => KeyboardInputGenerator.TypeText(DateTime.Now.ToString("yyyy-MM-dd")));
        #endregion
    }

    public static void TileWindows()
    {
        #region TileWindows
        // Place the visible windows of the primary display next to each other
        var primary = DisplayInfo.AllDisplayInfos.First(display => display.IsPrimary);
        // GetVisibleApplicationWindows skips hidden, minimized and untitled windows and tool windows
        var windows = InteropWindowQuery.GetVisibleApplicationWindows()
            .Where(window => primary.Bounds.Contains(window.GetInfo().Bounds.Location))
            .ToList();
        if (windows.Count == 0)
        {
            return;
        }
        var width = primary.WorkingArea.Width / windows.Count;
        for (var i = 0; i < windows.Count; i++)
        {
            windows[i].Restore();
            User32Api.SetWindowPos(windows[i].Handle, IntPtr.Zero,
                primary.WorkingArea.Left + i * width, primary.WorkingArea.Top, width, primary.WorkingArea.Height,
                WindowPos.SWP_NOZORDER | WindowPos.SWP_NOACTIVATE);
        }
        #endregion
    }

    public static void MinimizeOthers()
    {
        #region MinimizeOthers
        var active = InteropWindowQuery.GetForegroundWindow();
        foreach (var window in InteropWindowQuery.GetVisibleApplicationWindows().Where(w => w.Handle != active.Handle))
        {
            window.Minimize();
        }
        #endregion
    }

    public static void SingleInstance(Action showMainWindow)
    {
        #region SingleInstance
        // The first instance listens, a second instance broadcasts and exits
        uint showMessage = RegisteredWindowMessages.Register("MyApp.ShowMainWindow");
        using var mutex = new Mutex(true, "MyApp.SingleInstance", out var isFirstInstance);
        if (!isFirstInstance)
        {
            // HWND_BROADCAST: every top-level window gets it, also the SharedMessageWindow of the first instance
            User32Api.PostMessage(WindowHandles.HWND_BROADCAST, showMessage, IntPtr.Zero, IntPtr.Zero);
            return;
        }
        var subscription = SharedMessageWindow.Messages
            .Where(m => (uint)m.Msg == showMessage)
            .ObserveOn(SynchronizationContext.Current)
            .Subscribe(_ => showMainWindow());
        #endregion
    }

    public static void Debounce()
    {
        #region Debounce
        // Location changes arrive by the hundred while a window is dragged: wait until it is quiet for 250ms
        var subscription = WinEventHook.Create(WinEvents.EVENT_OBJECT_LOCATIONCHANGE)
            .Where(info => info.ObjectIdentifier == ObjectIdentifiers.Window)
            .Throttle(TimeSpan.FromMilliseconds(250))
            .Subscribe(info => Console.WriteLine("A window stopped moving"));

        // Or collect them and process them in batches
        var batches = ClipboardNative.OnUpdate
            .Buffer(TimeSpan.FromSeconds(1))
            .Where(batch => batch.Count > 0)
            .Subscribe(batch => Console.WriteLine($"{batch.Count} clipboard changes in the last second"));
        #endregion
    }
}
