// Copyright (c) Dapplo and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using Dapplo.Windows.Gdi32.Structs;
using Dapplo.Windows.Vfw32.Enums;
using Dapplo.Windows.Vfw32.Extensions;
using Dapplo.Windows.Vfw32.Structs;

namespace Dapplo.Windows.Vfw32
{
    /// <summary>
    /// Represent a codec, with all the information and its API
    /// </summary>
    public class VfwCodec : ICodec
    {
        private IntPtr _compressorHandle;
        private BitmapInfoHeader _inputBitmapFormat;
        private BitmapInfoHeader _outputBitmapFormat;
        private bool _isOpen;
        private bool _didBegin;
        private int _quality = 0;
        private int _frameIndex;

        /// <summary>
        /// Details about the codec
        /// </summary>
        public IcInfo IcInfo  { get; private set; }

        /// <summary>
        /// The bitmap format used to supply frames
        /// </summary>
        public BitmapInfoHeader InputBitmapFormat
        {
            get => _inputBitmapFormat;
            private set => _inputBitmapFormat = value;
        }

        /// <summary>
        /// The bitmap format when encoded
        /// </summary>
        public BitmapInfoHeader OutputBitmapFormat
        {
            get => _outputBitmapFormat;
            set => _outputBitmapFormat = value;
        }

        /// <summary>
        /// Inform the codec that the compression can begin
        /// </summary>
        public void BeginCompress()
        {
            if (_didBegin)
            {
                return;
            }
            var result = (IcResults)Vfw32Api.ICSendMessage(_compressorHandle, IcMessages.ICM_COMPRESS_BEGIN, ref _inputBitmapFormat, ref _outputBitmapFormat);
            if (!result.IsSuccess())
            {
                throw new InvalidOperationException($"Not able to begin compress: {result.GetErrorDescription()}");
            }

            _frameIndex = 0;
            _didBegin = true;
        }

        /// <summary>
        /// Inform the codec that the compression is ended
        /// </summary>
        public void EndCompress()
        {
            if (_compressorHandle == IntPtr.Zero)
            {
                return;
            }
            if (_didBegin)
            {
                var result = Vfw32Api.ICSendMessage(_compressorHandle, IcMessages.ICM_COMPRESS_END, IntPtr.Zero, IntPtr.Zero);
                if (!result.IsSuccess())
                {
                    throw new InvalidOperationException($"Not able to end compress: {result.GetErrorDescription()}");
                }

                _didBegin = false;
            }
        }

        /// <inheritdoc cref="IDisposable"/>
        public void Dispose()
        {
            if (_compressorHandle == IntPtr.Zero)
            {
                return;
            }

            EndCompress();

            if (!_isOpen)
            {
                return;
            }
            var result = Vfw32Api.ICClose(_compressorHandle);
            if (!result.IsSuccess())
            {
                throw new InvalidOperationException($"Not able to close the codec: {result.GetErrorDescription()}");
            }
            _compressorHandle = IntPtr.Zero;
            _isOpen = false;

        }

        /// <inheritdoc cref="ICodec"/>
        public void Initialize()
        {
            if (_isOpen)
            {
                return;
            }
            _compressorHandle = Vfw32Api.ICOpen(IcInfo.FccType, IcInfo.FccHandler, IcModes.ICMODE_COMPRESS);
            if (_compressorHandle == IntPtr.Zero)
            {
                throw new InvalidOperationException("Not able to open the codec");
            }

            _isOpen = true;
        }

        /// <summary>
        /// This 
        /// </summary>
        /// <param name="source">Memory with the </param>
        /// <param name="destination">Memory of where the frame will be encoded to</param>
        /// <param name="isKeyFrame"></param>
        /// <returns>uint with the size of the encoded frame</returns>
        public bool EncodeFrame(Memory<byte> source, Memory<byte> destination, out bool isKeyFrame, out uint encodedSize)
        {
            var outInfo = _outputBitmapFormat;
            outInfo.Size = (uint)destination.Length;

            var flags = IcCompressFlags.ICCOMPRESS_KEYFRAME; //_framesFromLastKey >= _keyFrameRate ? IcCompressFlags.ICCOMPRESS_KEYFRAME : IcCompressFlags.ICCOMPRESS_NONE;

            unsafe
            {
                fixed (byte* sp = &source.Span.GetPinnableReference())
                fixed (byte* dp = &destination.Span.GetPinnableReference())
                {
                    var encodedHandle = new IntPtr(dp);
                    var sourceHandle = new IntPtr(sp);
                    var result = Vfw32Api.ICCompress(_compressorHandle, flags,
                        ref outInfo, encodedHandle, ref _inputBitmapFormat, sourceHandle,
                        out int chunkId, out var outFlags, _frameIndex,
                        0, _quality, IntPtr.Zero, IntPtr.Zero);
                    encodedSize = outInfo.Size;
                    isKeyFrame = (outFlags & AviIndexFlags.KeyFrame) == AviIndexFlags.KeyFrame;
                    if (!result.IsSuccess())
                    {

                        return false;
                    }
                    _frameIndex++;

                    if (isKeyFrame)
                    {
                        //_framesFromLastKey = 1;
                    }
                    else
                    {
                        //_framesFromLastKey++;
                    }
                }
            }

            
            return true;
        }
        /// <summary>
        /// This provides all available VFW codecs
        /// </summary>
        /// <param name="inputBitmapInfoHeader">BitmapInfoHeader specifying what bitmap format is used to supply frames</param>
        public static IEnumerable<VfwCodec> CodecsFor(BitmapInfoHeader inputBitmapInfoHeader)
        {
            uint codecNr = 0;

            var icInfo = new IcInfo(FourCC.CodecTypeVideo);

            bool success;
            do
            {
                // Get the IcInfo details for the codec with the specified number
                success = Vfw32Api.ICInfo(icInfo.FccType, codecNr++, ref icInfo);
                if (!success)
                {
                    continue;
                }


                IntPtr hic = Vfw32Api.ICOpen(icInfo.FccType, icInfo.FccHandler, IcModes.ICMODE_QUERY);
                if (hic == IntPtr.Zero)
                {
                    continue;
                }

                try
                {
                    // We don't care about the output
                    var outHeader = IntPtr.Zero;
                    var queryResult = (IcResults)Vfw32Api.ICSendMessage(hic, IcMessages.ICM_COMPRESS_QUERY, ref inputBitmapInfoHeader, outHeader);
                    if (queryResult.IsSuccess())
                    {
                        var outBitmapInfoHeader = BitmapInfoHeader.Create(0, 0, 24);

                        var result = (IcResults)Vfw32Api.ICSendMessage(hic, IcMessages.ICM_COMPRESS_GET_FORMAT, ref inputBitmapInfoHeader, ref outBitmapInfoHeader);
                        if (!result.IsSuccess())
                        {
                            throw new InvalidOperationException($"Not able to retrieve format: {result.GetErrorDescription()}");
                        }
                        var infoSize = Vfw32Api.ICGetInfo(hic, out var currentCodecInfo, IcInfo.SizeOf);
                        if (infoSize > 0)
                        {
                            var codec = new VfwCodec
                            {
                                IcInfo = currentCodecInfo,
                                InputBitmapFormat = inputBitmapInfoHeader,
                                OutputBitmapFormat = outBitmapInfoHeader
                            };
                            yield return codec;
                        }
                    }
                }
                finally
                {
                    Vfw32Api.ICClose(hic);
                }

            } while (success);
        }
    }
}
