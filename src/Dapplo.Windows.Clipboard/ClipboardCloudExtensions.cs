// Copyright (c) Dapplo and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Runtime.InteropServices;
using Dapplo.Windows.Clipboard.Internals;

namespace Dapplo.Windows.Clipboard;

/// <summary>
/// Extensions for Windows Cloud Clipboard and Clipboard History support.
/// See https://docs.microsoft.com/en-us/windows/win32/dataxchg/clipboard-formats#cloud-clipboard-and-clipboard-history-formats
/// </summary>
public static class ClipboardCloudExtensions
{
    /// <summary>
    /// Format name for controlling whether clipboard content can be included in clipboard history.
    /// </summary>
    public const string CanIncludeInClipboardHistoryFormat = "CanIncludeInClipboardHistory";

    /// <summary>
    /// Format name for excluding clipboard content from monitor processing.
    /// </summary>
    public const string ExcludeClipboardContentFromMonitorProcessingFormat = "ExcludeClipboardContentFromMonitorProcessing";

    /// <summary>
    /// Format name for controlling whether clipboard content can be uploaded to cloud clipboard.
    /// </summary>
    public const string CanUploadToCloudClipboardFormat = "CanUploadToCloudClipboard";

    /// <summary>
    /// Sets cloud clipboard options on the clipboard to control clipboard history and cloud sync behavior.
    /// Only the formats for the options which are specified are placed on the clipboard, without any option the Windows defaults apply.
    /// Call this after placing the content, with the same access token.
    /// </summary>
    /// <param name="clipboardAccessToken">The IClipboardAccessToken</param>
    /// <param name="canIncludeInHistory">
    /// null (default) doesn't place the CanIncludeInClipboardHistory format.
    /// true explicitly allows the content to be included in the clipboard history, false prevents it.
    /// </param>
    /// <param name="canUploadToCloud">
    /// null (default) doesn't place the CanUploadToCloudClipboard format.
    /// true explicitly allows the content to be synced to other devices, false prevents it.
    /// </param>
    /// <param name="excludeFromMonitoring">
    /// When true, the ExcludeClipboardContentFromMonitorProcessing format is placed, this excludes the content from clipboard history, cloud sync and clipboard monitoring applications.
    /// When false (default), the format is not placed (the mere presence of this format excludes the content, whatever its value).
    /// </param>
    public static void SetCloudClipboardOptions(
        this IClipboardAccessToken clipboardAccessToken,
        bool? canIncludeInHistory = null,
        bool? canUploadToCloud = null,
        bool excludeFromMonitoring = false)
    {
        clipboardAccessToken.ThrowWhenNoAccess();

        if (canIncludeInHistory.HasValue)
        {
            SetDWordFormat(clipboardAccessToken, CanIncludeInClipboardHistoryFormat, canIncludeInHistory.Value ? 1u : 0u);
        }
        if (canUploadToCloud.HasValue)
        {
            SetDWordFormat(clipboardAccessToken, CanUploadToCloudClipboardFormat, canUploadToCloud.Value ? 1u : 0u);
        }
        if (excludeFromMonitoring)
        {
            ExcludeFromMonitorProcessing(clipboardAccessToken);
        }
    }

    /// <summary>
    /// Sets whether clipboard content can be included in clipboard history, this places the CanIncludeInClipboardHistory format with a DWORD 1 or 0.
    /// </summary>
    /// <param name="clipboardAccessToken">The IClipboardAccessToken</param>
    /// <param name="canInclude">True to explicitly allow inclusion in history, false to prevent it.</param>
    public static void SetCanIncludeInClipboardHistory(this IClipboardAccessToken clipboardAccessToken, bool canInclude)
    {
        clipboardAccessToken.ThrowWhenNoAccess();
        SetDWordFormat(clipboardAccessToken, CanIncludeInClipboardHistoryFormat, canInclude ? 1u : 0u);
    }

    /// <summary>
    /// Sets whether clipboard content can be uploaded to cloud clipboard, this places the CanUploadToCloudClipboard format with a DWORD 1 or 0.
    /// </summary>
    /// <param name="clipboardAccessToken">The IClipboardAccessToken</param>
    /// <param name="canUpload">True to explicitly allow cloud upload, false to prevent it.</param>
    public static void SetCanUploadToCloudClipboard(this IClipboardAccessToken clipboardAccessToken, bool canUpload)
    {
        clipboardAccessToken.ThrowWhenNoAccess();
        SetDWordFormat(clipboardAccessToken, CanUploadToCloudClipboardFormat, canUpload ? 1u : 0u);
    }

    /// <summary>
    /// Excludes the clipboard content from clipboard history, cloud sync and clipboard monitoring applications,
    /// by placing the ExcludeClipboardContentFromMonitorProcessing format (its presence is what counts).
    /// Use this e.g. for passwords.
    /// </summary>
    /// <param name="clipboardAccessToken">The IClipboardAccessToken</param>
    public static void ExcludeFromMonitorProcessing(this IClipboardAccessToken clipboardAccessToken)
    {
        clipboardAccessToken.ThrowWhenNoAccess();
        SetDWordFormat(clipboardAccessToken, ExcludeClipboardContentFromMonitorProcessingFormat, 0u);
    }

    /// <summary>
    /// Helper method to set a DWORD (uint32) value on the clipboard for a specific format.
    /// </summary>
    /// <param name="clipboardAccessToken">The IClipboardAccessToken</param>
    /// <param name="format">The clipboard format name</param>
    /// <param name="value">The DWORD value to set</param>
    private static void SetDWordFormat(IClipboardAccessToken clipboardAccessToken, string format, uint value)
    {
        clipboardAccessToken.ThrowWhenNoAccess();
        
        var formatId = ClipboardFormatExtensions.MapFormatToId(format);
        
        // Allocate memory for a DWORD (4 bytes)
        using var writeInfo = clipboardAccessToken.WriteInfo(formatId, sizeof(uint));
        
        // Write the DWORD value to the memory
        Marshal.WriteInt32(writeInfo.MemoryPtr, (int)value);
        writeInfo.Commit();
    }
}
