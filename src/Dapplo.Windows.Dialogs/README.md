# Dapplo.Windows.Dialogs

Modern Windows file and folder picker dialogs via COM/P-Invoke — no WinForms or WPF dependency required.

Targets: `net480`, `netstandard2.0`, `net8.0-windows`, `net10.0-windows`

---

## Table of Contents

- [Overview](#overview)
- [Quick Start — Simple API](#quick-start--simple-api)
- [Builder Pattern](#builder-pattern)
  - [Open a single file](#open-a-single-file)
  - [Open multiple files](#open-multiple-files)
  - [Save a file](#save-a-file)
  - [Pick a folder](#pick-a-folder)
  - [Add custom sidebar locations](#add-custom-sidebar-locations)
- [Error Handling](#error-handling)
  - [Distinguishing Cancel from Errors](#distinguishing-cancel-from-errors)
  - [Threading — STA requirement](#threading--sta-requirement)
- [Power Features](#power-features)
- [Features Not Exposed by This Library](#features-not-exposed-by-this-library)

---

## Overview

`Dapplo.Windows.Dialogs` wraps the Windows **Common Item Dialog** (Vista+, `IFileOpenDialog` / `IFileSaveDialog`) in two layers:

| Layer | Type | When to use |
|---|---|---|
| **Simple API** | `FileDialog` (static) | One-liner for the most common cases |
| **Builder** | `FileOpenDialogBuilder`, `FileSaveDialogBuilder`, `FolderPickerBuilder` | When you need filters, suggested names, custom sidebar places, or multi-select |

---

## Quick Start — Simple API

All `FileDialog` methods return `null` on cancellation and throw on unexpected errors.

```csharp
using Dapplo.Windows.Dialogs;

// Open a file
string path = FileDialog.PickFileToOpen(
    title: "Open Document",
    initialDirectory: Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
    filters: new[] { ("Text files", "*.txt"), ("All files", "*.*") });

if (path != null)
    ProcessDocument(File.ReadAllText(path));

// Save a file
string savePath = FileDialog.PickFileToSave(
    title: "Save Screenshot",
    suggestedFileName: "screenshot.png",
    initialDirectory: Environment.GetFolderPath(Environment.SpecialFolder.MyPictures),
    filters: new[] { ("PNG Image", "*.png"), ("JPEG Image", "*.jpg") },
    defaultExtension: "png");

if (savePath != null)
    File.Copy(tempFile, savePath, overwrite: true);

// Pick multiple files
IReadOnlyList<string> files = FileDialog.PickFilesToOpen(
    filters: new[] { ("Images", "*.png;*.jpg;*.bmp") });

if (files != null)
    foreach (string file in files)
        ImportImage(file);

// Pick a folder
string folder = FileDialog.PickFolder(
    title: "Select Output Folder",
    initialDirectory: Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments));

if (folder != null)
    settings.OutputDirectory = folder;
```

---

## Builder Pattern

For more control, use the builder classes. They support the same options as the simple API plus
additional power features like custom sidebar locations. The simple `FileDialog` methods are implemented
on top of these builders — there is only one code path to the COM layer.

### Open a single file

```csharp
FileDialogResult result = new FileOpenDialogBuilder()
    .WithTitle("Open Configuration")
    .WithInitialDirectory(@"C:\ProgramData\MyApp")
    .AddFilter("Config files", "*.json;*.xml")
    .AddFilter("All files", "*.*")
    .WithDefaultExtension("json")
    .ShowDialog(ownerWindowHandle);

if (result.WasCancelled)
    return; // user pressed Cancel or Escape

string path = result.SelectedPath; // guaranteed non-null here
```

### Open multiple files

```csharp
FileDialogResult result = new FileOpenDialogBuilder()
    .WithTitle("Import Images")
    .AddFilter("Images", "*.png;*.jpg;*.bmp;*.gif")
    .AllowMultipleSelection()
    .ShowDialog();

if (!result.WasCancelled)
    foreach (string path in result.SelectedPaths)
        ImportImage(path);
```

### Save a file

```csharp
FileDialogResult result = new FileSaveDialogBuilder()
    .WithTitle("Export Report")
    .WithSuggestedFileName("report-2024-01.pdf")
    .WithDefaultExtension("pdf")
    .WithInitialDirectory(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments))
    .AddFilter("PDF files", "*.pdf")
    .AddFilter("All files", "*.*")
    .ShowDialog();

if (!result.WasCancelled)
    ExportReport(result.SelectedPath);
```

### Pick a folder

```csharp
FileDialogResult result = new FolderPickerBuilder()
    .WithTitle("Select Backup Location")
    .WithInitialDirectory(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments))
    .ShowDialog();

if (!result.WasCancelled)
    settings.BackupDirectory = result.SelectedPath;
```

### Add custom sidebar locations

Use `AddPlace` to pin application-specific directories in the dialog's navigation sidebar.

```csharp
string projectsDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "Projects");

FileDialogResult result = new FileOpenDialogBuilder()
    .WithTitle("Open Project File")
    .AddFilter("Project files", "*.proj")
    .AddPlace(projectsDir, atTop: true)   // pinned at the top of the sidebar
    .ShowDialog();
```

---

## Error Handling

### Distinguishing Cancel from Errors

There are three distinct outcomes when calling `ShowDialog()`:

| Outcome | Behaviour |
|---|---|
| **User cancelled** | `ShowDialog()` returns a `FileDialogResult` with `WasCancelled == true`. No exception. |
| **File selected** | `ShowDialog()` returns a `FileDialogResult` with `WasCancelled == false` and `SelectedPath` set. |
| **Unexpected failure** | `ShowDialog()` **throws** — `COMException` for COM errors, `PlatformNotSupportedException` on non-Windows. |

```csharp
FileDialogResult result;
try
{
    result = new FileOpenDialogBuilder()
        .AddFilter("All files", "*.*")
        .ShowDialog();
}
catch (System.Runtime.InteropServices.COMException ex)
{
    // Unexpected COM failure (e.g. shell extension crash).
    logger.LogError(ex, "File dialog failed with HRESULT 0x{Hr:X8}", ex.ErrorCode);
    return;
}
catch (PlatformNotSupportedException)
{
    // Running on Linux/macOS — fall back to a text-input field.
    return;
}

if (result.WasCancelled)
    return; // Not an error — user simply pressed Cancel.

ProcessFile(result.SelectedPath); // SelectedPath is guaranteed non-null here.
```

The simple `FileDialog` methods follow the same contract but return `null` for cancellations
instead of wrapping the outcome in a `FileDialogResult`.

### Threading — STA requirement

The Windows COM dialog must be shown from a **Single-Threaded Apartment (STA)** thread.
The main UI thread in WinForms and WPF applications is always STA.

In console applications or background threads, apply `[STAThread]` or marshal the call:

```csharp
// Option A: mark the entry point
[STAThread]
static void Main() { ... }

// Option B: run on a dedicated STA thread
string path = null;
var thread = new Thread(() =>
{
    path = FileDialog.PickFileToOpen();
});
thread.SetApartmentState(ApartmentState.STA);
thread.Start();
thread.Join();
```

If the dialog is called from an MTA thread, a `COMException` with `HRESULT 0x8001010D`
(`RPC_E_WRONG_THREAD`) will be thrown.

---

## Power Features

| Feature | How to use |
|---|---|
| Multiple file types | `.AddFilter("Images", "*.png;*.jpg")` — separate patterns with `;` |
| Default selected filter | The first filter added is always selected by default |
| Pre-filled filename | `FileSaveDialogBuilder.WithSuggestedFileName("output.csv")` |
| Custom sidebar location | `.AddPlace(@"C:\Projects", atTop: true)` |
| Multi-select | `FileOpenDialogBuilder.AllowMultipleSelection()` |
| Window ownership | Pass an `IntPtr` hwnd to `ShowDialog(ownerHandle)` |
| Folder picker | `FolderPickerBuilder` (uses `IFileOpenDialog` with `PickFolders` flag) |

---

## Features Not Exposed by This Library

The following `IFileDialog` capabilities are intentionally not surfaced through the builder or simple
API because they cover advanced or uncommon scenarios. If you need them, you can access the raw COM
interfaces directly via `Type.GetTypeFromCLSID` (see the Windows documentation links below).

| Feature | COM Method | Notes |
|---|---|---|
| Dialog event callbacks | `IFileDialog.Advise` / `Unadvise` | Receive events for folder change, file type change, user selection change, etc. |
| Per-client state persistence | `IFileDialog.SetClientGuid` / `ClearClientData` | Persist the last-used folder keyed to a GUID rather than the application |
| Save-dialog property collection | `IFileSaveDialog.SetProperties`, `GetProperties`, `ApplyProperties` | Embed file metadata (e.g. author, tags) in the saved file |
| Save-dialog pre-selection | `IFileSaveDialog.SetSaveAsItem` | Pre-select an existing file as the save target |
| Custom shell filter | `IFileDialog.SetFilter` | Deprecated since Windows 7; use `SetFileTypes` instead |
| `OK` button label | `IFileDialog.SetOkButtonLabel` | Change the label of the confirm button |
| File-name label | `IFileDialog.SetFileNameLabel` | Change the label of the file-name edit box |

```csharp
// Example: access IFileOpenDialog directly for advanced event handling
var clsid = new Guid("DC1C5A9C-E88A-4DDE-A5A1-60F82A20AEF7");
dynamic dialog = Activator.CreateInstance(Type.GetTypeFromCLSID(clsid));
// dialog is now a raw COM object — cast to the interface you need
```
