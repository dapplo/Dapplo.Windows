// Copyright (c) Dapplo and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
using System;
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
using Xunit.Abstractions;

namespace Dapplo.Windows.Tests
{
    /// <summary>
    /// All clipboard related tests
    /// </summary>
    public class ClipboardTests
    {
        private static readonly LogSource Log = new LogSource();

        public ClipboardTests(ITestOutputHelper testOutputHelper)
        {
            LogSettings.RegisterDefaultLogger<XUnitLogger>(LogLevels.Verbose, testOutputHelper);

            static IntPtr WinProcClipboardHandler(IntPtr hWnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
            {
                // We can use the GetClipboardFormatName to get the string for the windows message... weird but it works
                Log.Verbose().WriteLine("WinProc {0}, {1}", hWnd, WindowsMessage.GetWindowsMessage((uint) msg));
                return IntPtr.Zero;
            }

            WinProcHandler.Instance.Subscribe(new WinProcHandlerHook(WinProcClipboardHandler));
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
        [WpfFact]
        public async Task TestClipboardMonitor_DelayedRender()
        {
            var tcs = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);

            // This is what is going to be called, as soon as the format is retrieved of the clipboard
            var subscription = ClipboardNative.OnRenderFormat.Subscribe(clipboardRenderFormatRequest =>
            {
                switch (clipboardRenderFormatRequest)
                {
                    case { } request when request.IsDestroyClipboard:
                        Log.Debug().WriteLine("Destroy clipboard");
                        tcs.TrySetResult(false);
                        break;
                    case { } request when request.RenderAllFormats:
                        Log.Debug().WriteLine("Render all formats");
                        tcs.TrySetResult(false);
                        break;
                    default:
                        Log.Debug().WriteLine("Format to render {0}", clipboardRenderFormatRequest);
                        // Satisfy the request
                        clipboardRenderFormatRequest.AccessToken.SetAsUnicodeString("Hi", clipboardRenderFormatRequest.RequestedFormat);
                        tcs.TrySetResult(true);
                        break;
                }
            });

            var formatToTestWith = "TEST_FORMAT";
            // Make the clipboard ready for testing
            using (var clipboardAccessToken = await ClipboardNative.AccessAsync())
            {
                clipboardAccessToken.ClearContents();
                // Set delayed rendered content
                clipboardAccessToken.SetDelayedRenderedContent(formatToTestWith);
            }

            await Task.Delay(100).ConfigureAwait(true);

            using (var clipboardAccessToken = await ClipboardNative.AccessAsync())
            {
                // Request the missing content, this should trigger the
                clipboardAccessToken.GetAsUnicodeString(formatToTestWith);
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
            using var clipboard = ClipboardNative.Access();
            clipboard.ClearContents();
            clipboard.SetAsUnicodeString("Blub", format);
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
            using (var clipboardAccessToken = ClipboardNative.Access())
            {
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
            using (var clipboardAccessToken = ClipboardNative.Access())
            {
                clipboardAccessToken.ClearContents();
                clipboardAccessToken.SetAsUnicodeString(testString);
            }
            await Task.Delay(100);
            using (var clipboardAccessToken = ClipboardNative.Access())
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

            using (var clipboardAccessToken = ClipboardNative.Access())
            {
                clipboardAccessToken.ClearContents();
                clipboardAccessToken.SetAsStream(StandardClipboardFormats.UnicodeText, testStream);
            }
            await Task.Delay(100);
            using (var clipboardAccessToken = ClipboardNative.Access())
            {
                Assert.Equal(testString, clipboardAccessToken.GetAsUnicodeString());
                var unicodeBytes = clipboardAccessToken.GetAsBytes(StandardClipboardFormats.UnicodeText);
                Assert.Equal(testString, Encoding.Unicode.GetString(unicodeBytes, 0, unicodeBytes.Length).TrimEnd('\0'));

                var unicodeStream = clipboardAccessToken.GetAsStream(StandardClipboardFormats.UnicodeText);
                await using var memoryStream = new MemoryStream();
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
            using var outerClipboardAccessToken = await ClipboardNative.AccessAsync();
            Assert.True(outerClipboardAccessToken.CanAccess);
            using var clipboardAccessToken = await ClipboardNative.AccessAsync();
            Assert.True(clipboardAccessToken.IsLockTimeout);
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
    }
}