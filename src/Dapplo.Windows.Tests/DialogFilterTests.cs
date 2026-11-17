// Copyright (c) Dapplo and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading;
using Dapplo.Windows.Dialogs;
using Xunit;

namespace Dapplo.Windows.Tests;

/// <summary>
/// Tests for the filter handling of the dialog builders, without showing a dialog.
/// The helpers are internal (the assembly is strong-named, so no InternalsVisibleTo) and called through reflection.
/// </summary>
public class DialogFilterTests
{
    private static readonly Type ComDialogHelperType = typeof(FileDialogResult).Assembly.GetType("Dapplo.Windows.Dialogs.ComDialogHelper", throwOnError: true);

    private static object InvokeHelper(string name, params object[] arguments)
    {
        try
        {
            return ComDialogHelperType.GetMethod(name, BindingFlags.NonPublic | BindingFlags.Static).Invoke(null, arguments);
        }
        catch (TargetInvocationException ex) when (ex.InnerException != null)
        {
            throw ex.InnerException;
        }
    }

    private static uint ToComFileTypeIndex(int filterIndex, int filterCount) => (uint)InvokeHelper("ToComFileTypeIndex", filterIndex, filterCount);

    private static int FromComFileTypeIndex(uint comFileTypeIndex, int filterCount) => (int)InvokeHelper("FromComFileTypeIndex", comFileTypeIndex, filterCount);

    private static List<(string Name, string Pattern)> ParseFilter(string filter) => (List<(string Name, string Pattern)>)InvokeHelper("ParseFilter", filter);

    [Theory]
    [InlineData(0, 3, 1u)]
    [InlineData(1, 3, 2u)]
    [InlineData(2, 3, 3u)]
    public void ToComFileTypeIndex_IsOneBased(int filterIndex, int filterCount, uint expected)
    {
        Assert.Equal(expected, ToComFileTypeIndex(filterIndex, filterCount));
    }

    [Theory]
    [InlineData(-1, 3)]
    [InlineData(3, 3)]
    [InlineData(int.MaxValue, 3)]
    [InlineData(int.MinValue, 3)]
    [InlineData(0, 0)]
    public void ToComFileTypeIndex_OutOfRange_SelectsTheFirstFilter(int filterIndex, int filterCount)
    {
        Assert.Equal(1u, ToComFileTypeIndex(filterIndex, filterCount));
    }

    [Theory]
    [InlineData(1u, 3, 0)]
    [InlineData(2u, 3, 1)]
    [InlineData(3u, 3, 2)]
    public void FromComFileTypeIndex_IsZeroBased(uint comFileTypeIndex, int filterCount, int expected)
    {
        Assert.Equal(expected, FromComFileTypeIndex(comFileTypeIndex, filterCount));
    }

    [Theory]
    [InlineData(0u, 3)]
    [InlineData(4u, 3)]
    [InlineData(uint.MaxValue, 3)]
    [InlineData(1u, 0)]
    public void FromComFileTypeIndex_NotAFilter_IsMinusOne(uint comFileTypeIndex, int filterCount)
    {
        Assert.Equal(-1, FromComFileTypeIndex(comFileTypeIndex, filterCount));
    }

    [Fact]
    public void FileTypeIndex_RoundTrips()
    {
        for (int i = 0; i < 7; i++)
        {
            Assert.Equal(i, FromComFileTypeIndex(ToComFileTypeIndex(i, 7), 7));
        }
    }

    [Fact]
    public void ParseFilter_ReadsNameAndPatternPairs()
    {
        var filters = ParseFilter("PNG (*.png)|*.png|JPEG (*.jpg;*.jpeg)|*.jpg;*.jpeg|All files|*.*");

        Assert.Equal(new List<(string, string)>
        {
            ("PNG (*.png)", "*.png"),
            ("JPEG (*.jpg;*.jpeg)", "*.jpg;*.jpeg"),
            ("All files", "*.*")
        }, filters);
    }

    [Theory]
    [InlineData((string)null)]
    [InlineData("")]
    public void ParseFilter_NullOrEmpty_GivesNoFilters(string filter)
    {
        Assert.Empty(ParseFilter(filter));
    }

    [Theory]
    [InlineData("Images")]
    [InlineData("Images|*.png|All files")]
    public void ParseFilter_OddNumberOfParts_Throws(string filter)
    {
        Assert.Throws<ArgumentException>(() => ParseFilter(filter));
    }

    [Fact]
    public void AddFilters_InvalidText_ThrowsOnBothBuilders()
    {
        Assert.Throws<ArgumentException>(() => new FileSaveDialogBuilder().AddFilters("Images"));
        Assert.Throws<ArgumentException>(() => new FileOpenDialogBuilder().AddFilters("Images"));
    }

    [Fact]
    public void Cancelled_HasNoSelectedFilter()
    {
        var cancelled = (FileDialogResult)typeof(FileDialogResult).GetMethod("Cancelled", BindingFlags.NonPublic | BindingFlags.Static).Invoke(null, null);

        Assert.True(cancelled.WasCancelled);
        Assert.Equal(-1, cancelled.SelectedFilterIndex);
    }

    [Fact]
    public void ResolveOwner_KeepsAnExplicitOwner_AndIsZeroWithoutActiveWindow()
    {
        var explicitOwner = new IntPtr(0x1234);
        Assert.Equal(explicitOwner, (IntPtr)InvokeHelper("ResolveOwner", explicitOwner));

        // A new thread has no windows, so it has no active window either
        IntPtr resolved = new IntPtr(-1);
        var thread = new Thread(() => resolved = (IntPtr)InvokeHelper("ResolveOwner", IntPtr.Zero));
        thread.SetApartmentState(ApartmentState.STA);
        thread.Start();
        Assert.True(thread.Join(TimeSpan.FromSeconds(10)));
        Assert.Equal(IntPtr.Zero, resolved);
    }
}
