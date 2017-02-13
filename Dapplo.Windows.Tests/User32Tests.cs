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
using System.Diagnostics;
using System.Reactive.Linq;
using System.Threading.Tasks;
using Dapplo.Log;
using Dapplo.Log.XUnit;
using Xunit;
using Xunit.Abstractions;
using Dapplo.Windows.Desktop;
using Dapplo.Windows.Enums;
using Dapplo.Windows.Native;
using Dapplo.Windows.Structs;

#endregion

namespace Dapplo.Windows.Tests
{
	public class User32Tests
	{
		private static readonly LogSource Log = new LogSource();

		public User32Tests(ITestOutputHelper testOutputHelper)
		{
			LogSettings.RegisterDefaultLogger<XUnitLogger>(LogLevels.Verbose, testOutputHelper);
		}

		/// <summary>
		/// Test GetWindow
		/// </summary>
		/// <returns></returns>
		//[Fact]
		private void TestGetWindow()
		{
			IntPtr windowPtr = User32.GetTopWindow(IntPtr.Zero);

			do
			{
				var window = NativeWindowInfo.CreateFor(windowPtr);
				if (!window.Fill().HasParent && !User32.IsIconic(window.Handle) && !string.IsNullOrEmpty(window.Text) && !window.Bounds.IsEmpty)
				{
					Debug.WriteLine("{0} - {1}", window.Classname, window.Text);
				}
				windowPtr = User32.GetWindow(windowPtr, GetWindowCommands.GW_HWNDNEXT);

			} while (windowPtr != IntPtr.Zero);
		}
	}
}