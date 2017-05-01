//  Dapplo - building blocks for desktop applications
//  Copyright (C) 2016-2017 Dapplo
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
using System.Threading.Tasks;
using Dapplo.Log;
using Dapplo.Log.XUnit;
using Dapplo.Clipboard;
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
        }

        /// <summary>
        ///     Test monitoring the clipboard
        /// </summary>
        /// <returns></returns>
        //[WpfFact]
        public async Task TestClipboardMonitor_Anything()
        {
            var tcs = new TaskCompletionSource<bool>();
            var subscription = ClipboardMonitor.OnPasted.Subscribe(clipboard =>
            {
                Log.Debug().WriteLine("Formats {0}", string.Join(",", clipboard.Formats));
                Log.Debug().WriteLine("Owner {0}", clipboard.OwnerHandle);
                Log.Debug().WriteLine("Sequence {0}", clipboard.Id);

                var stream = clipboard["PNG"];
                using (var fileStream = File.Create(@"D:\test.png"))
                {
                    stream.CopyTo(fileStream);
                }
                tcs.TrySetResult(true);
            });

            await tcs.Task;

            subscription.Dispose();

        }

        /// <summary>
        ///     Test monitoring the clipboard
        /// </summary>
        /// <returns></returns>
        //[WpfFact]
        public async Task TestClipboardMonitor_Text()
        {
            const string testString = "Dapplo.Windows.Tests.ClipboardTests";
            bool hasNewContent = false;
            var subscription = ClipboardMonitor.OnPasted.Where(clipboard => clipboard.Formats.Contains("CF_TEXT")).Subscribe(clipboard =>
            {
                Log.Debug().WriteLine("Detected change {0}", string.Join(",", clipboard.Formats));
                Log.Debug().WriteLine("Owner {0}", clipboard.OwnerHandle);
                Log.Debug().WriteLine("Sequence {0}", clipboard.Id);

                hasNewContent = true;
            });

            ClipboardNative.Put(testString);
            await Task.Delay(1000);
            subscription.Dispose();

            // Doesn't work on AppVeyor!!
            Assert.True(hasNewContent);
        }

        /// <summary>
        ///     Test monitoring the clipboard
        /// </summary>
        /// <returns></returns>
        //[WpfFact]
        public async Task TestClipboardStore()
        {
            const string testString = "Dapplo.Windows.Tests.ClipboardTests";
            ClipboardNative.Put(testString);
            await Task.Delay(1000);
            Assert.Equal(testString,ClipboardNative.GetText());
        }
    }
}