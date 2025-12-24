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
    /// This is a simplified method that sets all three cloud clipboard formats at once.
    /// </summary>
    /// <param name="clipboardAccessToken">The IClipboardAccessToken</param>
    /// <param name="canIncludeInHistory">
    /// When true (default), allows the clipboard content to be included in clipboard history.
    /// When false, prevents the content from appearing in clipboard history.
    /// </param>
    /// <param name="canUploadToCloud">
    /// When true (default), allows the clipboard content to be uploaded to the cloud clipboard.
    /// When false, prevents the content from being synced to cloud.
    /// </param>
    /// <param name="excludeFromMonitoring">
    /// When true, excludes the clipboard content from being processed by clipboard monitoring applications.
    /// When false (default), allows monitoring applications to process the content.
    /// </param>
    public static void SetCloudClipboardOptions(
        this IClipboardAccessToken clipboardAccessToken,
        bool canIncludeInHistory = true,
        bool canUploadToCloud = true,
        bool excludeFromMonitoring = false)
    {
        clipboardAccessToken.ThrowWhenNoAccess();

        // Set the three cloud clipboard formats as DWORD values (0 or 1)
        SetDWordFormat(clipboardAccessToken, CanIncludeInClipboardHistoryFormat, canIncludeInHistory ? 1u : 0u);
        SetDWordFormat(clipboardAccessToken, CanUploadToCloudClipboardFormat, canUploadToCloud ? 1u : 0u);
        SetDWordFormat(clipboardAccessToken, ExcludeClipboardContentFromMonitorProcessingFormat, excludeFromMonitoring ? 1u : 0u);
    }

    /// <summary>
    /// Sets whether clipboard content can be included in clipboard history.
    /// </summary>
    /// <param name="clipboardAccessToken">The IClipboardAccessToken</param>
    /// <param name="canInclude">True to allow inclusion in history, false to prevent it.</param>
    public static void SetCanIncludeInClipboardHistory(this IClipboardAccessToken clipboardAccessToken, bool canInclude = true)
    {
        clipboardAccessToken.ThrowWhenNoAccess();
        SetDWordFormat(clipboardAccessToken, CanIncludeInClipboardHistoryFormat, canInclude ? 1u : 0u);
    }

    /// <summary>
    /// Sets whether clipboard content can be uploaded to cloud clipboard.
    /// </summary>
    /// <param name="clipboardAccessToken">The IClipboardAccessToken</param>
    /// <param name="canUpload">True to allow cloud upload, false to prevent it.</param>
    public static void SetCanUploadToCloudClipboard(this IClipboardAccessToken clipboardAccessToken, bool canUpload = true)
    {
        clipboardAccessToken.ThrowWhenNoAccess();
        SetDWordFormat(clipboardAccessToken, CanUploadToCloudClipboardFormat, canUpload ? 1u : 0u);
    }

    /// <summary>
    /// Sets whether clipboard content should be excluded from clipboard monitor processing.
    /// </summary>
    /// <param name="clipboardAccessToken">The IClipboardAccessToken</param>
    /// <param name="exclude">True to exclude from monitoring, false to allow monitoring.</param>
    public static void SetExcludeClipboardContentFromMonitorProcessing(this IClipboardAccessToken clipboardAccessToken, bool exclude = true)
    {
        clipboardAccessToken.ThrowWhenNoAccess();
        SetDWordFormat(clipboardAccessToken, ExcludeClipboardContentFromMonitorProcessingFormat, exclude ? 1u : 0u);
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
    }
}
