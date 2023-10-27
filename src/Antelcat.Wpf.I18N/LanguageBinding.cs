using System.ComponentModel;
using System.Windows.Data;

namespace System.Windows;

/// <summary>
/// It's not a binding, Just a key to get the value from the dictionary.
/// </summary>
public class LanguageBinding : Binding
{
    public LanguageBinding()
    {
        Source = I18NExtension.Target;
    }
    
    [DefaultValue("")]
    public string? Key
    {
        get => key;
        set
        {
            if (string.IsNullOrEmpty(value)) return;
            key    = value;
            Source = I18NExtension.GetSource(value!);
        }
    }

    private string? key;
}