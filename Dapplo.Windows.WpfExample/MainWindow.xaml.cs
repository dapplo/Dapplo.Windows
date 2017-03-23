using System;
using System.Windows;
using System.Windows.Interop;
using Dapplo.Windows.Dpi;
using Dapplo.Windows.Enums;

namespace Dapplo.Windows.WpfExample
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
            this.HandleWpfDpiChanges();
        }
    }
}
