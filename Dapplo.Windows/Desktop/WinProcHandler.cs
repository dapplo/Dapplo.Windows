//  Dapplo - building blocks for desktop applications
//  Copyright (C) 2016-2017 Dapplo
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
using System.Collections.Generic;
using System.Linq;
using System.Windows.Interop;

#endregion

namespace Dapplo.Windows
{
	/// <summary>
	///     This can be used to handle WinProc messages, for instance when there is no running winproc
	/// </summary>
	public class WinProcHandler
	{
		/// <summary>
		///     Hold the singeton
		/// </summary>
		private static readonly Lazy<WinProcHandler> Singleton = new Lazy<WinProcHandler>(() => new WinProcHandler());

		/// <summary>
		///     Store hooks, so they can be removed
		/// </summary>
		private readonly IList<HwndSourceHook> _hooks = new List<HwndSourceHook>();

		/// <summary>
		///     Special HwndSource which is only there for handling messages
		/// </summary>
		private readonly HwndSource _hwndSource = new HwndSource(0, 0, 0, 0, 0, 0, 0, "DapploWinProc", IntPtr.Zero);

		/// <summary>
		///     Singleton instance of the WinProcHandler
		/// </summary>
		public static WinProcHandler Instance => Singleton.Value;

		/// <summary>
		/// The actual handle for the HwndSource
		/// </summary>
		public IntPtr Handle => _hwndSource.Handle;

		/// <summary>
		///     Add a hook to handle messages
		/// </summary>
		/// <param name="hook">HwndSourceHook</param>
		public void AddHook(HwndSourceHook hook)
		{
			_hwndSource.AddHook(hook);
			_hooks.Add(hook);
		}

		/// <summary>
		///     Unregister a hook
		/// </summary>
		/// <param name="hook">HwndSourceHook</param>
		public void RemoveHook(HwndSourceHook hook)
		{
			_hwndSource.RemoveHook(hook);
			_hooks.Remove(hook);
		}

		/// <summary>
		///     Remove all current hooks
		/// </summary>
		public void RemoveHooks()
		{
			foreach (var hwndSourceHook in _hooks.ToList())
			{
				RemoveHook(hwndSourceHook);
			}
		}
	}
}