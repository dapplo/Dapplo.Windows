/*
 * dapplo - building blocks for desktop applications
 * Copyright (C) Dapplo 2015-2016
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

using System;

namespace Dapplo.Windows.Enums
{
	/// <summary>
	/// See: http://msdn.microsoft.com/en-us/library/aa909766.aspx
	/// </summary>
	[Flags]
	public enum SoundFlags : uint
	{
		SND_SYNC = 0x0000, // play synchronously (default)
		SND_ASYNC = 0x0001, // play asynchronously
		SND_NODEFAULT = 0x0002, // silence (!default) if sound not found
		SND_MEMORY = 0x0004, // pszSound points to a memory file
		SND_LOOP = 0x0008, // loop the sound until next sndPlaySound
		SND_NOSTOP = 0x0010, // don't stop any currently playing sound
		SND_NOWAIT = 0x00002000, // don't wait if the driver is busy
		SND_ALIAS = 0x00010000, // name is a registry alias
		SND_ALIAS_ID = 0x00110000, // alias is a predefined id
		SND_FILENAME = 0x00020000, // name is file name
	}
}
