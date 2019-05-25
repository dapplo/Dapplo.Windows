//  Dapplo - building blocks for desktop applications
//  Copyright (C) 2017-2019  Dapplo
// 
//  For more information see: http://dapplo.net/
//  Dapplo repositories are hosted on GitHub: https://github.com/dapplo
// 
//  This file is part of Dapplo.Windows
// 
//  Dapplo.Windows is free software: you can redistribute it and/or modify
//  it under the terms of the GNU Lesser General Public License as published by
//  the Free Software Foundation, either version 3 of the License, or
//  (at your option) any later version.
// 
//  Dapplo.Windows is distributed in the hope that it will be useful,
//  but WITHOUT ANY WARRANTY; without even the implied warranty of
//  MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
//  GNU Lesser General Public License for more details.
// 
//  You should have a copy of the GNU Lesser General Public License
//  along with Dapplo.Windows. If not, see <http://www.gnu.org/licenses/lgpl.txt>.

#region using

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

#endregion

namespace Dapplo.Windows.Tests
{
    public class ClipboardTests
    {
        private static readonly LogSource Log = new LogSource();

        public ClipboardTests(ITestOutputHelper testOutputHelper)
        {
            LogSettings.RegisterDefaultLogger<XUnitLogger>(LogLevels.Verbose, testOutputHelper);

            IntPtr WinProcClipboardHandler(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
            {
                // We can use the GetClipboardFormatName to get the string for the windows message... weird but it works
                Log.Verbose().WriteLine("WinProc {0}, {1}", hwnd, WindowsMessage.GetWindowsMessage((uint) msg));
                return IntPtr.Zero;
            }

            WinProcHandler.Instance.Subscribe(WinProcClipboardHandler);
        }

        /// <summary>
        ///     Test monitoring the clipboard
        /// </summary>
        /// <returns></returns>
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
                    using (var clipboard = ClipboardNative.Access())
                    {
                        using (var stream = clipboard.GetAsStream("PNG"))
                        {
                            using (var fileStream = File.Create(@"c:\projects\test.png"))
                            {
                                stream.CopyTo(fileStream);
                            }
                        }
                    }
                }
                
                tcs.TrySetResult(true);
            });

            await tcs.Task;

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
            using (var clipboard = ClipboardNative.Access())
            {
                clipboard.ClearContents();
                clipboard.SetAsUnicodeString("Blub", format);
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
        public void TestClipboard_Filenames()
        {
            using (var clipboardAccessToken = ClipboardNative.Access())
            {
                var filenames = clipboardAccessToken.GetFilenames();
                Assert.True(filenames.Any());
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
                using (var memoryStream = new MemoryStream())
                {
                    unicodeStream.CopyTo(memoryStream);
                    Assert.Equal(testString, Encoding.Unicode.GetString(memoryStream.GetBuffer(), 0, (int)memoryStream.Length).TrimEnd('\0'));

                }
            }
        }

        /// <summary>
        ///     Test AccessAsync
        /// </summary>
        [WpfFact]
        public async Task Test_ClipboardAccess_LockTimeout()
        {
            using (var outerClipboardAcess = await ClipboardNative.AccessAsync())
            {
                Assert.True(outerClipboardAcess.CanAccess);
                var clipboardAccessToken = await ClipboardNative.AccessAsync();
                Assert.True(clipboardAccessToken.IsLockTimeout);
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
                var clipboardAccessToken = await ClipboardNative.AccessAsync();
                Assert.Throws<ClipboardAccessDeniedException>(() => clipboardAccessToken.ThrowWhenNoAccess());
            }
        }
    }
}