using System.ComponentModel;
#if WPF
using System.Windows.Data;
#elif AVALONIA
using Avalonia.Data;
#endif

#if WPF
namespace System.Windows;
#elif AVALONIA
namespace Avalonia.Markup.Xaml.MarkupExtensions;
#endif

/// <summary>
/// It's not a binding, Just a key to get the value from the dictionary.
/// </summary>
public class LanguageBinding : 
#if WPF
    Binding
#elif AVALONIA
    Binding
#endif
{
    public LanguageBinding() { }

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
    void CreateExpressionObserver(AvaloniaObject avaloniaObject, 
        AvaloniaProperty? avaloniaProperty, object? a, bool b)
    {
        throw new NotImplementedException();
    }
#endif
}