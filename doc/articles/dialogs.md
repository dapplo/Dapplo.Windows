# File and folder dialogs

**Dapplo.Windows.Dialogs** shows the Windows file open, file save and folder picker dialogs (the Common Item Dialog,
`IFileOpenDialog` / `IFileSaveDialog`) through COM. It needs neither Windows Forms nor WPF, so it also works in console
applications and other UI frameworks.

```powershell
dotnet add package Dapplo.Windows.Dialogs
```

Namespace: `Dapplo.Windows.Dialogs`.

## The simple API

`FileDialog` has one method per dialog. Each returns `null` when the user cancels and throws on real errors.

<!-- sample: DialogSamples.SimpleApi -->
```csharp
// Every FileDialog method returns null when the user cancels
string path = FileDialog.PickFileToOpen(
    title: "Open Document",
    initialDirectory: Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
    filters: new[] { ("Text files", "*.txt"), ("All files", "*.*") });
if (path != null)
{
    Console.WriteLine(File.ReadAllText(path));
}

string savePath = FileDialog.PickFileToSave(
    title: "Save Screenshot",
    suggestedFileName: "screenshot.png",
    filters: new[] { ("PNG Image", "*.png"), ("JPEG Image", "*.jpg") },
    defaultExtension: "png");

IReadOnlyList<string> files = FileDialog.PickFilesToOpen(filters: new[] { ("Images", "*.png;*.jpg;*.bmp") });

string folder = FileDialog.PickFolder(title: "Select Output Folder");
```

## The builders

For more options use `FileOpenDialogBuilder`, `FileSaveDialogBuilder` and `FolderPickerBuilder`. `FileDialog` uses
them too. `ShowDialog` returns a `FileDialogResult`: `WasCancelled`, `SelectedPath` and, for multiple selection,
`SelectedPaths`. Pass the handle of your window to `ShowDialog` to make the dialog modal for it.

<!-- sample: DialogSamples.OpenFile -->
```csharp
FileDialogResult result = new FileOpenDialogBuilder()
    .WithTitle("Open Configuration")
    .WithInitialDirectory(@"C:\ProgramData\MyApp")
    .AddFilter("Config files", "*.json;*.xml")
    .AddFilter("All files", "*.*")
    .WithDefaultExtension("json")
    // The dialog is modal for this window
    .ShowDialog(ownerWindowHandle);

if (result.WasCancelled)
{
    return;
}
string configPath = result.SelectedPath;
```

<!-- sample: DialogSamples.OpenFiles -->
```csharp
FileDialogResult result = new FileOpenDialogBuilder()
    .WithTitle("Import Images")
    .AddFilter("Images", "*.png;*.jpg;*.bmp;*.gif")
    .AllowMultipleSelection()
    .ShowDialog();

if (!result.WasCancelled)
{
    foreach (string file in result.SelectedPaths)
    {
        Console.WriteLine(file);
    }
}
```

<!-- sample: DialogSamples.SaveFile -->
```csharp
FileDialogResult result = new FileSaveDialogBuilder()
    .WithTitle("Export Report")
    .WithSuggestedFileName("report.pdf")
    .WithDefaultExtension("pdf")
    .AddFilter("PDF files", "*.pdf")
    .ShowDialog();

if (!result.WasCancelled)
{
    Console.WriteLine($"Export to {result.SelectedPath}");
}
```

<!-- sample: DialogSamples.PickFolder -->
```csharp
FileDialogResult result = new FolderPickerBuilder()
    .WithTitle("Select Backup Location")
    .WithInitialDirectory(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments))
    .ShowDialog();

if (!result.WasCancelled)
{
    Console.WriteLine($"Backup to {result.SelectedPath}");
}
```

`AddPlace` adds a folder to the navigation pane of this dialog:

<!-- sample: DialogSamples.AddPlace -->
```csharp
string projects = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "Projects");

FileDialogResult result = new FileOpenDialogBuilder()
    .WithTitle("Open Project")
    .AddFilter("Project files", "*.proj")
    // Pinned at the top of the navigation pane, only for this dialog
    .AddPlace(projects, atTop: true)
    .ShowDialog();
```

| Option | How |
|---|---|
| File types | `AddFilter("Images", "*.png;*.jpg")`, the first filter is selected |
| Default extension | `WithDefaultExtension("png")`, without the dot |
| Start folder | `WithInitialDirectory(path)`; without it Windows uses the folder the user used last |
| File name | `FileSaveDialogBuilder.WithSuggestedFileName("report.pdf")` |
| Several files | `FileOpenDialogBuilder.AllowMultipleSelection()` |
| Owner window | `ShowDialog(ownerHandle)` |

## Errors and threading

A cancel is not an error: `WasCancelled` is `true` (or the `FileDialog` method returns `null`). Real failures throw a
`COMException`.

<!-- sample: DialogSamples.Errors -->
```csharp
FileDialogResult result;
try
{
    result = new FileOpenDialogBuilder().AddFilter("All files", "*.*").ShowDialog();
}
catch (COMException ex)
{
    // Not a cancel: something went wrong, e.g. a shell extension failed
    Console.WriteLine($"The dialog failed: 0x{ex.ErrorCode:X8}");
    return;
}

if (result.WasCancelled)
{
    // Not an error, the user pressed Cancel or Escape
    return;
}
Console.WriteLine(result.SelectedPath);
```

The dialog must be shown on an STA thread. The UI threads of Windows Forms and WPF are STA. A console application
needs `[STAThread]` on `Main`, other threads need a dedicated STA thread:

<!-- sample: DialogSamples.StaThread -->
```csharp
// The dialog needs an STA thread. UI threads of WinForms and WPF are STA, a console application's Main needs [STAThread],
// from other threads use a dedicated STA thread:
string path = null;
var thread = new Thread(() => path = FileDialog.PickFileToOpen());
thread.SetApartmentState(ApartmentState.STA);
thread.Start();
thread.Join();
```

On any other thread `ShowDialog` (and the `FileDialog` methods) throw an `InvalidOperationException` which says so,
before any COM object is created. Thread pool threads (`Task.Run`, `await` continuations without a UI
`SynchronizationContext`) are always MTA:

<!-- sample: DialogSamples.StaCheck -->
```csharp
try
{
    // Task.Run uses a thread pool thread, which is MTA: the builder throws before any COM object is created
    await Task.Run(() => new FolderPickerBuilder().ShowDialog());
}
catch (InvalidOperationException ex)
{
    // "FolderPickerBuilder.ShowDialog must be called from an STA thread, but the current thread (...) is MTA. ..."
    Console.WriteLine(ex.Message);
}
```

## Not covered

The builders don't expose dialog events (`IFileDialog.Advise`), client GUIDs for separate "last folder" memory, custom
button labels, or save-dialog property stores. For those, use the COM interfaces directly.
