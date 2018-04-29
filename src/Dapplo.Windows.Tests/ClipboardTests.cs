//  Dapplo - building blocks for desktop applications
//  Copyright (C) 2017-2018  Dapplo
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
        //[WpfFact]
        public async Task TestClipboardMonitor_WaitForCopy()
        {
            var tcs = new TaskCompletionSource<bool>();
            var subscription = ClipboardMonitor.OnUpdate.Subscribe(clipboard =>
            {
                Log.Debug().WriteLine("Formats {0}", string.Join(",", clipboard.Formats));
                Log.Debug().WriteLine("Owner {0}", clipboard.OwnerHandle);
                Log.Debug().WriteLine("Sequence {0}", clipboard.Id);

                if (clipboard.Formats.Contains("PNG"))
                {
                    using (ClipboardNative.Lock())
                    {
                        var stream = ClipboardNative.GetAsStream("PNG");
                        using (var fileStream = File.Create(@"D:\test.png"))
                        {
                            stream.CopyTo(fileStream);
                        }
                        
                    }
                }
                tcs.TrySetResult(true);
            });

            await tcs.Task;

            subscription.Dispose();
        }

        /// <summary>
        ///     Test registering a clipboard format for the clipboard
        /// </summary>
        [WpfFact]
        public void TestClipboard_RegisterFormat()
        {
            string format = "DAPPLO.DOPY" + ClipboardNative.SequenceNumber;

            // Register the format
            var id1 = ClipboardNative.RegisterFormat(format);
            // Register the format again
            var id2 = ClipboardNative.RegisterFormat(format);

            Assert.Equal(id1, id2);

            // Make sure it works
            using (ClipboardNative.Lock())
            {
                ClipboardNative.Clear();
                ClipboardNative.SetAsUnicodeString("Blub", format);
            }
        }

        /// <summary>
        ///     Test monitoring the clipboard
        /// </summary>
        /// <returns></returns>
        [WpfFact]
        public async Task TestClipboardMonitor_Text()
        {
            const string testString = "Dapplo.Windows.Tests.ClipboardTests";
            bool hasNewContent = false;
            var subscription = ClipboardMonitor.OnUpdate.Where(clipboard => clipboard.Formats.Contains("TEST_FORMAT")).Subscribe(clipboard =>
            {
                Log.Debug().WriteLine("Detected change {0}", string.Join(",", clipboard.Formats));
                Log.Debug().WriteLine("Owner {0}", clipboard.OwnerHandle);
                Log.Debug().WriteLine("Sequence {0}", clipboard.Id);

                hasNewContent = true;
            });
            using (ClipboardNative.Lock())
            {
                ClipboardNative.Clear();
                ClipboardNative.SetAsUnicodeString(testString, "TEST_FORMAT");
            }
            await Task.Delay(1000);
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
            using (ClipboardNative.Lock())
            {
                ClipboardNative.Clear();
                ClipboardNative.SetAsUnicodeString(testString);
            }
            await Task.Delay(1000);
            using (ClipboardNative.Lock())
            {
                Assert.Equal(testString, ClipboardNative.GetAsUnicodeString());
            }
        }


        /// <summary>
        ///     Test if the clipboard contains files
        /// </summary>
        /// <returns></returns>
        //[WpfFact]
        public void TestClipboard_Filenames()
        {
            using (ClipboardNative.Lock())
            {
                var filenames = ClipboardNative.GetFilenames();
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
   
            Assert.Equal(testString, Encoding.Unicode.GetString(testStream.GetBuffer(), 0, (int)testStream.Length).TrimEnd('\0'));

            MemoryStream resultStream;
            using (ClipboardNative.Lock())
            {
                ClipboardNative.Clear();
                ClipboardNative.SetAsStream("CF_UNICODETEXT", testStream);
            }
            await Task.Delay(1000);
            using (ClipboardNative.Lock())
            {
                resultStream = ClipboardNative.GetAsStream("CF_UNICODETEXT");

            }
            Assert.Equal(testString, Encoding.Unicode.GetString(resultStream.GetBuffer(), 0, (int)resultStream.Length).TrimEnd('\0'));
        }
    }
}