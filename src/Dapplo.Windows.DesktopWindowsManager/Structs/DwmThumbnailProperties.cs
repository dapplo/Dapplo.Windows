// Copyright (c) Dapplo and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
using System.Runtime.InteropServices;
using Dapplo.Windows.Common.Structs;
using Dapplo.Windows.DesktopWindowsManager.Enums;

namespace Dapplo.Windows.DesktopWindowsManager.Structs
{
    /// <summary>
    ///     Specifies Desktop Window Manager (DWM) thumbnail properties. Used by the DwmUpdateThumbnailProperties function.
    ///     See: http://msdn.microsoft.com/en-us/library/aa969502.aspx
    /// </summary>
    [StructLayout(LayoutKind.Sequential)]
    public struct DwmThumbnailProperties
    {
        /// <summary>
        ///     A bitwise combination of DWM thumbnail constant values that indicates which members of this structure are set.
        /// </summary>
        private DwmThumbnailPropertyFlags _dwFlags;

        /// <summary>
        ///     The area in the destination window where the thumbnail will be rendered.
        /// </summary>
        private NativeRect _rcDestination;

        /// <summary>
        ///     The region of the source window to use as the thumbnail. By default, the entire window is used as the thumbnail.
        /// </summary>
        private NativeRect _rcSource;

        /// <summary>
        ///     The opacity with which to render the thumbnail. 0 is fully transparent while 255 is fully opaque. The default value
        ///     is 255.
        /// </summary>
        private byte _opacity;

        /// <summary>
        ///     TRUE to make the thumbnail visible; otherwise, FALSE. The default is FALSE.
        /// </summary>
        private bool _fVisible;

        /// <summary>
        ///     TRUE to use only the thumbnail source's client area; otherwise, FALSE. The default is FALSE.
        /// </summary>
        private bool _fSourceClientAreaOnly;

        /// <summary>
        ///     Property for the destination, setting this also changes the dwFlags
        /// </summary>
        public NativeRect Destination
        {
            get => _rcDestination;
            set
            {
                _dwFlags |= DwmThumbnailPropertyFlags.Destination;
                _rcDestination = value;
            }
        }

        /// <summary>
        ///     Property for the source, setting this also changes the dwFlags
        /// </summary>
        public NativeRect Source
        {
            get => _rcSource;
            set
            {
                _dwFlags |= DwmThumbnailPropertyFlags.Source;
                _rcSource = value;
            }
        }

        /// <summary>
        ///     Property for the Opacity, setting this also changes the dwFlags
        /// </summary>
        public byte Opacity
        {
            get => _opacity;
            set
            {
                _dwFlags |= DwmThumbnailPropertyFlags.Opacity;
                _opacity = value;
            }
        }

        /// <summary>
        ///     Visible property, setting this also changes the dwFlags
        /// </summary>
        public bool Visible
        {
            get => _fVisible;
            set
            {
                _dwFlags |= DwmThumbnailPropertyFlags.Visible;
                _fVisible = value;
            }
        }

        /// <summary>
        ///     Property SourceClientAreaOnly, setting this also changes the dwFlags
        /// </summary>
        public bool SourceClientAreaOnly
        {
            get => _fSourceClientAreaOnly;
            set
            {
                _dwFlags |= DwmThumbnailPropertyFlags.SourceClientAreaOnly;
                _fSourceClientAreaOnly = value;
            }
        }
    }
}