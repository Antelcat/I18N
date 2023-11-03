namespace Antelcat.
#if WPF
    Wpf
#endif
    .I18N.SourceGenerators;

public static class Global
{
    public const string Namespace =
#if WPF
        $"{nameof(Antelcat)}.{nameof(Wpf)}.{nameof(I18N)}";
#endif
}