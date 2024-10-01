using Antelcat.I18N.Avalonia.Demo.ViewModels;
using Antelcat.I18N.Avalonia.Demo.Views;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Avalonia.Styling;

namespace Antelcat.I18N.Avalonia.Demo
{
    public partial class App : Application
    {
        public override void Initialize()
        {
            AvaloniaXamlLoader.Load(this);
            if (Design.IsDesignMode)
            {
                RequestedThemeVariant = ThemeVariant.Dark;
            }
        }

        public override void OnFrameworkInitializationCompleted()
        {
            if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
            {
                desktop.MainWindow = new MainWindow
                {
                    DataContext = new ViewModel(),
                };
            }

            base.OnFrameworkInitializationCompleted();
        }
    }
}
