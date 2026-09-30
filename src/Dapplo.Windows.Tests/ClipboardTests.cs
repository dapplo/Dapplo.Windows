// Copyright (c) Dapplo and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reactive.Linq;
using System.Text;
using System.Threading.Tasks;
using Dapplo.Log;
using Dapplo.Log.XUnit;
using Dapplo.Windows.Clipboard;
using Dapplo.Windows.Messages;
using Xunit;

namespace Dapplo.Windows.Tests;

/// <summary>
/// All clipboard related tests
/// </summary>
/// <remarks>Interactive: these tests change the real desktop (input, clipboard or registry). They are excluded by default, run them with --filter Category=Interactive.</remarks>
[Trait("Category", "Interactive")]
public class ClipboardTests
{
    private static readonly LogSource Log = new LogSource();

    public ClipboardTests(ITestOutputHelper testOutputHelper)
    {
        LogSettings.RegisterDefaultLogger<XUnitLogger>(LogLevels.Verbose, testOutputHelper);
    }

    /// <summary>
    ///     Test delayed rendering of the clipboard, without any other subscription keeping the SharedMessageWindow busy
    /// </summary>
    [WpfFact]
    public async Task TestClipboardMonitor_DelayedRender()
    {
        var testString = "Hi";
        var formatToTestWith = "TEST_FORMAT_DELAYED";
        var formatId = ClipboardFormatExtensions.MapFormatToId(formatToTestWith);
        Log.Debug().WriteLine("Registered clipboard format {0} as {1}", formatToTestWith, formatId);
        int renderCount = 0;
        int? rendererThreadId = null;
        bool? renderAllFormats = null;

        // This is what is going to be called, as soon as the format is retrieved of the clipboard.
        // It runs on the SharedMessageWindow thread: only record here, the xUnit logger and asserts belong to the test thread.
        using var renderer = ClipboardNative.RegisterDelayedRenderer(formatId, request =>
        {
            rendererThreadId = Environment.CurrentManagedThreadId;
            renderAllFormats = request.RenderAllFormats;
            // Satisfy the request
            request.AccessToken.SetAsUnicodeString(testString, request.RequestedFormatId);
            renderCount++;
        });

        // Make the clipboard ready for testing
        using (var clipboardAccessToken = await ClipboardNative.AccessAsync())
        {
            Assert.True(clipboardAccessToken.CanAccess);
            Assert.False(clipboardAccessToken.IsLockTimeout);
            clipboardAccessToken.ClearContents();
            // Set delayed rendered content
            clipboardAccessToken.SetDelayedRenderedContent(formatId);
        }

        await Task.Delay(100);

        using (var clipboardAccessToken = await ClipboardNative.AccessAsync())
        {
            Assert.True(clipboardAccessToken.CanAccess);
            Assert.False(clipboardAccessToken.IsLockTimeout);
            Log.Debug().WriteLine("Test if the clipboard has our format {0} as {1}", formatToTestWith, formatId);
            Assert.True(ClipboardNative.HasFormat(formatToTestWith));
            // Request the missing content, this should trigger the rendering
            Log.Debug().WriteLine("Request the clipboard for our format {0} as {1}", formatToTestWith, formatId);
            var resultString = clipboardAccessToken.GetAsUnicodeString(formatToTestWith);
            Assert.Equal(testString, resultString);
        }
        Assert.Equal(1, renderCount);
        Assert.False(renderAllFormats);
        Assert.NotEqual(Environment.CurrentManagedThreadId, rendererThreadId);
    }

    /// <summary>
    ///     Delayed rendering without a registered renderer must fail early
    /// </summary>
    [WpfFact]
    public void TestClipboard_DelayedRender_WithoutRenderer_Throws()
    {
        var formatId = ClipboardFormatExtensions.MapFormatToId("TEST_FORMAT_NO_RENDERER");
        using var clipboardAccessToken = ClipboardNative.Access();
        Assert.True(clipboardAccessToken.CanAccess);
        clipboardAccessToken.ClearContents();
        Assert.Throws<InvalidOperationException>(() => clipboardAccessToken.SetDelayedRenderedContent(formatId));
    }

    /// <summary>
    ///     The access token can only be used on the thread which opened the clipboard
    /// </summary>
    [WpfFact]
    public async Task TestClipboardAccess_OtherThread_NoAccess()
    {
        using var clipboardAccessToken = ClipboardNative.Access();
        Assert.True(clipboardAccessToken.CanAccess);
        var canAccessOnOtherThread = await Task.Run(() => clipboardAccessToken.CanAccess);
        Assert.False(canAccessOnOtherThread);
        var exception = await Task.Run(() => Record.Exception(() => clipboardAccessToken.ThrowWhenNoAccess()));
        Assert.IsType<InvalidOperationException>(exception);
        Assert.True(clipboardAccessToken.CanAccess);
    }

    /// <summary>
    ///     OnUpdate must not need to open the clipboard, so it also works while the clipboard is open
    /// </summary>
    [WpfFact]
    public async Task TestClipboardMonitor_InitialValue_WhileClipboardIsOpen()
    {
        const string testString = "Dapplo.Windows.Tests.ClipboardTests.InitialValue";
        using (var clipboardAccessToken = ClipboardNative.Access())
        {
            clipboardAccessToken.ClearContents();
            clipboardAccessToken.SetAsUnicodeString(testString);
        }

        using (ClipboardNative.Access())
        {
            var clipboardUpdateInformation = await ClipboardNative.OnUpdate.FirstAsync();
            Assert.Contains((uint)StandardClipboardFormats.UnicodeText, clipboardUpdateInformation.FormatIds);
            Assert.Equal(ClipboardNative.SequenceNumber, clipboardUpdateInformation.Id);
        }
    }

    /// <summary>
    ///     Test the format mappers
    /// </summary>
    [WpfFact]
    public void TestClipboard_Formats()
    {
        Assert.Equal((uint)StandardClipboardFormats.DisplayBitmap, ClipboardFormatExtensions.MapFormatToId(StandardClipboardFormats.DisplayBitmap.AsString()));
    }

    /// <summary>
    ///     Test registering a clipboard format for the clipboard
    /// </summary>
    [WpfFact]
    public void TestClipboard_RegisterFormat()
    {
        string format = "DAPPLO.DOPY" + ClipboardNative.SequenceNumber;

        // Register the format
        var id1 = ClipboardFormatExtensions.RegisterFormat(format);
        // Register the format again
        var id2 = ClipboardFormatExtensions.RegisterFormat(format);

        Assert.Equal(id1, id2);

        // Make sure it works
        using (var clipboardAccessToken = ClipboardNative.Access())
        {
            Assert.True(clipboardAccessToken.CanAccess);
            Assert.False(clipboardAccessToken.IsLockTimeout);
            clipboardAccessToken.ClearContents();
            clipboardAccessToken.SetAsUnicodeString("Blub", format);
        }
    }

    /// <summary>
    ///     Test monitoring the clipboard
    /// </summary>
    [WpfFact]
    public async Task TestClipboardMonitor_Text()
    {
        const string testString = "Dapplo.Windows.Tests.ClipboardTests";
        var tcs = new TaskCompletionSource<ClipboardUpdateInformation>(TaskCreationOptions.RunContinuationsAsynchronously);
        // The updates are published on the SharedMessageWindow thread, only complete the task there
        using var subscription = ClipboardNative.OnUpdate.Skip(1).Where(clipboard => clipboard.Formats.Contains("TEST_FORMAT")).Subscribe(clipboard => tcs.TrySetResult(clipboard));
        using (var clipboardAccessToken = await ClipboardNative.AccessAsync())
        {
            Assert.True(clipboardAccessToken.CanAccess);
            Assert.False(clipboardAccessToken.IsLockTimeout);
            clipboardAccessToken.ClearContents();
            clipboardAccessToken.SetAsUnicodeString(testString, "TEST_FORMAT");
        }

        var completed = await Task.WhenAny(tcs.Task, Task.Delay(2000));
        Assert.Same(tcs.Task, completed);
        var clipboardUpdateInformation = await tcs.Task;
        Log.Debug().WriteLine("Detected change {0}", string.Join(",", clipboardUpdateInformation.Formats));
        Log.Debug().WriteLine("Owner {0}", clipboardUpdateInformation.OwnerHandle);
        Log.Debug().WriteLine("Sequence {0}", clipboardUpdateInformation.Id);
        Assert.Equal(SharedMessageWindow.Handle, (nint)clipboardUpdateInformation.OwnerHandle);
    }

    /// <summary>
    ///     Test that file names longer than MAX_PATH are not truncated
    /// </summary>
    [WpfFact]
    public void TestClipboard_SetFileNames_LongPath()
    {
        var longFile = @"C:\" + string.Join(@"\", Enumerable.Repeat(new string('a', 50), 8)) + @"\file.txt";
        Assert.True(longFile.Length > 260);

        using (var clipboardAccessToken = ClipboardNative.Access())
        {
            clipboardAccessToken.ClearContents();
            clipboardAccessToken.SetFileNames(new[] { longFile });
        }

        using (var clipboardAccessToken = ClipboardNative.Access())
        {
            var result = clipboardAccessToken.GetFileNames().ToList();
            Assert.Single(result);
            Assert.Equal(longFile, result[0]);
        }
    }

    /// <summary>
    ///     Test monitoring the clipboard
    /// </summary>
    /// <returns></returns>
    [WpfFact]
    public async Task TestClipboardStore_String()
    {

        const string testString = "Dapplo.Windows.Tests.ClipboardTests";
        using (var clipboardAccessToken = await ClipboardNative.AccessAsync())
        {
            clipboardAccessToken.ClearContents();
            clipboardAccessToken.SetAsUnicodeString(testString);
        }
        await Task.Delay(100);
        using (var clipboardAccessToken = await ClipboardNative.AccessAsync())
        {
            Assert.Equal(testString, clipboardAccessToken.GetAsUnicodeString());
        }
    }


    /// <summary>
    ///     Test setting file names on the clipboard and reading them back
    /// </summary>
    [WpfFact]
    public async Task TestClipboard_SetFileNames()
    {
        // Note: DROPFILES stores file name strings in the clipboard buffer without
        // needing the files to actually exist on disk. The paths are stored as-is.
        var testFiles = new List<string>
        {
            @"C:\path\to\file1.txt",
            @"C:\path\to\file2.txt"
        };

        using (var clipboardAccessToken = await ClipboardNative.AccessAsync())
        {
            Assert.True(clipboardAccessToken.CanAccess);
            Assert.False(clipboardAccessToken.IsLockTimeout);
            clipboardAccessToken.ClearContents();
            clipboardAccessToken.SetFileNames(testFiles);
        }

        await Task.Delay(100);

        using (var clipboardAccessToken = await ClipboardNative.AccessAsync())
        {
            Assert.True(clipboardAccessToken.CanAccess);
            Assert.False(clipboardAccessToken.IsLockTimeout);
            var result = clipboardAccessToken.GetFileNames().ToList();
            Assert.Equal(testFiles.Count, result.Count);
            Assert.Equal(testFiles[0], result[0]);
            Assert.Equal(testFiles[1], result[1]);
        }
    }

    /// <summary>
    ///     Test monitoring the clipboard
    /// </summary>
    /// <returns></returns>
    [WpfFact]
    public async Task TestClipboardStore_MemoryStream()
    {
        const string testString = "Dapplo.Windows.Tests.ClipboardTests";
        var testStream = new MemoryStream();
        var bytes = Encoding.Unicode.GetBytes(testString + "\0");
        Assert.Equal(testString, Encoding.Unicode.GetString(bytes).TrimEnd('\0'));
        testStream.Write(bytes, 0, bytes.Length);

        testStream.Seek(0, SeekOrigin.Begin);
        Assert.Equal(testString, Encoding.Unicode.GetString(testStream.GetBuffer(), 0, (int)testStream.Length).TrimEnd('\0'));

        using (var clipboardAccessToken = await ClipboardNative.AccessAsync())
        {
            Assert.True(clipboardAccessToken.CanAccess);
            Assert.False(clipboardAccessToken.IsLockTimeout);
            clipboardAccessToken.ClearContents();
            clipboardAccessToken.SetAsStream(StandardClipboardFormats.UnicodeText, testStream);
        }
        await Task.Delay(100);
        using (var clipboardAccessToken = await ClipboardNative.AccessAsync())
        {
            Assert.True(clipboardAccessToken.CanAccess);
            Assert.False(clipboardAccessToken.IsLockTimeout);
            Assert.Equal(testString, clipboardAccessToken.GetAsUnicodeString());
            var unicodeBytes = clipboardAccessToken.GetAsBytes(StandardClipboardFormats.UnicodeText);
            Assert.Equal(testString, Encoding.Unicode.GetString(unicodeBytes, 0, unicodeBytes.Length).TrimEnd('\0'));

            using var unicodeStream = clipboardAccessToken.GetAsStream(StandardClipboardFormats.UnicodeText);
            using var memoryStream = new MemoryStream();
            unicodeStream.CopyTo(memoryStream);
            Assert.Equal(testString, Encoding.Unicode.GetString(memoryStream.GetBuffer(), 0, (int)memoryStream.Length).TrimEnd('\0'));
        }
    }

    /// <summary>
    ///     Test AccessAsync
    /// </summary>
    [WpfFact]
    public async Task Test_ClipboardAccess_LockTimeout()
    {
        using (var outerClipboardAccessToken = await ClipboardNative.AccessAsync())
        {
            Assert.True(outerClipboardAccessToken.CanAccess);
            using (var clipboardAccessToken = await ClipboardNative.AccessAsync())
            {
                Assert.True(clipboardAccessToken.IsLockTimeout);
            }
        }
    }

    /// <summary>
    ///     Test AccessAsync
    /// </summary>
    [WpfFact]
    public async Task Test_ClipboardAccess_LockTimeout_Exception()
    {
        using (await ClipboardNative.AccessAsync())
        {
            using var clipboardAccessToken = await ClipboardNative.AccessAsync();
            Assert.Throws<ClipboardAccessDeniedException>(() => clipboardAccessToken.ThrowWhenNoAccess());
        }
    }

    /// <summary>
    ///     Test setting cloud clipboard options
    /// </summary>
    [WpfFact]
    public async Task TestCloudClipboard_SetOptions()
    {
        const string testString = "Cloud clipboard test";
        using (var clipboardAccessToken = await ClipboardNative.AccessAsync())
        {
            Assert.True(clipboardAccessToken.CanAccess);
            Assert.False(clipboardAccessToken.IsLockTimeout);
            clipboardAccessToken.ClearContents();
            clipboardAccessToken.SetAsUnicodeString(testString);
            
            // Set cloud clipboard options
            clipboardAccessToken.SetCloudClipboardOptions(
                canIncludeInHistory: false,
                canUploadToCloud: false,
                excludeFromMonitoring: true
            );
        }

        await Task.Delay(100);

        // Verify the formats were set
        using (var clipboardAccessToken = await ClipboardNative.AccessAsync())
        {
            Assert.True(clipboardAccessToken.CanAccess);
            Assert.False(clipboardAccessToken.IsLockTimeout);
            
            var formats = clipboardAccessToken.AvailableFormats().ToList();
            Assert.Contains(ClipboardCloudExtensions.CanIncludeInClipboardHistoryFormat, formats);
            Assert.Contains(ClipboardCloudExtensions.CanUploadToCloudClipboardFormat, formats);
            Assert.Contains(ClipboardCloudExtensions.ExcludeClipboardContentFromMonitorProcessingFormat, formats);
            
            // Verify the text is still there
            Assert.Equal(testString, clipboardAccessToken.GetAsUnicodeString());
        }
    }

    /// <summary>
    ///     Test setting individual cloud clipboard options
    /// </summary>
    [WpfFact]
    public async Task TestCloudClipboard_SetIndividualOptions()
    {
        const string testString = "Individual cloud clipboard test";
        using (var clipboardAccessToken = await ClipboardNative.AccessAsync())
        {
            Assert.True(clipboardAccessToken.CanAccess);
            clipboardAccessToken.ClearContents();
            clipboardAccessToken.SetAsUnicodeString(testString);
            
            // Set individual options
            clipboardAccessToken.SetCanIncludeInClipboardHistory(false);
            clipboardAccessToken.SetCanUploadToCloudClipboard(true);
            clipboardAccessToken.ExcludeFromMonitorProcessing();
        }

        await Task.Delay(100);

        // Verify the formats were set
        using (var clipboardAccessToken = await ClipboardNative.AccessAsync())
        {
            var formats = clipboardAccessToken.AvailableFormats().ToList();
            Assert.Contains(ClipboardCloudExtensions.CanIncludeInClipboardHistoryFormat, formats);
            Assert.Contains(ClipboardCloudExtensions.CanUploadToCloudClipboardFormat, formats);
            Assert.Contains(ClipboardCloudExtensions.ExcludeClipboardContentFromMonitorProcessingFormat, formats);
        }
    }

    /// <summary>
    ///     Test setting cloud clipboard options with default values
    /// </summary>
    [WpfFact]
    public async Task TestCloudClipboard_DefaultOptions()
    {
        const string testString = "Default cloud clipboard test";
        using (var clipboardAccessToken = await ClipboardNative.AccessAsync())
        {
            clipboardAccessToken.ClearContents();
            clipboardAccessToken.SetAsUnicodeString(testString);
            
            // Use default values: nothing is specified, so no format may be placed (the Windows defaults apply)
            clipboardAccessToken.SetCloudClipboardOptions();
        }

        await Task.Delay(100);

        // Verify no formats were set, especially not the exclusion format which would opt-out of history and cloud sync
        using (var clipboardAccessToken = await ClipboardNative.AccessAsync())
        {
            var formats = clipboardAccessToken.AvailableFormats().ToList();
            Assert.DoesNotContain(ClipboardCloudExtensions.CanIncludeInClipboardHistoryFormat, formats);
            Assert.DoesNotContain(ClipboardCloudExtensions.CanUploadToCloudClipboardFormat, formats);
            Assert.DoesNotContain(ClipboardCloudExtensions.ExcludeClipboardContentFromMonitorProcessingFormat, formats);
            Assert.Equal(testString, clipboardAccessToken.GetAsUnicodeString());
        }
    }

    /// <summary>
    ///     Allowing history and cloud must not place the exclusion format
    /// </summary>
    [WpfFact]
    public async Task TestCloudClipboard_AllowOptions()
    {
        const string testString = "Allow cloud clipboard test";
        using (var clipboardAccessToken = await ClipboardNative.AccessAsync())
        {
            clipboardAccessToken.ClearContents();
            clipboardAccessToken.SetAsUnicodeString(testString);
            clipboardAccessToken.SetCloudClipboardOptions(canIncludeInHistory: true, canUploadToCloud: true);
        }

        await Task.Delay(100);

        using (var clipboardAccessToken = await ClipboardNative.AccessAsync())
        {
            var formats = clipboardAccessToken.AvailableFormats().ToList();
            Assert.Contains(ClipboardCloudExtensions.CanIncludeInClipboardHistoryFormat, formats);
            Assert.Contains(ClipboardCloudExtensions.CanUploadToCloudClipboardFormat, formats);
            Assert.DoesNotContain(ClipboardCloudExtensions.ExcludeClipboardContentFromMonitorProcessingFormat, formats);
            Assert.Equal(1, BitConverter.ToInt32(clipboardAccessToken.GetAsBytes(ClipboardCloudExtensions.CanIncludeInClipboardHistoryFormat), 0));
        }
    }

    /// <summary>
    ///     Test TryGetAsStream with available format
    /// </summary>
    [WpfFact]
    public async Task TestClipboardTryGetAsStream_Success()
    {
        const string testString = "Dapplo.Windows.Tests.TryGetAsStream";
        var testStream = new MemoryStream();
        var bytes = Encoding.Unicode.GetBytes(testString + "\0");
        testStream.Write(bytes, 0, bytes.Length);
        testStream.Seek(0, SeekOrigin.Begin);

        using (var clipboardAccessToken = await ClipboardNative.AccessAsync())
        {
            Assert.True(clipboardAccessToken.CanAccess);
            clipboardAccessToken.ClearContents();
            clipboardAccessToken.SetAsStream(StandardClipboardFormats.UnicodeText, testStream);
        }
        
        await Task.Delay(100);
        
        using (var clipboardAccessToken = await ClipboardNative.AccessAsync())
        {
            Assert.True(clipboardAccessToken.CanAccess);
            
            // Try to get the stream - should succeed
            bool success = clipboardAccessToken.TryGetAsStream(StandardClipboardFormats.UnicodeText, out var stream);
            Assert.True(success);
            Assert.NotNull(stream);
            
            using var memoryStream = new MemoryStream();
            using (stream)
            {
                stream.CopyTo(memoryStream);
            }
            var resultString = Encoding.Unicode.GetString(memoryStream.GetBuffer(), 0, (int)memoryStream.Length).TrimEnd('\0');
            Assert.Equal(testString, resultString);
        }
    }

    /// <summary>
    ///     Test TryGetAsStream with unavailable format
    /// </summary>
    [WpfFact]
    public async Task TestClipboardTryGetAsStream_Failure()
    {
        using (var clipboardAccessToken = await ClipboardNative.AccessAsync())
        {
            Assert.True(clipboardAccessToken.CanAccess);
            clipboardAccessToken.ClearContents();
        }
        
        await Task.Delay(100);
        
        using (var clipboardAccessToken = await ClipboardNative.AccessAsync())
        {
            Assert.True(clipboardAccessToken.CanAccess);
            
            // Try to get a non-existent format - should fail gracefully
            bool success = clipboardAccessToken.TryGetAsStream(StandardClipboardFormats.UnicodeText, out var stream);
            Assert.False(success);
            Assert.Null(stream);
        }
    }

    /// <summary>
    ///     Test TryGetAsStream with custom format
    /// </summary>
    [WpfFact]
    public async Task TestClipboardTryGetAsStream_CustomFormat()
    {
        const string customFormat = "CUSTOM_TEST_FORMAT";
        const string testString = "Custom format test data";
        var testStream = new MemoryStream();
        var bytes = Encoding.UTF8.GetBytes(testString);
        testStream.Write(bytes, 0, bytes.Length);
        testStream.Seek(0, SeekOrigin.Begin);

        using (var clipboardAccessToken = await ClipboardNative.AccessAsync())
        {
            Assert.True(clipboardAccessToken.CanAccess);
            clipboardAccessToken.ClearContents();
            clipboardAccessToken.SetAsStream(customFormat, testStream);
        }
        
        await Task.Delay(100);
        
        using (var clipboardAccessToken = await ClipboardNative.AccessAsync())
        {
            Assert.True(clipboardAccessToken.CanAccess);
            
            // Try to get with correct format - should succeed
            bool success = clipboardAccessToken.TryGetAsStream(customFormat, out var stream);
            Assert.True(success);
            Assert.NotNull(stream);
            
            using var memoryStream = new MemoryStream();
            using (stream)
            {
                stream.CopyTo(memoryStream);
            }
            var resultString = Encoding.UTF8.GetString(memoryStream.GetBuffer(), 0, (int)memoryStream.Length);
            Assert.Equal(testString, resultString);
            
            // Try to get with wrong format - should fail
            success = clipboardAccessToken.TryGetAsStream("WRONG_FORMAT", out stream);
            Assert.False(success);
            Assert.Null(stream);
        }
    }
}
