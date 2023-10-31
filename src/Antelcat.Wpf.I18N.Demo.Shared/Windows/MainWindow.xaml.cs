using Antelcat.Wpf.I18N.Demo.ViewModels;

namespace Antelcat.Wpf.I18N.Demo.Windows
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : MicrosoftPleaseFixAutogenWindow
    {
        public MainWindow()
        {
            InitializeComponent();
            DataContext = new ViewModel();
        }
    }
}