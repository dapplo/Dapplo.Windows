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
	public enum SysCommands
	{
		SC_SIZE = 0xF000,
		SC_MOVE = 0xF010,
		SC_MINIMIZE = 0xF020,
		SC_MAXIMIZE = 0xF030,
		SC_NEXTWINDOW = 0xF040,
		SC_PREVWINDOW = 0xF050,
		SC_CLOSE = 0xF060,
		SC_VSCROLL = 0xF070,
		SC_HSCROLL = 0xF080,
		SC_MOUSEMENU = 0xF090,
		SC_KEYMENU = 0xF100,
		SC_ARRANGE = 0xF110,
		SC_RESTORE = 0xF120,
		SC_TASKLIST = 0xF130,
		SC_SCREENSAVE = 0xF140,
		SC_HOTKEY = 0xF150,
		//#if(WINVER >= 0x0400) //Win95
		SC_DEFAULT = 0xF160,
		SC_MONITORPOWER = 0xF170,
		SC_CONTEXTHELP = 0xF180,
		SC_SEPARATOR = 0xF00F,
		//#endif /* WINVER >= 0x0400 */

		//#if(WINVER >= 0x0600) //Vista
		SCF_ISSECURE = 0x00000001,
		//#endif /* WINVER >= 0x0600 */

		/*
		  * Obsolete names
		  */
		SC_ICON = SC_MINIMIZE,
		SC_ZOOM = SC_MAXIMIZE
	}
}