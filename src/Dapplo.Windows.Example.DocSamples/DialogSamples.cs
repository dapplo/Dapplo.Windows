// Copyright (c) Dapplo and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using Dapplo.Windows.Dialogs;

namespace Dapplo.Windows.Example.DocSamples;

/// <summary>
/// Samples for doc/articles/dialogs.md, src/Dapplo.Windows.Dialogs/README.md, wiki/Dialogs.md
/// </summary>
public static class DialogSamples
{
    public static void SimpleApi()
    {
        #region SimpleApi
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
        #endregion
    }

    public static void OpenFile(IntPtr ownerWindowHandle)
    {
        #region OpenFile
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
        #endregion
    }

    public static void OpenFiles()
    {
        #region OpenFiles
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
        #endregion
    }

    public static void SaveFile()
    {
        #region SaveFile
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
        #endregion
    }

    public static void PickFolder()
    {
        #region PickFolder
        FileDialogResult result = new FolderPickerBuilder()
            .WithTitle("Select Backup Location")
            .WithInitialDirectory(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments))
            .ShowDialog();

        if (!result.WasCancelled)
        {
            Console.WriteLine($"Backup to {result.SelectedPath}");
        }
        #endregion
    }

    public static void AddPlace()
    {
        #region AddPlace
        string projects = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "Projects");

        FileDialogResult result = new FileOpenDialogBuilder()
            .WithTitle("Open Project")
            .AddFilter("Project files", "*.proj")
            // Pinned at the top of the navigation pane, only for this dialog
            .AddPlace(projects, atTop: true)
            .ShowDialog();
        #endregion
    }

    public static void Errors()
    {
        #region Errors
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
        #endregion
    }

    public static void StaThread()
    {
        #region StaThread
        // The dialog needs an STA thread. UI threads of WinForms and WPF are STA, a console application's Main needs [STAThread],
        // from other threads use a dedicated STA thread:
        string path = null;
        var thread = new Thread(() => path = FileDialog.PickFileToOpen());
        thread.SetApartmentState(ApartmentState.STA);
        thread.Start();
        thread.Join();
        #endregion
    }

    public static async Task StaCheck()
    {
        #region StaCheck
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
        #endregion
    }
}
