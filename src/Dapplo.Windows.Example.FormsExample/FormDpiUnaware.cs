// Copyright (c) Dapplo and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
using Dapplo.Windows.Dpi.Forms;

namespace Dapplo.Windows.Example.FormsExample;

/// <summary>
/// This extends the DpiUnawareForm to disable DPI awareness
/// </summary>
public partial class FormDpiUnaware : DpiUnawareForm
{
    public FormDpiUnaware()
    {
        InitializeComponent();
    }
}