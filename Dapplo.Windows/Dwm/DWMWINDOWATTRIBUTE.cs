using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Dapplo.Windows.Dwm {
	/// <summary>
	/// Flags used by the DwmGetWindowAttribute and DwmSetWindowAttribute functions to specify window attributes for non-client rendering.
	/// See http://msdn.microsoft.com/en-us/library/aa969530.aspx
	/// </summary>
	public enum DWMWINDOWATTRIBUTE {
		DWMWA_NCRENDERING_ENABLED = 1,
		DWMWA_NCRENDERING_POLICY,
		DWMWA_TRANSITIONS_FORCEDISABLED,
		DWMWA_ALLOW_NCPAINT,
		DWMWA_CAPTION_BUTTON_BOUNDS,
		DWMWA_NONCLIENT_RTL_LAYOUT,
		DWMWA_FORCE_ICONIC_REPRESENTATION,
		DWMWA_FLIP3D_POLICY,
		DWMWA_EXTENDED_FRAME_BOUNDS, // This is the one we need for retrieving the Window size since Windows Vista
		DWMWA_HAS_ICONIC_BITMAP,        // Since Windows 7
		DWMWA_DISALLOW_PEEK,            // Since Windows 7
		DWMWA_EXCLUDED_FROM_PEEK,       // Since Windows 7
		DWMWA_CLOAK,                    // Since Windows 8
		DWMWA_CLOAKED,                  // Since Windows 8
		DWMWA_FREEZE_REPRESENTATION,    // Since Windows 8
		DWMWA_LAST
	}
}
