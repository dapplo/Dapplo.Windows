// Copyright (c) Dapplo and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Linq;
using System.Threading.Tasks;
using Dapplo.Windows.Clipboard;
using Xunit;

namespace Dapplo.Windows.Tests;

/// <summary>
/// Clipboard format mapping tests, these don't touch the clipboard content
/// </summary>
public class ClipboardFormatTests
{
    /// <summary>
    /// Windows compares clipboard format names case-insensitive, the cache must do the same
    /// </summary>
    [Fact]
    public void MapFormatToId_IsCaseInsensitive()
    {
        Assert.Equal((uint)StandardClipboardFormats.UnicodeText, ClipboardFormatExtensions.MapFormatToId("cf_unicodetext"));

        // Fixed names: registered clipboard formats are a session-wide resource which is never freed
        const string name = "Dapplo.Test.CaseFormat";
        var id1 = ClipboardFormatExtensions.MapFormatToId(name);
        var id2 = ClipboardFormatExtensions.MapFormatToId(name.ToUpperInvariant());
        Assert.Equal(id1, id2);
        Assert.Equal(name, ClipboardFormatExtensions.MapIdToFormat(id1));
    }

    /// <summary>
    /// Registering an invalid format must not cache 0
    /// </summary>
    [Fact]
    public void RegisterFormat_Empty_Throws()
    {
        Assert.Throws<ArgumentException>(() => ClipboardFormatExtensions.RegisterFormat(""));
        Assert.Throws<ArgumentException>(() => ClipboardFormatExtensions.MapFormatToId(null));
    }

    /// <summary>
    /// The format caches are used from several threads at the same time
    /// </summary>
    [Fact]
    public void MapFormat_Concurrent()
    {
        var names = Enumerable.Range(0, 10).Select(i => $"Dapplo.Test.Concurrent{i}").ToArray();
        Parallel.For(0, 400, i =>
        {
            var name = names[i % names.Length];
            var id = ClipboardFormatExtensions.MapFormatToId(name);
            Assert.NotEqual(0u, id);
            Assert.Equal(name, ClipboardFormatExtensions.MapIdToFormat(id), StringComparer.OrdinalIgnoreCase);
        });
    }
}
