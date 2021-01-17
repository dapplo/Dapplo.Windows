namespace Dapplo.Windows.Vfw32.Enums
{
    /// <summary>
    /// See <a href="https://docs.microsoft.com/en-us/windows/win32/api/vfw/ns-vfw-iccompress">ICCOMPRESS structure</a>
    /// The ICCOMPRESS structure contains compression parameters used with the ICM_COMPRESS message.
    /// </summary>
    public enum IcCompressFlags
    {
        /// <summary>
        /// Default frame
        /// </summary>
        ICCOMPRESS_NONE = 0,
        /// <summary>
        /// Input data should be treated as a key frame.
        /// </summary>
        ICCOMPRESS_KEYFRAME = 0x00000001
    }
}
