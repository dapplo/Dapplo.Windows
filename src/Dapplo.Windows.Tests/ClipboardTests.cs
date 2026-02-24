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
public class ClipboardTests  : IDisposable
{
    private static readonly LogSource Log = new LogSource();
    private readonly IDisposable subscription;

    public ClipboardTests(ITestOutputHelper testOutputHelper)
    {
        LogSettings.RegisterDefaultLogger<XUnitLogger>(LogLevels.Verbose, testOutputHelper);
        subscription = SharedMessageWindow.Listen().Subscribe();
    }

    /// <summary>
    ///     Test monitoring the clipboard
    /// </summary>
    //[WpfFact]
    public async Task TestClipboardMonitor_WaitForCopy()
    {
        var tcs = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        var subscription = ClipboardNative.OnUpdate.Skip(1).Subscribe(clipboardUpdateInformation =>
        {
            Log.Debug().WriteLine("Formats {0}", string.Join(",", clipboardUpdateInformation.Formats));
            Log.Debug().WriteLine("Owner {0}", clipboardUpdateInformation.OwnerHandle);
            Log.Debug().WriteLine("Sequence {0}", clipboardUpdateInformation.Id);

            if (clipboardUpdateInformation.Formats.Contains("PNG"))
            {
                using var clipboard = ClipboardNative.Access();
                using var stream = clipboard.GetAsStream("PNG");
                using var fileStream = File.Create(@"c:\projects\test.png");
                stream.CopyTo(fileStream);
            }

            tcs.TrySetResult(true);
        });

        await tcs.Task;

        subscription.Dispose();
    }

    /// <summary>
    ///     Test delayed rendering of the clipboard
    /// </summary>
    //[WpfFact]
    public async Task TestClipboardMonitor_DelayedRender()
    {
        var testString = "Hi";
        var tcs = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);

        // This is what is going to be called, as soon as the format is retrieved of the clipboard
        var subscription = ClipboardNative.OnRenderFormat.Subscribe(clipboardRenderFormatRequest =>
        {
            switch (clipboardRenderFormatRequest)
            {
                case {IsDestroyClipboard: true}:
                    Log.Debug().WriteLine("Destroy clipboard");
                    tcs.TrySetResult(false);
                    break;
                case {RenderAllFormats: true}:
                    Log.Debug().WriteLine("Render all formats");
                    tcs.TrySetResult(false);
                    break;
                default:
                    Log.Debug().WriteLine("Got request render {0} ({1}) to the clipboard", clipboardRenderFormatRequest.RequestedFormat, clipboardRenderFormatRequest.RequestedFormatId);
                    // Satisfy the request
                    clipboardRenderFormatRequest.AccessToken.SetAsUnicodeString(testString, clipboardRenderFormatRequest.RequestedFormat);
                    tcs.TrySetResult(true);
                    break;
            }
        });

        var formatToTestWith = "TEST_FORMAT";
        var formatId = ClipboardFormatExtensions.MapFormatToId(formatToTestWith);
        Log.Debug().WriteLine("Registered clipboard format {0} as {1}", formatToTestWith, formatId);
        // Make the clipboard ready for testing
        using (var clipboardAccessToken = await ClipboardNative.AccessAsync())
        {
            Assert.True(clipboardAccessToken.CanAccess);
            Assert.False(clipboardAccessToken.IsLockTimeout);
            clipboardAccessToken.ClearContents();
            // Set delayed rendered content
            clipboardAccessToken.SetDelayedRenderedContent(formatId);
        }

        await Task.Delay(200);

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
        var result = await tcs.Task;
        Assert.True(result);
        subscription.Dispose();
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
    //[WpfFact]
    public async Task TestClipboardMonitor_Text()
    {
        const string testString = "Dapplo.Windows.Tests.ClipboardTests";
        bool hasNewContent = false;
        var subscription = ClipboardNative.OnUpdate.Where(clipboard => clipboard.Formats.Contains("TEST_FORMAT")).Subscribe(clipboard =>
        {
            Log.Debug().WriteLine("Detected change {0}", string.Join(",", clipboard.Formats));
            Log.Debug().WriteLine("Owner {0}", clipboard.OwnerHandle);
            Log.Debug().WriteLine("Sequence {0}", clipboard.Id);

            hasNewContent = true;
        });
        using (var clipboardAccessToken = await ClipboardNative.AccessAsync())
        {
            Assert.True(clipboardAccessToken.CanAccess);
            Assert.False(clipboardAccessToken.IsLockTimeout);
            clipboardAccessToken.ClearContents();
            clipboardAccessToken.SetAsUnicodeString(testString, "TEST_FORMAT");
        }
        await Task.Delay(400);
        subscription.Dispose();

        // Doesn't work on AppVeyor!!
        Assert.True(hasNewContent);
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
    ///     Test if the clipboard contains files
    /// </summary>
    /// <returns></returns>
    //[WpfFact]
    public void TestClipboard_FileNames()
    {
        using var clipboardAccessToken = ClipboardNative.Access();
        var fileNames = clipboardAccessToken.GetFileNames();
        Assert.True(fileNames.Any());
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

            var unicodeStream = clipboardAccessToken.GetAsStream(StandardClipboardFormats.UnicodeText);
            await using var memoryStream = new MemoryStream();
            await unicodeStream.CopyToAsync(memoryStream);
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
            clipboardAccessToken.SetExcludeClipboardContentFromMonitorProcessing(true);
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
            
            // Use default values (should allow history and cloud, not exclude monitoring)
            clipboardAccessToken.SetCloudClipboardOptions();
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
            
            await using var memoryStream = new MemoryStream();
            await stream.CopyToAsync(memoryStream);
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
            
            await using var memoryStream = new MemoryStream();
            await stream.CopyToAsync(memoryStream);
            var resultString = Encoding.UTF8.GetString(memoryStream.GetBuffer(), 0, (int)memoryStream.Length);
            Assert.Equal(testString, resultString);
            
            // Try to get with wrong format - should fail
            success = clipboardAccessToken.TryGetAsStream("WRONG_FORMAT", out stream);
            Assert.False(success);
            Assert.Null(stream);
        }
    }

    public void Dispose()
    {
        subscription.Dispose();
    }
}
