namespace Antelcat.
#if WPF
    Wpf
#elif AVALONIA
    Avalonia
#endif
    .I18N.SourceGenerators;

public static class Global
{
    public const string Namespace =
#if WPF
        $"{nameof(Antelcat)}.{nameof(Wpf)}.{nameof(I18N)}";
#elif AVALONIA
        $"{nameof(Antelcat)}.{nameof(Avalonia)}.{nameof(I18N)}";
#endif
}