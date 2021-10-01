// Copyright (c) Dapplo and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Runtime.InteropServices;
using Dapplo.Windows.Gdi32.Structs;
using Dapplo.Windows.Vfw32.Enums;
using Dapplo.Windows.Vfw32.Structs;

namespace Dapplo.Windows.Vfw32
{
    /// <summary>
    /// Selected constants, structures and functions from Video for Windows APIs.
    /// </summary>
    /// <remarks>
    /// Useful for implementing stream encoding using VCM codecs.
    /// See Windows API documentation on the meaning and usage of all this stuff.
    /// </remarks>
    public static class Vfw32Api
    {
        /// <summary>
        /// The DLL Name for the Vfw32 library
        /// </summary>
        public const string Vfw32 = "msvfw32";

        /// <summary>
        /// The ICOpen function opens a codec (compressor or de-compressor).
        /// </summary>
        /// <param name="fccType">Four-character code indicating the type of compressor or de-compressor to open. For video streams, the value of this parameter is "VIDC".</param>
        /// <param name="fccHandler">Preferred handler of the specified type. Typically, the handler type is stored in the stream header in an AVI file.</param>
        /// <param name="mode">IcModes Flag defining the use of the compressor or de-compressor.</param>
        /// <returns>IntPtr</returns>
        [DllImport(Vfw32)]
        public static extern IntPtr ICOpen(uint fccType, uint fccHandler, IcModes mode);

        /// <summary>
        /// Get info on the codec, see <a href="https://docs.microsoft.com/en-us/windows/win32/api/vfw/nf-vfw-icinfo">ICInfo function</a>
        /// </summary>
        /// <param name="fccType">uint</param>
        /// <param name="fccHandler">uint</param>
        /// <param name="lpIcInfo">IcInfo ref</param>
        /// <returns>bool</returns>
        [DllImport(Vfw32)]
        public static extern bool ICInfo(uint fccType, uint fccHandler, ref IcInfo lpIcInfo);

        /// <summary>
        /// The ICClose function closes a compressor or de-compressor.
        /// </summary>
        /// <param name="handle">Handle to a compressor or de-compressor.</param>
        /// <returns>IcResults Returns ICERR_OK if successful or an error otherwise.</returns>
        [DllImport(Vfw32)]
        public static extern IcResults ICClose(IntPtr handle);

        /// <summary>
        /// 
        /// </summary>
        /// <param name="handle"></param>
        /// <param name="message"></param>
        /// <param name="param1"></param>
        /// <param name="param2"></param>
        /// <returns></returns>
        [DllImport(Vfw32)]
        public static extern IcResults ICSendMessage(IntPtr handle, IcMessages message, IntPtr param1, IntPtr param2);

        /// <summary>
        /// 
        /// </summary>
        /// <param name="handle"></param>
        /// <param name="message"></param>
        /// <param name="inHeader"></param>
        /// <param name="outHeader"></param>
        /// <returns></returns>
        [DllImport(Vfw32)]
        public static extern int ICSendMessage(IntPtr handle, IcMessages message, ref BitmapInfoHeader inHeader, ref BitmapInfoHeader outHeader);

        /// <summary>
        /// 
        /// </summary>
        /// <param name="handle"></param>
        /// <param name="message"></param>
        /// <param name="inHeader"></param>
        /// <param name="outHeader"></param>
        /// <returns></returns>
        [DllImport(Vfw32)]
        public static extern int ICSendMessage(IntPtr handle, IcMessages message, ref BitmapInfoHeader inHeader, IntPtr outHeader);

        /// <summary>
        /// The ICGetInfo function obtains information about a compressor.
        /// </summary>
        /// <param name="handle">Handle to a compressor.</param>
        /// <param name="lpicinfo">Pointer to the ICINFO structure to return information about the compressor.</param>
        /// <param name="infoSize">Size, in bytes, of the structure pointed to by lpicinfo.</param>
        /// <returns>Returns the number of bytes copied into the structure or zero if an error occurs.</returns>
        [DllImport(Vfw32)]
        public static extern int ICGetInfo(IntPtr handle, out IcInfo lpicinfo, int infoSize);

        /// <summary>
        /// The ICCompress function compresses a single video image.
        /// </summary>
        /// <param name="handle">Handle to the compressor to use.</param>
        /// <param name="inFlags">Compression flag. The following value is defined:
        ///     ICCOMPRESS_KEYFRAME Compressor should make this frame a key frame.
        /// </param>
        /// <param name="outHeader">Pointer to a BITMAPINFOHEADER structure containing the output format.</param>
        /// <param name="encodedData">Pointer to an output buffer large enough to contain a compressed frame.</param>
        /// <param name="inHeader">Pointer to a BITMAPINFOHEADER structure containing the input format.</param>
        /// <param name="frameData">Pointer to the input buffer.</param>
        /// <param name="chunkID">Reserved; do not use.</param>
        /// <param name="outFlags">AviIndexFlags flags used in the AVI index. The following value is defined:
        ///     KeyFrame Current frame is a key frame.
        /// </param>
        /// <param name="frameNumber">Frame number.</param>
        /// <param name="requestedFrameSize">Requested frame size, in bytes. Specify a nonzero value if the compressor supports a suggested frame size, as indicated by the presence of the VIDCF_CRUNCH flag returned by the ICGetInfo function. If this flag is not set or a data rate for the frame is not specified, specify zero for this parameter.
        /// 
        /// A compressor might have to sacrifice image quality or make some other trade-off to obtain the size goal specified in this parameter.</param>
        /// <param name="requestedQuality">Requested quality value for the frame. Specify a nonzero value if the compressor supports a suggested quality value, as indicated by the presence of the VIDCF_QUALITY flag returned by ICGetInfo. Otherwise, specify zero for this parameter.</param>
        /// <param name="prevHeaderPtr">Pointer to a BITMAPINFOHEADER structure containing the format of the previous frame.</param>
        /// <param name="prevFrameData">Pointer to the uncompressed image of the previous frame. This parameter is not used for fast temporal compression. Specify NULL for this parameter when compressing a key frame, if the compressor does not support temporal compression, or if the compressor does not require an external buffer to store the format and data of the previous image.</param>
        /// <returns>IcResults Returns ICERR_OK if successful or an error otherwise.</returns>
        [DllImport(Vfw32, CallingConvention = CallingConvention.Cdecl)]
        public static extern IcResults ICCompress(IntPtr handle, IcCompressFlags inFlags,
                                             ref BitmapInfoHeader outHeader, IntPtr encodedData,
                                             ref BitmapInfoHeader inHeader, IntPtr frameData,
                                             out int chunkID, out AviIndexFlags outFlags, int frameNumber,
                                             int requestedFrameSize, int requestedQuality,
                                             IntPtr prevHeaderPtr, IntPtr prevFrameData);
    }
}
