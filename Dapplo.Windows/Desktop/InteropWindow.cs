//  Dapplo - building blocks for desktop applications
//  Copyright (C) 2016 Dapplo
// 
//  For more information see: http://dapplo.net/
//  Dapplo repositories are hosted on GitHub: https://github.com/dapplo
// 
//  This file is part of Dapplo.Windows
// 
//  Dapplo.Windows is free software: you can redistribute it and/or modify
//  it under the terms of the GNU Lesser General Public License as published by
//  the Free Software Foundation, either version 3 of the License, or
//  (at your option) any later version.
// 
//  Dapplo.Windows is distributed in the hope that it will be useful,
//  but WITHOUT ANY WARRANTY; without even the implied warranty of
//  MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
//  GNU Lesser General Public License for more details.
// 
//  You should have a copy of the GNU Lesser General Public License
//  along with Dapplo.Windows. If not, see <http://www.gnu.org/licenses/lgpl.txt>.

#region using

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Dapplo.Windows.Enums;
using Dapplo.Windows.Structs;

#endregion

namespace Dapplo.Windows.Desktop
{
	/// <summary>
	/// Information about a native window
	/// Note: This is a dumb container, and doesn't retrieve anything about the window itself
	/// </summary>
	public class InteropWindow : IEquatable<InteropWindow>
	{
		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="handle"></param>
		internal InteropWindow(IntPtr handle)
		{
			Handle = handle;
		}

		/// <summary>
		/// Handle (ID) of the window
		/// </summary>
		public IntPtr Handle { get; }

		/// <summary>
		/// Returns the bounds of this window
		/// </summary>
		public RECT? Bounds { get; set; }

		/// <summary>
		/// Returns the client bounds of this window
		/// </summary>
		public RECT? ClientBounds { get; set; }

		/// <summary>
		/// Returns the children of this window
		/// </summary>
		public IEnumerable<InteropWindow> Children { get; set; }

		/// <summary>
		/// Test if there are any children
		/// </summary>
		public bool HasChildren => Children?.Any() == true;

		/// <summary>
		/// string with the name of the internal class for the window
		/// </summary>
		public string Classname { get; set; }

		/// <summary>
		/// Does the window have a classname?
		/// </summary>
		public bool HasClassname => !string.IsNullOrEmpty(Classname);

		/// <summary>
		/// Does this window have parent?
		/// </summary>
		public bool HasParent => Parent.HasValue && Parent != IntPtr.Zero;

		/// <summary>
		/// The parent window to which this window belongs
		/// </summary>
		public IntPtr? Parent { get; set; }

		/// <summary>
		/// Return the title of the window, if any
		/// </summary>
		public string Caption { get; set; }

		/// <summary>
		/// Return the text (not title) of the window, if any
		/// </summary>
		public string Text { get; set; }

		/// <summary>
		/// Returns true if the window is visible
		/// </summary>
		public bool? IsVisible { get; set; }

		/// <summary>
		/// Returns true if the window is minimized
		/// </summary>
		public bool? IsMinimized { get; set; }

		/// <summary>
		/// Returns true if the window is maximized
		/// </summary>
		public bool? IsMaximized { get; set; }

		/// <summary>
		/// Get the process ID this window belongs to
		/// </summary>
		public int? ProcessId { get; set; }

		/// <summary>
		/// ExtendedWindowStyleFlags for the Window
		/// </summary>
		public ExtendedWindowStyleFlags? ExtendedStyle { get; set; }

		/// <summary>
		/// WindowStyleFlags for the Window
		/// </summary>
		public WindowStyleFlags? Style { get; set; }


		/// <summary>
		/// WindowPlacement for the Window
		/// </summary>
		public WindowPlacement? Placement { get; set; }

		/// <summary>
		///     Implicit cast IntPtr to InteropWindow
		/// </summary>
		/// <param name="handle">IntPtr</param>
		/// <returns>InteropWindow</returns>
		public static implicit operator InteropWindow(IntPtr handle)
		{
			return CreateFor(handle);
		}

		/// <summary>
		///     Implicit cast InteropWindow to IntPtr
		/// </summary>
		/// <param name="interopWindow">InteropWindow</param>
		/// <returns>IntPtr for the handle</returns>
		public static implicit operator IntPtr(InteropWindow interopWindow)
		{
			return interopWindow.Handle;
		}

		/// <inheritdoc />
		public override int GetHashCode()
		{
			return Handle.GetHashCode();
		}

		/// <inheritdoc />
		public override bool Equals(object obj)
		{
			if (ReferenceEquals(null, obj)) return false;
			if (ReferenceEquals(this, obj)) return true;
			if (obj.GetType() != GetType()) return false;
			return Equals((InteropWindow) obj);
		}

		/// <summary>
		/// Factory method to create a InteropWindow for the supplied handle
		/// </summary>
		/// <param name="handle">IntPtr</param>
		/// <returns>InteropWindow</returns>
		public static InteropWindow CreateFor(IntPtr handle)
		{
			return new InteropWindow(handle);
		}

		/// <inheritdoc />
		public bool Equals(InteropWindow other)
		{
			if (ReferenceEquals(null, other)) return false;
			if (ReferenceEquals(this, other)) return true;
			return Handle.Equals(other.Handle);
		}

		/// <inheritdoc />
		public static bool operator ==(InteropWindow left, InteropWindow right)
		{
			return Equals(left, right);
		}

		/// <summary>
		/// Dump the information in the InteropWindow for debugging
		/// </summary>
		/// <param name="dump">StringBuilder to dump to</param>
		/// <param name="indentation">int</param>
		/// <returns>StringBuilder</returns>
		public StringBuilder Dump(StringBuilder dump = null, string indentation = "")
		{
			dump = dump ?? new StringBuilder();
			this.Fill(false);
			dump.AppendLine($"{indentation}{nameof(Handle)}={Handle}");
			dump.AppendLine($"{indentation}{nameof(Classname)}={Classname}");
			dump.AppendLine($"{indentation}{nameof(Caption)}={Caption}");
			dump.AppendLine($"{indentation}{nameof(Text)}={Text}");
			dump.AppendLine($"{indentation}{nameof(Bounds)}={Bounds}");
			dump.AppendLine($"{indentation}{nameof(ClientBounds)}={Bounds}");
			dump.AppendLine($"{indentation}{nameof(ExtendedStyle)}={ExtendedStyle}");
			dump.AppendLine($"{indentation}{nameof(Style)}={Style}");
			dump.AppendLine($"{indentation}{nameof(IsMaximized)}={IsMaximized}");
			dump.AppendLine($"{indentation}{nameof(IsMinimized)}={IsMinimized}");
			dump.AppendLine($"{indentation}{nameof(IsVisible)}={IsVisible}");
			dump.AppendLine($"{indentation}{nameof(Parent)}={Parent}");
			if (!HasParent)
			{
				foreach (var child in this.GetZOrderedChildren())
				{
					child.Dump(dump, indentation + "\t");
				}
			}
			return dump;
		}

		/// <inheritdoc />
		public static bool operator !=(InteropWindow left, InteropWindow right)
		{
			return !Equals(left, right);
		}
	}
}