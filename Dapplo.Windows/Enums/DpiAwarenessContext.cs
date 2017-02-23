using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Dapplo.Windows.Enums
{
	/// <summary>
	/// 
	/// </summary>
	public enum DpiAwarenessContext
	{
		/// <summary>
		/// DPI unaware.
		/// This window does not scale for DPI changes and is always assumed to have a scale factor of 100% (96 DPI).
		/// It will be automatically scaled by the system on any other DPI setting.
		/// </summary>
		ContextUnaware = -1,
		/// <summary>
		/// System DPI aware.
		/// This window does not scale for DPI changes.
		/// It will query for the DPI once and use that value for the lifetime of the process.
		/// If the DPI changes, the process will not adjust to the new DPI value.
		/// It will be automatically scaled up or down by the system when the DPI changes from the system value.
		/// </summary>
		ContextSystemAware = -2,
		/// <summary>
		/// Per monitor DPI aware.
		/// This window checks for the DPI when it is created and adjusts the scale factor whenever the DPI changes.
		/// These processes are not automatically scaled by the system.
		/// </summary>
		ContextPerMonitorAware = -3
	}
}
