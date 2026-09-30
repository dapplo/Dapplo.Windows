# Dialogs

Package **Dapplo.Windows.Dialogs**: the Windows file open, file save and folder picker dialogs, without Windows Forms
or WPF. Full version: [File and folder dialogs](https://www.dapplo.net/Dapplo.Windows/articles/dialogs.html).

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

A cancel is not an error (`WasCancelled`, or `null` from `FileDialog`); real failures throw a `COMException`. The
dialogs need an STA thread, on any other thread (e.g. `Task.Run`) they throw an `InvalidOperationException` before any
COM object is created:

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
