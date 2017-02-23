using System;
using System.Collections.Generic;
using System.Windows.Forms;
using System.Windows.Interop;

namespace Dapplo.Windows.Forms
{
	/// <summary>
	/// This is a Listener for WinProc messages
	/// </summary>
	public class WinProcListener : NativeWindow, IDisposable
	{
		private readonly IList<HwndSourceHook> _hooks = new List<HwndSourceHook>();
		/// <summary>
		/// Adds an event handler
		/// </summary>
		/// <param name="hook"></param>
		public void AddHook(HwndSourceHook hook)
		{
			_hooks.Add(hook);
		}

		/// <summary>
		/// Removes the event handlers that were added by AddHook
		/// </summary>
		/// <param name="hook">The event handler to remove.</param>
		public void RemoveHook(HwndSourceHook hook)
		{
			_hooks.Remove(hook);
		}

		/// <summary>
		/// Constructor for a window listener
		/// </summary>
		/// <param name="parent">Control to listen to</param>
		public WinProcListener(Control parent)
		{
			parent.HandleCreated += OnHandleCreated;
			parent.HandleDestroyed += OnHandleDestroyed;
		}

		/// <summary>
		/// Listen for the control's window creation and then hook into it.
		/// </summary>
		/// <param name="sender">object</param>
		/// <param name="e">EventArgs</param>
		private void OnHandleCreated(object sender, EventArgs e)
		{
			// Window is now created, assign handle to NativeWindow.
			AssignHandle(((Form)sender).Handle);
		}

		/// <summary>
		/// Remove the handle
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		private void OnHandleDestroyed(object sender, EventArgs e)
		{
			// Window was destroyed, release hook.
			ReleaseHandle();
		}

		/// <inheritdoc />
		[System.Security.Permissions.PermissionSet(System.Security.Permissions.SecurityAction.Demand, Name = "FullTrust")]
		protected override void WndProc(ref Message m)
		{
			bool handled = false;
			foreach (var hwndSourceHook in _hooks)
			{
				m.Result = hwndSourceHook.Invoke(m.HWnd, m.Msg, m.WParam, m.LParam, ref handled);
				if (handled)
				{
					break;
				}
			}
			if (!handled)
			{
				base.WndProc(ref m);
			}
		}

		/// <inheritdoc />
		public void Dispose()
		{
			_hooks.Clear();
			ReleaseHandle();
		}
	}
}
