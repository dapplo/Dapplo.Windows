# Dapplo.Windows.Dialogs

The Windows file open, file save and folder picker dialogs (the Common Item Dialog) for .NET, through COM: no Windows
Forms or WPF needed. Targets `net480` and `net10.0-windows`.

This package is part of [Dapplo.Windows](https://github.com/dapplo/Dapplo.Windows). Full documentation:
[File and folder dialogs](https://www.dapplo.net/Dapplo.Windows/articles/dialogs.html), changes:
[changelog](https://github.com/dapplo/Dapplo.Windows/blob/master/CHANGELOG.md).

Namespace: `Dapplo.Windows.Dialogs`.

## Simple API

`FileDialog` returns `null` when the user cancels.

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

## Builders

`FileOpenDialogBuilder`, `FileSaveDialogBuilder` and `FolderPickerBuilder` return a `FileDialogResult` with
`WasCancelled`, `SelectedPath` and `SelectedPaths`.

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

## Errors and threading

Cancel is not an error. Real failures throw a `COMException`. The dialogs need an STA thread: the UI thread of
Windows Forms or WPF, `[STAThread]` on `Main` of a console application, or a dedicated STA thread. On any other thread
(e.g. `Task.Run`) `ShowDialog` throws an `InvalidOperationException` before any COM object is created.

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
