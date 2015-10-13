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

using Dapplo.Windows.Enums;
using System;
using System.Runtime.InteropServices;

namespace Dapplo.Windows.Native
{
	/// <summary>
	/// Windows Media 
	/// </summary>
	public class WinMM
	{
		[DllImport("winmm.dll", SetLastError = true)]
		public static extern bool PlaySound(byte[] ptrToSound, UIntPtr hmod, SoundFlags fdwSound);

		[DllImport("winmm.dll", SetLastError = true)]
		public static extern bool PlaySound(IntPtr ptrToSound, UIntPtr hmod, SoundFlags fdwSound);
	}
}
