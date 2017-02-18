using System;

namespace Dapplo.Windows.Enums
{
	/// <summary>
	/// These flags define which values are retrieved and if they are cached or not
	/// </summary>
	[Flags]
	public enum InteropWindowCacheFlags : uint
	{
		/// <summary>
		/// 
		/// </summary>
		None = 0,
		/// <summary>
		/// Forces an update of the specified flags.
		/// </summary>
		ForceUpdate = 1 << 0,
		/// <summary>
		/// Retrieve the bounds
		/// </summary>
		Bounds = 1 << 1,
		/// <summary>
		/// Retrieve the client bounds
		/// </summary>
		ClientBounds = 1 << 2,
		/// <summary>
		/// Retrieve the caption
		/// </summary>
		Caption = 1 << 3,
		/// <summary>
		/// Retrieve the class name
		/// </summary>
		Classname = 1 << 4,
		/// <summary>
		/// Retrieve the extended style
		/// </summary>
		ExtendedStyle = 1 << 5,
		/// <summary>
		/// Retrieve the style
		/// </summary>
		Style = 1 << 6,
		/// <summary>
		/// Retrieve the matching process id
		/// </summary>
		ProcessId = 1 << 7,
		/// <summary>
		/// Retrieve the parent
		/// </summary>
		Parent = 1 << 8,
		/// <summary>
		/// Retrieve the placement
		/// </summary>
		Placement = 1 << 9,
		/// <summary>
		/// Retrieve the is visible
		/// </summary>
		Visible = 1 << 10,
		/// <summary>
		/// Retrieve the zoom state (maximized)
		/// </summary>
		Maximized = 1 << 11,
		/// <summary>
		/// Retrieve the icon state (minimized)
		/// </summary>
		Minimized = 1 << 12,
		/// <summary>
		/// Retrieve the children
		/// </summary>
		Children = 1 << 13,
		/// <summary>
		/// Retrieve the text
		/// </summary>
		Text = 1 << 14,
		/// <summary>
		/// Retrieve the scroll info
		/// </summary>
		ScrollInfo = 1 << 15,
		/// <summary>
		/// Cache all, don't force
		/// </summary>
		CacheAll = 0xfffffffe,
	}
}
