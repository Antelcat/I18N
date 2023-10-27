using System;
using System.Globalization;
using System.Threading.Tasks;
using System.Timers;
using System.Windows;
using System.Windows.Data;

namespace Antelcat.Wpf.I18N.Windows.Tests
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private readonly ViewModel vm;
        public MainWindow()
        {
            vm = new ViewModel();
            InitializeComponent();
            DataContext = vm;
            
            var viewModelTimer = new Timer
            {
                Interval = 2000,
                AutoReset = true
            };
            var languageTimer = new Timer
            {
                Interval  = 2000,
                AutoReset = true
            };
            var culture = new CultureInfo("zh");
            viewModelTimer.Elapsed += (_, _) =>
            {
                vm.Language = vm.Language == "Chinese" ? "English" : "Chinese";
            };
            languageTimer.Elapsed += (_, _) =>
            {
                culture = culture.EnglishName == "Chinese" ? new CultureInfo("en") : new CultureInfo("zh");
                I18NExtension.Culture = culture;
            };
            
            viewModelTimer.Start();

            
            Task.Delay(1000).ContinueWith(_ => { languageTimer.Start(); });
        }

        protected override void OnInitialized(EventArgs e)
        {
            base.OnInitialized(e);
        }
    }
}