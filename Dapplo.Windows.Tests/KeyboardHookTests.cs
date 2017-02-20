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

using System.Reactive.Linq;
using System.Threading.Tasks;
using Dapplo.Log;
using Dapplo.Log.XUnit;
using Dapplo.Windows.Enums;
using Dapplo.Windows.Reactive;
using Xunit.Abstractions;

#endregion

namespace Dapplo.Windows.Tests
{
	public class KeyboardHookTests
	{
		public KeyboardHookTests(ITestOutputHelper testOutputHelper)
		{
			LogSettings.RegisterDefaultLogger<XUnitLogger>(LogLevels.Verbose, testOutputHelper);
		}

		//[StaFact]
		private async Task TestHandlingKeyAsync()
		{
			await KeyboardHook.KeyboardEvents.Where(args => args.IsWindows && args.IsShift && args.IsControl && args.IsAlt)
				.Select(args =>
				{
					args.Handled = true;
					return args;
				}).FirstAsync();
		}

		//[StaFact]
		private async Task TestMappingAsync()
		{
			await KeyboardHook.KeyboardEvents.FirstAsync(info => info.IsLeftShift && info.IsKeyDown);
		}

		//[StaFact]
		private async Task TestSuppressVolumeAsync()
		{
			await KeyboardHook.KeyboardEvents.Where(args =>
			{
				if (args.Key != VirtualKeyCodes.VOLUME_UP)
				{
					return true;
				}
				args.Handled = true;
				return false;
			}).FirstAsync();
		}
	}
}