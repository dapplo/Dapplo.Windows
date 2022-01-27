﻿// Copyright (c) Dapplo and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
using System;
using Dapplo.Log;
using Dapplo.Log.XUnit;
using Dapplo.Windows.App;
using Dapplo.Windows.Com;
using Dapplo.Windows.Tests.ComInterfaces;
using Xunit;
using Xunit.Abstractions;

namespace Dapplo.Windows.Tests;

public class ComTests
{
    private static LogSource Log = new LogSource();
    public ComTests(ITestOutputHelper testOutputHelper)
    {
        LogSettings.RegisterDefaultLogger<XUnitLogger>(LogLevels.Verbose, testOutputHelper);
    }

    /// <summary>
    ///     Test the clsId and progId conversion code, works only when Excel is installed
    /// </summary>
    //[Fact]
    public void Test_ClsIdProgId()
    {
        const string progId = "Excel.Application";
        var clsId = Ole32Api.ClassIdFromProgId(progId);
        Log.Info().WriteLine("The prog-id {0} has clsid {1}", progId, clsId);
        Assert.False(clsId == default);
        var progIdResolve = Ole32Api.ProgIdFromClassId(clsId);
        Log.Info().WriteLine("The prog-id {0}, resolve back from {1}, is: {2}", progId, clsId, progIdResolve);
        Assert.StartsWith(progId, progIdResolve);

        using (var app = DisposableCom.Create(Activator.CreateInstance<ExcelApplicationClass>()))
        {
            Assert.NotNull(app);

            // Try to get the instance we just created
            using (var excelApp = OleAut32Api.GetActiveObject<IExcelApplication>(progId))
            {
                Assert.NotNull(excelApp);
                Assert.True(ReferenceEquals(excelApp.ComObject, app.ComObject));
            }
        }
    }
}