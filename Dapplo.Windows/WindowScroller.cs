#region Dapplo 2017 - GNU Lesser General Public License

// Dapplo - building blocks for .NET applications
// Copyright (C) 2017 Dapplo
// 
// For more information see: http://dapplo.net/
// Dapplo repositories are hosted on GitHub: https://github.com/dapplo
// 
// This file is part of Dapplo.Windows
// 
// Dapplo.Windows is free software: you can redistribute it and/or modify
// it under the terms of the GNU Lesser General Public License as published by
// the Free Software Foundation, either version 3 of the License, or
// (at your option) any later version.
// 
// Dapplo.Windows is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU Lesser General Public License for more details.
// 
// You should have a copy of the GNU Lesser General Public License
// along with Dapplo.Windows. If not, see <http://www.gnu.org/licenses/lgpl.txt>.

#endregion

#region Usings

using System;
using System.Collections.Generic;
using Dapplo.Log;
using Dapplo.Windows.Desktop;
using Dapplo.Windows.Enums;
using Dapplo.Windows.Native;
using Dapplo.Windows.Structs;
using Microsoft.Win32;

#endregion

namespace Dapplo.Windows
{
	/// <summary>
	///     The is a container class to scroll a window
	/// </summary>
	public class WindowScroller
	{
		private static readonly LogSource Log = new LogSource();

		/// <summary>
		/// Method to set the ScrollbarInfo, if we can get it
		/// </summary>
		internal void FillScrollbarInfo()
		{
			var objectId = ObjectIdentifiers.Client;
			switch (ScrollBar)
			{
				case ScrollBarTypes.Control:
					objectId = ObjectIdentifiers.Client;
					break;
				case ScrollBarTypes.Vertical:
					objectId = ObjectIdentifiers.VerticalScrollbar;
					break;
				case ScrollBarTypes.Horizontal:
					objectId = ObjectIdentifiers.HorizontalScrollbar;
					break;

			}
			var scrollbarInfo = new ScrollBarInfo(true);
			bool hasScrollbarInfo = User32.GetScrollBarInfo(ScrollingArea.Handle, objectId, ref scrollbarInfo);
			if (!hasScrollbarInfo)
			{
				var error = Win32.GetLastErrorCode();
				Log.Verbose().WriteLine("Error retrieving Scrollbar info : {0}", Win32.GetMessage(error));
			}
			else
			{
				ScrollBarInfo = scrollbarInfo;
			}
		}

		/// <summary>
		///     Area which is scrolling, can be the WindowToScroll
		/// </summary>
		public IInteropWindow ScrollingArea { get; set; }

		/// <summary>
		///     What scrollbar to use
		/// </summary>
		public ScrollBarTypes ScrollBar { get; internal set; } = ScrollBarTypes.Vertical;

		/// <summary>
		/// Get the information on the used scrollbar, if any.
		/// This can be used to detect the location of the scrollbar
		/// </summary>
		public ScrollBarInfo? ScrollBarInfo { get; internal set; }

		/// <summary>
		///     Does the scrollbar need to represent the changes?
		/// </summary>
		public bool ShowChanges { get; set; } = true;

		/// <summary>
		/// Specify which scroll mode needs to be used
		/// </summary>
		public ScrollModes ScrollMode { get; set; } = ScrollModes.WindowsMessage;

		/// <summary>
		/// This is used to be able to reset the location, and also detect if we are at the end.
		/// Some windows might add content when the user is (almost) at the end.
		/// </summary>
		public ScrollInfo InitialScrollInfo { get; internal set; }

		/// <summary>
		/// Some windows might add content when the user is (almost) at the end.
		/// If this is true, the scrolling doesn't go beyond the intial bounds.
		/// If this is false, the initial value is only used for reset.
		/// </summary>
		public bool KeepInitialBounds { get; set; } = true;

		/// <summary>
		///     Returns true if the scroller is at the start
		/// </summary>
		/// <returns>bool</returns>
		public bool IsAtStart
		{
			get
			{
				ScrollInfo scrollInfo;
				if (!GetPosition(out scrollInfo))
				{
					return false;
				}
				if (KeepInitialBounds)
				{
					return InitialScrollInfo.nMin >= Math.Max(scrollInfo.nPos, scrollInfo.nTrackPos);
				}
				return scrollInfo.nMin >= Math.Max(scrollInfo.nPos, scrollInfo.nTrackPos);
			}
		}

		/// <summary>
		///     Returns true if the scroller is at the end
		/// </summary>
		/// <returns>bool</returns>
		public bool IsAtEnd
		{
			get
			{
				ScrollInfo scrollInfo;
				if (!GetPosition(out scrollInfo))
				{
					return false;
				}
				if (KeepInitialBounds)
				{
					return InitialScrollInfo.nMax <= Math.Max(scrollInfo.nPos, scrollInfo.nTrackPos) + scrollInfo.nPage - 1;
				}
				return scrollInfo.nMax <= Math.Max(scrollInfo.nPos, scrollInfo.nTrackPos) + scrollInfo.nPage - 1;
			}
		}

		/// <summary>
		/// Amount of delta the scrollwheel scrolls
		/// </summary>
		public int WheelDelta { get; set; }

		/// <summary>
		/// Get the scroll-lines from the registry
		/// </summary>
		public static int ScrollWheelLinesFromRegistry
		{
			get
			{
				using (var key = Registry.CurrentUser.OpenSubKey(@"Control Panel\Desktop", false))
				{
					string wheelScrollLines = key?.GetValue("WheelScrollLines") as string;
					if (wheelScrollLines != null)
					{
						int scrollLines;
						if (int.TryParse(wheelScrollLines, out scrollLines))
						{
							return scrollLines;
						}
					}
				}
				return 3;
			}
		}

		/// <summary>
		/// Get current position
		/// </summary>
		/// <returns>SCROLLINFO</returns>
		public bool GetPosition(out ScrollInfo scrollInfo)
		{
			scrollInfo = new ScrollInfo(ScrollInfoMask.All);

			return User32.GetScrollInfo(ScrollingArea.Handle, ScrollBar, ref scrollInfo);
		}

		/// <summary>
		/// Apply position from the scrollInfo
		/// </summary>
		/// <param name="scrollInfo">SCROLLINFO ref</param>
		/// <returns>bool</returns>
		private bool ApplyPosition(ref ScrollInfo scrollInfo)
		{
			if (ShowChanges)
			{
				User32.SetScrollInfo(ScrollingArea.Handle, ScrollBar, ref scrollInfo, true);
			}
			switch (ScrollBar)
			{
					case ScrollBarTypes.Horizontal:
						User32.SendMessage(ScrollingArea.Handle, WindowsMessages.WM_HSCROLL, 4 + 0x10000 * scrollInfo.nPos, 0);
						break;
					case ScrollBarTypes.Vertical:
					case ScrollBarTypes.Control:
						User32.SendMessage(ScrollingArea.Handle, WindowsMessages.WM_VSCROLL, (int)(((uint)ScrollBarCommands.SB_THUMBPOSITION) + (scrollInfo.nPos << 16)), 0);
						break;
			}
			return true;
		}

		/// <summary>
		/// Helper method to send the right message
		/// </summary>
		/// <param name="scrollBarCommand">ScrollBarCommands enum to specify where to scroll</param>
		/// <returns>true if this was possible</returns>
		private bool SendScrollMessage(ScrollBarCommands scrollBarCommand)
		{
			switch (ScrollBar)
			{
				case ScrollBarTypes.Horizontal:
					User32.SendMessage(ScrollingArea.Handle, WindowsMessages.WM_HSCROLL, scrollBarCommand, 0);
					return true;
				case ScrollBarTypes.Vertical:
					User32.SendMessage(ScrollingArea.Handle, WindowsMessages.WM_VSCROLL, scrollBarCommand, 0);
					return true;
				default:
					return false;
			}
		}

		/// <summary>
		/// Retrieve position from the scrollInfo
		/// </summary>
		/// <param name="scrollInfo">ScrollInfo out</param>
		/// <returns>bool</returns>
		private bool TryRetrievePosition(out ScrollInfo scrollInfo)
		{
			bool hasScrollInfo = GetPosition(out scrollInfo);
			if (Log.IsVerboseEnabled())
			{
				if (hasScrollInfo)
				{
					Log.Verbose().WriteLine("Retrieved ScrollInfo: {0}", scrollInfo);
				}
				else
				{
					Log.Verbose().WriteLine("Couldn't get scrollinfo.");
				}
			}
			return hasScrollInfo;
		}

		/// <summary>
		/// Returns true if the window needs focus to scroll
		/// </summary>
		/// <returns>true if focus is needed</returns>
		public bool NeedsFocus()
		{
			return ScrollMode == ScrollModes.KeyboardPageUpDown;
		}

		/// <summary>
		///     Move to the start
		/// </summary>
		/// <returns>bool if this worked</returns>
		public bool Start()
		{
			bool result = false;
			ScrollInfo scrollInfoBefore;
			bool hasScrollInfo = TryRetrievePosition(out scrollInfoBefore);
			switch (ScrollMode)
			{
				case ScrollModes.KeyboardPageUpDown:
					InputGenerator.KeyDown(VirtualKeyCodes.CONTROL);
					InputGenerator.KeyPress(VirtualKeyCodes.HOME);
					InputGenerator.KeyUp(VirtualKeyCodes.CONTROL);
					result = true;
					break;
				case ScrollModes.WindowsMessage:
					result = SendScrollMessage(ScrollBarCommands.SB_TOP);
					break;
				case ScrollModes.AbsoluteWindowMessage:
					result = hasScrollInfo;
					if (hasScrollInfo)
					{
						// Calculate start position, clone the scrollInfoBefore
						var scrollInfoForStart = scrollInfoBefore;
						scrollInfoForStart.nPos = scrollInfoBefore.nMin;
						result = ApplyPosition(ref scrollInfoForStart);
					}
					break;
				case ScrollModes.MouseWheel:
					result = true;
					while (!IsAtStart)
					{
						if (!Previous())
						{
							break;
						}
					}
					break;
			}
			return result;
		}


		/// <summary>
		///     Move to the end
		/// </summary>
		/// <returns>bool if this worked</returns>
		public bool End()
		{
			bool result = false;
			ScrollInfo scrollInfoBefore;
			bool hasScrollInfo = TryRetrievePosition(out scrollInfoBefore);
			switch (ScrollMode)
			{
				case ScrollModes.KeyboardPageUpDown:
					InputGenerator.KeyDown(VirtualKeyCodes.CONTROL);
					InputGenerator.KeyPress(VirtualKeyCodes.END);
					InputGenerator.KeyUp(VirtualKeyCodes.CONTROL);
					result = true;
					break;
				case ScrollModes.WindowsMessage:
					result = SendScrollMessage(ScrollBarCommands.SB_BOTTOM);
					break;
				case ScrollModes.AbsoluteWindowMessage:
					result = hasScrollInfo;
					if (hasScrollInfo)
					{
						// Calculate end position, clone the scrollInfoBefore
						var scrollInfoForEnd = scrollInfoBefore;
						scrollInfoForEnd.nPos = scrollInfoBefore.nMax;
						result = ApplyPosition(ref scrollInfoForEnd);
					}
					break;
				case ScrollModes.MouseWheel:
					result = true;
					while (!IsAtEnd)
					{
						if (!Next())
						{
							break;
						}
					}
					break;
			}
			return result;
		}

		/// <summary>
		///     Go to the previous "page"
		/// </summary>
		/// <returns>bool if this worked</returns>
		public bool Previous()
		{
			bool result = false;
			ScrollInfo scrollInfoBefore;
			bool hasScrollInfo = TryRetrievePosition(out scrollInfoBefore);

			switch (ScrollMode)
			{
				case ScrollModes.KeyboardPageUpDown:
					result = InputGenerator.KeyPress(VirtualKeyCodes.PRIOR) == 2;
					break;
				case ScrollModes.WindowsMessage:
					result = SendScrollMessage(ScrollBarCommands.SB_PAGEUP);
					break;
				case ScrollModes.AbsoluteWindowMessage:
					if (!hasScrollInfo)
					{
						return false;
					}
					// Calculate previous position, clone the scrollInfoBefore
					var scrollInfoForPrevious = scrollInfoBefore;
					scrollInfoForPrevious.nPos = Math.Max(scrollInfoBefore.nMin, scrollInfoBefore.nPos - (int)scrollInfoBefore.nPage);
					result = ApplyPosition(ref scrollInfoForPrevious);
					break;
				case ScrollModes.MouseWheel:
					var bounds = ScrollingArea.GetBounds();
					var middlePoint = new POINT(bounds.X + bounds.Width / 2, bounds.Y + bounds.Height / 2);
					result = InputGenerator.MoveMouseWheel(WheelDelta, middlePoint) == 1;
					break;
			}
			return result;
		}

		/// <summary>
		///     Go to the next "page"
		/// </summary>
		/// <returns>bool if this worked</returns>
		public bool Next()
		{
			bool result = false;
			ScrollInfo scrollInfoBefore;
			bool hasScrollInfo = TryRetrievePosition(out scrollInfoBefore);

			switch (ScrollMode)
			{
				case ScrollModes.KeyboardPageUpDown:
					result = InputGenerator.KeyPress(VirtualKeyCodes.NEXT) == 2;
					break;
				case ScrollModes.WindowsMessage:
					result = SendScrollMessage(ScrollBarCommands.SB_PAGEDOWN);
					break;
				case ScrollModes.AbsoluteWindowMessage:
					if (!hasScrollInfo)
					{
						return false;
					}
					// Calculate next position, clone the scrollInfoBefore
					var scrollInfoForPrevious = scrollInfoBefore;
					scrollInfoForPrevious.nPos = Math.Min(scrollInfoBefore.nMax, scrollInfoBefore.nPos + (int)scrollInfoBefore.nPage);
					result = ApplyPosition(ref scrollInfoForPrevious);
					break;
				case ScrollModes.MouseWheel:
					var bounds = ScrollingArea.GetBounds();
					var middlePoint = new POINT(bounds.X + bounds.Width / 2, bounds.Y + bounds.Height / 2);
					result = InputGenerator.MoveMouseWheel(-WheelDelta, middlePoint) == 1;
					break;
			}
			return result;
		}

		/// <summary>
		/// Set the position back to the original, only works for windows which support ScrollModes.WindowsMessage
		/// </summary>
		/// <returns>true if this worked</returns>
		public bool Reset()
		{
			var initialScrollInfo = InitialScrollInfo;
			return ApplyPosition(ref initialScrollInfo);
		}
	}
}