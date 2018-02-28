using Dapplo.Windows.Dpi.Wpf;

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
        }
    }
}
