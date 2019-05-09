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
using Dapplo.Log;
using Dapplo.Log.XUnit;
using Dapplo.Windows.App;
using Dapplo.Windows.Com;
using Dapplo.Windows.Tests.ComInterfaces;
using Xunit;
using Xunit.Abstractions;

#endregion

namespace Dapplo.Windows.Tests
{
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
}