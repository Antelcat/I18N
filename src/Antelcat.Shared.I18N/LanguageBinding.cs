using System.ComponentModel;
#if WPF
using System.Windows.Data;
namespace System.Windows;
#elif AVALONIA
using Avalonia.Data;
namespace Avalonia.Markup.Xaml.MarkupExtensions;
#endif

/// <summary>
/// It's not a binding, Just a key to get the value from the dictionary.
/// </summary>
public sealed class LanguageBinding : 
#if WPF
    Binding
#elif AVALONIA
    MarkupExtension
#endif
{
    public LanguageBinding()
    {
    }
    
    public LanguageBinding(string key)
    {
        Key = key;
    }
    
    [DefaultValue("")]
    public string? Key
    {
        get => key;
        set
        {
            if (string.IsNullOrEmpty(value)) return;
            key = value;
        }
    }

    private string? key;
    
#if AVALONIA
    public override object ProvideValue(IServiceProvider serviceProvider)
    {
        throw new NotImplementedException();
    }
#endif
}