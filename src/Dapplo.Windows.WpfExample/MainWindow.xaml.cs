using System.Windows;
using Dapplo.Windows.Dpi;

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
            this.AttachWindowDpiHandler();
        }
    }
}
