# Common Scenarios

Real-world recipes that combine multiple Dapplo.Windows packages to accomplish common tasks.

## Application Monitoring

### Detect When a Specific Application Starts

**Packages:** `Dapplo.Windows`

```csharp
using Dapplo.Windows.Desktop;
using System.Reactive.Linq;

var sub = WinEventHook.Create(WinEvents.EVENT_OBJECT_CREATE)
    .Where(e =>
    {
        var window = InteropWindow.FromHandle(e.Handle);
        return window.GetProcessPath()?.Contains("notepad.exe") ?? false;
    })
    .Subscribe(_ => Console.WriteLine("Notepad started!"));
```

### Track Which Application Is Active (Time Tracking)

**Packages:** `Dapplo.Windows`

```csharp
using Dapplo.Windows.Desktop;

string currentApp = "";

WinEventHook.Create(WinEvents.EVENT_SYSTEM_FOREGROUND)
    .Subscribe(e =>
    {
        var window = InteropWindow.FromHandle(e.Handle);
        string appName = window.GetCaption();

        if (appName != currentApp)
        {
            LogTimeSpent(currentApp);           // record previous app duration
            currentApp = appName;
            Console.WriteLine($"Switched to: {currentApp}");
        }
    });
```

### Detect Screen Lock / Unlock

**Packages:** `Dapplo.Windows.Messages`

```csharp
using Dapplo.Windows.Messages;

WinProcHandler.Instance.Subscribe(new WinProcHandlerHook(
    (hwnd, msg, wparam, lparam, ref handled) =>
    {
        if ((uint)msg == WindowsMessages.WM_WTSSESSION_CHANGE)
        {
            switch (wparam.ToInt32())
            {
                case 0x7: Console.WriteLine("Screen locked");   break;
                case 0x8: Console.WriteLine("Screen unlocked"); break;
            }
        }
        return IntPtr.Zero;
    }));
```

---

## Screenshot Tool with Hotkey

**Packages:** `Dapplo.Windows`, `Dapplo.Windows.Input`, `Dapplo.Windows.Dpi`

```csharp
using Dapplo.Windows.Desktop;
using Dapplo.Windows.Dpi;
using Dapplo.Windows.Input.Keyboard;
using System.Drawing.Imaging;
using System.Reactive.Linq;

using var keyboardHook = KeyboardHook.Create();

keyboardHook.KeyboardEvents
    .Where(e => e.IsDown && e.Key == VirtualKeyCode.PrintScreen)
    .Subscribe(e =>
    {
        var handle = User32Api.GetForegroundWindow();
        var window = InteropWindow.FromHandle(handle);

        using var bitmap = window.Capture();
        var dpi = DpiHandler.GetDpiForWindow(handle);
        bitmap.SetResolution(dpi, dpi);

        var filename = $"screenshot_{DateTime.Now:yyyyMMdd_HHmmss}.png";
        bitmap.Save(filename, ImageFormat.Png);
        Console.WriteLine($"Saved: {filename}");

        e.Handled = true;  // suppress default Print Screen
    });

Console.ReadLine();
```

---

## Clipboard History Logger

**Packages:** `Dapplo.Windows.Clipboard`

```csharp
using Dapplo.Windows.Clipboard;
using System.Collections.Generic;
using System.Reactive.Linq;

var history = new List<string>();

ClipboardNative.OnUpdate
    .Where(info => info.Formats.Contains(StandardClipboardFormats.UnicodeText.AsString()))
    .Throttle(TimeSpan.FromMilliseconds(300))
    .Subscribe(info =>
    {
        using var clipboard = ClipboardNative.Access();
        var text = clipboard.GetAsUnicodeString();

        if (!string.IsNullOrEmpty(text) && !history.Contains(text))
        {
            if (history.Count >= 100) history.RemoveAt(0);
            history.Add(text);
            Console.WriteLine($"[{history.Count}] {text[..Math.Min(60, text.Length)]}");
        }
    });

Console.ReadLine();
```

### Automatically Save Clipboard Images

**Packages:** `Dapplo.Windows.Clipboard`

```csharp
using Dapplo.Windows.Clipboard;
using System.IO;
using System.Reactive.Linq;

ClipboardNative.OnUpdate
    .Where(info => info.Formats.Contains("PNG"))
    .Subscribe(info =>
    {
        using var clipboard = ClipboardNative.Access();
        using var stream = clipboard.GetAsStream("PNG");
        var filename = $"clipboard_{DateTime.Now:yyyyMMdd_HHmmss}.png";
        using var file = File.Create(filename);
        stream.CopyTo(file);
        Console.WriteLine($"Saved image: {filename}");
    });
```

### Protect Sensitive Clipboard Data

**Packages:** `Dapplo.Windows.Clipboard`

```csharp
using Dapplo.Windows.Clipboard;

void CopyPassword(string password)
{
    using var clipboard = ClipboardNative.Access();
    clipboard.SetAsUnicodeString(password);
    clipboard.SetCloudClipboardOptions(
        canIncludeInHistory: false,
        canUploadToCloud:    false,
        excludeFromMonitoring: true);
}
```

---

## Text Expansion (AutoHotKey-style)

**Packages:** `Dapplo.Windows.Input`

```csharp
using Dapplo.Windows.Input.Keyboard;
using System.Collections.Generic;

var expansions = new Dictionary<string, string>
{
    ["btw"] = "by the way",
    ["brb"] = "be right back",
    ["thx"] = "thanks"
};

using var hook = KeyboardHook.Create();
string buffer = "";

hook.KeyboardEvents
    .Where(e => e.IsDown)
    .Subscribe(e =>
    {
        if (e.Key >= VirtualKeyCode.A && e.Key <= VirtualKeyCode.Z)
        {
            buffer += (char)('a' + (e.Key - VirtualKeyCode.A));
        }
        else if (e.Key == VirtualKeyCode.Space)
        {
            if (expansions.TryGetValue(buffer, out var expansion))
            {
                for (int i = 0; i < buffer.Length; i++)
                    KeyboardInputGenerator.KeyPress(VirtualKeyCode.Back);
                KeyboardInputGenerator.TypeText(expansion);
            }
            buffer = "";
        }
        else if (e.Key == VirtualKeyCode.Back && buffer.Length > 0)
        {
            buffer = buffer[..^1];
        }
        else
        {
            buffer = "";
        }
    });

Console.ReadLine();
```

---

## Global Hotkey with Timestamp Insertion

**Packages:** `Dapplo.Windows.Input`

Uses `TriggerOnKeyUp` to ensure modifier keys are released before typing, so the inserted text is not accidentally modified.

```csharp
using Dapplo.Windows.Input.Keyboard;

using var hook = KeyboardHook.Create();

var handler = new KeyCombinationHandler(
    VirtualKeyCode.Control,
    VirtualKeyCode.Menu,   // Alt
    VirtualKeyCode.LeftWin,
    VirtualKeyCode.T)
{
    TriggerOnKeyUp = true
};

hook.KeyboardEvents
    .Where(handler)
    .Subscribe(e =>
    {
        KeyboardInputGenerator.TypeText(DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
        e.Handled = true;
    });

Console.ReadLine();
```

---

## Window Tiling

**Packages:** `Dapplo.Windows`, `Dapplo.Windows.User32`

```csharp
using Dapplo.Windows.Desktop;
using Dapplo.Windows.Common.Structs;
using Dapplo.Windows.User32;
using System.Linq;

void TileAllVisibleWindows()
{
    var windows = InteropWindowQuery.GetTopLevelWindows()
        .Where(w => w.IsVisible() && w.IsAppWindow())
        .ToList();

    if (windows.Count == 0) return;

    var workArea = User32Api.GetWorkArea();
    int cols = (int)Math.Ceiling(Math.Sqrt(windows.Count));
    int rows = (int)Math.Ceiling(windows.Count / (double)cols);
    int w = workArea.Width  / cols;
    int h = workArea.Height / rows;

    for (int i = 0; i < windows.Count; i++)
    {
        int col = i % cols;
        int row = i / cols;
        windows[i].SetPlacement(new NativeRect(col * w, row * h, (col + 1) * w, (row + 1) * h));
    }
}
```

---

## DPI-Aware Application Bootstrap

**Packages:** `Dapplo.Windows`, `Dapplo.Windows.Dpi`

Combines window management with DPI handling for an application that looks crisp on any display:

```csharp
using Dapplo.Windows.Dpi;
using Dapplo.Windows.Dpi.Forms;
using System.Windows.Forms;

static class Program
{
    [STAThread]
    static void Main()
    {
        // Enable per-monitor v2 DPI awareness before any UI is created
        DpiHandler.SetProcessDpiAwareness(DpiAwarenessContext.PerMonitorAwareV2);

        Application.EnableVisualStyles();
        Application.SetCompatibleTextRenderingDefault(false);
        Application.Run(new MainForm());
    }
}

// Extend DpiAwareForm so fonts, bitmaps, and layout scale automatically
public class MainForm : DpiAwareForm
{
    public MainForm()
    {
        InitializeComponent();
    }
}
```

---

## Citrix-Aware Application

**Packages:** `Dapplo.Windows.Citrix`

Detect whether the app is running in a Citrix session and adapt behavior accordingly:

```csharp
using Dapplo.Windows.Citrix;

if (WinFrame.IsAvailable)
{
    var clientName    = WinFrame.QuerySessionInformation(InfoClasses.ClientName);
    var clientAddress = WinFrame.QuerySessionInformation(InfoClasses.ClientAddress);
    Console.WriteLine($"Citrix client: {clientName} ({clientAddress})");

    // Disable GPU-intensive features, reduce network calls, etc.
    EnableCitrixMode();
}
```

---

## Application with Automatic Restart (Installer Integration)

**Packages:** `Dapplo.Windows.AppRestartManager`

```csharp
using Dapplo.Windows.AppRestartManager;
using Dapplo.Windows.AppRestartManager.Enums;

static class Program
{
    [STAThread]
    static void Main(string[] args)
    {
        // Register so Windows Restart Manager can restart us after an update
        ApplicationRestartManager.RegisterForRestart("/restore");

        if (ApplicationRestartManager.WasRestartRequested())
            RestorePreviousState();

        var shutdownSub = ApplicationRestartManager.ListenForEndSession()
            .Subscribe(reason =>
            {
                if (reason.HasFlag(EndSessionReasons.ENDSESSION_CLOSEAPP))
                    SaveApplicationState();
            });

        Application.Run(new MainForm());

        shutdownSub.Dispose();
        ApplicationRestartManager.UnregisterForRestart();
    }
}
```

---

## Performance Tips

### Debounce High-Frequency Events

```csharp
// Window moves fire many events — debounce to 100 ms
WinEventHook.Create(WinEvents.EVENT_OBJECT_LOCATIONCHANGE)
    .Throttle(TimeSpan.FromMilliseconds(100))
    .Subscribe(e => UpdateLayout(e.Handle));
```

### Batch Clipboard Events

```csharp
ClipboardNative.OnUpdate
    .Buffer(TimeSpan.FromSeconds(5))
    .Where(batch => batch.Count > 0)
    .Subscribe(batch => SaveBatchToDatabase(batch));
```

### Load Only What You Need

```csharp
// Loading all window properties has a cost — request only what you need
window.Fill(InteropWindowRetrieveSettings.Caption | InteropWindowRetrieveSettings.Info);
```

## See Also

- [[Window-Management]]
- [[Clipboard]]
- [[Input-Handling]]
- [[DPI-Awareness]]
- [[Restart-Manager]]
