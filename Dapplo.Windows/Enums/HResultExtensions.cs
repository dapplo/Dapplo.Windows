using System.Runtime.InteropServices;

namespace Dapplo.Windows.Enums
{
	/// <summary>
	///     Extensions to handle the HResult
	/// </summary>
	public static class HResultExtensions
	{
		/// <summary>
		/// Test if the HResult respresents a fail
		/// </summary>
		/// <param name="hResult">HResult</param>
		/// <returns>bool</returns>
		public static bool Failed(this HResult hResult)
		{
			return hResult < 0;
		}

		/// <summary>
		/// Test if the HResult respresents a success
		/// </summary>
		/// <param name="hResult">HResult</param>
		/// <returns>bool</returns>
		public static bool Succeeded(this HResult hResult)
		{
			return hResult >= 0;
		}

		/// <summary>
		/// Throw an exception on Failure
		/// </summary>
		/// <param name="hResult">HResult</param>
		public static void ThrowOnFailure(this HResult hResult)
		{
			if (Failed(hResult))
			{
				throw Marshal.GetExceptionForHR((int) hResult);
			}
		}
	}
}