using System.Reactive.Linq;
using System.Windows;
using Dapplo.Windows.Dpi.Wpf;
using Dapplo.Windows.Messages;
using System;

namespace Dapplo.Windows.WpfExample
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow
    {
        public MainWindow()
        {
            InitializeComponent();
            this.AttachDpiHandler();


            this.WindowMessages().Where(m => m.Message == WindowsMessages.WM_DESTROY)
                .Subscribe(m => { MessageBox.Show($"{m.Message}"); });


        }
    }
}
