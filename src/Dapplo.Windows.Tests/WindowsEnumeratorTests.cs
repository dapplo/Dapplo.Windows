// Copyright (c) Dapplo and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
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

namespace Dapplo.Windows.Tests;

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