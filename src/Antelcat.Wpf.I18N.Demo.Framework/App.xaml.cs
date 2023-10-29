using System.Windows;
using Antelcat.Wpf.I18N.Demo.Models;
using Antelcat.Wpf.I18N.Demo.Windows;

namespace Antelcat.Wpf.I18N.Demo
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App
    {
        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);
            new MainWindow().Show();
        }
    }
}