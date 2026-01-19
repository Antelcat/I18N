using Antelcat.I18N.Avalonia.Demo;
using Antelcat.I18N.Avalonia.Demo.Models;
using Avalonia;
using Avalonia.ReactiveUI;


BuildAvaloniaApp()
    .StartWithClassicDesktopLifetime(args);

static AppBuilder BuildAvaloniaApp()
    => AppBuilder.Configure<App>()
        .UsePlatformDetect()
        .LogToTrace()
        .UseReactiveUI();
