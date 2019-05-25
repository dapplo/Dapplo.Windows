//  Dapplo - building blocks for desktop applications
//  Copyright (C) 2017-2019  Dapplo
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

namespace Dapplo.Windows.User32.Enums
{
    /// <summary>
    /// The display element whose color is to be retrieved.
    /// See <a href="https://msdn.microsoft.com/en-us/library/windows/desktop/ms724371.aspx">GetSysColor function</a>
    /// </summary>
    public enum SysColorIndexes
    {
        /// <summary>
        /// Scroll bar gray area.
        /// </summary>
        ColorScrollbar = 0,
        /// <summary>
        /// Desktop.
        /// </summary>
        ColorBackground = 1,
        /// <summary>
        /// Active window title bar.
        /// The associated foreground color is COLOR_CAPTIONTEXT.
        /// Specifies the left side color in the color gradient of an active window's title bar if the gradient effect is enabled.
        /// </summary>
        ColorActivecaption = 2,
        /// <summary>
        /// Inactive window caption.
        /// The associated foreground color is COLOR_INACTIVECAPTIONTEXT.
        /// Specifies the left side color in the color gradient of an inactive window's title bar if the gradient effect is enabled.
        /// </summary>
        ColorInactivecaption = 3,
        /// <summary>
        /// Menu background.
        /// The associated foreground color is COLOR_MENUTEXT.
        /// </summary>
        ColorMenu = 4,
        /// <summary>
        /// Window background.
        /// The associated foreground colors are COLOR_WINDOWTEXT and COLOR_HOTLITE.
        /// </summary>
        ColorWindow = 5,
        /// <summary>
        /// Window frame.
        /// </summary>
        ColorWindowframe = 6,
        /// <summary>
        /// Text in menus.
        /// The associated background color is COLOR_MENU.
        /// </summary>
        ColorMenutext = 7,
        /// <summary>
        /// Text in windows.
        /// The associated background color is COLOR_WINDOW.
        /// </summary>
        ColorWindowtext = 8,
        /// <summary>
        /// Text in caption, size box, and scroll bar arrow box.
        /// The associated background color is COLOR_ACTIVECAPTION
        /// </summary>
        ColorCaptiontext = 9,
        /// <summary>
        /// Active window border.
        /// </summary>
        ColorActiveborder = 10,
        /// <summary>
        /// Inactive window border.
        /// </summary>
        ColorInactiveborder = 11,
        /// <summary>
        /// Background color of multiple document interface (MDI) applications.
        /// </summary>
        ColorAppworkspace = 12,
        /// <summary>
        /// Item(s) selected in a control.
        /// The associated foreground color is COLOR_HIGHLIGHTTEXT.
        /// </summary>
        ColorHighlight = 13,
        /// <summary>
        /// Text of item(s) selected in a control.
        /// The associated background color is COLOR_HIGHLIGHT.
        /// </summary>
        ColorHighlighttext = 14,
        /// <summary>
        /// Face color for three-dimensional display elements and for dialog box backgrounds.
        /// </summary>
        ColorBtnface = 15,
        /// <summary>
        /// Shadow color for three-dimensional display elements (for edges facing away from the light source).
        /// </summary>
        ColorBtnshadow = 16,
        /// <summary>
        /// Grayed (disabled) text.
        /// This color is set to 0 if the current display driver does not support a solid gray color.
        /// </summary>
        ColorGraytext = 17,
        /// <summary>
        /// Text on push buttons.
        /// The associated background color is COLOR_BTNFACE.
        /// </summary>
        ColorBtntext = 18,
        /// <summary>
        /// Color of text in an inactive caption.
        /// The associated background color is COLOR_INACTIVECAPTION.
        /// </summary>
        ColorInactivecaptiontext = 19,
        /// <summary>
        /// Highlight color for three-dimensional display elements (for edges facing the light source.)
        /// </summary>
        ColorBtnhighlight = 20,
        /// <summary>
        /// Dark shadow for three-dimensional display elements.
        /// </summary>
        Color3Ddkshadow = 21,
        /// <summary>
        /// Light color for three-dimensional display elements (for edges facing the light source.)
        /// </summary>
        Color3Dlight = 22,
        /// <summary>
        /// Text color for tooltip controls.
        /// The associated background color is COLOR_INFOBK.
        /// </summary>
        ColorInfotext = 23,
        /// <summary>
        /// Background color for tooltip controls.
        /// The associated foreground color is COLOR_INFOTEXT.
        /// </summary>
        ColorInfobk = 24,

        /// <summary>
        /// Color for a hyperlink or hot-tracked item.
        /// The associated background color is COLOR_WINDOW.
        /// </summary>
        ColorHotlight = 26,
        /// <summary>
        /// Right side color in the color gradient of an active window's title bar.
        /// COLOR_ACTIVECAPTION specifies the left side color.
        /// Use SPI_GETGRADIENTCAPTIONS with the SystemParametersInfo function to determine whether the gradient effect is enabled.
        /// </summary>
        ColorGradientactivecaption = 27,
        /// <summary>
        /// Right side color in the color gradient of an inactive window's title bar.
        /// COLOR_INACTIVECAPTION specifies the left side color.
        /// </summary>
        ColorGradientinactivecaption = 28,
        /// <summary>
        /// The color used to highlight menu items when the menu appears as a flat menu (see SystemParametersInfo).
        /// The highlighted menu item is outlined with COLOR_HIGHLIGHT.
        ///Windows 2000:  This value is not supported.
        /// </summary>
        ColorMenuhighlight = 29,
        /// <summary>
        /// The background color for the menu bar when menus appear as flat menus (see SystemParametersInfo).
        /// However, COLOR_MENU continues to specify the background color of the menu popup.
        ///Windows 2000:  This value is not supported.
        /// </summary>
        ColorMenubar = 30,

        /// <summary>
        /// Desktop
        /// </summary>
        ColorDesktop = 1,
        /// <summary>
        /// Shadow color for three-dimensional display elements (for edges facing away from the light source).
        /// </summary>
        Color3Dface = 16,
        /// <summary>
        /// Shadow color for three-dimensional display elements (for edges facing away from the light source).
        /// </summary>
        Color3Dshadow = 16,
        /// <summary>
        /// Highlight color for three-dimensional display elements (for edges facing the light source.)
        /// </summary>
        Color3Dhighlight = 20,
        /// <summary>
        /// Highlight color for three-dimensional display elements (for edges facing the light source.)
        /// </summary>
        Color3Dhilight = 20,
        /// <summary>
        /// Highlight color for three-dimensional display elements (for edges facing the light source.)
        /// </summary>
        ColorBtnhilight = 20,
        /// <summary>
        /// Maximum value
        /// </summary>
        ColorMaxvalue = 30
    }
}
