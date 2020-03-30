// Copyright (c) Dapplo and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Interop;
using Windows.Storage;
using Windows.Storage.Streams;
using Dapplo.Log;
using Dapplo.Windows.Desktop;
using Dapplo.Windows.Enums;
using Dapplo.Windows.Ten.Internal;
using Dapplo.Windows.Ten.Notifications.Native;
using Color = Windows.UI.Color;
using Window = System.Windows.Window;

namespace Dapplo.Windows.Ten.Sharing
{
    /// <summary>
    /// This uses the Share from Windows 10 to make the capture available to apps.
    /// </summary>
    public class Win1Windows10Share0Share : IDisposable
    {
        private static readonly LogSource Log = new LogSource();

        private MemoryRandomAccessStream _logoStream;

        /// <summary>
        /// This creates a hidden background window, which is used by the sharing UI
        /// The sharing UI will try to fit accordingly
        /// </summary>
        /// <param name="width">int with width of the Windows</param>
        /// <param name="height">int with height of the Windows</param>
        /// <returns>Window</returns>
        public Window CreateHiddenBackgroundWindow(int width, int height) => new Window
        {
            WindowState = WindowState.Normal,
            WindowStartupLocation = WindowStartupLocation.CenterScreen,
            WindowStyle = WindowStyle.None,
            Width = width,
            Height = height,
            AllowsTransparency = true,
            Background = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Colors.Transparent)
        };

        /// <summary>
        /// Set the logo for the sharing UI
        /// </summary>
        /// <param name="logo">Bitmap</param>
        public void SetLogo(Bitmap logo)
        {
            _logoStream = new MemoryRandomAccessStream();
            logo.Save(_logoStream, ImageFormat.Png);
        }

        /// <summary>
        /// Share something with a windows app
        /// </summary>
        /// <returns>string with the name of the application which was shared to</returns>
        public string Share(SharingArguments arguments)
        {
            var triggerWindow = CreateHiddenBackgroundWindow(400, 400);

            var shareInfo = new ShareInfo();

            triggerWindow.Show();
            var windowHandle = new WindowInteropHelper(triggerWindow).Handle;

            // This is a bad trick, but don't know how else to do it.
            // Wait for the focus to return, and depending on the state close the window!
            var windowMonitor = WinEventHook.WindowCreateDestroyObservable()
                .Subscribe(winEventInfo =>
                {
                    if (winEventInfo.WinEvent == WinEvents.EVENT_OBJECT_CREATE)
                    {
                        var windowsDetails = InteropWindowFactory.CreateFor(winEventInfo.Handle);
                        if ("Windows Shell Experience Host" == windowsDetails.GetText())
                        {
                            shareInfo.SharingHwnd = winEventInfo.Handle;
                        }

                        return;
                    }

                    if (winEventInfo.Handle != shareInfo.SharingHwnd) return;
                    if (shareInfo.ApplicationName != null)
                    {
                        return;
                    }

                    shareInfo.ShareTask.TrySetResult(false);
                });

            Share(shareInfo, windowHandle, arguments).GetAwaiter().GetResult();
            Log.Debug().WriteLine("Sharing finished, closing window.");
            triggerWindow.Close();
            windowMonitor.Dispose();

            return shareInfo.ApplicationName;

        }

        /// <summary>
        /// Share the surface by using the Share-UI of Windows 10
        /// </summary>
        /// <param name="shareInfo">ShareInfo</param>
        /// <param name="handle">IntPtr with the handle for the hosting window</param>
        /// <param name="arguments">SharingArguments</param>
        /// <returns>Task with string, which describes the application which was used to share with</returns>
        private async Task Share(ShareInfo shareInfo, IntPtr handle, SharingArguments arguments)
        {
            using var dataToShareStream = new MemoryRandomAccessStream();
            
            Log.Debug().WriteLine("Created RandomAccessStreamReference for the sharing");
            var dataToShareRandomAccessStreamReference = RandomAccessStreamReference.CreateFromStream(dataToShareStream);

            // Create thumbnail
            using var thumbnailStream = new MemoryRandomAccessStream();
            arguments.Thumbnail.Save(thumbnailStream, ImageFormat.Png);
            thumbnailStream.Position = 0;
            RandomAccessStreamReference thumbnailRandomAccessStreamReference = RandomAccessStreamReference.CreateFromStream(thumbnailStream);
    
            // Create logo
            _logoStream.Position = 0;
            RandomAccessStreamReference logoRandomAccessStreamReference = RandomAccessStreamReference.CreateFromStream(_logoStream);

            var dataTransferManagerHelper = new DataTransferManagerHelper(handle);
            dataTransferManagerHelper.DataTransferManager.ShareProvidersRequested += (sender, args) =>
            {
                shareInfo.AreShareProvidersRequested = true;
                Log.Debug().WriteLine("Share providers requested: {0}", string.Join(",", args.Providers.Select(p => p.Title)));
            };
            dataTransferManagerHelper.DataTransferManager.TargetApplicationChosen += (dtm, args) =>
            {
                shareInfo.ApplicationName = args.ApplicationName;
                Log.Debug().WriteLine("TargetApplicationChosen: {0}", args.ApplicationName);
            };
            var storageFile = await StorageFile.CreateStreamedFileAsync(arguments.Filename, async streamedFileDataRequest =>
            {
                shareInfo.IsDeferredFileCreated = true;
                // Information on the "how" was found here: https://socialeboladev.wordpress.com/2013/03/15/how-to-use-createstreamedfileasync/
                Log.Debug().WriteLine("Creating deferred file {0}", arguments.Filename);
                try
                {
                    using (var deferredStream = streamedFileDataRequest.AsStreamForWrite())
                    {
                        await arguments.StreamData.CopyToAsync(deferredStream).ConfigureAwait(false);
                        await arguments.StreamData.FlushAsync().ConfigureAwait(false);
                    }
                    // Signal that the stream is ready
                    streamedFileDataRequest.Dispose();
                    // Signal that the action is ready, bitmap was exported
                    shareInfo.ShareTask.TrySetResult(true);
                }
                catch (Exception)
                {
                    streamedFileDataRequest.FailAndClose(StreamedFileFailureMode.Incomplete);
                }
            }, dataToShareRandomAccessStreamReference).AsTask().ConfigureAwait(false);

            dataTransferManagerHelper.DataTransferManager.DataRequested += (dataTransferManager, dataRequestedEventArgs) =>
            {
                var deferral = dataRequestedEventArgs.Request.GetDeferral();
                try
                {
                    shareInfo.IsDataRequested = true;
                    Log.Debug().WriteLine("DataRequested with operation {0}", dataRequestedEventArgs.Request.Data.RequestedOperation);
                    var dataPackage = dataRequestedEventArgs.Request.Data;
                    dataPackage.OperationCompleted += (dp, eventArgs) =>
                    {
                        Log.Debug().WriteLine("OperationCompleted: {0}, shared with", eventArgs.Operation);
                        shareInfo.CompletedWithOperation = eventArgs.Operation;
                        shareInfo.AcceptedFormat = eventArgs.AcceptedFormatId;

                        shareInfo.ShareTask.TrySetResult(true);
                    };
                    dataPackage.Destroyed += (dp, o) =>
                    {
                        shareInfo.IsDestroyed = true;
                        Log.Debug().WriteLine("Destroyed");
                        shareInfo.ShareTask.TrySetResult(true);
                    };
                    dataPackage.ShareCompleted += (dp, shareCompletedEventArgs) =>
                    {
                        shareInfo.IsShareCompleted = true;
                        Log.Debug().WriteLine("ShareCompleted");
                        shareInfo.ShareTask.TrySetResult(true);
                    };
                    dataPackage.Properties.Title = arguments.Title;
                    dataPackage.Properties.ApplicationName = arguments.ApplicationName;
                    dataPackage.Properties.Description = arguments.Description;
                    dataPackage.Properties.Thumbnail = thumbnailRandomAccessStreamReference;
                    dataPackage.Properties.Square30x30Logo = logoRandomAccessStreamReference;
                    dataPackage.Properties.LogoBackgroundColor = Color.FromArgb(0xff, 0x3d, 0x3d, 0x3d);
                    dataPackage.SetStorageItems(new[] { storageFile });
                }
                finally
                {
                    deferral.Complete();
                    Log.Debug().WriteLine("Called deferral.Complete()");
                }
            };
            dataTransferManagerHelper.ShowShareUi();
            Log.Debug().WriteLine("ShowShareUi finished.");
            await shareInfo.ShareTask.Task.ConfigureAwait(false);
        }

        /// <inheritdoc />
        public void Dispose()
        {
            _logoStream.Dispose();
        }
    }
}
