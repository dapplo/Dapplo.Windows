// Copyright (c) Dapplo and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Threading;
using System.Windows;
using Dapplo.Log;
using Dapplo.Log.XUnit;
using Dapplo.Windows.Common.Structs;
using Dapplo.Windows.Desktop;
using Dapplo.Windows.User32;
using Dapplo.Windows.User32.Enums;
using Dapplo.Windows.User32.Structs;
using Dapplo.Windows.Wpf;
using Xunit;

namespace Dapplo.Windows.Tests;

public class User32Tests
{
    private static readonly LogSource Log = new LogSource();

    public User32Tests(ITestOutputHelper testOutputHelper)
    {
        LogSettings.RegisterDefaultLogger<XUnitLogger>(LogLevels.Verbose, testOutputHelper);
    }

    [Fact]
    public void TestGetClassname()
    {
        var desktopHandle = InteropWindowQuery.GetDesktopWindow();

        var classname = User32Api.GetClassname(desktopHandle.Handle);
        Assert.Equal("#32769", classname);
    }

    /// <summary>
    ///     Test GetTopWindows finds at least one visible window
    /// </summary>
    /// <returns></returns>
    [Fact]
    public void TestGetTopWindows()
    {
        var foundWindow = false;
        foreach (var window in InteropWindowQuery.GetTopWindows().Where(window => window.IsVisible()))
        {
            foundWindow = true;

            Log.Debug().WriteLine("{0}", window.Dump());
            break;
        }
        Assert.True(foundWindow);
    }


    /// <summary>
    ///     Test WindowPlacement_TypeConverter
    /// </summary>
    /// <returns></returns>
    [Fact]
    public void TestWindowPlacement_TypeConverter()
    {
        var windowPlacement = WindowPlacement.Create();
        windowPlacement.MinPosition = new NativePoint(10, 10);
        windowPlacement.MaxPosition = new NativePoint(100, 100);
        windowPlacement.NormalPosition = new NativeRect(100, 100, 200, 200);
        windowPlacement.ShowCmd = ShowWindowCommands.Normal;

        var typeConverter = TypeDescriptor.GetConverter(typeof(WindowPlacement));
        Assert.NotNull(typeConverter);
        var stringRepresentation = typeConverter.ConvertToInvariantString(windowPlacement);
        Assert.Equal("Normal|10,10|100,100|100,100,200,200", stringRepresentation);
        var windowPlacementResult = (WindowPlacement?)typeConverter.ConvertFromInvariantString(stringRepresentation);
        Assert.True(windowPlacementResult.HasValue);
        if (windowPlacementResult.HasValue)
        {
            Assert.Equal(windowPlacement, windowPlacementResult.Value);
        }
    }

    /// <summary>
    ///     Test GetTextFromWindow
    /// </summary>
    [WpfFact]
    [Trait("Category", "Interactive")]
    public void Test_GetTextFromWindow()
    {
        const string title = "1234567890";
        var window = new Window
        {
            Title = title
        };

        window.Show();
        var handle = window.GetHandle();
        var text = User32Api.GetTextFromWindow(handle);
        window.Close();
        Assert.Equal(title, text);
    }

    /// <summary>
    ///     Test GetTextFromWindow with a text which is too large for the stack
    /// </summary>
    [WpfFact]
    [Trait("Category", "Interactive")]
    public void Test_GetTextFromWindow_LargeText()
    {
        var largeText = new string('x', 600_000);
        using var textBox = new System.Windows.Forms.TextBox
        {
            Multiline = true,
            MaxLength = 0
        };
        var handle = textBox.Handle;
        textBox.Text = largeText;

        var text = User32Api.GetTextFromWindow(handle);
        Assert.Equal(largeText, text);
    }

    /// <summary>
    ///     The UINT overload of SystemParametersInfo is public (it was private, only its XML documentation was published)
    /// </summary>
    [Fact]
    public void SystemParametersInfo_Uint_IsPublicAndReadsTheValue()
    {
        var method = typeof(User32Api).GetMethod(nameof(User32Api.SystemParametersInfo), System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static, null,
            new[] { typeof(SystemParametersInfoActions), typeof(uint), typeof(uint).MakeByRefType(), typeof(SystemParametersInfoBehaviors) }, null);
        Assert.NotNull(method);

        Assert.True(User32Api.SystemParametersInfo(SystemParametersInfoActions.SPI_GETWHEELSCROLLLINES, 0, out uint wheelScrollLines, SystemParametersInfoBehaviors.None));
        Assert.Equal(User32Api.GetWheelScrollLines(), wheelScrollLines);
    }

    /// <summary>
    ///     IsScreenReaderRunning is the BOOL of SPI_GETSCREENREADER
    /// </summary>
    [Fact]
    public void IsScreenReaderRunning_IsTheScreenReaderFlag()
    {
        Assert.True(User32Api.SystemParametersInfo(SystemParametersInfoActions.SPI_GETSCREENREADER, 0, out uint screenReader, SystemParametersInfoBehaviors.None));
        Assert.Equal(screenReader != 0, User32Api.IsScreenReaderRunning());
    }

    /// <summary>
    ///     ScreenReaderChanges starts with the current value and reports a change which is broadcast with SPIF_UPDATEINIFILE | SPIF_SENDCHANGE,
    ///     as NVDA does. Interactive: it changes the system wide screen reader flag (and restores it).
    /// </summary>
    [Fact]
    [Trait("Category", "Interactive")]
    public void ScreenReaderChanges_ReportsTheFlag()
    {
        var initial = User32Api.IsScreenReaderRunning();
        var values = new System.Collections.Concurrent.BlockingCollection<bool>();
        using var subscription = EnvironmentMonitor.ScreenReaderChanges.Subscribe(value => values.Add(value));
        Assert.True(values.TryTake(out var first, 1000, TestContext.Current.CancellationToken));
        Assert.Equal(initial, first);
        try
        {
            Assert.True(User32Api.SystemParametersInfo(SystemParametersInfoActions.SPI_SETSCREENREADER, initial ? 0u : 1u, (string)null,
                SystemParametersInfoBehaviors.UpdateIniFile | SystemParametersInfoBehaviors.SendChange));
            Assert.True(values.TryTake(out var changed, 5000, TestContext.Current.CancellationToken), "No change was reported");
            Assert.Equal(!initial, changed);
        }
        finally
        {
            User32Api.SystemParametersInfo(SystemParametersInfoActions.SPI_SETSCREENREADER, initial ? 1u : 0u, (string)null,
                SystemParametersInfoBehaviors.UpdateIniFile | SystemParametersInfoBehaviors.SendChange);
        }
        Assert.True(values.TryTake(out var restored, 5000, TestContext.Current.CancellationToken), "The restore was not reported");
        Assert.Equal(initial, restored);
    }
}
