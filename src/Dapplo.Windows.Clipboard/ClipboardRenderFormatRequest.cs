// Copyright (c) Dapplo and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Dapplo.Windows.Clipboard;

/// <summary>
/// Information about a delayed rendering request, this is passed to a renderer registered with <see cref="ClipboardNative.RegisterDelayedRenderer(uint, System.Action{ClipboardRenderFormatRequest})"/>.
/// </summary>
/// <remarks>
/// The request, and its <see cref="AccessToken"/>, are only valid while the renderer runs.
/// The renderer is called synchronously on the SharedMessageWindow thread, render the data directly: don't switch threads, await or open the clipboard.
/// </remarks>
public sealed class ClipboardRenderFormatRequest
{
    private string _requestedFormat;

    internal ClipboardRenderFormatRequest(uint requestedFormatId, bool renderAllFormats, IClipboardAccessToken accessToken)
    {
        RequestedFormatId = requestedFormatId;
        RenderAllFormats = renderAllFormats;
        AccessToken = accessToken;
    }

    /// <summary>
    /// The format ID which was requested
    /// </summary>
    public uint RequestedFormatId { get; }

    /// <summary>
    /// The format which was requested
    /// </summary>
    public string RequestedFormat => _requestedFormat ??= ClipboardFormatExtensions.MapIdToFormat(RequestedFormatId);

    /// <summary>
    /// True when the format is rendered because the clipboard owner (the SharedMessageWindow) is being destroyed (WM_RENDERALLFORMATS),
    /// false when another application requested the format (WM_RENDERFORMAT).
    /// </summary>
    public bool RenderAllFormats { get; }

    /// <summary>
    /// The access token to place the requested format on the clipboard with, e.g. with SetAsUnicodeString or SetAsStream and the <see cref="RequestedFormatId"/>.
    /// This is only valid during the call of the renderer, on the calling thread, the library takes care of opening (if needed) and closing the clipboard.
    /// </summary>
    public IClipboardAccessToken AccessToken { get; }
}
