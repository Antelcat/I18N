using Antelcat.I18N.WPF.Demo.ViewModels;

namespace Antelcat.I18N.WPF.Demo.Windows
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