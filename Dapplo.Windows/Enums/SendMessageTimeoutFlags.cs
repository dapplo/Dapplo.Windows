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
	///     See: http://www.pinvoke.net/default.aspx/Enums/SendMessageTimeoutFlags.html
	/// </summary>
	[Flags]
	public enum SendMessageTimeoutFlags : uint
	{
		SMTO_NORMAL = 0x0,
		SMTO_BLOCK = 0x1,
		SMTO_ABORTIFHUNG = 0x2,
		SMTO_NOTIMEOUTIFNOTHUNG = 0x8
	}
}