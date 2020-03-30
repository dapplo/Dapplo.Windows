// Copyright (c) Dapplo and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using Dapplo.Windows.Ten.Notifications.Native;

namespace Dapplo.Windows.Ten.Notifications
{
    /// <summary>
    /// This is the interface for the notification service implementations
    /// </summary>
    public interface INotificationService
    {
        /// <summary>
        /// Specifies which notifications are allowed to be shown
        /// </summary>
        NotificationType AllowedNotificationTypes { get; set; }

        /// <summary>
        /// This will show a warning message to the user
        /// </summary>
        /// <param name="message">string</param>
        /// <param name="timeout"></param>
        /// <param name="onClickAction">Action called if the user clicks the notification</param>
        /// <param name="onClosedAction">Action</param>
        void ShowWarningMessage(string message, int timeout, Action onClickAction = null, Action onClosedAction = null);

        /// <summary>
        /// This will show an error message to the user
        /// </summary>
        /// <param name="message">string</param>
        /// <param name="timeout"></param>
        /// <param name="onClickAction">Action called if the user clicks the notification</param>
        /// <param name="onClosedAction">Action</param>
        void ShowErrorMessage(string message, int timeout, Action onClickAction = null, Action onClosedAction = null);

        /// <summary>
        /// This will show an info message to the user
        /// </summary>
        /// <param name="message">string</param>
        /// <param name="timeout">int</param>
        /// <param name="onClickAction">Action called if the user clicks the notification</param>
        /// <param name="onClosedAction">Action</param>
        void ShowInfoMessage(string message, int timeout, Action onClickAction = null, Action onClosedAction = null);
    }
}