using System.ComponentModel;
using System.Windows.Data;

namespace System.Windows;

/// <summary>
/// It's not a binding, Just a key to get the value from the dictionary.
/// </summary>
public class LanguageBinding : Binding
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
}