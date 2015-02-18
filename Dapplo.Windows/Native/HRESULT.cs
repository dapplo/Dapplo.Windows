using System.Runtime.InteropServices;

namespace Dapplo.Windows.Native {
	public enum HRESULT : long {
		S_OK = 0,
		S_FALSE = 1,
		E_FAIL = unchecked((int)0x80004005),
		E_INVALIDARG = unchecked((int)0x80070057),
		E_NOTIMPL = unchecked((int)0x80004001),
		E_POINTER = unchecked((int)0x80004003),
		E_PENDING = unchecked((int)0x8000000A),
		E_NOINTERFACE = unchecked((int)0x80004002),
		E_ABORT = unchecked((int)0x80004004),
		E_ACCESSDENIED = unchecked((int)0x80070006),
		E_HANDLE = unchecked((int)0x80070006),
		E_UNEXPECTED = unchecked((int)0x8000FFFF),
		E_FILENOTFOUND = unchecked((int)0x80070002),
		E_PATHNOTFOUND = unchecked((int)0x80070003),
		E_INVALID_DATA = unchecked((int)0x8007000D),
		E_OUTOFMEMORY = unchecked((int)0x8007000E),
		E_INSUFFICIENT_BUFFER = unchecked((int)0x8007007A),
		WSAECONNABORTED = unchecked((int)0x80072745),
		WSAECONNRESET = unchecked((int)0x80072746),
		ERROR_TOO_MANY_CMDS = unchecked((int)0x80070038),
		ERROR_NOT_SUPPORTED = unchecked((int)0x80070032),
		TYPE_E_ELEMENTNOTFOUND = unchecked((int)0x8002802B)
	}
	/// <summary>
	/// Wrapper util to handle the HResult
	/// </summary>
	public static class HResult {

		public static bool Failed(HRESULT hResult) {
			return hResult < 0;
		}

		public static bool Succeeded(HRESULT hResult) {
			return hResult >= 0;
		}

		public static void ThrowOnFailure(HRESULT hResult) {
			if (Failed(hResult)) throw Marshal.GetExceptionForHR((int)hResult);
		}
	}
}
