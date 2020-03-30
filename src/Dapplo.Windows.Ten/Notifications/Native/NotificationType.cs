// Copyright (c) Dapplo and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;

namespace Dapplo.Windows.Ten.Notifications.Native
{
    /// <summary>
    /// This specifies which notification types are allowed
    /// </summary>
    [Flags]
    public enum NotificationType
    {
        /// <summary>
        /// Ignore all notifications
        /// </summary>
        None = 0,
        /// <summary>
        /// Show error notifications
        /// </summary>
        Error = 1,
        /// <summary>
        /// Show warning notifications
        /// </summary>
        Warning = 2,
        /// <summary>
        /// Show info notifications
        /// </summary>
        Info = 4
    }
}
