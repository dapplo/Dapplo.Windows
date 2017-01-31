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

using System;

#endregion

namespace Dapplo.Windows.Enums
{
	[Flags]
	public enum DwmThumbnailPropertyFlags
	{
		// A value for the rcDestination member has been specified.
		DWM_TNP_RECTDESTINATION = 0x00000001,
		// A value for the rcSource member has been specified.
		DWM_TNP_RECTSOURCE = 0x00000002,
		// A value for the opacity member has been specified.
		DWM_TNP_OPACITY = 0x00000004,
		// A value for the fVisible member has been specfied.
		DWM_TNP_VISIBLE = 0x00000008,
		// A value for the fSourceClientAreaOnly member has been specified.
		DWM_TNP_SOURCECLIENTAREAONLY = 0x00000010
	}
}