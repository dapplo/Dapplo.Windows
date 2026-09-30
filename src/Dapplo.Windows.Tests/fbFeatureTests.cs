// Copyright (c) Dapplo and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Windows.Forms;
using Dapplo.Windows.Clipboard;
using Dapplo.Windows.Dialogs;
using Dapplo.Windows.Messages;
using Xunit;

namespace Dapplo.Windows.Tests;

/// <summary>
/// Tests for SharedMessageWindow.Shutdown, the ClipboardContents builder and the STA check of the dialogs.
/// These don't change the clipboard or the desktop, they only destroy and recreate the hidden SharedMessageWindow.
/// </summary>
public class FbFeatureTests
{
    [DllImport("user32")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool IsWindow(nint hWnd);

    [Fact]
    public void Shutdown_DestroysWindow_AndNextUseCreatesANewOne()
    {
        var oldHandle = SharedMessageWindow.Handle;
        Assert.True(IsWindow(oldHandle));

        Assert.True(SharedMessageWindow.Shutdown(TimeSpan.FromSeconds(5)));
        Assert.False(IsWindow(oldHandle));
        Assert.False(SharedMessageWindow.IsProcessExiting);

        // Not exiting: the next use creates a new window, which works
        var newHandle = SharedMessageWindow.Handle;
        Assert.True(IsWindow(newHandle));
        var invoked = false;
        SharedMessageWindow.Invoke(hwnd => invoked = hwnd == newHandle && SharedMessageWindow.IsWindowThread);
        Assert.True(invoked);
    }

    [Fact]
    public void Shutdown_OnTheWindowThread_DestroysTheWindow()
    {
        var oldHandle = SharedMessageWindow.Handle;
        var result = false;
        SharedMessageWindow.Invoke(_ => result = SharedMessageWindow.Shutdown());
        Assert.True(result);
        Assert.False(IsWindow(oldHandle));
        Assert.True(IsWindow(SharedMessageWindow.Handle));
    }

    [Fact]
    public void Shutdown_ListenTeardownIsNotCalledForTheDestroyedWindow()
    {
        var setupCount = 0;
        var teardownCount = 0;
        var subscription = SharedMessageWindow.Listen(_ => setupCount++, _ => teardownCount++).Subscribe(_ => { });
        Assert.Equal(1, setupCount);

        Assert.True(SharedMessageWindow.Shutdown(TimeSpan.FromSeconds(5)));
        subscription.Dispose();

        // The registrations died with the window: no teardown, and no new window just for the teardown
        Assert.Equal(0, teardownCount);

        // A normal subscribe / dispose on the new window still calls both
        using (SharedMessageWindow.Listen(_ => setupCount++, _ => teardownCount++).Subscribe(_ => { }))
        {
            Assert.Equal(2, setupCount);
        }
        Assert.Equal(1, teardownCount);
    }

    [Fact]
    public void Shutdown_NegativeTimeout_Throws()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => SharedMessageWindow.Shutdown(TimeSpan.FromSeconds(-1)));
        Assert.Throws<ArgumentOutOfRangeException>(() => SharedMessageWindow.ProcessExitShutdownTimeout = TimeSpan.FromSeconds(-1));
    }

    [Fact]
    public void ClipboardContents_KeepsTheOrder_AndRejectsDuplicateFormats()
    {
        var contents = new ClipboardContents()
            .AddUnicodeString("Hello")
            .AddBytes(Encoding.UTF8.GetBytes("{}"), "Dapplo.Windows.Tests.FbFeatureTests.Json")
            .AddFileNames(new[] { @"C:\Temp\file.txt" })
            .ExcludeFromMonitorProcessing();

        var jsonFormatId = ClipboardFormatExtensions.MapFormatToId("Dapplo.Windows.Tests.FbFeatureTests.Json");
        Assert.Equal(new[] { (uint)StandardClipboardFormats.UnicodeText, jsonFormatId, (uint)StandardClipboardFormats.Drop }, contents.FormatIds);

        Assert.Throws<ArgumentException>(() => contents.AddUnicodeString("Again"));
        Assert.Throws<ArgumentException>(() => contents.AddDelayedRendered(jsonFormatId));
        Assert.Throws<ArgumentNullException>(() => contents.AddBytes(null, "PNG"));
        Assert.Throws<ArgumentOutOfRangeException>(() => contents.AddBytes(new byte[1], 0u));
    }

    [Fact]
    public void Dialogs_OnAnMtaThread_ThrowBeforeCreatingComObjects()
    {
        var exceptions = new List<Exception>();
        var thread = new Thread(() =>
        {
            foreach (var show in new Func<FileDialogResult>[]
                     {
                         () => new FileOpenDialogBuilder().ShowDialog(),
                         () => new FileSaveDialogBuilder().ShowDialog(),
                         () => new FolderPickerBuilder().ShowDialog()
                     })
            {
                try
                {
                    show();
                }
                catch (Exception ex)
                {
                    exceptions.Add(ex);
                }
            }
            try
            {
                Dapplo.Windows.Dialogs.FileDialog.PickFolder();
            }
            catch (Exception ex)
            {
                exceptions.Add(ex);
            }
        });
        thread.SetApartmentState(ApartmentState.MTA);
        thread.Start();
        Assert.True(thread.Join(TimeSpan.FromSeconds(10)), "A dialog was shown on the MTA thread");

        Assert.Equal(4, exceptions.Count);
        Assert.All(exceptions, ex =>
        {
            var invalidOperationException = Assert.IsType<InvalidOperationException>(ex);
            Assert.Contains("STA thread", invalidOperationException.Message);
        });
    }
}

/// <summary>
/// Tests which change the real clipboard.
/// </summary>
/// <remarks>Interactive: these tests change the clipboard. They are excluded by default, run them with --filter Category=Interactive.</remarks>
[Trait("Category", "Interactive")]
public class FbInteractiveFeatureTests
{
    [DllImport("user32")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool IsWindow(nint hWnd);

    [Fact]
    public void DelayedRendering_IsRenderedWhenTheWindowShutsDown()
    {
        var formatId = ClipboardFormatExtensions.RegisterFormat("Dapplo.Windows.Tests.FbFeatureTests.Delayed");
        var expected = Guid.NewGuid().ToString();
        var renderAllFormats = new List<bool>();
        using (ClipboardNative.RegisterDelayedRenderer(formatId, request =>
               {
                   lock (renderAllFormats)
                   {
                       renderAllFormats.Add(request.RenderAllFormats);
                   }
                   request.AccessToken.SetAsUnicodeString(expected, request.RequestedFormatId);
               }))
        {
            ClipboardNative.ReplaceContents(new ClipboardContents().AddDelayedRendered(formatId));
            var oldHandle = SharedMessageWindow.Handle;
            Assert.True(ClipboardNative.HasFormat(formatId));

            // What happens at process exit: the window is destroyed, Windows sends WM_RENDERALLFORMATS
            Assert.True(SharedMessageWindow.Shutdown(TimeSpan.FromSeconds(5)));
            Assert.False(IsWindow(oldHandle));
        }

        // The renderer is gone, and so is its window: the data must be on the clipboard now
        lock (renderAllFormats)
        {
            // A clipboard monitor (e.g. the clipboard history) might have requested the format before (WM_RENDERFORMAT)
            Assert.NotEmpty(renderAllFormats);
        }
        using var clipboard = ClipboardNative.Access();
        Assert.True(clipboard.CanAccess);
        Assert.Equal(expected, clipboard.GetAsUnicodeString(formatId));
    }

    [Fact]
    public void ReplaceContents_PlacesAllFormats_AndAddToCurrentContentsAddsOne()
    {
        var jsonFormat = "Dapplo.Windows.Tests.FbFeatureTests.Json";
        ClipboardNative.ReplaceContents(new ClipboardContents()
            .AddUnicodeString("Hello")
            .AddBytes(Encoding.UTF8.GetBytes("{\"a\":1}"), jsonFormat));

        using (var clipboard = ClipboardNative.Access())
        {
            clipboard.AddToCurrentContents(new ClipboardContents().AddUnicodeString("<b>Hello</b>", "Dapplo.Windows.Tests.FbFeatureTests.Html"));
        }

        using (var clipboard = ClipboardNative.Access())
        {
            Assert.Equal("Hello", clipboard.GetAsUnicodeString());
            Assert.StartsWith("{\"a\":1}", Encoding.UTF8.GetString(clipboard.GetAsBytes(jsonFormat)));
            Assert.Equal("<b>Hello</b>", clipboard.GetAsUnicodeString("Dapplo.Windows.Tests.FbFeatureTests.Html"));
        }
    }

    [Fact]
    public void SetOnContentOfAnotherWindow_Throws_ReplaceContentsWorks()
    {
        var otherWindow = new NativeWindow();
        otherWindow.CreateHandle(new CreateParams());
        try
        {
            // Content owned by another window
            using (var clipboard = ClipboardNative.Access(otherWindow.Handle))
            {
                Assert.True(clipboard.CanAccess);
                clipboard.ClearContents();
                clipboard.SetAsUnicodeString("Owned by another window");
            }

            using (var clipboard = ClipboardNative.Access())
            {
                Assert.True(clipboard.CanAccess);
                Assert.Throws<InvalidOperationException>(() => clipboard.SetAsUnicodeString("Mixed"));
                Assert.Throws<InvalidOperationException>(() => clipboard.AddToCurrentContents(new ClipboardContents().AddUnicodeString("Mixed")));
                // The content is unchanged
                Assert.Equal("Owned by another window", clipboard.GetAsUnicodeString());

                clipboard.ReplaceContents(new ClipboardContents().AddUnicodeString("Replaced"));
                Assert.True(SharedMessageWindow.Handle == ClipboardNative.CurrentOwner, "The SharedMessageWindow must own the replaced content");
            }

            using (var clipboard = ClipboardNative.Access())
            {
                Assert.Equal("Replaced", clipboard.GetAsUnicodeString());
            }
        }
        finally
        {
            otherWindow.DestroyHandle();
        }
    }
}
