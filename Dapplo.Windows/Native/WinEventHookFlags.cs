/*
 * dapplo - building blocks for desktop applications
 * Copyright (C) 2015 Robin Krom
 * 
 * For more information see: http://dapplo.net/
 * dapplo repositories are hosted on GitHub: https://github.com/dapplo
 * 
 * This program is free software: you can redistribute it and/or modify
 * it under the terms of the GNU General Public License as published by
 * the Free Software Foundation, either version 1 of the License, or
 * (at your option) any later version.
 * 
 * This program is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 * GNU General Public License for more details.
 * 
 * You should have received a copy of the GNU General Public License
 * along with this program. If not, see <http://www.gnu.org/licenses/>.
 */

namespace Dapplo.Windows.Native {
	/// <summary>
	/// Used for User32.SetWinEventHook
	/// See: http://msdn.microsoft.com/en-us/library/windows/desktop/dd373640.aspx
	/// </summary>
	public enum WinEventHookFlags : int {
		WINEVENT_OUTOFCONTEXT = 0,
		WINEVENT_SKIPOWNTHREAD = 1,
		WINEVENT_SKIPOWNPROCESS = 2,
		WINEVENT_INCONTEXT = 4
	}
}
