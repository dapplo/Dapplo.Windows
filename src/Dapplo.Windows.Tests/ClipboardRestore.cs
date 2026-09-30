// Copyright (c) Dapplo and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Threading.Tasks;
using Dapplo.Windows.Clipboard;

namespace Dapplo.Windows.Tests;

/// <summary>
/// Keeps the clipboard content of the user around an interactive test: the formats stored in memory (up to 64 MiB each) are saved
/// before the test and written back afterwards. Handle formats (e.g. CF_BITMAP, CF_ENHMETAFILE) can't be saved, Windows synthesizes
/// CF_DIB / CF_DIBV5 for a bitmap and those are restored.
/// </summary>
internal sealed class ClipboardRestore
{
    private const long MaxBytesPerFormat = 64L * 1024 * 1024;
    private ClipboardSnapshot _snapshot;

    private ClipboardRestore()
    {
    }

    /// <summary>
    /// Save the current clipboard content, best effort
    /// </summary>
    public static async Task<ClipboardRestore> SaveAsync()
    {
        var restore = new ClipboardRestore();
        try
        {
            restore._snapshot = await ClipboardNative.ReadSnapshotAsync(null, MaxBytesPerFormat);
        }
        catch (Exception)
        {
            // Nothing to restore
        }
        return restore;
    }

    /// <summary>
    /// Write the saved content back, an empty clipboard stays empty. Failures are ignored, the test result counts.
    /// </summary>
    public async Task RestoreAsync()
    {
        try
        {
            if (_snapshot == null)
            {
                return;
            }
            if (_snapshot.Formats.Count == 0)
            {
                await ClipboardNative.UseAsync(clipboard => clipboard.ClearContents());
                return;
            }
            var contents = _snapshot.ToContents();
            await ClipboardNative.UseAsync(clipboard => clipboard.ReplaceContents(contents));
        }
        catch (Exception)
        {
            // Best effort
        }
    }
}
