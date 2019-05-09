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

using System;
using System.Threading.Tasks;
using Dapplo.Log;
using Dapplo.Log.XUnit;
using Dapplo.Windows.Advapi32;
using Microsoft.Win32;
using Xunit;
using Xunit.Abstractions;

#endregion

namespace Dapplo.Windows.Tests
{
    public class RegistryTests
    {
        private static readonly LogSource Log = new LogSource();

        public RegistryTests(ITestOutputHelper testOutputHelper)
        {
            LogSettings.RegisterDefaultLogger<XUnitLogger>(LogLevels.Verbose, testOutputHelper);
        }

        /// <summary>
        ///     Test if the observable functions
        /// </summary>
        [Fact]
        public async Task Test_RegistryMonitor()
        {
            const string internetSettingsKey = @"Software\Microsoft\Windows\CurrentVersion\Internet Settings";
            const string autoConfigUrlKey = "AutoConfigURL";
            const string autoConfigUrlBullshit = "http://somedomain/file.pac";

            using (var regKey = Registry.CurrentUser.OpenSubKey(internetSettingsKey, RegistryKeyPermissionCheck.ReadWriteSubTree))
            {
                Assert.NotNull(regKey);
                var originalValue = regKey.GetValue(autoConfigUrlKey) as string;
                Log.Debug().WriteLine("Original value {0}", originalValue);
                try
                {
                    var tcs = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
                    using (RegistryMonitor.ObserveChanges(RegistryHive.CurrentUser, internetSettingsKey).Subscribe(unit =>
                        {
                            Log.Debug().WriteLine("Got registry change!");
                            tcs.SetResult(true);
                        }))
                    {
                        // Set some value
                        regKey.SetValue(autoConfigUrlKey, autoConfigUrlBullshit);

                        // Timeout
                        var timeoutTask = Task.Delay(1000);

                        // Wait for the value to arrive
                        await Task.WhenAny(timeoutTask, tcs.Task).ConfigureAwait(true);
                    }

                    // The Task should have ran to completion
                    Assert.Equal(TaskStatus.RanToCompletion, tcs.Task.Status);

                    var currentValue = regKey.GetValue(autoConfigUrlKey) as string;
                    Log.Debug().WriteLine("Current value {0}", currentValue);
                }
                finally
                {
                    // Restore back to the original
                    if (originalValue == null)
                    {
                        regKey.DeleteValue(autoConfigUrlKey);
                    }
                    else
                    {
                        regKey.SetValue(autoConfigUrlKey, originalValue);
                    }
                    var resetValue = regKey.GetValue(autoConfigUrlKey) as string;
                    Log.Debug().WriteLine("Reset back to value {0}", resetValue);
                }
            }
        }
   }
}