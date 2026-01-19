using Antelcat.I18N.Avalonia.AotDemo.Models;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Avalonia.Styling;

namespace Antelcat.I18N.Avalonia.AotDemo
{
    public partial class App : Application
    {
        public override void Initialize()
        {
            LangKeys.ResourcesProvider.Initialize();
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
                desktop.MainWindow = new MainWindow();
            }

            base.OnFrameworkInitializationCompleted();
        }
    }
}
