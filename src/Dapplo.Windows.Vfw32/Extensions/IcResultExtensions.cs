using Dapplo.Windows.Vfw32.Enums;

namespace Dapplo.Windows.Vfw32.Extensions
{
    /// <summary>
    /// Extension methods for IcResults
    /// </summary>
    public static class IcResultExtensions
    {
        /// <summary>
        /// Test if the call was successful.
        /// </summary>
        /// <param name="result">IcResults</param>
        /// <returns>bool</returns>
        public static bool IsSuccess(this IcResults result) => result == IcResults.ICERR_OK;

        /// <summary>
        /// Get a description for the IcResults
        /// </summary>
        /// <param name="error">IcResults</param>
        /// <returns>string</returns>
        public static string GetErrorDescription(this IcResults error) =>
            error switch
            {
                IcResults.ICERR_OK => "OK",
                IcResults.ICERR_UNSUPPORTED => "Unsupported",
                IcResults.ICERR_BAD_FORMAT => "Bad format",
                IcResults.ICERR_MEMORY => "Memory",
                IcResults.ICERR_INTERNAL => "Internal",
                IcResults.ICERR_BAD_FLAGS => "Bad flags",
                IcResults.ICERR_BAD_PARAMETER => "Bad parameter",
                IcResults.ICERR_BAD_SIZE => "Bad size",
                IcResults.ICERR_BAD_HANDLE => "Bad handle",
                IcResults.ICERR_CANNOT_UPDATE => "Can't update",
                IcResults.ICERR_ABORT => "Abort",
                IcResults.ICERR_ERROR => "Error",
                IcResults.ICERR_BAD_BIT_DEPTH => "Bad bit depth",
                IcResults.ICERR_BAD_IMAGE_SIZE => "Bad image size",
                _ => ("Unknown " + error)
            };
    }
}
