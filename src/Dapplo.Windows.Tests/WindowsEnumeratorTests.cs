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
using System.Linq;
using System.Reactive.Linq;
using System.Reactive.Threading.Tasks;
using System.Threading.Tasks;
using System.Windows.Forms;
using Dapplo.Log;
using Dapplo.Log.XUnit;
using Dapplo.Windows.Desktop;
using Xunit;
using Xunit.Abstractions;

#endregion

namespace Dapplo.Windows.Tests
{
    public class WindowsEnumeratorTests
    {
        public WindowsEnumeratorTests(ITestOutputHelper testOutputHelper)
        {
            LogSettings.RegisterDefaultLogger<XUnitLogger>(LogLevels.Verbose, testOutputHelper);
        }

        [StaFact]
        private void EnumerateWindows()
        {
            var windows = WindowsEnumerator.EnumerateWindows().ToList();
            Assert.True(windows.Count > 0);
        }

        [StaFact]
        private void EnumerateWindowHandles()
        {
            var windows = WindowsEnumerator.EnumerateWindowHandles().ToList();
            Assert.True(windows.Count > 0);
        }

        [StaFact]
        private void EnumerateWindows_Take10()
        {
            var windows = WindowsEnumerator.EnumerateWindows().Take(10).ToList();
            Assert.True(windows.Count == 10);
        }

        [StaFact]
        private async Task EnumerateWindowsAsync()
        {
            var windows = await WindowsEnumerator.EnumerateWindowsAsync().ToList().ToTask().ConfigureAwait(false);
            Assert.True(windows.Count > 0);
        }

        [StaFact]
        private async Task EnumerateWindowHandlesAsync()
        {
            var windows = await WindowsEnumerator.EnumerateWindowHandlesAsync().ToList().ToTask().ConfigureAwait(false);
            Assert.True(windows.Count > 0);
        }

        [StaFact]
        private async Task EnumerateWindowsAsync_Find()
        {
            var textValue = Guid.NewGuid().ToString();
            var form = new Form
            {
                Text = textValue,
                TopLevel = true
            };
            form.Show();
            // Important, otherwise Windows doesn't have time to display the window!
            Application.DoEvents();

            await Task.Delay(400);
            var window = await WindowsEnumerator.EnumerateWindowsAsync().Where(info => info.GetCaption().Contains(textValue)).FirstOrDefaultAsync();

            Assert.NotNull(window);
        }

        [StaFact]
        private async Task EnumerateWindowsAsync_Take10()
        {
            var windows = await WindowsEnumerator.EnumerateWindowsAsync().Take(10).ToList().ToTask().ConfigureAwait(false);
            Assert.True(windows.Count == 10);
        }
    }
}