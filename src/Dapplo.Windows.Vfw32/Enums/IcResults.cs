namespace Dapplo.Windows.Vfw32.Enums
{
    /// <summary>
    /// Return values for various vfw functions
    /// </summary>
    public enum IcResults
    {
        /// <summary>
        /// OK
        /// </summary>
        ICERR_OK = 0,
        ICERR_UNSUPPORTED = -1,
        ICERR_BAD_FORMAT = -2,
        ICERR_MEMORY = -3,
        ICERR_INTERNAL = -4,
        ICERR_BAD_FLAGS = -5,
        ICERR_BAD_PARAMETER = -6,
        ICERR_BAD_SIZE = -7,
        ICERR_BAD_HANDLE = -8,
        ICERR_CANNOT_UPDATE = -9,
        ICERR_ABORT = -10,
        ICERR_ERROR = -100,
        ICERR_BAD_BIT_DEPTH = -200,
        ICERR_BAD_IMAGE_SIZE = -201
    }
}
