// Copyright (c) Dapplo and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
using System;
using System.Runtime.InteropServices;
using Dapplo.Windows.Common.Structs;
using Dapplo.Windows.User32.Enums;

namespace Dapplo.Windows.User32.Structs;

/// <summary>
///     See<a href="https://msdn.microsoft.com/en-us/library/windows/desktop/bb787535(v=vs.85).aspx">SCROLLBARINFO structure</a>
/// </summary>
[Serializable]
[StructLayout(LayoutKind.Sequential)]
public struct ScrollBarInfo
{
	/// <summary>
	///     Size of this struct
	/// </summary>
	private uint _cbSize;

	private NativeRect _rcScrollBar;
	private int _dxyLineButton;
	private int _thumbBottom;
	private int _thumbTop;
	private int _reserved;
	[MarshalAs(UnmanagedType.ByValArray, SizeConst = 6)]
	private ObjectStates[] _states;

	/// <summary>
	///     Coordinates of the scroll bar as specified in a RECT structure.
	/// </summary>
	public NativeRect Bounds => _rcScrollBar;

	/// <summary>
	///     Height or width of the thumb.
	/// </summary>
	public int ThumbSize => _dxyLineButton;

	/// <summary>
	///     Position of the bottom or right of the thumb.
	/// </summary>
	public int ThumbBottom => _thumbBottom;

	/// <summary>
	///     Position of the top or left of the thumb.
	/// </summary>
	public int ThumbTop => _thumbTop;

	/// <summary>
	///     An array of object states.
	/// Each element indicates the state of a scroll bar component, the element is specified via the ScrollBarStateIndexes.
	/// </summary>
	public ObjectStates[] States => _states;

	/// <inheritdoc />
	public override string ToString()
	{
		var statesString = string.Join(",", States);
		return $"{{Bounds = {Bounds}; ThumbSize = {ThumbSize};ThumbBottom = {ThumbBottom};ThumbTop = {ThumbTop};States = {statesString};}}";
	}

	/// <summary>
	///     Create a ScrollBarInfo struct
	/// </summary>
	public static ScrollBarInfo Create()
	{
		return new ScrollBarInfo
		{
			_cbSize = (uint)Marshal.SizeOf(typeof(ScrollBarInfo)),
			_states = new ObjectStates[6],
			_rcScrollBar = new NativeRect(),
			_dxyLineButton = 0,
			_thumbBottom = 0,
			_thumbTop = 0,
			_reserved = 0
		};
	}
}