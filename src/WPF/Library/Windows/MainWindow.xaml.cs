using Antelcat.I18N.WPF.Library.ViewModels;

namespace Antelcat.I18N.WPF.Library.Windows
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