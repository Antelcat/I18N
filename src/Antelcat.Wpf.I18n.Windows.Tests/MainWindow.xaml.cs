using System;
using System.Globalization;
using System.Timers;
using System.Windows;

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
            
            var timer = new Timer
            {
                Interval = 2000,
                AutoReset = true
            };
            var culture = new CultureInfo("zh");
            timer.Elapsed += (_, _) =>
            {
                vm.Language = vm.Language == "Chinese" ? "English" : "Chinese";
                /*culture = culture.EnglishName == "Chinese" ? new CultureInfo("en") : new CultureInfo("zh");
                LangExtension.Culture = culture;*/
            };
            
            timer.Start();
        }

        protected override void OnInitialized(EventArgs e)
        {
            base.OnInitialized(e);
        }
    }
}