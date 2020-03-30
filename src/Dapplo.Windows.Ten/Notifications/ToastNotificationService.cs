// Copyright (c) Dapplo and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using Windows.UI.Notifications;
using Dapplo.Log;
using Dapplo.Windows.Ten.Notifications.Native;

namespace Dapplo.Windows.Ten.Notifications
{
    /// <summary>
    /// This service provides a way to inform (notify) the user.
    /// </summary>
    public class ToastNotificationService<TActivator> : INotificationService where TActivator : NotificationActivator
    {
        private static readonly LogSource Log = new LogSource();

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="applicationUserModelId">string</param>
        public ToastNotificationService(string applicationUserModelId)
        {
            // Register AUMID and COM server (for Desktop Bridge apps, this no-ops)
            DesktopNotificationManagerCompat.RegisterAumidAndComServer<TActivator>(applicationUserModelId);
            // Register COM server and activator type
            DesktopNotificationManagerCompat.RegisterActivator<TActivator>();
        }

        /// <summary>
        /// This is the path to the icon which is shown in the toast
        /// </summary>
        public string IconPath { get; set; }

        /// <summary>
        /// Create a file for the supplied bitmap so it can be shown on the toasts
        /// </summary>
        /// <param name="icon">Bitmap</param>
        /// <param name="applicationName">string</param>
        public void GenerateIcon(Bitmap icon, string applicationName)
        {
            var localAppData = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), applicationName);
            if (!Directory.Exists(localAppData))
            {
                Directory.CreateDirectory(localAppData);
            }
            IconPath = Path.Combine(localAppData, $"{applicationName}.png");

            if (File.Exists(IconPath))
            {
                return;
            }
            icon.Save(IconPath, ImageFormat.Png);
        }

        /// <summary>
        /// This creates the actual toast
        /// </summary>
        /// <param name="message">string</param>
        /// <param name="timeout">milliseconds until the toast timeouts</param>
        /// <param name="notificationType">NotificationType</param>
        /// <param name="onClickAction">Action called when clicked</param>
        /// <param name="onClosedAction">Action called when the toast is closed</param>
        private void ShowMessage(string message, int timeout, NotificationType notificationType, Action onClickAction, Action onClosedAction)
        {
            // Do not inform the user if this is disabled
            if ((AllowedNotificationTypes & notificationType) == 0)
            {
                // This message type is not allowed, ignore
                return;
            }

            // Prepare the toast notifier. Be sure to specify the AppUserModelId on your application's shortcut!
            var toastNotifier = DesktopNotificationManagerCompat.CreateToastNotifier();
            if (toastNotifier.Setting != NotificationSetting.Enabled)
            {
                Log.Debug().WriteLine("Ignored toast due to {0}", toastNotifier.Setting);
                return;
            }

            // Get a toast XML template
            var toastXml = ToastNotificationManager.GetTemplateContent(ToastTemplateType.ToastImageAndText01);

            // Fill in the text elements
            var stringElement = toastXml.GetElementsByTagName("text").First();
            stringElement.AppendChild(toastXml.CreateTextNode(message));

            if (IconPath != null && File.Exists(IconPath))
            {
                // Specify the absolute path to an image
                var imageElement = toastXml.GetElementsByTagName("image").First();
                var imageSrcNode = imageElement.Attributes.GetNamedItem("src");
                if (imageSrcNode != null)
                {
                    imageSrcNode.NodeValue = IconPath;
                }
            }

            // Create the toast and attach event listeners
            var toast = new ToastNotification(toastXml)
            {
                ExpiresOnReboot = true,
                ExpirationTime = timeout > 0 ? DateTimeOffset.Now.AddMilliseconds(timeout) : (DateTimeOffset?)null
            };

            void ToastActivatedHandler(ToastNotification toastNotification, object sender)
            {
                try
                {
                    onClickAction?.Invoke();
                }
                catch (Exception ex)
                {
                    Log.Warn().WriteLine("Exception while handling the onclick action: ", ex);
                }

                toast.Activated -= ToastActivatedHandler;
            }

            if (onClickAction != null)
            {
                toast.Activated += ToastActivatedHandler;
            }

            void ToastDismissedHandler(ToastNotification toastNotification, ToastDismissedEventArgs eventArgs)
            {
                Log.Debug().WriteLine("Toast closed");
                try
                {
                    onClosedAction?.Invoke();
                }
                catch (Exception ex)
                {
                    Log.Warn().WriteLine("Exception while handling the onClosed action: ", ex);
                }

                toast.Dismissed -= ToastDismissedHandler;
                // Remove the other handler too
                toast.Activated -= ToastActivatedHandler;
                toast.Failed -= ToastOnFailed;
            }
            toast.Dismissed += ToastDismissedHandler;
            toast.Failed += ToastOnFailed;
            toastNotifier.Show(toast);
        }

        private void ToastOnFailed(ToastNotification sender, ToastFailedEventArgs args)
        {
            Log.Warn().WriteLine("Failed to display a toast due to {0}", args.ErrorCode);
            Log.Debug().WriteLine(sender.Content.GetXml());
        }

        /// <inheritdoc cref="NotificationType"/>
        public NotificationType AllowedNotificationTypes { get; set; } =
            NotificationType.Error | NotificationType.Warning | NotificationType.Info;

        /// <inheritdoc />
        public void ShowWarningMessage(string message, int timeout, Action onClickAction = null, Action onClosedAction = null)
        {
            ShowMessage(message, timeout, NotificationType.Warning, onClickAction, onClosedAction);
        }

        /// <inheritdoc />
        public void ShowErrorMessage(string message, int timeout, Action onClickAction = null, Action onClosedAction = null)
        {
            ShowMessage(message, timeout, NotificationType.Error, onClickAction, onClosedAction);
        }

        /// <inheritdoc />
        public void ShowInfoMessage(string message, int timeout, Action onClickAction = null, Action onClosedAction = null)
        {
            ShowMessage(message, timeout, NotificationType.Info, onClickAction, onClosedAction);
        }
    }
}
