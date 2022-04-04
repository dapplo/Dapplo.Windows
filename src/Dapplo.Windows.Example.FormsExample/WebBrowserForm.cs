// Copyright (c) Dapplo and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using Dapplo.Log;
using Dapplo.Windows.Dpi.Forms;
using Dapplo.Windows.EmbeddedBrowser;

namespace Dapplo.Windows.Example.FormsExample;

public partial class WebBrowserForm : DpiAwareForm
{
    private static readonly LogSource Log = new LogSource();
    public WebBrowserForm()
    {
        InternetExplorerVersion.ChangeEmbeddedVersion();

        InitializeComponent();
        extendedWebBrowser1.OnNavigating().Subscribe(args =>
        {
            Log.Info().WriteLine(args.Url.AbsoluteUri);
        });
    }
}