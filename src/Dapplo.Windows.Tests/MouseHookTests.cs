//  Dapplo - building blocks for desktop applications
//  Copyright (C) 2017-2019  Dapplo
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
using Dapplo.Windows.Input.Mouse;
using Dapplo.Windows.Messages;
using Xunit.Abstractions;

#endregion

namespace Dapplo.Windows.Tests
{
    /// <summary>
    ///     Test mouse hooking
    /// </summary>
    public class MouseHookTests
    {
        public MouseHookTests(ITestOutputHelper testOutputHelper)
        {
            LogSettings.RegisterDefaultLogger<XUnitLogger>(LogLevels.Verbose, testOutputHelper);
        }

        //[StaFact]
        private async Task Test_LeftMouseDownAsync()
        {
            // This takes care of having a WinProc handler, to make sure the messages arrive
            // ReSharper disable once UnusedVariable
            var winProcHandler = WinProcHandler.Instance;
            await MouseHook.MouseEvents.Where(args => args.WindowsMessage == WindowsMessages.WM_LBUTTONDOWN).FirstAsync();
        }
    }
}