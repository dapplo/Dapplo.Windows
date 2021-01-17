// Copyright (c) Dapplo and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Dapplo.Windows.Vfw32.Enums
{
    /// <summary>
    /// See <a href="https://docs.microsoft.com/zh-tw/windows/win32/multimedia/multimedia-messages">Multimedia messages</a>
    /// </summary>
    public enum IcMessages
    {
        /// <summary>
        /// TODO: Define
        /// </summary>
        ICM_USER = 0x4000,
        /// <summary>
        /// The ICM_COMPRESS_GET_FORMAT message requests the output format of the compressed data from a video compression driver. 
        /// </summary>
        ICM_COMPRESS_GET_FORMAT = ICM_USER + 4,
        /// <summary>
        /// The ICM_COMPRESS_GET_SIZE message requests that the video compression driver supply the maximum size of one frame of data when compressed into the specified output format.
        /// </summary>
        ICM_COMPRESS_GET_SIZE = ICM_USER + 5,
        /// <summary>
        /// The ICM_COMPRESS_QUERY message queries a video compression driver to determine if it supports a specific input format or if it can compress a specific input format to a specific output format.
        /// </summary>
        ICM_COMPRESS_QUERY = ICM_USER + 6,
        /// <summary>
        /// The ICM_COMPRESS_BEGIN message notifies a video compression driver to prepare to compress data.
        /// </summary>
        ICM_COMPRESS_BEGIN = ICM_USER + 7,
        /// <summary>
        /// The ICM_COMPRESS message notifies a video compression driver to compress a frame of data into an application-defined buffer.
        /// </summary>
        ICM_COMPRESS = ICM_USER + 8,
        /// <summary>
        /// The ICM_COMPRESS_END message notifies a video compression driver to end compression and free resources allocated for compression. 
        /// </summary>
        ICM_COMPRESS_END = ICM_USER + 9,
        /// <summary>
        /// The ICM_DECOMPRESS_GET_FORMAT message requests the output format of the decompressed data from a video decompression driver.
        /// </summary>
        ICM_DECOMPRESS_GET_FORMAT = ICM_USER + 10,
        /// <summary>
        /// The ICM_DECOMPRESS_QUERY message queries a video decompression driver to determine if it supports a specific input format or if it can decompress a specific input format to a specific output format. 
        /// </summary>
        ICM_DECOMPRESS_QUERY = ICM_USER + 11,
        /// <summary>
        /// The ICM_DECOMPRESS_BEGIN message notifies a video decompression driver to prepare to decompress data. 
        /// </summary>
        ICM_DECOMPRESS_BEGIN = ICM_USER + 12,
        /// <summary>
        /// The ICM_DECOMPRESS message notifies a video decompression driver to decompress a frame of data into an application-defined buffer.
        /// </summary>
        ICM_DECOMPRESS = ICM_USER + 13,
        /// <summary>
        /// The ICM_DECOMPRESS_END message notifies a video decompression driver to end decompression and free resources allocated for decompression.
        /// </summary>
        ICM_DECOMPRESS_END = ICM_USER + 14,
        /// <summary>
        /// The ICM_DECOMPRESS_SET_PALETTE message specifies a palette for a video decompression driver to use if it is decompressing to a format that uses a palette.
        /// </summary>
        ICM_DECOMPRESS_SET_PALETTE = ICM_USER + 29,
        /// <summary>
        /// The ICM_DECOMPRESS_GET_PALETTE message requests that the video decompression driver supply the color table of the output BITMAPINFOHEADER structure.
        /// </summary>
        ICM_DECOMPRESS_GET_PALETTE = ICM_USER + 30,
        /// <summary>
        /// The ICM_DRAW_QUERY message queries a rendering driver to determine if it can render data in a specific format. 
        /// </summary>
        ICM_DRAW_QUERY = ICM_USER + 31,
        /// <summary>
        /// The ICM_DRAW_BEGIN message notifies a rendering driver to prepare to draw data.
        /// </summary>
        ICM_DRAW_BEGIN = ICM_USER + 15,
        /// <summary>
        /// The ICM_DRAW_GET_PALETTE message requests a rendering driver to return a palette.
        /// </summary>
        ICM_DRAW_GET_PALETTE = ICM_USER + 16,
        /// <summary>
        /// TODO: Define
        /// </summary>
        ICM_DRAW_UPDATE = ICM_USER + 17,
        /// <summary>
        /// The ICM_DRAW_START message notifies a rendering driver to start its internal clock for the timing of drawing frames.
        /// </summary>
        ICM_DRAW_START = ICM_USER + 18,
        /// <summary>
        /// The ICM_DRAW_STOP message notifies a rendering driver to stop its internal clock for the timing of drawing frames.
        /// </summary>
        ICM_DRAW_STOP = ICM_USER + 19,
        /// <summary>
        /// TODO: Define
        /// </summary>
        ICM_DRAW_BITS = ICM_USER + 20,
        /// <summary>
        /// The ICM_DRAW_END message notifies a rendering driver to decompress the current image to the screen and to release resources allocated for decompression and drawing. 
        /// </summary>
        ICM_DRAW_END = ICM_USER + 21,
        /// <summary>
        /// The ICM_DRAW_GETTIME message requests a rendering driver that controls the timing of drawing frames to return the current value of its internal clock. 
        /// </summary>
        ICM_DRAW_GETTIME = ICM_USER + 32,
        /// <summary>
        /// The ICM_DRAW message notifies a rendering driver to decompress a frame of data and draw it to the screen.
        /// </summary>
        ICM_DRAW = ICM_USER + 33,
        /// <summary>
        /// The ICM_DRAW_WINDOW message notifies a rendering driver that the window specified for the ICM_DRAW_BEGIN message needs to be redrawn. The window has moved or become temporarily obscured.
        /// </summary>
        ICM_DRAW_WINDOW = ICM_USER + 34,
        /// <summary>
        /// The ICM_DRAW_SETTIME provides synchronization information to a rendering driver that handles the timing of drawing frames. The synchronization information is the sample number of the frame to draw.
        /// </summary>
        ICM_DRAW_SETTIME = ICM_USER + 35,
        /// <summary>
        /// The ICM_DRAW_REALIZE message notifies a rendering driver to realize its drawing palette while drawing. 
        /// </summary>
        ICM_DRAW_REALIZE = ICM_USER + 36,
        /// <summary>
        /// he ICM_DRAW_FLUSH message notifies a rendering driver to render the contents of any image buffers that are waiting to be drawn. 
        /// </summary>
        ICM_DRAW_FLUSH = ICM_USER + 37,
        /// <summary>
        /// The ICM_DRAW_RENDERBUFFER message notifies a rendering driver to draw the frames that have been passed to it. 
        /// </summary>
        ICM_DRAW_RENDERBUFFER = ICM_USER + 38,
        /// <summary>
        /// The ICM_DRAW_START_PLAY message provides the start and end times of a play operation to a rendering driver.
        /// </summary>
        ICM_DRAW_START_PLAY = ICM_USER + 39,
        /// <summary>
        /// The ICM_DRAW_STOP_PLAY message notifies a rendering driver when a play operation is complete. 
        /// </summary>
        ICM_DRAW_STOP_PLAY = ICM_USER + 40,
        /// <summary>
        /// The ICM_DRAW_SUGGESTFORMAT message queries a rendering driver to suggest a decompressed format that it can draw.
        /// </summary>
        ICM_DRAW_SUGGESTFORMAT = ICM_USER + 50,
        /// <summary>
        /// The ICM_DRAW_CHANGEPALETTE message notifies a rendering driver that the movie palette is changing. 
        /// </summary>
        ICM_DRAW_CHANGEPALETTE = ICM_USER + 51,
        /// <summary>
        /// TODO: Define
        /// </summary>
        ICM_DRAW_IDLE = ICM_USER + 52,
        /// <summary>
        /// The ICM_GETBUFFERSWANTED message queries a driver for the number of buffers to allocate. 
        /// </summary>
        ICM_GETBUFFERSWANTED = ICM_USER + 41,
        /// <summary>
        /// The ICM_GETDEFAULTKEYFRAMERATE message queries a video compression driver for its default (or preferred) key-frame spacing. 
        /// </summary>
        ICM_GETDEFAULTKEYFRAMERATE = ICM_USER + 42,
        /// <summary>
        /// The ICM_DECOMPRESSEX_BEGIN message notifies a video compression driver to prepare to decompress data.
        /// </summary>
        ICM_DECOMPRESSEX_BEGIN = ICM_USER + 60,
        /// <summary>
        /// The ICM_DECOMPRESSEX_QUERY message queries a video compression driver to determine if it supports a specific input format or if it can decompress a specific input format to a specific output format.
        /// </summary>
        ICM_DECOMPRESSEX_QUERY = ICM_USER + 61,
        /// <summary>
        /// The ICM_DECOMPRESSEX message notifies a video compression driver to decompress a frame of data directly to the screen, decompress to an upside-down DIB, or decompress images described with source and destination rectangles.
        /// </summary>
        ICM_DECOMPRESSEX = ICM_USER + 62,
        /// <summary>
        /// The ICM_DECOMPRESSEX_END message notifies a video decompression driver to end decompression and free resources allocated for decompression. You can send this message explicitly or by using the ICDecompressExEnd macro.
        /// </summary>
        ICM_DECOMPRESSEX_END = ICM_USER + 63,
        /// <summary>
        /// The ICM_COMPRESS_FRAMES_INFO message notifies a compression driver to set the parameters for the pending compression.
        /// </summary>
        ICM_COMPRESS_FRAMES_INFO = ICM_USER + 70,
        /// <summary>
        /// TODO: Define
        /// </summary>
        ICM_COMPRESS_FRAMES = ICM_USER + 71,
        /// <summary>
        /// The ICM_SET_STATUS_PROC message provides a status callback function with the status of a lengthy operation.
        /// </summary>
        ICM_SET_STATUS_PROC = ICM_USER + 72,
    }
}
