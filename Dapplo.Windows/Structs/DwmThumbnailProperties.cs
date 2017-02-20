#region Dapplo 2017 - GNU Lesser General Public License

// Dapplo - building blocks for .NET applications
// Copyright (C) 2017 Dapplo
// 
// For more information see: http://dapplo.net/
// Dapplo repositories are hosted on GitHub: https://github.com/dapplo
// 
// This file is part of Dapplo.Windows
// 
// Dapplo.Windows is free software: you can redistribute it and/or modify
// it under the terms of the GNU Lesser General Public License as published by
// the Free Software Foundation, either version 3 of the License, or
// (at your option) any later version.
// 
// Dapplo.Windows is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU Lesser General Public License for more details.
// 
// You should have a copy of the GNU Lesser General Public License
// along with Dapplo.Windows. If not, see <http://www.gnu.org/licenses/lgpl.txt>.

#endregion

#region Usings

using System.Runtime.InteropServices;
using Dapplo.Windows.Enums;

#endregion

namespace Dapplo.Windows.Structs
{
	/// <summary>
	///     Specifies Desktop Window Manager (DWM) thumbnail properties. Used by the DwmUpdateThumbnailProperties function.
	///     See: http://msdn.microsoft.com/en-us/library/aa969502.aspx
	/// </summary>
	[StructLayout(LayoutKind.Sequential)]
	public struct DwmThumbnailProperties
	{
		/// <summary>
		/// A bitwise combination of DWM thumbnail constant values that indicates which members of this structure are set.
		/// </summary>
		public DwmThumbnailPropertyFlags dwFlags;
		/// <summary>
		/// The area in the destination window where the thumbnail will be rendered.
		/// </summary>
		public RECT rcDestination;
		/// <summary>
		/// The region of the source window to use as the thumbnail. By default, the entire window is used as the thumbnail.
		/// </summary>
		public RECT rcSource;
		/// <summary>
		/// The opacity with which to render the thumbnail. 0 is fully transparent while 255 is fully opaque. The default value is 255.
		/// </summary>
		public byte opacity;
		/// <summary>
		/// TRUE to make the thumbnail visible; otherwise, FALSE. The default is FALSE.
		/// </summary>
		public bool fVisible;
		/// <summary>
		/// TRUE to use only the thumbnail source's client area; otherwise, FALSE. The default is FALSE.
		/// </summary>
		public bool fSourceClientAreaOnly;

		public RECT Destination
		{
			set
			{
				dwFlags |= DwmThumbnailPropertyFlags.DWM_TNP_RECTDESTINATION;
				rcDestination = value;
			}
		}

		public RECT Source
		{
			set
			{
				dwFlags |= DwmThumbnailPropertyFlags.DWM_TNP_RECTSOURCE;
				rcSource = value;
			}
		}

		public byte Opacity
		{
			set
			{
				dwFlags |= DwmThumbnailPropertyFlags.DWM_TNP_OPACITY;
				opacity = value;
			}
		}

		public bool Visible
		{
			set
			{
				dwFlags |= DwmThumbnailPropertyFlags.DWM_TNP_VISIBLE;
				fVisible = value;
			}
		}

		public bool SourceClientAreaOnly
		{
			set
			{
				dwFlags |= DwmThumbnailPropertyFlags.DWM_TNP_SOURCECLIENTAREAONLY;
				fSourceClientAreaOnly = value;
			}
		}
	}
}