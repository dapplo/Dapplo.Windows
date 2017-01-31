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
	/// <summary>
	///     Used for User32.SetWinEventHook
	///     See <a href="http://msdn.microsoft.com/en-us/library/windows/desktop/dd373640.aspx">here</a>
	/// </summary>
	[Flags]
	public enum WinEventHookFlags
	{
		WINEVENT_OUTOFCONTEXT = 0,
		WINEVENT_SKIPOWNTHREAD = 1,
		WINEVENT_SKIPOWNPROCESS = 2,
		WINEVENT_INCONTEXT = 4
	}
}