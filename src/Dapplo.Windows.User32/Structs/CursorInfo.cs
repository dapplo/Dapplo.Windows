// Copyright (c) Dapplo and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
using System;
using System.Runtime.InteropServices;
using Dapplo.Windows.Common.Structs;
using Dapplo.Windows.User32.Enums;

namespace Dapplo.Windows.User32.Structs;

/// <summary>
/// See <a href="https://msdn.microsoft.com/en-us/library/windows/desktop/ms648381(v=vs.85).aspx"></a>
/// </summary>
[StructLayout(LayoutKind.Sequential)]
public struct CursorInfo
{
	/// <summary>
	/// Size of the struct
	/// </summary>
	private int _cbSize;

	private readonly CursorInfoFlags _flags;
	private readonly IntPtr _hCursor;
	private readonly NativePoint _ptScreenPos;


	/// <summary>
	/// The cursor state, as CursorInfoFlags
	/// </summary>
	public CursorInfoFlags Flags => _flags;

	/// <summary>
	/// Handle (IntPtr) to the Cursor
	/// </summary>
	public IntPtr CursorHandle => _hCursor;

	/// <summary>
	/// A structure that receives the screen coordinates of the cursor.
	/// </summary>
	public NativePoint Location => _ptScreenPos;

	/// <summary>
	/// Factory for the structure
	/// </summary>
	public static CursorInfo Create()
	{
		var result = new CursorInfo
		{
			_cbSize = Marshal.SizeOf(typeof(CursorInfo))
		};
		return result;
	}
}