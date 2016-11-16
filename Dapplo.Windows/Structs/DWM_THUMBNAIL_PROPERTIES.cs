//  Dapplo - building blocks for desktop applications
//  Copyright (C) 2016 Dapplo
// 
//  For more information see: http://dapplo.net/
//  Dapplo repositories are hosted on GitHub: https://github.com/dapplo
// 
//  This file is part of Dapplo.Windows
// 
//  Dapplo.Windows is free software: you can redistribute it and/or modify
//  it under the terms of the GNU Lesser General Public License as published by
//  the Free Software Foundation, either version 3 of the License, or
//  (at your option) any later version.
// 
//  Dapplo.Windows is distributed in the hope that it will be useful,
//  but WITHOUT ANY WARRANTY; without even the implied warranty of
//  MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
//  GNU Lesser General Public License for more details.
// 
//  You should have a copy of the GNU Lesser General Public License
//  along with Dapplo.Windows. If not, see <http://www.gnu.org/licenses/lgpl.txt>.

#region using

using System.Runtime.InteropServices;

#endregion

namespace Dapplo.Windows.Structs
{
	/// <summary>
	///     Specifies Desktop Window Manager (DWM) thumbnail properties. Used by the DwmUpdateThumbnailProperties function.
	///     See: http://msdn.microsoft.com/en-us/library/aa969502.aspx
	/// </summary>
	[StructLayout(LayoutKind.Sequential)]
	public struct DWM_THUMBNAIL_PROPERTIES
	{
		// A bitwise combination of DWM thumbnail constant values that indicates which members of this structure are set.
		public int dwFlags;
		// The area in the destination window where the thumbnail will be rendered.
		public RECT rcDestination;
		// The region of the source window to use as the thumbnail. By default, the entire window is used as the thumbnail.
		public RECT rcSource;
		// The opacity with which to render the thumbnail. 0 is fully transparent while 255 is fully opaque. The default value is 255.
		public byte opacity;
		// TRUE to make the thumbnail visible; otherwise, FALSE. The default is FALSE.
		public bool fVisible;
		// TRUE to use only the thumbnail source's client area; otherwise, FALSE. The default is FALSE.
		public bool fSourceClientAreaOnly;

		public RECT Destination
		{
			set
			{
				dwFlags |= DWM_TNP_RECTDESTINATION;
				rcDestination = value;
			}
		}

		public RECT Source
		{
			set
			{
				dwFlags |= DWM_TNP_RECTSOURCE;
				rcSource = value;
			}
		}

		public byte Opacity
		{
			set
			{
				dwFlags |= DWM_TNP_OPACITY;
				opacity = value;
			}
		}

		public bool Visible
		{
			set
			{
				dwFlags |= DWM_TNP_VISIBLE;
				fVisible = value;
			}
		}

		public bool SourceClientAreaOnly
		{
			set
			{
				dwFlags |= DWM_TNP_SOURCECLIENTAREAONLY;
				fSourceClientAreaOnly = value;
			}
		}

		// A value for the rcDestination member has been specified.
		public const int DWM_TNP_RECTDESTINATION = 0x00000001;
		// A value for the rcSource member has been specified.
		public const int DWM_TNP_RECTSOURCE = 0x00000002;
		// A value for the opacity member has been specified.
		public const int DWM_TNP_OPACITY = 0x00000004;
		// A value for the fVisible member has been specified.
		public const int DWM_TNP_VISIBLE = 0x00000008;
		// A value for the fSourceClientAreaOnly member has been specified.
		public const int DWM_TNP_SOURCECLIENTAREAONLY = 0x00000010;
	}
}