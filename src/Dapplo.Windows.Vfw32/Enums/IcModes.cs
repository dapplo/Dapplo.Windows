namespace Dapplo.Windows.Vfw32.Enums
{
    public enum IcModes : uint
    {
        /// <summary>
        /// Compressor will perform normal compression.
        /// </summary>
        ICMODE_COMPRESS = 1,
        /// <summary>
        /// Decompressor will perform normal decompression.
        /// </summary>
        ICMODE_DECOMPRESS,
        /// <summary>
        ///  Decompressor will decompress and draw the data directly to hardware.
        /// </summary>
        ICMODE_DRAW,
        /// <summary>
        ///  Compressor will perform fast (real-time) compression.
        /// </summary>
        ICMODE_FASTCOMPRESS,
        /// <summary>
        /// Decompressor will perform fast (real-time) decompression.
        /// </summary>
        ICMODE_FASTDECOMPRESS,
        /// <summary>
        ///  Queries the compressor or decompressor for information.
        /// </summary>
        ICMODE_QUERY
    }
}
