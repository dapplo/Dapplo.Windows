/*
 * dapplo - building blocks for desktop applications
 * Copyright (C) 2015 Robin Krom
 * 
 * For more information see: http://dapplo.net/
 * dapplo repositories are hosted on GitHub: https://github.com/dapplo
 * 
 * This program is free software: you can redistribute it and/or modify
 * it under the terms of the GNU General Public License as published by
 * the Free Software Foundation, either version 1 of the License, or
 * (at your option) any later version.
 * 
 * This program is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 * GNU General Public License for more details.
 * 
 * You should have received a copy of the GNU General Public License
 * along with this program. If not, see <http://www.gnu.org/licenses/>.
 */

using Dapplo.Windows.Native;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Windows.Input;

namespace Dapplo.Windows {
	/// <summary>
	/// Plain hook delegate for the callback of the WindowsHook
	/// This delegate is NOT called if nCode lt 0, if so the CallNextHookEx is called
	/// This delegate does not need to call CallNextHookEx itself.
	/// </summary>
	/// <param name="nCode"></param>
	/// <param name="wParam"></param>
	/// <param name="lParam"></param>
	/// <returns>true if other hooks need to be called</returns>
	public delegate bool BasicHookDelegate(int nCode, IntPtr wParam, IntPtr lParam);

	/// <summary>
	/// Keyboard hook delegate for the callback of the WindowHook Keyboard hooking
	/// This delegate is NOT called if nCode lt 0, if so the CallNextHookEx is called
	/// This delegate does not need to call CallNextHookEx itself.
	/// </summary>
	/// <param name="windowsMessage">WindowsMessages</param>
	/// <param name="key">Key</param>
	/// <returns>true if other hooks need to be called</returns>
	public delegate bool KeyboardHookDelegate(WindowsMessages windowsMessage, Key key);


	/// <summary>
	/// Mouse hook delegate for the callback of the WindowHook Mouse hooking
	/// This delegate is NOT called if nCode lt 0, if so the CallNextHookEx is called
	/// This delegate does not need to call CallNextHookEx itself.
	/// </summary>
	/// <param name="windowsMessage">WindowsMessages</param>
	/// <param name="mouseHookStruct">MSLLHOOK</param>
	/// <returns>true if other hooks need to be called</returns>
	public delegate bool MouseHookDelegate(WindowsMessages windowsMessage, MSLLHOOK mouseHookStruct);

	/// <summary>
	/// This class makes it possible to use the SetWindowsHookEx simplified.
	/// Make sure your application has a message pump, otherwise the hook won't work!
	/// </summary>
	public class WindowsHook {
		/// <summary>
		/// Delegate for the callback of the WindowsHook
		/// </summary>
		private delegate IntPtr InternalHookDelegate(int nCode, IntPtr wParam, IntPtr lParam);

		#region DllImports
		[DllImport("user32.dll")]
		private static extern IntPtr SetWindowsHookEx(HookType hookType, InternalHookDelegate callback, IntPtr hInstance, uint threadId);
		[DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
		[return: MarshalAs(UnmanagedType.Bool)]
		private static extern bool UnhookWindowsHookEx(IntPtr hhk);
		[DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
		private static extern IntPtr CallNextHookEx(IntPtr hhk, int nCode, IntPtr wParam, IntPtr lParam);
		[DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
		private static extern IntPtr GetModuleHandle(string lpModuleName);
		#endregion

		// Store the current hooks here, this allows us to cleanup AND keeps a reference to the delegate
		private static IDictionary<IntPtr, InternalHookDelegate> hooks = new Dictionary<IntPtr, InternalHookDelegate>();

		/// <summary>
		/// Create a basic hook (not specific)
		/// </summary>
		/// <param name="hookType">Specify your HookType</param>
		/// <param name="callback">The callback that is called</param>
		/// <returns>IntPtr for the internalHook, so it can be unhooked again</returns>
		public static IntPtr BasicHook(HookType hookType, BasicHookDelegate callback) {
			InternalHookDelegate internalHook = delegate(int nCode, IntPtr wParam, IntPtr lParam) {
				if (nCode >= 0) {
					if (!callback(nCode, wParam, lParam)) {
						return IntPtr.Zero;
					}
				}
				return CallNextHookEx(IntPtr.Zero, nCode, wParam, lParam);
			};

			// Trap the reference, otherwise it will be removed by the GC
			IntPtr hookId = SetHook(hookType, internalHook);
			hooks.Add(hookId, internalHook);
			return hookId;
		}

		/// <summary>
		/// Create hook for the keyboard
		/// </summary>
		/// <param name="callback">KeyboardHookDelegate</param>
		/// <returns>IntPtr for the internalHook, so it can be unhooked again</returns>
		public static IntPtr KeyboardHook(KeyboardHookDelegate callback) {
			InternalHookDelegate internalHook = delegate(int nCode, IntPtr wParam, IntPtr lParam) {
				if (nCode >= 0) {
					Key key = System.Windows.Input.KeyInterop.KeyFromVirtualKey(Marshal.ReadInt32(lParam));
					WindowsMessages windowsMessage = (WindowsMessages)wParam.ToInt32();
					if (!callback(windowsMessage, key)) {
						return IntPtr.Zero;
					}
				}
				return CallNextHookEx(IntPtr.Zero, nCode, wParam, lParam);
			};

			// Trap the reference, otherwise it will be removed by the GC
			IntPtr hookId = SetHook(HookType.WH_KEYBOARD_LL, internalHook);
			hooks.Add(hookId, internalHook);
			return hookId;
		}

		/// <summary>
		/// Create hook for the mouse
		/// </summary>
		/// <param name="callback">MouseHookDelegate</param>
		/// <returns>IntPtr for the internalHook, so it can be unhooked again</returns>
		public static IntPtr MouseHook(MouseHookDelegate callback) {
			InternalHookDelegate internalHook = delegate(int nCode, IntPtr wParam, IntPtr lParam) {
				if (nCode == 0) {
					WindowsMessages windowsMessage = (WindowsMessages)wParam.ToInt32();
					MSLLHOOK mouseHookStruct = (MSLLHOOK)Marshal.PtrToStructure(lParam, typeof(MSLLHOOK));
					if (!callback(windowsMessage, mouseHookStruct)) {
						return IntPtr.Zero;
					}
				}
				return CallNextHookEx(IntPtr.Zero, nCode, wParam, lParam);
			};

			// Trap the reference, otherwise it will be removed by the GC
			IntPtr hookId = SetHook(HookType.WH_MOUSE_LL, internalHook);
			hooks.Add(hookId, internalHook);
			return hookId;
		}

		/// <summary>
		/// Remove a specify WindowsHook, the parameter is the return value from the BasicHook / KeyboardHook call
		/// </summary>
		/// <param name="hookId">IntPtr</param>
		public static void Unhook(IntPtr hookId) {
			if (hooks.ContainsKey(hookId)) {
				UnhookWindowsHookEx(hookId);
				hooks.Remove(hookId);
			} else {
				throw new InvalidOperationException("No hook for " + hookId);
			}
		}

		/// <summary>
		/// Remove ALL WindowsHooks
		/// </summary>
		/// <param name="hookType"></param>
		public static void UnhookAll() {
			IList<IntPtr> hookIds = new List<IntPtr>(hooks.Keys);
			foreach (IntPtr hookId in hookIds) {
				Unhook(hookId);
			}
		}

		/// <summary>
		/// Create a internalHook
		/// </summary>
		/// <param name="hookType">HookType</param>
		/// <param name="callback">BasicHookDelegate</param>
		/// <returns>IntPtr for the internalHook, so it can be unhooked again</returns>
		private static IntPtr SetHook(HookType hookType, InternalHookDelegate callback) {
			using (Process curProcess = Process.GetCurrentProcess())
			using (ProcessModule curModule = curProcess.MainModule) {
				return SetWindowsHookEx(hookType, callback, GetModuleHandle(curModule.ModuleName), 0);
			}
		}

		/// <summary>
		/// This is an example Keyboard hook for debugging
		/// </summary>
		/// <returns>true if other hooks need to be called</returns>
		public static bool KeyboardDebugCallback(WindowsMessages windowsMessage, Key key) {
			switch (windowsMessage) {
				case WindowsMessages.WM_KEYDOWN:
					Debug.WriteLine("Down: " + key);
					break;
				case WindowsMessages.WM_KEYUP:
					Debug.WriteLine("Up: " + key);
					break;
			}
			return true;
		}

		/// <summary>
		/// This is an example mouse hook for debugging
		/// </summary>
		/// <returns>true if other hooks need to be called</returns>
		public static bool MouseDebugCallback(WindowsMessages windowsMessage, MSLLHOOK mouseHookStruct) {
			switch (windowsMessage) {
				case WindowsMessages.WM_MOUSEMOVE:
					Debug.WriteLine("Move: " + mouseHookStruct.pt);
					break;
				case WindowsMessages.WM_LBUTTONDOWN:
					Debug.WriteLine("Click: " + mouseHookStruct.pt);
					break;
			}
			return true;
		}
	}
}
