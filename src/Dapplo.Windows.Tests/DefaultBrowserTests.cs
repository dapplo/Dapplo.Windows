// Copyright (c) Dapplo and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Dapplo.Windows.Shell32;
using Dapplo.Windows.Shell32.Enums;
using Xunit;

namespace Dapplo.Windows.Tests;

public class DefaultBrowserTests
{
    private const string Address = "https://www.example.com/search?q=a%20b";

    [Fact]
    public void CreateStartInfo_Edge_TargetAfterSingleArgumentIsNotQuoted()
    {
        var startInfo = DefaultBrowser.CreateStartInfo(@"""C:\Program Files (x86)\Microsoft\Edge\Application\msedge.exe"" --single-argument %1", Address);

        Assert.NotNull(startInfo);
        Assert.Equal(@"C:\Program Files (x86)\Microsoft\Edge\Application\msedge.exe", startInfo.FileName);
        Assert.Equal("--single-argument " + Address, startInfo.Arguments);
        Assert.False(startInfo.UseShellExecute);
    }

    [Fact]
    public void CreateStartInfo_Edge_LocalPathBecomesUnquotedFileAddress()
    {
        var startInfo = DefaultBrowser.CreateStartInfo(@"""C:\Program Files (x86)\Microsoft\Edge\Application\msedge.exe"" --single-argument %1", @"C:\Temp\my page.html");

        Assert.NotNull(startInfo);
        Assert.Equal("--single-argument file:///C:/Temp/my%20page.html", startInfo.Arguments);
    }

    [Fact]
    public void CreateStartInfo_QuotedPlaceholderAfterSingleArgument_QuotesAreDropped()
    {
        var startInfo = DefaultBrowser.CreateStartInfo(@"""C:\Chrome\chrome.exe"" --single-argument ""%1""", Address);

        Assert.NotNull(startInfo);
        Assert.Equal("--single-argument " + Address, startInfo.Arguments);
    }

    [Fact]
    public void CreateStartInfo_Firefox_QuotedPlaceholderGetsQuotedTarget()
    {
        var startInfo = DefaultBrowser.CreateStartInfo(@"""C:\Program Files\Mozilla Firefox\firefox.exe"" -osint -url ""%1""", Address);

        Assert.NotNull(startInfo);
        Assert.Equal(@"C:\Program Files\Mozilla Firefox\firefox.exe", startInfo.FileName);
        Assert.Equal($"-osint -url \"{Address}\"", startInfo.Arguments);
    }

    [Fact]
    public void CreateStartInfo_UnquotedPlaceholder_GetsQuotedTarget()
    {
        var startInfo = DefaultBrowser.CreateStartInfo(@"C:\Browser\browser.exe -new-tab %1", Address);

        Assert.NotNull(startInfo);
        Assert.Equal(@"C:\Browser\browser.exe", startInfo.FileName);
        Assert.Equal($"-new-tab \"{Address}\"", startInfo.Arguments);
    }

    [Fact]
    public void CreateStartInfo_NoPlaceholder_AppendsQuotedTarget()
    {
        var startInfo = DefaultBrowser.CreateStartInfo(@"""C:\Browser\browser.exe"" -new-window", Address);

        Assert.NotNull(startInfo);
        Assert.Equal(@"C:\Browser\browser.exe", startInfo.FileName);
        Assert.Equal($"-new-window \"{Address}\"", startInfo.Arguments);
    }

    [Fact]
    public void CreateStartInfo_ProgramOnly_TargetIsTheArgument()
    {
        var startInfo = DefaultBrowser.CreateStartInfo(@"C:\Browser\browser.exe", Address);

        Assert.NotNull(startInfo);
        Assert.Equal(@"C:\Browser\browser.exe", startInfo.FileName);
        Assert.Equal($"\"{Address}\"", startInfo.Arguments);
    }

    [Fact]
    public void CreateStartInfo_PercentOneInTarget_IsNotReplaced()
    {
        var startInfo = DefaultBrowser.CreateStartInfo(@"""C:\Browser\browser.exe"" ""%1""", "https://example.com/?q=%1A");

        Assert.NotNull(startInfo);
        Assert.Equal("\"https://example.com/?q=%1A\"", startInfo.Arguments);
    }

    [Theory]
    [InlineData(@"""C:\Program Files\Browser\browser.exe --single-argument %1")]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public void CreateStartInfo_InvalidCommand_ReturnsNull(string command)
    {
        Assert.Null(DefaultBrowser.CreateStartInfo(command, Address));
    }

    [Theory]
    [InlineData(@"C:\Temp\my page.html", "file:///C:/Temp/my%20page.html")]
    [InlineData(@"\\server\share\page.html", "file://server/share/page.html")]
    [InlineData("https://www.example.com/", "https://www.example.com/")]
    [InlineData("file:///C:/Temp/page.html", "file:///C:/Temp/page.html")]
    public void ToAddress(string target, string expected)
    {
        Assert.Equal(expected, DefaultBrowser.ToAddress(target));
    }

    [Fact]
    public void CreateStartInfo_Target_ReturnsStartInfo()
    {
        var startInfo = DefaultBrowser.CreateStartInfo(Address);

        Assert.NotNull(startInfo);
        Assert.False(string.IsNullOrEmpty(startInfo.FileName));
    }

    [Fact]
    public void GetAssociationString_UnknownProtocol_ReturnsNull()
    {
        Assert.Null(ShlwapiApi.GetAssociationString(AssocStr.Command, "dapplo-no-such-protocol-4711", "open", AssocF.IsProtocol));
    }

    [Fact]
    public void GetAssociationString_Https_ReturnsCommand()
    {
        var command = ShlwapiApi.GetAssociationString(AssocStr.Command, "https", "open", AssocF.IsProtocol);
        if (command == null)
        {
            Assert.Skip("No application is registered for https on this machine.");
        }
        Assert.NotNull(DefaultBrowser.CreateStartInfo(command, Address));
        Assert.False(string.IsNullOrEmpty(ShlwapiApi.GetAssociationString(AssocStr.Executable, "https", "open", AssocF.IsProtocol)));
    }
}
