// Copyright (c) Dapplo and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
using System.Reactive.Linq;
using System.Threading.Tasks;
using Dapplo.Log;
using Dapplo.Log.XUnit;
using Dapplo.Windows.Input.Mouse;
using Dapplo.Windows.Messages.Enumerations;
using Xunit.Abstractions;

namespace Dapplo.Windows.Tests;

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
        await MouseHook.MouseEvents.Where(args => args.WindowsMessage == WindowsMessages.WM_LBUTTONDOWN).FirstAsync();
    }
}