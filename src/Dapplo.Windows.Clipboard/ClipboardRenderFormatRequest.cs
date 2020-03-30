// Copyright (c) Dapplo and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Dapplo.Windows.Clipboard
{
    /// <summary>
    /// Information about the render format request
    /// </summary>
    public class ClipboardRenderFormatRequest
    {
        /// <summary>
        /// The format which was requested
        /// </summary>
        public string RequestedFormat { get; internal set; }

        /// <summary>
        /// Specifies if this request specifies that the clipboard is destroyed
        /// </summary>
        public bool IsDestroyClipboard { get; internal set; }

        /// <summary>
        /// If you need to render all formats, this is true
        /// </summary>
        public bool RenderAllFormats => string.IsNullOrEmpty(RequestedFormat);

        /// <summary>
        /// The access token for the clipboard access
        /// </summary>
        public IClipboardAccessToken AccessToken { get; internal set; }
    }
}
