// Copyright (c) Dapplo and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Linq;
using Dapplo.Log;
using Dapplo.Log.XUnit;
using Dapplo.Windows.Gdi32.Structs;
using Dapplo.Windows.Vfw32;
using Xunit;
using Xunit.Abstractions;

namespace Dapplo.Windows.Tests
{
    public class Vfw32Tests
    {
        private static readonly LogSource Log = new LogSource();

        public Vfw32Tests(ITestOutputHelper testOutputHelper)
        {
            LogSettings.RegisterDefaultLogger<XUnitLogger>(LogLevels.Verbose, testOutputHelper);
        }

        /// <summary>
        /// Test if there is a list of codecs being returned
        /// </summary>
        [Fact]
        public void Test_CodecsFor()
        {
            var frameFormat = BitmapInfoHeader.Create(100, 100, 24);
            var foundOne = false;
            foreach (var codec in VfwCodec.AllCodecs(frameFormat))
            {
                Log.Debug().WriteLine($"VfwCodec {codec.IcInfo.Name}");
                foundOne = true;
            }

            Assert.True(foundOne);
        }

        /// <summary>
        /// Test the life cycle of a VfwCodec
        /// </summary>
        [Fact]
        public void Test_CodecLifeCycle()
        {
            var frameFormat = BitmapInfoHeader.Create(100, 100, 24);
            var codec = VfwCodec.AllCodecs(frameFormat).First();
            
            codec.Initialize();
            codec.BeginCompress();
            codec.EndCompress();
            codec.Dispose();
        }

        /// <summary>
        /// Test if XVID can be found
        /// You can install this with "choco install xvid"
        /// </summary>
        [Fact]
        public void Test_XvidCodecFromList()
        {
            var frameFormat = BitmapInfoHeader.Create(100, 100, 24);
            var hasXvidCodec = VfwCodec.AllCodecs(frameFormat).Any(vfwCodec => vfwCodec.Name == "XVID");
            Assert.True(hasXvidCodec);
        }

        /// <summary>
        /// Test if XVID can be found
        /// You can install this with "choco install xvid"
        /// </summary>
        [Fact]
        public void Test_XvidCodec()
        {
            var frameFormat = BitmapInfoHeader.Create(100, 100, 24);
            var xvidCodec = VfwCodec.CodecByName("XVID", BitmapInfoHeader.Create(100,100,24));
            Assert.Equal("XVID", xvidCodec.Name);
        }

        /// <summary>
        /// Test the compression of a codec
        /// </summary>
        [WpfFact]
        public void Test_Compress()
        {

        }
    }
}