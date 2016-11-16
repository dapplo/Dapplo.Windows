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

namespace Dapplo.Windows.Enums
{
	/// <summary>
	///     Flags used by the DwmGetWindowAttribute and DwmSetWindowAttribute functions to specify window attributes for
	///     non-client rendering.
	///     See http://msdn.microsoft.com/en-us/library/aa969530.aspx
	/// </summary>
	public enum DWMWINDOWATTRIBUTE
	{
		DWMWA_NCRENDERING_ENABLED = 1,
		DWMWA_NCRENDERING_POLICY,
		DWMWA_TRANSITIONS_FORCEDISABLED,
		DWMWA_ALLOW_NCPAINT,
		DWMWA_CAPTION_BUTTON_BOUNDS,
		DWMWA_NONCLIENT_RTL_LAYOUT,
		DWMWA_FORCE_ICONIC_REPRESENTATION,
		DWMWA_FLIP3D_POLICY,
		DWMWA_EXTENDED_FRAME_BOUNDS, // This is the one we need for retrieving the Window size since Windows Vista
		DWMWA_HAS_ICONIC_BITMAP, // Since Windows 7
		DWMWA_DISALLOW_PEEK, // Since Windows 7
		DWMWA_EXCLUDED_FROM_PEEK, // Since Windows 7
		DWMWA_CLOAK, // Since Windows 8
		DWMWA_CLOAKED, // Since Windows 8
		DWMWA_FREEZE_REPRESENTATION, // Since Windows 8
		DWMWA_LAST
	}
}