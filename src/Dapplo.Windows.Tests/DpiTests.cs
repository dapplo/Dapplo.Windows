// Copyright (c) Dapplo and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
using Dapplo.Log;
using Dapplo.Log.XUnit;
using Dapplo.Windows.Common.Structs;
using Dapplo.Windows.Dpi;
using Xunit;
using Xunit.Abstractions;

namespace Dapplo.Windows.Tests
{
    public class DpiTests
    {
        public DpiTests(ITestOutputHelper testOutputHelper)
        {
            LogSettings.RegisterDefaultLogger<XUnitLogger>(LogLevels.Verbose, testOutputHelper);
        }

        /// <summary>
        ///     Test ScaleWithDpi
        /// </summary>
        [Fact]
        public void Test_ScaleWithDpi()
        {
            var size96 = DpiHandler.ScaleWithDpi(16, 96);
            Assert.Equal(16, size96);
            var size120 = DpiHandler.ScaleWithDpi(16, 120);
            Assert.Equal(20, size120);
            var size144 = DpiHandler.ScaleWithDpi(16, 144);
            Assert.Equal(24, size144);
            var size192 = DpiHandler.ScaleWithDpi(16, 192);
            Assert.Equal(32, size192);
        }

        /// <summary>
        ///     Test UnscaleWithDpi
        /// </summary>
        [Fact]
        public void Test_UnscaleWithDpi()
        {
            var size96 = DpiHandler.UnscaleWithDpi(16, 96);
            Assert.Equal(16, size96);
            var size120 = DpiHandler.UnscaleWithDpi(16, 120);
            Assert.Equal(12, size120);
            var size144 = DpiHandler.UnscaleWithDpi(16, 144);
            Assert.Equal(10, size144);
            var size192 = DpiHandler.UnscaleWithDpi(16, 192);
            Assert.Equal(8, size192);
        }

        /// <summary>
        ///     Test scale -> unscale
        /// </summary>
        [Fact]
        public void Test_ScaleWithDpi_UnscaleWithDpi()
        {
            var testSize = new NativeSize(16, 16);
            var size96 = DpiHandler.ScaleWithDpi(testSize, 96);
            var resultSize96 = DpiHandler.UnscaleWithDpi(size96, 96);
            Assert.Equal(testSize, resultSize96);

            var size120 = DpiHandler.ScaleWithDpi(testSize, 120);
            var resultSize120 = DpiHandler.UnscaleWithDpi(size120, 120);
            Assert.Equal(testSize, resultSize120);

            var size144 = DpiHandler.ScaleWithDpi(testSize, 144);
            var resultSize144 = DpiHandler.UnscaleWithDpi(size144, 144);
            Assert.Equal(testSize, resultSize144);
        }
    }
}