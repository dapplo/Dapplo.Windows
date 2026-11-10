// Copyright (c) Dapplo and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using BenchmarkDotNet.Attributes;
using Dapplo.Windows.Clipboard;

namespace Dapplo.Windows.Tests.Benchmarks;

/// <summary>
/// Benchmarks for encoding and decoding a screen sized DIB, as it is placed on and read from the clipboard
/// </summary>
[MinColumn, MaxColumn, MemoryDiagnoser]
public class DibBenchmarks
{
    private const int Width = 2560;
    private const int Height = 1440;
    private byte[] _bgra32;
    private byte[] _dibV5;
    private byte[] _decoded;

    [GlobalSetup]
    public void Setup()
    {
        _bgra32 = new byte[Width * Height * 4];
        for (var i = 0; i < _bgra32.Length; i++)
        {
            // Opaque, with some variation in the colors
            _bgra32[i] = (i & 3) == 3 ? (byte)255 : (byte)(i * 7);
        }
        _dibV5 = DibImage.CreateDibV5(_bgra32, Width, Height, Width * 4, false);
        _decoded = new byte[Width * Height * 4];
    }

    [Benchmark]
    public byte[] EncodeDibV5() => DibImage.CreateDibV5(_bgra32, Width, Height, Width * 4, false);

    [Benchmark]
    public byte[] EncodeDibV5Premultiplied() => DibImage.CreateDibV5(_bgra32, Width, Height, Width * 4, true);

    [Benchmark]
    public bool DecodeDibV5IntoBuffer() => DibImage.TryDecode(_dibV5, DibImage.DefaultMaxPixelCount, _decoded, Width * 4);
}
