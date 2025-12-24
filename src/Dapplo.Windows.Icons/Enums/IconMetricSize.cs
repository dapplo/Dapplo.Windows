// Copyright (c) Dapplo and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Dapplo.Windows.Icons.Enums;

/// <summary>
///     Options to specify the size of icons for LoadIconMetric and LoadIconWithScaleDown.
///     See https://docs.microsoft.com/en-us/windows/win32/api/commctrl/nf-commctrl-loadiconmetric
/// </summary>
public enum IconMetricSize
{
    /// <summary>
    ///     Use the system small icon size (SM_CXSMICON, SM_CYSMICON).
    ///     These metrics are used for icons in window captions and small icon view.
    /// </summary>
    SmallIcon = 0,

    /// <summary>
    ///     Use the system standard icon size (SM_CXICON, SM_CYICON).
    ///     These metrics are the default large icon dimensions.
    /// </summary>
    StandardIcon = 1
}
