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
    public void EnumerateWindows()
    {
        var windows = WindowsEnumerator.EnumerateWindows().ToList();
        Assert.True(windows.Count > 0);
    }

    [StaFact]
    public void EnumerateWindowHandles()
    {
        var windows = WindowsEnumerator.EnumerateWindowHandles().ToList();
        Assert.True(windows.Count > 0);
    }

    [StaFact]
    public void EnumerateWindows_Take10()
    {
        var windows = WindowsEnumerator.EnumerateWindows().Take(10).ToList();
        Assert.True(windows.Count == 10);
    }

    [StaFact]
    public async Task EnumerateWindowsAsync()
    {
        var windows = await WindowsEnumerator.EnumerateWindowsAsync().ToList().ToTask().ConfigureAwait(false);
        Assert.True(windows.Count > 0);
    }

    [StaFact]
    public async Task EnumerateWindowHandlesAsync()
    {
        var windows = await WindowsEnumerator.EnumerateWindowHandlesAsync().ToList().ToTask().ConfigureAwait(false);
        Assert.True(windows.Count > 0);
    }

    [StaFact]
    public async Task EnumerateWindowsAsync_Find()
    {
        var textValue = Guid.NewGuid().ToString();
        using var form = new Form
        {
            Text = textValue,
            TopLevel = true
        };
        form.Show();
        try
        {
            // Important, otherwise Windows doesn't have time to display the window!
            Application.DoEvents();

            IInteropWindow window = null;
            var stopwatch = System.Diagnostics.Stopwatch.StartNew();
            while (window == null && stopwatch.Elapsed < TestWait.DefaultTimeout)
            {
                window = await WindowsEnumerator.EnumerateWindowsAsync().Where(info => info.GetCaption().Contains(textValue)).FirstOrDefaultAsync();
                if (window == null)
                {
                    Application.DoEvents();
                    await Task.Delay(50, TestContext.Current.CancellationToken);
                }
            }

            Assert.NotNull(window);
        }
        finally
        {
            form.Close();
        }
    }

    [StaFact]
    public async Task EnumerateWindowsAsync_Take10()
    {
        var windows = await WindowsEnumerator.EnumerateWindowsAsync().Take(10).ToList().ToTask().ConfigureAwait(false);
        Assert.True(windows.Count == 10);
    }
}