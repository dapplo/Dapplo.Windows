// Copyright (c) Dapplo and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Threading.Tasks;
using Windows.ApplicationModel.DataTransfer;

namespace Dapplo.Windows.Ten.Sharing
{
    internal class ShareInfo
    {
        public string ApplicationName { get; set; }

        public bool AreShareProvidersRequested { get; set; }

        public bool IsDeferredFileCreated { get; set; }

        public DataPackageOperation CompletedWithOperation { get; set; }

        public string AcceptedFormat { get; set; }

        public bool IsDestroyed { get; set; }

        public bool IsShareCompleted { get; set; }

        public TaskCompletionSource<bool> ShareTask { get; } = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);

        public bool IsDataRequested { get; set; }

        public IntPtr SharingHwnd { get; set; }
    }
}
